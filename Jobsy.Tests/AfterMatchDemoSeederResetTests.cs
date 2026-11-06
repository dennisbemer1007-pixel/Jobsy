using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Time;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public sealed class AfterMatchDemoSeederResetTests
{
    private static readonly Guid WestlandId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid BranchManagerId = Guid.Parse("bbbbbbbb-1111-1111-1111-111111111111");

    [Fact]
    public void Demo_slot_starts_are_in_the_future()
    {
        var slots = AfterMatchEmployerDemoSlots.InitialUtc(DateTime.UtcNow);
        Assert.Equal(3, slots.Length);
        foreach (var start in slots)
        {
            Assert.True(start > DateTime.UtcNow.AddHours(-1));
        }
    }

    [Fact]
    public async Task Ensure_removes_westland_inbox_applications_and_refreshes_4790_slot()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new JobsyDbContext(options);
        var logger = NullLogger.Instance;

        db.Companies.Add(new Company
        {
            Id = WestlandId,
            Name = "Den Haag Centrum",
            KvkNumber = "12345678",
            KvkEstablishmentId = "12345678_0002",
            Type = CompanyType.Employer,
            Location = new GeoPoint(52.07, 4.31),
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.AdminCreated,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        });
        var vacancyId = Guid.NewGuid();
        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            CompanyId = WestlandId,
            Title = "Test",
            Description = "x",
            HourlyWage = 14m,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(1),
            Status = VacancyStatus.Active,
            Location = new GeoPoint(52.07, 4.31),
            PublishedAtUtc = DateTime.UtcNow
        });
        db.Users.Add(new User
        {
            Id = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111"),
            Email = "kandidaat@jobsy.local",
            FullName = "Sanne de Vries",
            Role = UserRole.Candidate,
            IsActive = true
        });
        db.Users.Add(new User
        {
            Id = BranchManagerId,
            Email = "ondernemer@jobsy.local",
            FullName = "Demo Filiaalmanager",
            Role = UserRole.BranchManager,
            CompanyId = WestlandId,
            IsActive = true
        });
        db.Users.Add(new User
        {
            Id = Guid.Parse("aaaaaaaa-2222-2222-2222-222222222222"),
            Email = "kandidaat.denhaag@jobsy.local",
            FullName = "Demo Kandidaat Den Haag",
            Role = UserRole.Candidate,
            IsActive = true
        });
        db.Applications.Add(new Application
        {
            Id = Guid.NewGuid(),
            VacancyId = vacancyId,
            CandidateUserId = Guid.Parse("aaaaaaaa-2222-2222-2222-222222222222"),
            CandidateName = "Stale",
            CandidateEmail = "stale@example.com",
            PreferredTransport = "Fiets",
            Status = ApplicationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            EmailVerifiedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        await AfterMatchDemoSeeder.EnsureAsync(db, logger);

        var keepIds = new[]
        {
            AfterMatchDemoIds.WaitApp,
            AfterMatchDemoIds.PickSlotsApp,
            AfterMatchDemoIds.ScheduledApp,
            AfterMatchDemoIds.RescheduleApp,
            AfterMatchDemoIds.NotNowApp,
            AfterMatchDemoIds.Verkoop4790App
        };
        Assert.Equal(0, await db.Applications.CountAsync(a =>
            a.Vacancy.CompanyId == WestlandId && !keepIds.Contains(a.Id)));
        Assert.Equal(6, await db.Applications.CountAsync(a => keepIds.Contains(a.Id)));
        Assert.All(
            await db.Applications.Where(a => keepIds.Contains(a.Id)).Select(a => a.CandidateUserId).ToListAsync(),
            id => Assert.Equal(Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111"), id));
        Assert.Equal(6, await db.Applications.CountAsync(a => a.Vacancy.CompanyId == WestlandId));

        var app4790 = await db.Applications.Include(a => a.Vacancy)
            .SingleAsync(a => a.Id == AfterMatchDemoIds.Verkoop4790App);
        var interview = await db.Interviews.Include(i => i.Slots).SingleAsync(i => i.ApplicationId == app4790.Id);
        var chosen = interview.Slots.Single(s => s.IsSelected);
        Assert.True(chosen.StartUtc > DateTime.UtcNow.AddHours(-1));

        Assert.True(await db.UserCompanies.AnyAsync(m =>
            m.UserId == BranchManagerId && m.CompanyId == WestlandId));
        var recentCutoff = DateTime.UtcNow.AddDays(-14);
        Assert.All(
            await db.Applications.Where(a => keepIds.Contains(a.Id)).ToListAsync(),
            a =>
            {
                Assert.True(a.CreatedAt >= recentCutoff);
                Assert.True(a.RespondedAt >= recentCutoff);
            });
    }
}
