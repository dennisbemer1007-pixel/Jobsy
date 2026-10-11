using System.Text.Json;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services.CandidateExternalVacancies;

public sealed class ExternalVacancyEmployerInviteService : IExternalVacancyEmployerInviteService
{
    private readonly JobsyDbContext _db;
    private readonly IOneTimeLinkService _links;

    public ExternalVacancyEmployerInviteService(JobsyDbContext db, IOneTimeLinkService links)
    {
        _db = db;
        _links = links;
    }

    public async Task<ExternalVacancyEmployerInviteDto?> ResolveInviteAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var peek = await _links.PeekAsync(OneTimeLinkPurpose.ExternalVacancyEmployerInvite, token, cancellationToken);
        if (!peek.Valid || peek.LinkId is null || peek.ExpiresAtUtc is null)
        {
            return null;
        }

        var outbound = await _db.CandidateExternalVacancyOutbounds
            .Include(o => o.ExternalVacancy).ThenInclude(v => v.CandidateUser)
            .FirstOrDefaultAsync(o => o.OneTimeLinkId == peek.LinkId, cancellationToken);
        if (outbound is null)
        {
            return null;
        }

        if (outbound.ClickedAtUtc is null)
        {
            outbound.ClickedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        var facts = ParseSharedFacts(outbound.SharedFactsJson);
        var candidate = outbound.ExternalVacancy.CandidateUser;
        return new ExternalVacancyEmployerInviteDto(
            outbound.ExternalVacancy.Title,
            outbound.ExternalVacancy.CompanyName,
            outbound.ExternalVacancy.Place,
            candidate.FirstName ?? candidate.FullName,
            TrimMotivation(outbound.Motivation),
            facts,
            outbound.EmployerEmailNormalized,
            peek.ExpiresAtUtc.Value);
    }

    private static string TrimMotivation(string motivation)
    {
        var trimmed = motivation.Trim();
        return trimmed.Length <= 240 ? trimmed : trimmed[..240] + "…";
    }

    internal static IReadOnlyList<(string Label, string Value)> ParseSharedFacts(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return [];
            }

            return doc.RootElement.EnumerateObject()
                .Select(p => (p.Name, p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString() ?? "" : p.Value.ToString()))
                .Where(t => !string.IsNullOrWhiteSpace(t.Item2))
                .ToList();
        }
        catch
        {
            return [];
        }
    }
}
