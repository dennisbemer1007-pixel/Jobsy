using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class TalentPoolListForEmployerTests
{
    [Fact]
    public async Task ListForEmployer_preserves_order_and_reveals_pii_only_when_shared()
    {
        await using var db = CreateDb();
        var companyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var employerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var candidateSharedId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var candidatePendingId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var olderId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var newerId = Guid.Parse("66666666-6666-6666-6666-666666666666");

        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Acme BV",
            KvkNumber = "12345678",
            Address = "Straat 1",
            Location = new GeoPoint(52.1, 5.1)
        });
        db.Users.AddRange(
            new User
            {
                Id = employerId,
                Email = "boss@jobsy.local",
                FullName = "Boss",
                Role = UserRole.BranchManager,
                CompanyId = companyId,
                IsActive = true
            },
            new User
            {
                Id = candidateSharedId,
                Email = "shared@jobsy.local",
                FullName = "Shared Candidate",
                PhoneNumber = "+31600000001",
                Role = UserRole.Candidate,
                IsActive = true
            },
            new User
            {
                Id = candidatePendingId,
                Email = "pending@jobsy.local",
                FullName = "Pending Candidate",
                PhoneNumber = "+31600000002",
                Role = UserRole.Candidate,
                IsActive = true
            });

        var older = DateTime.UtcNow.AddHours(-2);
        var newer = DateTime.UtcNow.AddHours(-1);
        db.TalentContactRequests.AddRange(
            new TalentContactRequest
            {
                Id = olderId,
                CompanyId = companyId,
                EmployerUserId = employerId,
                CandidateUserId = candidatePendingId,
                Status = TalentContactStatus.Pending,
                Message = "Hello pending",
                CreatedAtUtc = older,
                RespondByUtc = older.AddHours(48)
            },
            new TalentContactRequest
            {
                Id = newerId,
                CompanyId = companyId,
                EmployerUserId = employerId,
                CandidateUserId = candidateSharedId,
                Status = TalentContactStatus.ContactShared,
                Message = "Hello shared",
                CreatedAtUtc = newer,
                RespondByUtc = newer.AddHours(48),
                ContactSharedAtUtc = newer.AddMinutes(30),
                RespondedAtUtc = newer.AddMinutes(30)
            });
        await db.SaveChangesAsync();

        var sut = new TalentPoolService(
            db,
            tokens: null!,
            routing: null!,
            notifications: null!,
            commercial: null!);

        var list = await sut.ListForEmployerAsync(companyId);

        Assert.Equal(2, list.Count);
        Assert.Equal(newerId, list[0].Id);
        Assert.Equal(olderId, list[1].Id);

        Assert.True(list[0].PiiRevealed);
        Assert.Equal("Shared Candidate", list[0].CandidateFullName);
        Assert.Equal("shared@jobsy.local", list[0].CandidateEmail);
        Assert.Equal("+31600000001", list[0].CandidatePhone);
        Assert.Equal("Acme BV", list[0].CompanyName);

        Assert.False(list[1].PiiRevealed);
        Assert.Null(list[1].CandidateFullName);
        Assert.Null(list[1].CandidateEmail);
        Assert.Null(list[1].CandidatePhone);
        Assert.Equal("Acme BV", list[1].CompanyName);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }
}
