using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Infrastructure.Services.CandidateExternalVacancies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class ExternalVacancyMetricsTests
{
    [Fact]
    public async Task Admin_metrics_count_click_account_and_accepted_from_outbound_rows()
    {
        await using var db = CreateDb();
        var extId = Guid.NewGuid();
        db.CandidateExternalVacancies.Add(new CandidateExternalVacancy
        {
            Id = extId,
            CandidateUserId = Guid.NewGuid(),
            SourceUrl = "https://x.nl",
            SourceHost = "x.nl",
            Title = "T",
            CompanyName = "C",
            Place = "P"
        });
        var now = DateTime.UtcNow;
        db.CandidateExternalVacancyOutbounds.AddRange(
            new CandidateExternalVacancyOutbound
            {
                Id = Guid.NewGuid(),
                ExternalVacancyId = extId,
                EmployerEmailNormalized = "a@test.nl",
                Motivation = "m",
                InitialSentAtUtc = now,
                ClickedAtUtc = now
            },
            new CandidateExternalVacancyOutbound
            {
                Id = Guid.NewGuid(),
                ExternalVacancyId = extId,
                EmployerEmailNormalized = "b@test.nl",
                Motivation = "m",
                InitialSentAtUtc = now,
                EmployerAccountCreatedAtUtc = now
            },
            new CandidateExternalVacancyOutbound
            {
                Id = Guid.NewGuid(),
                ExternalVacancyId = extId,
                EmployerEmailNormalized = "c@test.nl",
                Motivation = "m",
                InitialSentAtUtc = now,
                AcceptedAtUtc = now
            });
        await db.SaveChangesAsync();

        var rows = await db.CandidateExternalVacancyOutbounds.AsNoTracking().ToListAsync();
        Assert.Equal(3, rows.Count);
        Assert.Equal(1, rows.Count(r => r.ClickedAtUtc is not null));
        Assert.Equal(1, rows.Count(r => r.EmployerAccountCreatedAtUtc is not null));
        Assert.Equal(1, rows.Count(r => r.AcceptedAtUtc is not null));
    }

    [Fact]
    public async Task ResolveInvite_sets_clicked_timestamp_for_bekijk_sollicitatie_link()
    {
        await using var db = CreateDb();
        var candidateId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = candidateId,
            Email = "k@test.nl",
            FullName = "K",
            Role = UserRole.Candidate,
            IsActive = true
        });
        var extId = Guid.NewGuid();
        db.CandidateExternalVacancies.Add(new CandidateExternalVacancy
        {
            Id = extId,
            CandidateUserId = candidateId,
            SourceUrl = "https://x.nl",
            SourceHost = "x.nl",
            Title = "T",
            CompanyName = "C",
            Place = "P"
        });
        await db.SaveChangesAsync();

        var links = new OneTimeLinkService(db, NullLogger<OneTimeLinkService>.Instance);
        var link = await links.CreateAsync(
            OneTimeLinkPurpose.ExternalVacancyEmployerInvite,
            null,
            null,
            "wg@test.nl",
            OneTimeLinkRules.ExternalVacancyEmployerInviteLifetime,
            candidateId);
        var outboundId = Guid.NewGuid();
        db.CandidateExternalVacancyOutbounds.Add(new CandidateExternalVacancyOutbound
        {
            Id = outboundId,
            ExternalVacancyId = extId,
            EmployerEmailNormalized = "wg@test.nl",
            Motivation = "Hi",
            InitialSentAtUtc = DateTime.UtcNow,
            OneTimeLinkId = link.Id
        });
        await db.SaveChangesAsync();

        var invites = new ExternalVacancyEmployerInviteService(db, links);
        var dto = await invites.ResolveInviteAsync(link.Token);
        Assert.NotNull(dto);

        var outbound = await db.CandidateExternalVacancyOutbounds.FirstAsync(o => o.Id == outboundId);
        Assert.NotNull(outbound.ClickedAtUtc);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
