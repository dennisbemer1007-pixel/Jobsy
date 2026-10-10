using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services.EmployerPhase2;

public sealed class EmployerPhase2Service : IEmployerPhase2Service
{
    private readonly JobsyDbContext _db;
    private readonly IFeatureFlags _flags;
    private readonly ITokenLedgerService _tokens;
    private readonly IFlexCommercialService _commercial;
    private readonly IApplicationStatusRecorder _statusRecorder;
    private readonly ILogger<EmployerPhase2Service> _logger;

    public EmployerPhase2Service(
        JobsyDbContext db,
        IFeatureFlags flags,
        ITokenLedgerService tokens,
        IFlexCommercialService commercial,
        IApplicationStatusRecorder statusRecorder,
        ILogger<EmployerPhase2Service> logger)
    {
        _db = db;
        _flags = flags;
        _tokens = tokens;
        _commercial = commercial;
        _statusRecorder = statusRecorder;
        _logger = logger;
    }

    public async Task<bool> IsEnabledAsync(CancellationToken cancellationToken = default)
    {
        var snap = await _flags.GetAsync(cancellationToken);
        return snap.EmployersEnabled && snap.EmployerPhase2Enabled;
    }

    public async Task<EmployerPhase2AcceptResult> AcceptApplicationAsync(
        Guid applicationId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (!await IsEnabledAsync(cancellationToken))
        {
            return new(false, "feature_disabled", "Deze functie staat nog niet aan.", null);
        }

        var application = await _db.Applications
            .Include(a => a.Vacancy).ThenInclude(v => v.Company)
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);
        if (application is null)
        {
            return new(false, "not_found", null, null);
        }

        if (!ApplicationRules.CanEmployerReact(application.Status))
        {
            return new(false, "react_not_allowed", "Op deze sollicitatie kun je niet meer reageren.", null);
        }

        if (await _db.ApplicationPlacements.AnyAsync(p => p.ApplicationId == applicationId, cancellationToken))
        {
            return new(false, "already_accepted", "Er is al betaald voor deze kandidaat.", null);
        }

        var commercial = await _commercial.GetAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var cost = AcceptCandidateVacancyRules.ResolveAcceptCostTokens(
            application.Vacancy.Kind,
            commercial,
            today);
        var billingCompanyId = application.Vacancy.IntermediaryCompanyId ?? application.Vacancy.CompanyId;

        var respondedAt = DateTime.UtcNow;
        if (cost <= 0m)
        {
            if (!_statusRecorder.SetStatus(
                    application,
                    ApplicationStatus.Accepted,
                    ApplicationStatusActorKind.Employer,
                    actorUserId,
                    respondedAt))
            {
                return new(false, "react_failed", "Op deze sollicitatie kun je niet meer reageren.", null);
            }

            _db.ApplicationPlacements.Add(new ApplicationPlacement
            {
                ApplicationId = applicationId,
                BillingCompanyId = billingCompanyId,
                AcceptCostTokens = 0m
            });
            await _db.SaveChangesAsync(cancellationToken);
            var balance = await _tokens.GetBalanceAsync(billingCompanyId, cancellationToken);
            return new(true, null, null, balance);
        }

        var spend = await _tokens.TrySpendAsync(
            billingCompanyId,
            TokenSpendReason.AcceptCandidate,
            application.VacancyId,
            actorUserId,
            application.Vacancy.CompanyId,
            note: $"AcceptCandidate:{applicationId}",
            onSuccessBeforeCommit: async ct =>
            {
                if (!_statusRecorder.SetStatus(
                        application,
                        ApplicationStatus.Accepted,
                        ApplicationStatusActorKind.Employer,
                        actorUserId,
                        respondedAt))
                {
                    throw new InvalidOperationException("react_failed");
                }

                _db.ApplicationPlacements.Add(new ApplicationPlacement
                {
                    ApplicationId = applicationId,
                    BillingCompanyId = billingCompanyId,
                    AcceptCostTokens = cost
                });
                await _db.SaveChangesAsync(ct);
            },
            costOverrides: new Dictionary<TokenSpendReason, decimal>
            {
                [TokenSpendReason.AcceptCandidate] = cost
            },
            cancellationToken: cancellationToken);

        if (!spend.Succeeded)
        {
            return new(false, "insufficient_tokens", spend.ErrorMessage ?? "Onvoldoende tokens.", spend.Balance);
        }

        if (spend.Transaction is not null)
        {
            var placement = await _db.ApplicationPlacements
                .FirstAsync(p => p.ApplicationId == applicationId, cancellationToken);
            if (placement.AcceptSpendTransactionId is null)
            {
                placement.AcceptSpendTransactionId = spend.Transaction.Id;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        return new(true, null, null, spend.Balance);
    }

    public async Task<EmployerPhase2PlacementResult> ChooseEmploymentModeAsync(
        Guid applicationId,
        PlacementEmploymentMode mode,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (!await IsEnabledAsync(cancellationToken))
        {
            return new(false, "feature_disabled", "Deze functie staat nog niet aan.");
        }

        var placement = await _db.ApplicationPlacements
            .Include(p => p.Application).ThenInclude(a => a.Vacancy)
            .FirstOrDefaultAsync(p => p.ApplicationId == applicationId, cancellationToken);
        if (placement is null)
        {
            return new(false, "not_accepted", "Accepteer eerst de kandidaat.");
        }

        if (placement.Application.Vacancy.IntermediaryCompanyId is not null)
        {
            return new(false, "not_applicable", "Uitzendbureaus kiezen geen Maqqie-route.");
        }

        if (!AcceptCandidateVacancyRules.SupportsEmploymentModeChoice(placement.Application.Vacancy.Kind))
        {
            return new(false, "not_applicable", "Voor stage en vrijwilligerswerk is geen Maqqie-route.");
        }

        if (placement.Application.Status != ApplicationStatus.Accepted)
        {
            return new(false, "invalid_status", "Keuze kan alleen na accepteren.");
        }

        if (placement.EmploymentMode is not null)
        {
            return new(true, null, null);
        }

        placement.EmploymentMode = mode;
        placement.EmploymentModeChosenAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return new(true, null, null);
    }

    public async Task<EmployerPhase2WalletDto> GetWalletAsync(
        Guid billingCompanyId,
        CancellationToken cancellationToken = default)
    {
        var commercial = await _commercial.GetAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var balance = await _tokens.GetBalanceAsync(billingCompanyId, cancellationToken);
        var defaultCost = AcceptCandidatePricingRules.ResolveCostTokens(commercial, today);

        var placements = await _db.ApplicationPlacements
            .AsNoTracking()
            .Where(p => p.BillingCompanyId == billingCompanyId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(30)
            .Join(
                _db.Applications.AsNoTracking(),
                p => p.ApplicationId,
                a => a.Id,
                (p, a) => new { p, a })
            .Join(
                _db.Vacancies.AsNoTracking(),
                x => x.a.VacancyId,
                v => v.Id,
                (x, v) => new { x.p, x.a, v })
            .ToListAsync(cancellationToken);

        var lines = new List<EmployerPhase2WalletLineDto>();
        foreach (var row in placements)
        {
            lines.Add(new EmployerPhase2WalletLineDto(
                "accept",
                -row.p.AcceptCostTokens,
                "Kandidaat geaccepteerd",
                $"{MaskName(row.a.CandidateName)} · {row.v.Title}",
                row.p.CreatedAtUtc));

            if (row.p.MaqqieCreditGrantedAtUtc is DateTime credited)
            {
                lines.Add(new EmployerPhase2WalletLineDto(
                    "maqqie_credit",
                    row.p.AcceptCostTokens,
                    "½ token teruggezet",
                    $"Maqqie, week 1 gewerkt · {MaskName(row.a.CandidateName)}",
                    credited));
            }
        }

        var pending = placements
            .Where(x => AcceptCandidateVacancyRules.SupportsEmploymentModeChoice(x.v.Kind)
                        && x.p.EmploymentMode == PlacementEmploymentMode.Maqqie
                        && x.p.MaqqieCreditGrantedAtUtc is null
                        && x.p.AcceptCostTokens > 0)
            .Select(x => new EmployerPhase2PendingCreditDto(
                x.a.Id,
                MaskName(x.a.CandidateName),
                "Eerste werkdag · Maqqie meldt na week 1",
                null))
            .ToList();

        return new EmployerPhase2WalletDto(
            balance,
            AcceptCandidatePricingRules.IsPilotActive(commercial, today),
            defaultCost,
            AcceptCandidatePricingRules.EuroDisplay(defaultCost),
            lines.OrderByDescending(l => l.OccurredAtUtc).Take(20).ToList(),
            pending);
    }

    public async Task TryCreditMaqqieWeekOneAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        if (!await IsEnabledAsync(cancellationToken))
        {
            return;
        }

        var placement = await _db.ApplicationPlacements
            .Include(p => p.Application).ThenInclude(a => a.Vacancy)
            .FirstOrDefaultAsync(p => p.ApplicationId == applicationId, cancellationToken);
        if (placement is null
            || !AcceptCandidateVacancyRules.SupportsEmploymentModeChoice(placement.Application.Vacancy.Kind)
            || placement.EmploymentMode != PlacementEmploymentMode.Maqqie
            || placement.MaqqieCreditGrantedAtUtc is not null
            || placement.AcceptCostTokens <= 0)
        {
            return;
        }

        var note = $"MaqqieWeek1Credit:{applicationId}";
        var existing = await _db.TokenTransactions
            .AnyAsync(t => t.CompanyId == placement.BillingCompanyId && t.Note == note, cancellationToken);
        if (existing)
        {
            return;
        }

        var grant = await _tokens.GrantAsync(
            placement.BillingCompanyId,
            placement.AcceptCostTokens,
            note: note,
            cancellationToken: cancellationToken);

        placement.MaqqieCreditGrantedAtUtc = DateTime.UtcNow;
        placement.MaqqieCreditGrantTransactionId = grant.Id;
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Maqqie week-1 credit {Tokens} tokens for application {ApplicationId}",
            placement.AcceptCostTokens,
            applicationId);
    }

    private static string MaskName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Kandidaat";
        }

        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? "Kandidaat" : parts[0];
    }
}
