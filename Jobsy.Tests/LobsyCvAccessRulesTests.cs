using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;

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

    [Theory]
    [InlineData(ApplicationStatus.Pending, false)]
    [InlineData(ApplicationStatus.Accepted, false)]
    [InlineData(ApplicationStatus.EmployerContacting, false)]
    [InlineData(ApplicationStatus.Hired, true)]
    public void IsDirectContactRevealed_only_when_hired(ApplicationStatus status, bool expected)
    {
        Assert.Equal(expected, LobsyCvAccessRules.IsDirectContactRevealed(status));
        Assert.Equal(expected, ApplicationRules.IsDirectContactRevealed(status));
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
    public void Employer_download_model_omits_direct_contact_until_hired()
    {
        var application = new Application
        {
            CandidateName = "Ada Candidate",
            CandidateEmail = "ada@test.local",
            SnapshotPhoneNumber = "0612345678",
            SnapshotWhatsAppAllowed = true,
            SnapshotDateOfBirth = new DateOnly(1998, 4, 12),
            CandidateAgeYears = 27,
            Motivation = "Graag bij jullie starten",
            PreferredTransport = "Fiets",
            Status = ApplicationStatus.Accepted,
            Vacancy = new Vacancy
            {
                Title = "Magazijnmedewerker",
                Company = new Company { Name = "Demo BV", Address = "Industrieweg 1" },
                Location = new GeoPoint(51.99, 4.21)
            }
        };

        var accepted = LobsyCvModelFactory.FromApplicationForDownload(
            application,
            includePii: true,
            includeDirectContact: ApplicationRules.IsDirectContactRevealed(application.Status));
        Assert.Equal("Ada Candidate", accepted.FullName);
        Assert.Null(accepted.Email);
        Assert.Null(accepted.PhoneNumber);
        Assert.False(accepted.IncludeContactDetails);
        Assert.False(accepted.WhatsAppContactAllowed);
        Assert.Null(accepted.DateOfBirth);
        Assert.Equal(27, accepted.AgeYears);

        application.Status = ApplicationStatus.Hired;
        var hired = LobsyCvModelFactory.FromApplicationForDownload(
            application,
            includePii: true,
            includeDirectContact: ApplicationRules.IsDirectContactRevealed(application.Status));
        Assert.Equal("ada@test.local", hired.Email);
        Assert.Equal("0612345678", hired.PhoneNumber);
        Assert.True(hired.IncludeContactDetails);
        Assert.True(hired.WhatsAppContactAllowed);
        Assert.Equal(new DateOnly(1998, 4, 12), hired.DateOfBirth);
    }

    [Fact]
    public void ExtractCity_from_dutch_address()
    {
        Assert.Equal("Naaldwijk", LobsyCvModelFactory.ExtractCity("Voorstraat 1, 2671 AB Naaldwijk"));
        Assert.Equal("Den Haag", LobsyCvModelFactory.ExtractCity("Den Haag"));
    }
}
