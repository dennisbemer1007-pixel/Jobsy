using Jobsy.Api.Controllers;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class SetPasswordFlowTests
{
    [Fact]
    public async Task Invite_link_sets_password_once_and_enforces_rules()
    {
        await using var db = CreateDb();
        var links = new OneTimeLinkService(db, NullLogger<OneTimeLinkService>.Instance);
        var features = new AlwaysOnFeatures();
        var email = new RecordingEmail();
        var invite = new SalesManagerInviteService(
            db, email, links, features, NullLogger<SalesManagerInviteService>.Instance);

        var result = await invite.InviteAsync("new.sm@jobsy.local", "New SM");
        Assert.False(await db.LocalAuthCredentials.AnyAsync(c => c.UserId == result.UserId));
        Assert.Contains("/account/wachtwoord-instellen?t=", email.LastHtml);

        var token = ExtractToken(email.LastHtml!);
        var setup = new AccountSetupController(db, links);

        var bad = await setup.SetupPassword(
            new AccountSetupController.SetupPasswordRequest(token, "short"), CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(bad);

        var ok = await setup.SetupPassword(
            new AccountSetupController.SetupPasswordRequest(token, "GoedWachtwoord12!"), CancellationToken.None);
        Assert.IsType<OkObjectResult>(ok);

        var credential = await db.LocalAuthCredentials.SingleAsync(c => c.UserId == result.UserId);
        Assert.True(JobsyPasswordHasher.Verify("GoedWachtwoord12!", credential.PasswordHash));

        var reuse = await setup.SetupPassword(
            new AccountSetupController.SetupPasswordRequest(token, "AnderWachtwoord12!"), CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(reuse);
    }

    private static string ExtractToken(string html)
    {
        const string marker = "/account/wachtwoord-instellen?t=";
        var idx = html.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(idx >= 0);
        var start = idx + marker.Length;
        var end = html.IndexOf('"', start);
        if (end < 0)
        {
            end = html.IndexOf('\'', start);
        }

        Assert.True(end > start);
        return Uri.UnescapeDataString(html[start..end]);
    }

    private static JobsyDbContext CreateDb()
        => new(new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private sealed class RecordingEmail : IEmailService
    {
        public string? LastHtml { get; private set; }

        public Task<EmailDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            LastHtml = message.BodyHtml;
            return Task.FromResult(EmailDeliveryResult.Stub);
        }
    }
}
