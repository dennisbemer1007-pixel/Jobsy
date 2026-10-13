using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Jobsy.Core.Email;
using Jobsy.Core.Email.Model;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class ReferenceConfirmationTests
{
    [Fact]
    public async Task Request_requires_consent_and_stores_only_a_hash()
    {
        await using var db = CreateDb();
        var (user, reference) = await SeedAsync(db);
        var mail = new CapturingMailer();
        var sut = CreateSut(db, mail, new CapturingPush());

        var denied = await sut.RequestAsync(user.Id, reference.Id, "vakkenvuller", consentAccepted: false);
        Assert.False(denied.Ok);
        Assert.Equal("consent", denied.Error);
        Assert.Empty(mail.Sent);

        var ok = await sut.RequestAsync(user.Id, reference.Id, "vakkenvuller", consentAccepted: true);
        Assert.True(ok.Ok);
        Assert.Single(mail.Sent);

        var token = Regex.Match(mail.Sent[0].Mail.Text, @"/referentie/([a-f0-9]{64})").Groups[1].Value;
        Assert.Equal(64, token.Length);
        var stored = await db.ReferenceConfirmationTokens.SingleAsync();
        Assert.NotEqual(token, stored.TokenHash);
        Assert.DoesNotContain(token, stored.TokenHash, StringComparison.Ordinal);

        var log = await db.ReferenceConfirmationConsentLogs.SingleAsync();
        Assert.Equal("candidate", log.Actor);
        Assert.Equal("requested", log.Action);
        Assert.Equal(ReferenceConfirmationRules.ConsentVersion, log.ConsentVersion);
        Assert.Equal(ReferenceConfirmationRules.CandidateConsentText, log.Text);
        var flat = mail.Sent[0].Mail.Text.Replace("\n", " ", StringComparison.Ordinal);
        Assert.Contains(ReferenceConfirmationRules.RefereePrivacyText, flat, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Submit_notifies_the_candidate_and_a_used_or_expired_token_cannot_be_reused()
    {
        await using var db = CreateDb();
        var (user, reference) = await SeedAsync(db);
        var mail = new CapturingMailer();
        var pushes = new CapturingPush();
        var sut = CreateSut(db, mail, pushes);

        Assert.True((await sut.RequestAsync(user.Id, reference.Id, "vakkenvuller", true)).Ok);
        var token = TokenFrom(mail);

        var open = await sut.OpenAsync(token);
        Assert.Equal("open", open!.State);
        Assert.Equal("Sam", open.CandidateName);
        Assert.DoesNotContain(user.Email, open.CandidateName, StringComparison.Ordinal);

        var submitted = await sut.SubmitAsync(token, true, "2022 tot 2024", "Kwam op tijd", "ja", "Fijne collega");
        Assert.Equal("ok", submitted);

        var row = await db.ReferenceConfirmations.SingleAsync();
        Assert.Equal(ReferenceConfirmationStatus.Confirmed, row.Status);
        Assert.NotNull(row.ConfirmedAtUtc);
        Assert.Equal("yes", row.WorkAgain);
        Assert.Equal("Fijne collega", row.ExtraText);
        Assert.False(row.ShowOnPartnerPassport);

        var note = await db.UserNotifications.SingleAsync();
        Assert.Equal(user.Id, note.UserId);
        Assert.Equal("Je referent heeft geantwoord", note.Title);
        Assert.Contains("/candidate/paspoort?tab=proof", note.DeepLink, StringComparison.Ordinal);
        Assert.Single(pushes.Messages);
        Assert.Equal(user.Email, pushes.Messages[0].UserEmail);

        Assert.Equal("used", await sut.SubmitAsync(token, true, "2022 tot 2024", "Kwam op tijd", "ja", null));

        var expiredPlain = await IssueAndExpireAsync(db, sut, user.Id, reference.Id, mail);
        Assert.Equal("expired", await sut.SubmitAsync(expiredPlain, true, "2020 tot 2021", "Deed mee", "nee", null));
        Assert.Equal("expired", (await sut.OpenAsync(expiredPlain))!.State);
    }

    [Fact]
    public async Task Decline_is_stored_and_the_candidate_is_told()
    {
        await using var db = CreateDb();
        var (user, reference) = await SeedAsync(db, "Kim");
        var mail = new CapturingMailer();
        var pushes = new CapturingPush();
        var sut = CreateSut(db, mail, pushes);
        Assert.True((await sut.RequestAsync(user.Id, reference.Id, "kok", true)).Ok);
        var token = TokenFrom(mail);
        Assert.Equal("ok", await sut.DeclineAsync(token));
        var row = await db.ReferenceConfirmations.SingleAsync();
        Assert.Equal(ReferenceConfirmationStatus.Declined, row.Status);
        Assert.Equal("Je referent doet niet mee", (await db.UserNotifications.SingleAsync()).Title);
        Assert.Single(pushes.Messages);
        Assert.Equal("used", await sut.DeclineAsync(token));
    }

    [Fact]
    public async Task Caps_are_three_per_reference_and_ten_per_month()
    {
        await using var db = CreateDb();
        var user = await UserAsync(db, "Sam");
        var references = new List<CandidateReference>();
        for (var i = 0; i < 4; i++)
        {
            references.Add(await ReferenceAsync(db, user.Id, "Bakkerij " + i, "ref" + i + "@example.com"));
        }

        var mail = new CapturingMailer();
        var sut = CreateSut(db, mail, new CapturingPush());
        for (var n = 0; n < 3; n++)
        {
            Assert.True((await sut.RequestAsync(user.Id, references[0].Id, "vakkenvuller", true)).Ok);
        }

        var fourth = await sut.RequestAsync(user.Id, references[0].Id, "vakkenvuller", true);
        Assert.Equal("limit_reference", fourth.Error);

        for (var i = 1; i < 4; i++)
        {
            var rounds = i == 3 ? 1 : 3;
            for (var n = 0; n < rounds; n++)
            {
                Assert.True((await sut.RequestAsync(user.Id, references[i].Id, "vakkenvuller", true)).Ok);
            }
        }

        var extra = await ReferenceAsync(db, user.Id, "Nog een", "extra@example.com");
        var blocked = await sut.RequestAsync(user.Id, extra.Id, "vakkenvuller", true);
        Assert.Equal("limit_month", blocked.Error);
    }

    [Fact]
    public async Task Partner_passport_shows_only_answers_the_candidate_shares()
    {
        var row = new ReferenceConfirmation
        {
            Status = ReferenceConfirmationStatus.Confirmed,
            ConfirmedAtUtc = new DateTime(2026, 10, 4, 10, 0, 0, DateTimeKind.Utc),
            RoleTitle = "vakkenvuller",
            WorkedHere = true,
            PeriodText = "2022 tot 2024",
            DidWell = "Kwam op tijd",
            WorkAgain = "yes",
            ExtraText = "Fijne collega",
            ShowOnPartnerPassport = false
        };
        Assert.Null(ReferenceConfirmationRules.ForPartner(row, "Bakkerij", "Jan"));

        row.ShowOnPartnerPassport = true;
        row.ShareWorkedHere = true;
        row.ShareDidWell = true;
        var fact = ReferenceConfirmationRules.ForPartner(row, "Bakkerij", "Jan");
        Assert.NotNull(fact);
        Assert.True(fact!.WorkedHere);
        Assert.Equal("Kwam op tijd", fact.DidWell);
        Assert.Null(fact.Period);
        Assert.Null(fact.WorkAgain);
        Assert.Null(fact.Extra);
        Assert.Equal("vakkenvuller", fact.RoleTitle);
    }

    [Fact]
    public async Task Rebind_keeps_the_confirmation_when_the_reference_row_is_replaced()
    {
        await using var db = CreateDb();
        var (user, reference) = await SeedAsync(db);
        var mail = new CapturingMailer();
        var sut = CreateSut(db, mail, new CapturingPush());
        Assert.True((await sut.RequestAsync(user.Id, reference.Id, "vakkenvuller", true)).Ok);
        var confirmation = await db.ReferenceConfirmations.SingleAsync();
        var added = new CandidateReference
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            EmployerName = reference.EmployerName,
            ContactName = reference.ContactName,
            Email = reference.Email,
            Phone = reference.Phone,
            SortOrder = 0,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.CandidateReferences.Add(added);
        ReferenceConfirmationService.Rebind([reference], [added], [confirmation]);
        db.CandidateReferences.Remove(reference);
        await db.SaveChangesAsync();

        var kept = await db.ReferenceConfirmations.SingleAsync();
        Assert.Equal(added.Id, kept.CandidateReferenceId);
        Assert.Equal("vakkenvuller", kept.RoleTitle);
    }

    [Fact]
    public async Task Export_includes_answers_and_deletion_removes_them()
    {
        await using var db = CreateDb();
        var (user, reference) = await SeedAsync(db);
        var mail = new CapturingMailer();
        var sut = CreateSut(db, mail, new CapturingPush());
        Assert.True((await sut.RequestAsync(user.Id, reference.Id, "vakkenvuller", true)).Ok);
        var token = TokenFrom(mail);
        Assert.Equal("ok", await sut.SubmitAsync(token, true, "2022 tot 2024", "Kwam op tijd", "misschien", null));
        Assert.Equal("ok", await sut.ReportMisuseAsync(token, "Dit ben ik niet"));

        var privacy = new PrivacyDataService(db, new Lookup(db), mail, new Features());
        var exported = JsonSerializer.Serialize(await privacy.ExportAsync(Principal(user.Email)));
        Assert.Contains("vakkenvuller", exported, StringComparison.Ordinal);
        Assert.Contains("Kwam op tijd", exported, StringComparison.Ordinal);
        Assert.Contains("Dit ben ik niet", exported, StringComparison.Ordinal);
        var hash = (await db.ReferenceConfirmationTokens.SingleAsync()).TokenHash;
        Assert.DoesNotContain(hash, exported, StringComparison.Ordinal);

        await privacy.DeleteOrAnonymizeAsync(Principal(user.Email));
        Assert.Empty(await db.ReferenceConfirmations.ToListAsync());
        Assert.Empty(await db.ReferenceConfirmationTokens.ToListAsync());
        Assert.Empty(await db.ReferenceMisuseReports.ToListAsync());
        Assert.Empty(await db.ReferenceConfirmationConsentLogs.ToListAsync());
    }

    private static async Task<string> IssueAndExpireAsync(
        JobsyDbContext db,
        ReferenceConfirmationService sut,
        Guid userId,
        Guid referenceId,
        CapturingMailer mail)
    {
        db.ReferenceConfirmationTokens.RemoveRange(await db.ReferenceConfirmationTokens.ToListAsync());
        db.ReferenceConfirmations.RemoveRange(await db.ReferenceConfirmations.ToListAsync());
        await db.SaveChangesAsync();
        Assert.True((await sut.RequestAsync(userId, referenceId, "vakkenvuller", true)).Ok);
        var token = TokenFrom(mail);
        var row = await db.ReferenceConfirmationTokens.OrderByDescending(t => t.CreatedAtUtc).FirstAsync();
        row.ExpiresAtUtc = DateTime.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();
        return token;
    }

    private static string TokenFrom(CapturingMailer mail)
    {
        var token = Regex.Match(mail.Sent[^1].Mail.Text, @"/referentie/([a-f0-9]{64})").Groups[1].Value;
        Assert.False(string.IsNullOrWhiteSpace(token));
        return token;
    }

    private static ReferenceConfirmationService CreateSut(JobsyDbContext db, CapturingMailer mail, CapturingPush push)
        => new(db, mail, new UserNotificationService(db), push, new Language(), new Features());

    private static async Task<(User User, CandidateReference Reference)> SeedAsync(JobsyDbContext db, string contact = "Jan")
    {
        var user = await UserAsync(db, "Sam");
        var reference = await ReferenceAsync(db, user.Id, "Bakkerij De Gouden Korrel", contact.ToLowerInvariant() + "@example.com", contact);
        return (user, reference);
    }

    private static async Task<User> UserAsync(JobsyDbContext db, string first)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = first.ToLowerInvariant() + "@example.com",
            FullName = first + " de Vries",
            FirstName = first,
            Role = UserRole.Candidate,
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task<CandidateReference> ReferenceAsync(
        JobsyDbContext db,
        Guid userId,
        string employer,
        string email,
        string contact = "Jan")
    {
        var reference = new CandidateReference
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EmployerName = employer,
            ContactName = contact,
            Email = email,
            Phone = "0612345678",
            SortOrder = 0,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.CandidateReferences.Add(reference);
        await db.SaveChangesAsync();
        return reference;
    }

    private static JobsyDbContext CreateDb()
        => new(new DbContextOptionsBuilder<JobsyDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static ClaimsPrincipal Principal(string email)
        => new(new ClaimsIdentity([new Claim(ClaimTypes.Email, email)], "test"));

    private sealed class CapturingMailer : ITransactionalMailer
    {
        public List<(ComposedEmail Mail, string To)> Sent { get; } = [];

        public Task<EmailSendOutcome> SendAsync(
            ComposedEmail mail,
            string to,
            EmailSendOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Sent.Add((mail, to));
            return Task.FromResult(new EmailSendOutcome(true, false, null, EmailDeliveryKind.Provider));
        }
    }

    private sealed class CapturingPush : IPushNotificationService
    {
        public List<PushMessage> Messages { get; } = [];

        public Task SendAsync(PushMessage message, CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class Language : IEmailLanguageResolver
    {
        public Task<EmailCulture> ResolveAsync(EmailRecipient recipient, CancellationToken cancellationToken = default)
            => Task.FromResult(EmailCulture.Nl);
    }

    private sealed class Features : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(false, false, "https://lobsy.nl", null));

        public Task<PlatformFeatureSnapshot> UpdateAsync(PlatformFeatureUpdate update, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class Lookup(JobsyDbContext db) : IUserLookupService
    {
        public async Task<User?> FindByPrincipalAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
        {
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
            return await db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        }
    }
}
