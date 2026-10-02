using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

/// <summary>
/// Carrière 04 §1: the share preview and the revealed employer row come from one PII source,
/// so the promise shown to the candidate cannot drift from what the employer receives.
/// </summary>
public class TalentContactPiiTests
{
    [Fact]
    public void For_trims_values_and_reads_blanks_as_null()
    {
        var pii = TalentContactPii.For(new User
        {
            FullName = "  Kandidaat Test ",
            Email = "kandidaat@lobsy.local",
            PhoneNumber = "   "
        });

        Assert.Equal("Kandidaat Test", pii.Name);
        Assert.Equal("kandidaat@lobsy.local", pii.Email);
        Assert.Null(pii.Phone);
        Assert.Equal(TalentContactPii.None, TalentContactPii.For(null));
    }

    [Fact]
    public async Task Share_preview_equals_the_fields_revealed_to_the_employer()
    {
        await using var db = CreateDb();
        var (companyId, candidateId, requestId) = await SeedAsync(db, phone: "+31600000001");
        var sut = Service(db);

        var preview = await sut.GetSharePreviewAsync(candidateId, requestId);
        Assert.NotNull(preview);

        await sut.CandidateRespondAsync(candidateId, requestId, accept: true, alreadyPlaced: false);
        var employerRow = (await sut.ListForEmployerAsync(companyId)).Single();

        Assert.True(employerRow.PiiRevealed);
        Assert.Equal(employerRow.CandidateFullName, preview!.Name);
        Assert.Equal(employerRow.CandidateEmail, preview.Email);
        Assert.Equal(employerRow.CandidatePhone, preview.Phone);
        Assert.Equal(employerRow.CompanyName, preview.CompanyName);
    }

    [Fact]
    public async Task A_missing_phone_stays_null_in_the_preview()
    {
        await using var db = CreateDb();
        var (_, candidateId, requestId) = await SeedAsync(db, phone: null);

        var preview = await Service(db).GetSharePreviewAsync(candidateId, requestId);

        Assert.NotNull(preview);
        Assert.Null(preview!.Phone);
        Assert.False(string.IsNullOrWhiteSpace(preview.Name));
    }

    [Fact]
    public async Task A_foreign_request_has_no_preview()
    {
        await using var db = CreateDb();
        var (_, _, requestId) = await SeedAsync(db, phone: null);

        Assert.Null(await Service(db).GetSharePreviewAsync(Guid.NewGuid(), requestId));
    }

    [Fact]
    public async Task Declining_stores_why_and_accepting_clears_it()
    {
        await using var db = CreateDb();
        var (_, candidateId, requestId) = await SeedAsync(db, phone: null);
        var sut = Service(db);

        var declined = await sut.CandidateRespondAsync(
            candidateId, requestId, accept: false, alreadyPlaced: true);
        Assert.Equal(TalentContactStatus.CandidateDeclined, declined.Status);
        Assert.Equal(TalentContactDeclineReasons.AlreadyPlaced, declined.CandidateDeclineReason);

        var (_, otherCandidate, otherRequest) = await SeedAsync(db, phone: null);
        var plain = await sut.CandidateRespondAsync(
            otherCandidate, otherRequest, accept: false, alreadyPlaced: false);
        Assert.Equal(TalentContactDeclineReasons.NotInterested, plain.CandidateDeclineReason);
    }

    private static TalentPoolService Service(JobsyDbContext db)
        => new(db, tokens: null!, routing: null!, notifications: null!, commercial: null!);

    private static async Task<(Guid CompanyId, Guid CandidateId, Guid RequestId)> SeedAsync(
        JobsyDbContext db,
        string? phone)
    {
        var companyId = Guid.NewGuid();
        var employerId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Kwekerij De Klauw",
            KvkNumber = "12345678",
            Address = "Veilingweg 1",
            Location = new GeoPoint(52.0, 4.2),
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.AdminCreated,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        });
        db.Users.AddRange(
            new User
            {
                Id = employerId,
                Email = $"boss-{employerId:N}@lobsy.local",
                FullName = "Baas",
                Role = UserRole.BranchManager,
                CompanyId = companyId,
                IsActive = true
            },
            new User
            {
                Id = candidateId,
                Email = $"kandidaat-{candidateId:N}@lobsy.local",
                FullName = "Kandidaat Test",
                PhoneNumber = phone,
                Role = UserRole.Candidate,
                IsActive = true
            });

        var created = DateTime.UtcNow.AddHours(-1);
        db.TalentContactRequests.Add(new TalentContactRequest
        {
            Id = requestId,
            CompanyId = companyId,
            EmployerUserId = employerId,
            CandidateUserId = candidateId,
            Status = TalentContactStatus.Pending,
            Message = "We zoeken iemand zoals jij.",
            CreatedAtUtc = created,
            RespondByUtc = created.AddHours(48)
        });
        await db.SaveChangesAsync();
        return (companyId, candidateId, requestId);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }
}
