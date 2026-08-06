using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class LobsyCvAccessRulesTests
{
    [Theory]
    [InlineData(ApplicationStatus.Pending, false)]
    [InlineData(ApplicationStatus.Rejected, false)]
    [InlineData(ApplicationStatus.FilledElsewhere, false)]
    [InlineData(ApplicationStatus.Accepted, true)]
    [InlineData(ApplicationStatus.EmployerContacting, true)]
    [InlineData(ApplicationStatus.Hired, true)]
    public void IsPiiRevealed_matches_accept_pipeline(ApplicationStatus status, bool expected)
    {
        Assert.Equal(expected, LobsyCvAccessRules.IsPiiRevealed(status));
        Assert.Equal(expected, ApplicationRules.IsPiiRevealed(status));
    }

    [Fact]
    public void CanEmployerDownloadCv_requires_verified_and_revealed()
    {
        Assert.False(LobsyCvAccessRules.CanEmployerDownloadCv(ApplicationStatus.Accepted, null));
        Assert.False(LobsyCvAccessRules.CanEmployerDownloadCv(ApplicationStatus.Pending, DateTime.UtcNow));
        Assert.True(LobsyCvAccessRules.CanEmployerDownloadCv(ApplicationStatus.Accepted, DateTime.UtcNow));
    }

    [Fact]
    public void CanCandidateDownloadOwnApplication_matches_user_or_email()
    {
        var userId = Guid.NewGuid();
        Assert.True(LobsyCvAccessRules.CanCandidateDownloadOwnApplication(
            userId, userId, "a@test.nl", "other@test.nl"));
        Assert.True(LobsyCvAccessRules.CanCandidateDownloadOwnApplication(
            userId, null, "a@test.nl", "A@TEST.NL"));
        Assert.False(LobsyCvAccessRules.CanCandidateDownloadOwnApplication(
            userId, Guid.NewGuid(), "a@test.nl", "b@test.nl"));
    }

    [Fact]
    public void ExtractCity_from_dutch_address()
    {
        Assert.Equal("Naaldwijk", LobsyCvModelFactory.ExtractCity("Voorstraat 1, 2671 AB Naaldwijk"));
        Assert.Equal("Den Haag", LobsyCvModelFactory.ExtractCity("Den Haag"));
    }
}
