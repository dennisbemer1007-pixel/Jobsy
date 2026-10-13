using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class TokenRequestService : ITokenRequestService
{
    private readonly JobsyDbContext _db;
    private readonly ITokenLedgerService _ledger;
    private readonly IUserNotificationService _notifications;

    public TokenRequestService(
        JobsyDbContext db,
        ITokenLedgerService ledger,
        IUserNotificationService notifications)
    {
        _db = db;
        _ledger = ledger;
        _notifications = notifications;
    }

    private static string AllocationNote(Guid requestId) => $"Tokenaanvraag {requestId:N}";

    public Task<TokenRequestDto?> GetAsync(Guid requestId, CancellationToken cancellationToken = default)
        => ToDtoAsync(requestId, cancellationToken);

    public async Task<TokenRequestDto> CreateAsync(
        Guid branchCompanyId,
        Guid requestedByUserId,
        int amount,
        TokenRequestReason reason,
        string? note,
        CancellationToken cancellationToken = default)
    {
        if (amount < TokenRequest.MinAmount || amount > TokenRequest.MaxAmount)
        {
            throw new TokenRequestException(
                "invalid_amount",
                $"Aantal moet tussen {TokenRequest.MinAmount} en {TokenRequest.MaxAmount} liggen.");
        }

        var trimmedNote = note?.Trim();
        if (trimmedNote is { Length: > TokenRequest.MaxNoteLength })
        {
            throw new TokenRequestException(
                "note_too_long",
                $"Toelichting mag maximaal {TokenRequest.MaxNoteLength} tekens zijn.");
        }

        var branch = await _db.Companies
            .FirstOrDefaultAsync(c => c.Id == branchCompanyId, cancellationToken)
            ?? throw new TokenRequestException("branch_not_found", "Vestiging niet gevonden.", 404);

        var walletId = CandidateInsightsAccess.ResolveWalletCompanyId(branch);
        if (walletId == branch.Id && branch.ParentCompanyId is null)
        {
            // Standalone vestiging: no organisatiepot to ask from.
            throw new TokenRequestException(
                "no_organisation_wallet",
                "Voor deze vestiging is geen organisatiopot om tokens bij aan te vragen.");
        }

        var openCount = await _db.TokenRequests
            .CountAsync(
                r => r.BranchCompanyId == branchCompanyId && r.Status == TokenRequestStatus.Open,
                cancellationToken);
        if (openCount >= TokenRequest.MaxOpenPerBranch)
        {
            throw new TokenRequestException(
                "too_many_open_requests",
                $"Je hebt al {TokenRequest.MaxOpenPerBranch} openstaande aanvragen voor deze vestiging.",
                409);
        }

        var row = new TokenRequest
        {
            Id = Guid.NewGuid(),
            OrganisationCompanyId = walletId,
            BranchCompanyId = branch.Id,
            RequestedByUserId = requestedByUserId,
            Amount = amount,
            Reason = reason,
            Note = string.IsNullOrWhiteSpace(trimmedNote) ? null : trimmedNote,
            Status = TokenRequestStatus.Open,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.TokenRequests.Add(row);
        await _db.SaveChangesAsync(cancellationToken);

        await NotifyBedrijfsmanagersAsync(row, branch.Name, cancellationToken);

        return await ToDtoAsync(row.Id, cancellationToken)
            ?? throw new InvalidOperationException("Token request missing after create.");
    }

    public async Task<IReadOnlyList<TokenRequestDto>> ListAsync(
        IReadOnlyList<Guid> accessibleCompanyIds,
        Guid? forUserId,
        bool organisationWide,
        TokenRequestStatus? status,
        CancellationToken cancellationToken = default)
    {
        var ids = accessibleCompanyIds.ToHashSet();
        if (ids.Count == 0)
        {
            return [];
        }

        var query = _db.TokenRequests.AsNoTracking()
            .Where(r => ids.Contains(r.BranchCompanyId) || ids.Contains(r.OrganisationCompanyId));

        if (!organisationWide && forUserId is Guid uid)
        {
            query = query.Where(r => r.RequestedByUserId == uid);
        }

        if (status is TokenRequestStatus s)
        {
            query = query.Where(r => r.Status == s);
        }

        var rows = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(200)
            .Select(r => new
            {
                r.Id,
                r.OrganisationCompanyId,
                r.BranchCompanyId,
                BranchName = r.BranchCompany.Name,
                r.RequestedByUserId,
                RequestedByName = r.RequestedByUser.FullName,
                r.Amount,
                r.Reason,
                r.Note,
                r.Status,
                r.HandledByUserId,
                r.HandledAtUtc,
                r.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new TokenRequestDto(
            r.Id,
            r.OrganisationCompanyId,
            r.BranchCompanyId,
            r.BranchName,
            r.RequestedByUserId,
            r.RequestedByName,
            r.Amount,
            r.Reason.ToString(),
            r.Note,
            r.Status.ToString(),
            r.HandledByUserId,
            r.HandledAtUtc,
            r.CreatedAtUtc)).ToList();
    }

    public async Task<TokenRequestDto> ApproveAsync(
        Guid requestId,
        Guid handledByUserId,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.TokenRequests
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new TokenRequestException("not_found", "Aanvraag niet gevonden.", 404);

        if (row.Status == TokenRequestStatus.Toegewezen)
        {
            return await ToDtoAsync(row.Id, cancellationToken)
                ?? throw new InvalidOperationException("Token request missing after approve.");
        }

        if (row.Status != TokenRequestStatus.Open)
        {
            throw new TokenRequestException(
                "not_open",
                "Alleen openstaande aanvragen kunnen worden toegewezen.",
                409);
        }

        var note = AllocationNote(row.Id);
        var alreadyAllocated = await _db.TokenTransactions.AsNoTracking()
            .AnyAsync(
                t => t.Kind == TokenTransactionKind.Allocation
                     && t.Note == note
                     && t.CompanyId == row.BranchCompanyId
                     && t.Amount > 0,
                cancellationToken);
        if (!alreadyAllocated)
        {
            var central = await _ledger.GetBalanceAsync(row.OrganisationCompanyId, cancellationToken);
            if (central < row.Amount)
            {
                throw new TokenRequestException(
                    "insufficient_balance",
                    $"Onvoldoende centraal saldo ({central:0.##}) om {row.Amount} tokens toe te wijzen.",
                    409);
            }

            try
            {
                await _ledger.AllocateAsync(
                    row.OrganisationCompanyId,
                    row.BranchCompanyId,
                    row.Amount,
                    handledByUserId,
                    note,
                    cancellationToken);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("saldo", StringComparison.OrdinalIgnoreCase)
                                                       || ex.Message.Contains("balance", StringComparison.OrdinalIgnoreCase)
                                                       || ex.Message.Contains("Insufficient", StringComparison.OrdinalIgnoreCase))
            {
                throw new TokenRequestException("insufficient_balance", ex.Message, 409);
            }
        }

        // Re-load in case concurrent approve won the race.
        row = await _db.TokenRequests.FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new TokenRequestException("not_found", "Aanvraag niet gevonden.", 404);
        if (row.Status != TokenRequestStatus.Toegewezen)
        {
            row.Status = TokenRequestStatus.Toegewezen;
            row.HandledByUserId = handledByUserId;
            row.HandledAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            await NotifyRequesterHandledAsync(row, approved: true, cancellationToken);
        }

        return await ToDtoAsync(row.Id, cancellationToken)
            ?? throw new InvalidOperationException("Token request missing after approve.");
    }

    public async Task<TokenRequestDto> RejectAsync(
        Guid requestId,
        Guid handledByUserId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.TokenRequests
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new TokenRequestException("not_found", "Aanvraag niet gevonden.", 404);

        if (row.Status != TokenRequestStatus.Open)
        {
            throw new TokenRequestException(
                "not_open",
                "Alleen openstaande aanvragen kunnen worden afgewezen.",
                409);
        }

        row.Status = TokenRequestStatus.Afgewezen;
        row.HandledByUserId = handledByUserId;
        row.HandledAtUtc = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            var note = reason.Trim();
            if (note.Length > TokenRequest.MaxNoteLength)
            {
                note = note[..TokenRequest.MaxNoteLength];
            }

            row.Note = string.IsNullOrWhiteSpace(row.Note) ? note : $"{row.Note} · Afgewezen: {note}";
        }

        await _db.SaveChangesAsync(cancellationToken);
        await NotifyRequesterHandledAsync(row, approved: false, cancellationToken);

        return await ToDtoAsync(row.Id, cancellationToken)
            ?? throw new InvalidOperationException("Token request missing after reject.");
    }

    public async Task<TokenRequestDto> WithdrawAsync(
        Guid requestId,
        Guid requestedByUserId,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.TokenRequests
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new TokenRequestException("not_found", "Aanvraag niet gevonden.", 404);

        if (row.RequestedByUserId != requestedByUserId)
        {
            throw new TokenRequestException("forbidden", "Je kunt alleen eigen aanvragen intrekken.", 403);
        }

        if (row.Status != TokenRequestStatus.Open)
        {
            throw new TokenRequestException(
                "not_open",
                "Alleen openstaande aanvragen kunnen worden ingetrokken.",
                409);
        }

        row.Status = TokenRequestStatus.Ingetrokken;
        row.HandledAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(row.Id, cancellationToken)
            ?? throw new InvalidOperationException("Token request missing after withdraw.");
    }

    private async Task NotifyBedrijfsmanagersAsync(
        TokenRequest row,
        string branchName,
        CancellationToken cancellationToken)
    {
        var bmIds = await _db.Users.AsNoTracking()
            .Where(u => u.IsActive
                        && u.Role == UserRole.EnterpriseManager
                        && (u.CompanyId == row.OrganisationCompanyId
                            || _db.UserCompanies.Any(uc =>
                                uc.UserId == u.Id && uc.CompanyId == row.OrganisationCompanyId)))
            .Select(u => u.Id)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var bmId in bmIds)
        {
            await _notifications.CreateAsync(
                new NotificationCreateRequest(
                    bmId,
                    "Tokenaanvraag",
                    $"{branchName} vraagt {row.Amount} tokens aan.",
                    "token_request",
                    DeepLink: "/werkgever/te-doen",
                    ActionLabel: "Toewijzen",
                    ActionUrl: $"/werkgever/tokens?request={row.Id:D}",
                    RelatedEntityType: nameof(TokenRequest),
                    RelatedEntityId: row.Id),
                cancellationToken);
        }
    }

    private async Task NotifyRequesterHandledAsync(
        TokenRequest row,
        bool approved,
        CancellationToken cancellationToken)
    {
        var title = approved ? "Tokens toegewezen" : "Tokenaanvraag afgewezen";
        var body = approved
            ? $"Je aanvraag voor {row.Amount} tokens is toegewezen."
            : $"Je aanvraag voor {row.Amount} tokens is afgewezen.";
        await _notifications.CreateAsync(
            new NotificationCreateRequest(
                row.RequestedByUserId,
                title,
                body,
                "token_request",
                DeepLink: "/werkgever/tokens",
                RelatedEntityType: nameof(TokenRequest),
                RelatedEntityId: row.Id),
            cancellationToken);
    }

    private async Task<TokenRequestDto?> ToDtoAsync(Guid id, CancellationToken cancellationToken)
    {
        var r = await _db.TokenRequests.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.OrganisationCompanyId,
                x.BranchCompanyId,
                BranchName = x.BranchCompany.Name,
                x.RequestedByUserId,
                RequestedByName = x.RequestedByUser.FullName,
                x.Amount,
                x.Reason,
                x.Note,
                x.Status,
                x.HandledByUserId,
                x.HandledAtUtc,
                x.CreatedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (r is null)
        {
            return null;
        }

        return new TokenRequestDto(
            r.Id,
            r.OrganisationCompanyId,
            r.BranchCompanyId,
            r.BranchName,
            r.RequestedByUserId,
            r.RequestedByName,
            r.Amount,
            r.Reason.ToString(),
            r.Note,
            r.Status.ToString(),
            r.HandledByUserId,
            r.HandledAtUtc,
            r.CreatedAtUtc);
    }
}
