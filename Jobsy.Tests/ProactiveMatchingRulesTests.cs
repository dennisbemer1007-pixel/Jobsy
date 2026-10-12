using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Xunit;

namespace Jobsy.Tests;

public class ProactiveMatchingRulesTests
{
    [Fact]
    public void Availability_filter_excludes_not_open_for_work()
    {
        var open = new User
        {
            Id = Guid.NewGuid(),
            Role = UserRole.Candidate,
            IsActive = true,
            OpenForWork = true,
            TalentPoolConsentAt = DateTime.UtcNow,
            DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25))
        };
        open.TalentPoolConsentVersion = PrivacyConstants.CandidateProfilingConsentVersion;

        var closed = new User
        {
            Id = Guid.NewGuid(),
            Role = UserRole.Candidate,
            IsActive = true,
            OpenForWork = false,
            TalentPoolConsentAt = DateTime.UtcNow,
            DateOfBirth = open.DateOfBirth
        };
        closed.TalentPoolConsentVersion = PrivacyConstants.CandidateProfilingConsentVersion;

        Assert.True(ProactiveTalentAvailabilityRules.IsEligibleForEmployerShortlist(open));
        Assert.False(ProactiveTalentAvailabilityRules.IsEligibleForEmployerShortlist(closed));

        var filtered = ProactiveTalentAvailabilityRules.FilterEligible([open, closed]).ToList();
        Assert.Single(filtered);
        Assert.Equal(open.Id, filtered[0].Id);
    }

    [Fact]
    public void Same_vacancy_cooldown_blocks_repost_within_window()
    {
        var now = new DateTime(2026, 10, 12, 12, 0, 0, DateTimeKind.Utc);
        var history = new List<ProactiveDeliveryHistoryEntry>
        {
            new("Orderpicker (19)", "Westland Bloemen", now.AddDays(-10), null)
        };

        Assert.True(ProactiveMatchingRules.ShouldSkipProactiveDelivery(
            history,
            "Orderpicker (29)",
            "Westland Bloemen",
            now,
            cooldownDays: 30));

        Assert.False(ProactiveMatchingRules.ShouldSkipProactiveDelivery(
            history,
            "Orderpicker (29)",
            "Westland Bloemen",
            now.AddDays(21),
            cooldownDays: 30));
    }

    [Fact]
    public void Not_interested_blocks_same_title_and_company_forever()
    {
        var now = new DateTime(2026, 10, 12, 12, 0, 0, DateTimeKind.Utc);
        var history = new List<ProactiveDeliveryHistoryEntry>
        {
            new("Activiteitenbegeleider in Seghwaert", "Care Co", now.AddDays(-120), ProactiveMatchFeedback.NotInterested)
        };

        Assert.True(ProactiveMatchingRules.IsPermanentlyBlockedByNotInterested(
            history,
            "Activiteitenbegeleider in Seghwaert (9)",
            "Care Co"));

        Assert.True(ProactiveMatchingRules.ShouldSkipProactiveDelivery(
            history,
            "Activiteitenbegeleider in Seghwaert",
            "Care Co",
            now.AddYears(1),
            cooldownDays: 30));

        Assert.False(ProactiveMatchingRules.ShouldSkipProactiveDelivery(
            history,
            "Activiteitenbegeleider in Seghwaert",
            "Other Employer",
            now,
            cooldownDays: 30));
    }

    [Fact]
    public void Generic_stored_rationale_is_detected_for_enrichment()
    {
        Assert.True(ProactiveMatchingRules.IsGenericStoredRationale("Deze vacature past bij jouw profiel."));
        Assert.True(ProactiveMatchingRules.IsGenericStoredRationale(
            "Deze externe vacature sluit aan bij wat je eerder hebt opgeslagen."));
        Assert.False(ProactiveMatchingRules.IsGenericStoredRationale("Beroepsinteresse past · Goede reistijd"));
    }

    [Fact]
    public void In_app_notification_titles_follow_dennis_format()
    {
        Assert.Equal(
            "Nieuwe match: Orderpicker bij Westland Bloemen, 87%",
            ProactiveMatchingRules.FormatInAppNewMatchTitle("Orderpicker", "Westland Bloemen", 87));
        Assert.Equal(
            "Nieuw kennismakingsverzoek: Orderpicker bij Westland Bloemen",
            ProactiveMatchingRules.FormatInAppIntroductionTitle("Orderpicker", "Westland Bloemen"));
    }

    [Fact]
    public void Deferred_introduction_charges_token_only_after_accept()
    {
        var pending = new TalentContactRequest { TokenChargeOnAccept = true, Status = TalentContactStatus.Pending };
        Assert.True(ProactiveIntroductionTokenRules.ShouldChargeTokenOnAccept(pending, accept: true, alreadyPlaced: false));
        Assert.False(ProactiveIntroductionTokenRules.EmployerSeesPii(pending));

        var sharedWithoutToken = new TalentContactRequest
        {
            TokenChargeOnAccept = true,
            Status = TalentContactStatus.ContactShared
        };
        Assert.False(ProactiveIntroductionTokenRules.EmployerSeesPii(sharedWithoutToken));

        var sharedWithToken = new TalentContactRequest
        {
            TokenChargeOnAccept = true,
            Status = TalentContactStatus.ContactShared,
            SpendTransactionId = Guid.NewGuid()
        };
        Assert.True(ProactiveIntroductionTokenRules.EmployerSeesPii(sharedWithToken));

        var legacy = new TalentContactRequest
        {
            TokenChargeOnAccept = false,
            Status = TalentContactStatus.ContactShared
        };
        Assert.True(ProactiveIntroductionTokenRules.EmployerSeesPii(legacy));
        Assert.False(ProactiveIntroductionTokenRules.ShouldChargeTokenOnAccept(legacy, true, false));
    }
}
