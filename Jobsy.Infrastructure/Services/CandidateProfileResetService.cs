using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace Jobsy.Infrastructure.Services;

public sealed class CandidateProfileResetService : ICandidateProfileResetService
{
    /// <summary>Candidate onboarding re-entry after reset (new intro / wizard).</summary>
    public const string RedirectPathAfterReset = "/candidate/start";

    private readonly JobsyDbContext _db;
    private readonly IUserLookupService _users;

    public CandidateProfileResetService(JobsyDbContext db, IUserLookupService users)
    {
        _db = db;
        _users = users;
    }

    public async Task<ResetCandidateProfileResponse> ResetAsync(
        ClaimsPrincipal principal,
        string confirmPhrase,
        string? uiLanguage,
        CancellationToken cancellationToken = default)
    {
        if (!CandidateProfileResetConfirm.Matches(uiLanguage, confirmPhrase))
        {
            throw new ArgumentException("Bevestig met het exacte woord zoals in de dialoog.");
        }

        var user = await _users.FindByPrincipalAsync(principal, cancellationToken)
                   ?? throw new UnauthorizedAccessException("Gebruiker niet gevonden.");

        if (user.Role != UserRole.Candidate || !user.IsActive)
        {
            throw new InvalidOperationException("Alleen actieve kandidaten kunnen hun profiel resetten.");
        }

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            var useTransaction = _db.Database.IsRelational()
                                 && !string.Equals(
                                     _db.Database.ProviderName,
                                     "Microsoft.EntityFrameworkCore.InMemory",
                                     StringComparison.Ordinal);

            if (useTransaction)
            {
                await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
                await ResetCandidateDataCoreAsync(user, cancellationToken);
                await tx.CommitAsync(cancellationToken);
            }
            else
            {
                await ResetCandidateDataCoreAsync(user, cancellationToken);
            }

            return new ResetCandidateProfileResponse(
                "Je profiel is gewist. Je kunt opnieuw beginnen.",
                RedirectPathAfterReset);
        });
    }

    internal async Task ResetCandidateDataCoreAsync(User user, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var hadResearchConsent = user.ResearchConsentAt is not null;

        await CandidateOwnedDataCleanup.RemoveOwnedDataAsync(_db, user, cancellationToken);

        user.FirstName = null;
        user.LastName = null;
        user.PhoneNumber = null;
        user.PhoneVerifiedAtUtc = null;
        user.PhoneVerifiedE164 = null;
        user.WhatsAppContactAllowed = false;
        user.DateOfBirth = null;
        user.HomeLocation = null;
        user.PreferencesJson = null;
        user.OpenForWork = false;
        user.AvailableFromDate = null;
        user.TalentPoolConsentAt = null;
        user.TalentPoolConsentVersion = null;
        user.TestAiConsentAt = null;
        user.TestAiConsentVersion = null;
        user.ResearchConsentAt = null;
        user.ResearchConsentVersion = null;
        user.ResearchAgeBand = null;
        user.ResearchGender = null;
        user.ResearchEducationLevel = null;
        user.ResearchNativeLanguage = null;
        user.ResearchTestLanguage = null;
        user.ParentalConsentAt = null;
        user.ParentalConsentEmail = null;
        user.ParentalConsentTokenHash = null;
        user.ParentalConsentTokenExpiresAt = null;
        user.CandidateHowToCompletedAt = null;
        user.ReferredByAmbassadeurUserId = null;
        user.ReferredByAmbassadeurTrackingCode = null;
        user.SessionVersion++;

        var idHash = ComputeIdHash(user.Id);
        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "Privacy",
            Message = $"Kandidaat-reset uitgevoerd id-hash={idHash}",
            DetailsJson = JsonSerializer.Serialize(new
            {
                Event = "candidate_profile_reset",
                IdHash = idHash,
                AtUtc = now,
                HadResearchConsent = hadResearchConsent
            }),
            CreatedAt = now
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string ComputeIdHash(Guid userId)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(userId.ToString("N")));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }
}
