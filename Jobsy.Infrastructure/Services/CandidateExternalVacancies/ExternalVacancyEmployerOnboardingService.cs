using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services.CandidateExternalVacancies;

public sealed class ExternalVacancyEmployerOnboardingService : IExternalVacancyEmployerOnboardingService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly JobsyDbContext _db;
    private readonly IOneTimeLinkService _links;
    private readonly IVacancyDraftCreationService _drafts;
    private readonly IVacancyProductService _products;
    private readonly IApplicationStatusRecorder _statusRecorder;
    private readonly ILogger<ExternalVacancyEmployerOnboardingService> _logger;

    public ExternalVacancyEmployerOnboardingService(
        JobsyDbContext db,
        IOneTimeLinkService links,
        IVacancyDraftCreationService drafts,
        IVacancyProductService products,
        IApplicationStatusRecorder statusRecorder,
        ILogger<ExternalVacancyEmployerOnboardingService> logger)
    {
        _db = db;
        _links = links;
        _drafts = drafts;
        _products = products;
        _statusRecorder = statusRecorder;
        _logger = logger;
    }

    public async Task<Guid?> TryResolveOutboundIdAsync(
        string inviteToken,
        string normalizedContactEmail,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(inviteToken)
            || !CandidateExternalVacancyRules.IsValidEmployerEmail(normalizedContactEmail))
        {
            return null;
        }

        var email = CandidateExternalVacancyRules.NormalizeEmployerEmail(normalizedContactEmail);
        var peek = await _links.PeekAsync(
            OneTimeLinkPurpose.ExternalVacancyEmployerInvite,
            inviteToken.Trim(),
            cancellationToken);
        if (!peek.Valid || peek.LinkId is null)
        {
            return null;
        }

        var outbound = await _db.CandidateExternalVacancyOutbounds.AsNoTracking()
            .FirstOrDefaultAsync(o => o.OneTimeLinkId == peek.LinkId, cancellationToken);
        if (outbound is null)
        {
            return null;
        }

        return string.Equals(outbound.EmployerEmailNormalized, email, StringComparison.Ordinal)
            ? outbound.Id
            : null;
    }

    public async Task<ExternalVacancyOnboardingResult> CompleteRegistrationAsync(
        Guid outboundId,
        Guid branchCompanyId,
        Guid employerUserId,
        CancellationToken cancellationToken = default)
    {
        var outbound = await _db.CandidateExternalVacancyOutbounds
            .Include(o => o.ExternalVacancy).ThenInclude(v => v.CandidateUser)
            .FirstOrDefaultAsync(o => o.Id == outboundId, cancellationToken);
        if (outbound is null)
        {
            return new(false, null, "invite_not_found");
        }

        var external = outbound.ExternalVacancy;
        var candidate = external.CandidateUser;
        if (external.LinkedVacancyId is Guid existingVacancyId && external.ApplicationId is Guid existingAppId)
        {
            return new(
                true,
                BuildApplicantsPath(existingVacancyId, existingAppId),
                null);
        }

        var salaryTableId = await ResolveSalaryTableIdAsync(branchCompanyId, cancellationToken);
        if (salaryTableId is null)
        {
            return new(false, null, "salary_table_missing");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var description = BuildDescription(external);
        var draftInput = new VacancyDraftInput(
            branchCompanyId,
            string.IsNullOrWhiteSpace(external.Title) ? "Vacature via Lobsy" : external.Title.Trim(),
            description,
            HourlyWage: 0m,
            StartDate: today,
            EndDate: today.AddMonths(3),
            RequiredTransport: TransportMode.Bike | TransportMode.PublicTransport,
            WorkTypes: [WorkTypeLabels.Horeca],
            SalaryTableId: salaryTableId.Value,
            Kind: VacancyKind.Regular);

        var draft = await _drafts.CreateDraftAsync(draftInput, VacancySource.Manual, cancellationToken);
        if (!draft.Succeeded || draft.Vacancy is null)
        {
            _logger.LogWarning(
                "External vacancy draft failed for outbound {OutboundId}: {Error}",
                outboundId,
                draft.ErrorMessage);
            return new(false, null, "vacancy_draft_failed");
        }

        // Draft creation clears the change tracker; reload outbound + external before linking.
        outbound = await _db.CandidateExternalVacancyOutbounds
            .Include(o => o.ExternalVacancy).ThenInclude(v => v.CandidateUser)
            .FirstAsync(o => o.Id == outboundId, cancellationToken);
        external = outbound.ExternalVacancy;
        candidate = external.CandidateUser;

        var vacancy = await _db.Vacancies
            .Include(v => v.Company)
            .FirstAsync(v => v.Id == draft.Vacancy.Id, cancellationToken);

        _ = await _products.PublishAsync(
            vacancy,
            new VacancyPublishOptions(Highlight: false, PushBom: false, Extend: false),
            employerUserId,
            allowPendingApproval: true,
            cancellationToken);

        var application = await CreateApplicationAsync(
            vacancy,
            candidate,
            outbound.Motivation,
            external.TravelMinutesEstimate,
            cancellationToken);

        external.LinkedVacancyId = vacancy.Id;
        external.ApplicationId = application.Id;
        outbound.EmployerAccountCreatedAtUtc ??= DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return new(
            true,
            BuildApplicantsPath(vacancy.Id, application.Id),
            null);
    }

    private async Task<Application> CreateApplicationAsync(
        Vacancy vacancy,
        User candidate,
        string motivation,
        int? travelMinutes,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var preferences = ParsePreferences(candidate.PreferencesJson);
        var transport = string.IsNullOrWhiteSpace(preferences.PreferredTransport)
            ? "Bike"
            : preferences.PreferredTransport;

        var application = new Application
        {
            Id = Guid.NewGuid(),
            VacancyId = vacancy.Id,
            CandidateUserId = candidate.Id,
            CandidateName = CandidateNameRules.ComposeFullName(
                candidate.FirstName, candidate.LastName, candidate.FullName),
            CandidateEmail = candidate.Email,
            CandidateCity = preferences.City,
            CandidateAddress = preferences.HomeAddress,
            PreferredTransport = transport,
            EstimatedTravelMinutes = travelMinutes ?? 0,
            Motivation = Truncate(motivation.Trim(), 500),
            ConsentAcceptedAt = now,
            ConsentVersion = PrivacyConstants.CurrentConsentVersion,
            EmailVerifiedAt = now,
            SnapshotPhoneNumber = Truncate(candidate.PhoneNumber, 32),
            SnapshotHomeLatitude = candidate.HomeLocation?.Latitude,
            SnapshotHomeLongitude = candidate.HomeLocation?.Longitude,
            SnapshotAboutMe = Truncate(preferences.AboutMe, 1024),
            CreatedAt = now
        };

        _statusRecorder.RecordCreated(
            application,
            ApplicationStatusActorKind.System,
            null,
            now);
        _db.Applications.Add(application);
        await _db.SaveChangesAsync(cancellationToken);
        return application;
    }

    private async Task<Guid?> ResolveSalaryTableIdAsync(Guid branchCompanyId, CancellationToken cancellationToken)
    {
        await WmlSalaryTableService.EnsureForCompanyAsync(_db, branchCompanyId, cancellationToken);
        var orgId = await WmlSalaryTableService.ResolveOrganizationIdAsync(_db, branchCompanyId, cancellationToken);
        if (orgId is null)
        {
            return null;
        }

        var tableId = await _db.CompanySalaryTables.AsNoTracking()
            .Where(t => t.CompanyId == orgId.Value && t.IsSystemWml && t.IsActive && t.Rates.Any())
            .OrderBy(t => t.Id)
            .Select(t => t.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return tableId == Guid.Empty ? null : tableId;
    }

    private static string BuildDescription(CandidateExternalVacancy external)
    {
        var bullets = JsonSerializer.Deserialize<List<string>>(external.RequirementsBulletsJson, JsonOptions) ?? [];
        var lines = new List<string>
        {
            $"Externe vacature geïmporteerd via Lobsy.",
            $"Bron: {external.SourceUrl}",
            ""
        };
        if (!string.IsNullOrWhiteSpace(external.CompanyName))
        {
            lines.Add($"Bedrijf (bron): {external.CompanyName}");
        }

        if (!string.IsNullOrWhiteSpace(external.Place))
        {
            lines.Add($"Plaats (bron): {external.Place}");
        }

        if (bullets.Count > 0)
        {
            lines.Add("");
            lines.Add("Eisen:");
            lines.AddRange(bullets.Select(b => $"• {b}"));
        }

        var text = string.Join("\n", lines).Trim();
        return text.Length > 20_000 ? text[..20_000] : text;
    }

    private static string BuildApplicantsPath(Guid vacancyId, Guid applicationId)
        => $"/werkgever/sollicitaties?vacancy={vacancyId:D}&application={applicationId:D}";

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }

    private static (string? HomeAddress, string? City, string? AboutMe, string? PreferredTransport) ParsePreferences(
        string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return (null, null, null, null);
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            string? Read(string name) =>
                root.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String
                    ? p.GetString()
                    : null;
            return (Read("homeAddress"), Read("city"), Read("aboutMe"), Read("preferredTransport"));
        }
        catch
        {
            return (null, null, null, null);
        }
    }
}
