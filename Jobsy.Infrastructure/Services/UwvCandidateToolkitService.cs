using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Security;
using Jobsy.Core.Uwv;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed record UwvToolkitStateDto(
    string? OndernemerOutcomeKey,
    DateTime? OndernemerCompletedAtUtc,
    IReadOnlyList<string> DrijfverenDriverKeys,
    DateTime? DrijfverenCompletedAtUtc);

public sealed record UwvAdviseurShareCreatedDto(string Token, DateTime ExpiresAtUtc);

public sealed record UwvAdviseurShareViewDto(
    string Language,
    IReadOnlyList<string> Sections,
    string CandidateDisplayName,
    string? OndernemerOutcomeKey,
    IReadOnlyList<string> DrijfverenDriverKeys,
    IReadOnlyList<UwvAdviseurEndorsedCompetenceDto> EndorsedCompetences,
    DateTime ExpiresAtUtc);

public sealed record UwvAdviseurEndorsedCompetenceDto(string Label, string? RecognitionLine);

public sealed class UwvCandidateToolkitService(
    JobsyDbContext db,
    ReferenceVerificationService referenceVerification,
    IPlatformFeatureService features)
{
    public static readonly TimeSpan ShareLinkLifetime = TimeSpan.FromHours(48);

    public async Task<UwvToolkitStateDto> GetStateAsync(Guid userId, CancellationToken cancellationToken)
    {
        var row = await db.CandidateUwvToolkitStates.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        return ToDto(row);
    }

    public async Task<UwvToolkitStateDto> SaveOndernemerAsync(
        Guid userId,
        IReadOnlyList<int> answers,
        CancellationToken cancellationToken)
    {
        if (answers.Count is < 1 or > 12)
        {
            throw new ArgumentException("Invalid ondernemer answers count.");
        }

        foreach (var a in answers)
        {
            if (a is < 1 or > 5)
            {
                throw new ArgumentException("Answer out of range.");
            }
        }

        var outcome = ScoreOndernemerOutcome(answers);
        var now = DateTime.UtcNow;
        var row = await db.CandidateUwvToolkitStates.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (row is null)
        {
            row = new CandidateUwvToolkitState { UserId = userId };
            db.CandidateUwvToolkitStates.Add(row);
        }

        row.OndernemerAnswersJson = JsonSerializer.Serialize(answers);
        row.OndernemerOutcomeKey = outcome;
        row.OndernemerCompletedAtUtc = now;
        row.UpdatedAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(row);
    }

    public async Task<UwvToolkitStateDto> SaveDrijfverenAsync(
        Guid userId,
        IReadOnlyList<string> driverKeys,
        CancellationToken cancellationToken)
    {
        var keys = driverKeys.Where(UwvDriverKeys.IsValid).Distinct(StringComparer.Ordinal).Take(3).ToList();
        if (keys.Count == 0)
        {
            throw new ArgumentException("At least one driver key required.");
        }

        var now = DateTime.UtcNow;
        var row = await db.CandidateUwvToolkitStates.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (row is null)
        {
            row = new CandidateUwvToolkitState { UserId = userId };
            db.CandidateUwvToolkitStates.Add(row);
        }

        row.DrijfverenDriverKeysJson = JsonSerializer.Serialize(keys);
        row.DrijfverenCompletedAtUtc = now;
        row.UpdatedAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(row);
    }

    public async Task<UwvAdviseurShareCreatedDto> CreateShareLinkAsync(
        Guid userId,
        IReadOnlyList<string> sections,
        string language,
        CancellationToken cancellationToken)
    {
        var normalized = sections
            .Where(UwvAdviseurSectionKeys.IsValid)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (normalized.Count == 0)
        {
            throw new ArgumentException("Select at least one section.");
        }

        var lang = string.Equals(language, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "nl";
        var token = GenerateToken();
        var hash = VerificationCodes.Hash(token);
        var now = DateTime.UtcNow;
        var expires = now.Add(ShareLinkLifetime);

        db.CandidateUwvAdviseurShares.Add(new CandidateUwvAdviseurShare
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = hash,
            SectionsJson = JsonSerializer.Serialize(normalized),
            Language = lang,
            CreatedAtUtc = now,
            ExpiresAtUtc = expires
        });
        await db.SaveChangesAsync(cancellationToken);
        return new UwvAdviseurShareCreatedDto(token, expires);
    }

    public async Task<UwvAdviseurShareViewDto?> GetShareViewAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 128)
        {
            return null;
        }

        var hash = VerificationCodes.Hash(token.Trim());
        var now = DateTime.UtcNow;
        var share = await db.CandidateUwvAdviseurShares.AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.TokenHash == hash && s.RevokedAtUtc == null && s.ExpiresAtUtc > now,
                cancellationToken);
        if (share is null)
        {
            return null;
        }

        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == share.UserId)
            .Select(u => new { u.FullName, u.FirstName, u.LastName })
            .FirstOrDefaultAsync(cancellationToken);
        var display = user?.FullName;
        if (string.IsNullOrWhiteSpace(display))
        {
            display = string.Join(' ', new[] { user?.FirstName, user?.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
        }

        if (string.IsNullOrWhiteSpace(display))
        {
            display = share.Language == "en" ? "Candidate" : "Kandidaat";
        }

        var toolkit = await db.CandidateUwvToolkitStates.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == share.UserId, cancellationToken);
        var sections = JsonSerializer.Deserialize<List<string>>(share.SectionsJson) ?? [];
        var drivers = ParseDriverKeys(toolkit?.DrijfverenDriverKeysJson);

        var endorsed = new List<UwvAdviseurEndorsedCompetenceDto>();
        if (sections.Contains(UwvAdviseurSectionKeys.Competences, StringComparer.Ordinal))
        {
            var snap = await features.GetAsync(cancellationToken);
            if (snap.ReferenceVerificationEnabled)
            {
                var facts = await referenceVerification.VerifiedCompetencesForCandidateAsync(
                    share.UserId,
                    share.Language,
                    cancellationToken);
                endorsed.AddRange(facts.Select(f => new UwvAdviseurEndorsedCompetenceDto(f.Label, f.RecognitionLine)));
            }
        }

        return new UwvAdviseurShareViewDto(
            share.Language,
            sections,
            display.Trim(),
            toolkit?.OndernemerOutcomeKey,
            drivers,
            endorsed,
            share.ExpiresAtUtc);
    }

    private static string ScoreOndernemerOutcome(IReadOnlyList<int> answers)
    {
        var sum = answers.Sum();
        var max = answers.Count * 5;
        if (sum >= max - answers.Count)
        {
            return "ready";
        }

        if (sum >= max / 2)
        {
            return "explore";
        }

        return "cautious";
    }

    private static UwvToolkitStateDto ToDto(CandidateUwvToolkitState? row)
        => new(
            row?.OndernemerOutcomeKey,
            row?.OndernemerCompletedAtUtc,
            ParseDriverKeys(row?.DrijfverenDriverKeysJson),
            row?.DrijfverenCompletedAtUtc);

    private static IReadOnlyList<string> ParseDriverKeys(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json)?
                       .Where(UwvDriverKeys.IsValid)
                       .Distinct(StringComparer.Ordinal)
                       .Take(3)
                       .ToList()
                   ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
