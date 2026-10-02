using Jobsy.Api.Controllers;
using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class ParentalConsentFlowTests
{
    [Fact]
    public async Task Get_confirm_redirects_to_public_web_without_mutating()
    {
        await using var db = CreateDb();
        var (user, token) = await SeedPendingAsync(db);
        var features = new StubFeatures("https://app.lobsy.test");
        var sut = new ParentalConsentController(db, features)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await sut.ConfirmGet(token, CancellationToken.None);
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal($"https://app.lobsy.test/toestemming?t={Uri.EscapeDataString(token)}", redirect.Url);

        var reloaded = await db.Users.SingleAsync(u => u.Id == user.Id);
        Assert.Null(reloaded.ParentalConsentAt);
        Assert.NotNull(reloaded.ParentalConsentTokenHash);
    }

    [Fact]
    public async Task Preview_does_not_mutate_and_post_confirms_once()
    {
        await using var db = CreateDb();
        var (user, token) = await SeedPendingAsync(db, firstName: "Sam");
        var sut = new ParentalConsentController(db, new StubFeatures("https://lobsy.nl"));

        var preview = await sut.Preview(token, CancellationToken.None);
        var previewBody = Assert.IsType<OkObjectResult>(preview.Result).Value as ParentalConsentController.ParentalConsentPreviewResponse;
        Assert.NotNull(previewBody);
        Assert.True(previewBody.Valid);
        Assert.Equal("Sam", previewBody.ChildFirstName);
        Assert.Null((await db.Users.SingleAsync(u => u.Id == user.Id)).ParentalConsentAt);

        var post = await sut.ConfirmPost(new ParentalConsentController.ParentalConsentConfirmRequest(token), CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(post.Result).Value as ParentalConsentController.ParentalConsentConfirmResponse;
        Assert.NotNull(ok);
        Assert.True(ok.Ok);
        Assert.NotNull((await db.Users.SingleAsync(u => u.Id == user.Id)).ParentalConsentAt);

        var second = await sut.ConfirmPost(new ParentalConsentController.ParentalConsentConfirmRequest(token), CancellationToken.None);
        var secondOk = Assert.IsType<OkObjectResult>(second.Result).Value as ParentalConsentController.ParentalConsentConfirmResponse;
        Assert.NotNull(secondOk);
        Assert.True(secondOk.Ok);
    }

    [Fact]
    public void ParentalConsent_mail_names_child_and_escapes()
    {
        var withName = TransactionalEmails.ParentalConsent(
            "https://lobsy.nl", "<b>Sam</b>", "https://lobsy.nl/toestemming?t=abc", DateTime.UtcNow.AddDays(7));
        Assert.Contains("&lt;b&gt;Sam&lt;/b&gt;", withName.Html);
        Assert.Contains("<b>Sam</b> vraagt", withName.Subject);
        Assert.Equal("ParentalConsent", withName.Category);

        var without = TransactionalEmails.ParentalConsent(
            "https://lobsy.nl", null, "https://lobsy.nl/toestemming?t=abc", DateTime.UtcNow.AddDays(7));
        Assert.Equal("Je kind vraagt je toestemming voor Lobsy", without.Subject);
    }

    private static async Task<(User User, string Token)> SeedPendingAsync(
        JobsyDbContext db,
        string? firstName = "Sam")
    {
        var token = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "child@jobsy.local",
            FullName = "Sam Kind",
            FirstName = firstName,
            Role = UserRole.Candidate,
            IsActive = true,
            DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-15)),
            ParentalConsentEmail = "parent@example.com",
            ParentalConsentTokenHash = VerificationCodes.Hash(token),
            ParentalConsentTokenExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return (user, token);
    }

    private static JobsyDbContext CreateDb()
        => new(new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private sealed class StubFeatures(string baseUrl) : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(true, true, baseUrl, null));

        public Task<PlatformFeatureSnapshot> UpdateAsync(PlatformFeatureUpdate update, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
