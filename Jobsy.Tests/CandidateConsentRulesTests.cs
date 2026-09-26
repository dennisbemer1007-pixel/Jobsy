using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Privacy;

namespace Jobsy.Tests;

public class CandidateConsentRulesTests
{
    [Fact]
    public void Talent_pool_excludes_non_consenting_and_under_18()
    {
        var adultNoConsent = User(DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-25));
        Assert.False(CandidateConsentRules.CanAppearInTalentPool(adultNoConsent));

        var adultConsent = User(DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-25));
        adultConsent.TalentPoolConsentAt = DateTime.UtcNow;
        adultConsent.TalentPoolConsentVersion = PrivacyConstants.CandidateProfilingConsentVersion;
        Assert.True(CandidateConsentRules.CanAppearInTalentPool(adultConsent));

        var minorConsent = User(DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-17));
        minorConsent.TalentPoolConsentAt = DateTime.UtcNow;
        minorConsent.TalentPoolConsentVersion = PrivacyConstants.CandidateProfilingConsentVersion;
        Assert.False(CandidateConsentRules.CanAppearInTalentPool(minorConsent));
    }

    [Fact]
    public void Under_16_without_parental_consent_cannot_use_candidate_features()
    {
        var under16 = User(DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-15));
        Assert.True(CandidateConsentRules.RequiresParentalConsent(under16));
        Assert.False(CandidateConsentRules.CanUseCandidateFeatures(under16));

        under16.ParentalConsentAt = DateTime.UtcNow;
        Assert.True(CandidateConsentRules.CanUseCandidateFeatures(under16));
    }

    [Fact]
    public void Test_ai_consent_requires_current_version()
    {
        var user = User(DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-20));
        Assert.False(CandidateConsentRules.HasCurrentTestAiConsent(user));

        user.TestAiConsentAt = DateTime.UtcNow;
        user.TestAiConsentVersion = "oud";
        Assert.False(CandidateConsentRules.HasCurrentTestAiConsent(user));

        user.TestAiConsentVersion = PrivacyConstants.CandidateProfilingConsentVersion;
        Assert.True(CandidateConsentRules.HasCurrentTestAiConsent(user));
    }

    [Fact]
    public void Platform_legal_identity_uses_placeholders()
    {
        Assert.Equal("[BEDRIJFSNAAM]", PlatformLegalIdentity.CompanyName);
        Assert.Equal("[KVK-NUMMER]", PlatformLegalIdentity.KvkNumber);
        Assert.Equal("[ADRES]", PlatformLegalIdentity.Address);
        Assert.Equal("[CONTACT E-MAIL PRIVACY]", PlatformLegalIdentity.PrivacyEmail);
    }

    private static User User(DateOnly dob) => new()
    {
        Id = Guid.NewGuid(),
        Email = "consent@jobsy.local",
        FullName = "Consent Test",
        Role = UserRole.Candidate,
        IsActive = true,
        DateOfBirth = dob
    };
}
