using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
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
