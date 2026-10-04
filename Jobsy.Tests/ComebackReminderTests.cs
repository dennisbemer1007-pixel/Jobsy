using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Jobsy.Core.Email;
using Jobsy.Core.Email.Model;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Reminders;
using Jobsy.Core.Time;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Reminders;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class ComebackReminderTests
{
    private static readonly DateTime InsideWindow = new(2026, 10, 5, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Quiet_hours_follow_amsterdam()
    {
        Assert.True(AmsterdamClock.IsSendWindow(new DateTime(2026, 10, 5, 7, 0, 0, DateTimeKind.Utc)));
        Assert.True(AmsterdamClock.IsSendWindow(InsideWindow));
        Assert.False(AmsterdamClock.IsSendWindow(new DateTime(2026, 10, 5, 6, 30, 0, DateTimeKind.Utc)));
        Assert.False(AmsterdamClock.IsSendWindow(new DateTime(2026, 10, 5, 18, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public async Task Basic_tests_reminder_sends_once_after_seven_days_when_opted_in()
    {
        await using var db = CreateDb();
        var user = await SeedCandidate(db, InsideWindow.AddDays(-8), emailOptIn: true);
        var mail = new RecordingMailer();
        var sut = CreateSut(db, mail, whatsApp: false);

        var first = await sut.RunAsync(InsideWindow);
        Assert.False(first.Deferred);
        Assert.Equal(1, first.Sent);
        Assert.Single(mail.Sent);
        Assert.Equal(EmailOptionalCategories.ComebackReminder, mail.Sent[0].Mail.Key);
        Assert.Contains("Je 4 korte tests staan nog open", mail.Sent[0].Mail.Subject, StringComparison.Ordinal);
        Assert.Contains("/profiel/tests", mail.Sent[0].Mail.Html, StringComparison.Ordinal);

        var second = await sut.RunAsync(InsideWindow.AddHours(2));
        Assert.Equal(0, second.Sent);
        Assert.Single(mail.Sent);
        Assert.Equal(1, await db.ComebackReminderLogs.CountAsync());
    }

    [Fact]
    public async Task Does_not_send_without_opt_in_or_before_seven_days_or_when_tests_are_done()
    {
        await using var db = CreateDb();
        await SeedCandidate(db, InsideWindow.AddDays(-8), emailOptIn: false);
        var mail = new RecordingMailer();
        var sut = CreateSut(db, mail, whatsApp: false);
        Assert.Equal(0, (await sut.RunAsync(InsideWindow)).Sent);

        await using var early = CreateDb();
        await SeedCandidate(early, InsideWindow.AddDays(-6), emailOptIn: true);
        var earlyMail = new RecordingMailer();
        Assert.Equal(0, (await CreateSut(early, earlyMail, whatsApp: false).RunAsync(InsideWindow)).Sent);

        await using var done = CreateDb();
        var finished = await SeedCandidate(done, InsideWindow.AddDays(-8), emailOptIn: true);
        await MarkTestsDone(done, finished.Id);
        var doneMail = new RecordingMailer();
        Assert.Equal(0, (await CreateSut(done, doneMail, whatsApp: false).RunAsync(InsideWindow)).Sent);
        Assert.Empty(doneMail.Sent);
    }

    [Fact]
    public async Task Outside_quiet_hours_defers()
    {
        await using var db = CreateDb();
        await SeedCandidate(db, InsideWindow.AddDays(-8), emailOptIn: true);
        var mail = new RecordingMailer();
        var result = await CreateSut(db, mail, whatsApp: false)
            .RunAsync(new DateTime(2026, 10, 5, 6, 30, 0, DateTimeKind.Utc));
        Assert.True(result.Deferred);
        Assert.Equal(0, result.Sent);
        Assert.Empty(mail.Sent);
    }

    [Fact]
    public async Task Look_again_after_four_weeks_and_not_again_until_they_return()
    {
        await using var db = CreateDb();
        var user = await SeedCandidate(db, InsideWindow.AddDays(-40), emailOptIn: true);
        await MarkTestsDone(db, user.Id);
        user.LastLoginAtUtc = InsideWindow.AddDays(-30);
        await db.SaveChangesAsync();
        var mail = new RecordingMailer();
        var sut = CreateSut(db, mail, whatsApp: false);

        Assert.Equal(1, (await sut.RunAsync(InsideWindow)).Sent);
        Assert.Contains("Kijk nog eens", mail.Sent[0].Mail.Subject, StringComparison.Ordinal);
        Assert.Equal(0, (await sut.RunAsync(InsideWindow.AddDays(1))).Sent);

        user.LastLoginAtUtc = InsideWindow.AddDays(2);
        await db.SaveChangesAsync();
        Assert.Equal(0, (await sut.RunAsync(InsideWindow.AddDays(10))).Sent);
        Assert.Equal(1, (await sut.RunAsync(InsideWindow.AddDays(40))).Sent);
    }

    [Fact]
    public async Task Monthly_cap_is_two()
    {
        await using var db = CreateDb();
        var user = await SeedCandidate(db, InsideWindow.AddDays(-40), emailOptIn: true);
        db.ComebackReminderLogs.AddRange(
            Log(user.Id, InsideWindow.AddDays(-3)),
            Log(user.Id, InsideWindow.AddDays(-1)));
        await db.SaveChangesAsync();
        var mail = new RecordingMailer();
        var result = await CreateSut(db, mail, whatsApp: false).RunAsync(InsideWindow);
        Assert.Equal(0, result.Sent);
        Assert.Empty(mail.Sent);
        Assert.Equal(2, await db.ComebackReminderLogs.CountAsync());
    }

    [Fact]
    public async Task Opt_out_stops_the_mail_even_after_an_earlier_opt_in()
    {
        await using var db = CreateDb();
        var user = await SeedCandidate(db, InsideWindow.AddDays(-8), emailOptIn: true);
        var prefs = new EmailPreferenceService(db);
        await prefs.OptOutAsync(user.Email, EmailOptionalCategories.ComebackReminder, "OneClick");
        var mail = new RecordingMailer();
        Assert.Equal(0, (await CreateSut(db, mail, whatsApp: false).RunAsync(InsideWindow)).Sent);
        Assert.Empty(mail.Sent);
    }

    [Fact]
    public async Task Settings_opt_in_is_off_until_the_candidate_turns_it_on()
    {
        await using var db = CreateDb();
        var user = await SeedCandidate(db, InsideWindow, emailOptIn: false);
        var prefs = new EmailPreferenceService(db);
        var before = await prefs.GetForUserAsync(user.Id);
        var item = Assert.Single(before, i => i.Key == EmailOptionalCategories.ComebackReminder);
        Assert.False(item.Enabled);

        await prefs.OptInAsync(user.Email, EmailOptionalCategories.ComebackReminder);
        var after = await prefs.GetForUserAsync(user.Id);
        Assert.True(after.Single(i => i.Key == EmailOptionalCategories.ComebackReminder).Enabled);
    }

    [Fact]
    public async Task Mail_goes_through_the_transactional_mailer_with_an_unsubscribe_link()
    {
        await using var db = CreateDb();
        var email = new CapturingEmail();
        var mailer = new TransactionalMailer(
            email,
            new Flags(false),
            new Features("https://lobsy.nl"),
            new EmailPreferenceService(db),
            new FakeUnsubscribe(),
            Options.Create(new MailOptions()),
            db,
            new Env("Testing"),
            NullLogger<TransactionalMailer>.Instance);
        var mail = TransactionalEmails.ComebackReminder(
            "https://lobsy.nl", "Sanne", ComebackReminderKinds.BasicTests, EmailCulture.Nl);
        var outcome = await mailer.SendAsync(mail, "sanne@example.com");
        Assert.True(outcome.Sent);
        var headers = email.Sent[0].Headers;
        Assert.NotNull(headers);
        Assert.Contains("List-Unsubscribe", headers!.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("/mail/afmelden", email.Sent[0].BodyHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WhatsApp_sends_only_when_flag_config_and_opt_in_are_present()
    {
        var handler = new RecordingHandler();
        var configured = Options.Create(new WhatsAppReminderOptions { PhoneNumberId = "12345", AccessToken = "secret-token" });
        var flaggedOff = new WhatsAppReminderChannel(
            new HandlerFactory(handler),
            configured,
            new Flags(false),
            NullLogger<WhatsAppReminderChannel>.Instance);
        var off = await flaggedOff.SendAsync(Dispatch(sendWhatsApp: true, phone: "0612345678"));
        Assert.False(off.Sent);
        Assert.Equal(0, handler.Calls);

        var channel = new WhatsAppReminderChannel(
            new HandlerFactory(handler),
            configured,
            new Flags(true),
            NullLogger<WhatsAppReminderChannel>.Instance);
        var notOptedIn = await channel.SendAsync(Dispatch(sendWhatsApp: false, phone: "0612345678"));
        Assert.False(notOptedIn.Sent);
        Assert.Equal(0, handler.Calls);

        var missing = new WhatsAppReminderChannel(
            new HandlerFactory(handler),
            Options.Create(new WhatsAppReminderOptions()),
            new Flags(true),
            NullLogger<WhatsAppReminderChannel>.Instance);
        Assert.False((await missing.SendAsync(Dispatch(sendWhatsApp: true, phone: "0612345678"))).Sent);
        Assert.Equal(0, handler.Calls);

        var sent = await channel.SendAsync(Dispatch(sendWhatsApp: true, phone: "06 12345678"));
        Assert.True(sent.Sent);
        Assert.Equal(1, handler.Calls);
        Assert.Contains("/12345/messages", handler.LastUri, StringComparison.Ordinal);
        Assert.Equal("Bearer", handler.LastAuth?.Scheme);
        Assert.Equal("secret-token", handler.LastAuth?.Parameter);
        Assert.Contains("31612345678", handler.LastBody, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-token", handler.LastBody, StringComparison.Ordinal);

        await using var db = CreateDb();
        var user = await SeedCandidate(db, InsideWindow.AddDays(-8), emailOptIn: false);
        db.CandidateReminderPreferences.Add(new CandidateReminderPreference
        {
            UserId = user.Id,
            WhatsAppOptedInAtUtc = InsideWindow.AddDays(-1),
            WhatsAppPhone = "0612345678",
            UpdatedAtUtc = InsideWindow
        });
        await db.SaveChangesAsync();
        var mail = new RecordingMailer();
        var hidden = CreateSut(db, mail, whatsApp: false, handler: handler);
        Assert.False((await hidden.GetWhatsAppAsync(user.Id)).Available);
        Assert.Equal(0, (await hidden.RunAsync(InsideWindow)).Sent);
        Assert.Equal(1, handler.Calls);

        var visible = CreateSut(db, mail, whatsApp: true, handler: handler);
        var view = await visible.GetWhatsAppAsync(user.Id);
        Assert.True(view.Available);
        Assert.True(view.OptedIn);
        Assert.Equal(1, (await visible.RunAsync(InsideWindow)).Sent);
        Assert.Equal(2, handler.Calls);
        Assert.Contains("whatsapp", (await db.ComebackReminderLogs.SingleAsync()).Channels, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Admin_stats_are_counts_only()
    {
        await using var db = CreateDb();
        var back = await SeedCandidate(db, InsideWindow.AddDays(-20), emailOptIn: true);
        var away = await SeedCandidate(db, InsideWindow.AddDays(-20), emailOptIn: true, email: "away@example.com");
        back.LastLoginAtUtc = InsideWindow.AddDays(-10).AddDays(3);
        db.ComebackReminderLogs.Add(Log(back.Id, InsideWindow.AddDays(-10)));
        db.ComebackReminderLogs.Add(Log(away.Id, InsideWindow.AddDays(-10)));
        await db.SaveChangesAsync();

        var stats = await CreateSut(db, new RecordingMailer(), whatsApp: false).GetAnonymousStatsAsync();
        Assert.Equal(2, stats.Sent);
        Assert.Equal(1, stats.ReturnedWithin7Days);
        Assert.Equal(1, stats.ReturnedWithin30Days);
        var json = JsonSerializer.Serialize(stats);
        Assert.DoesNotContain("@", json, StringComparison.Ordinal);
        Assert.DoesNotContain("example.com", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Account_export_includes_the_reminder_and_deletion_drops_the_phone()
    {
        await using var db = CreateDb();
        var user = await SeedCandidate(db, InsideWindow.AddDays(-8), emailOptIn: true);
        db.CandidateReminderPreferences.Single().WhatsAppPhone = "0612345678";
        db.CandidateReminderPreferences.Single().WhatsAppOptedInAtUtc = InsideWindow;
        db.ComebackReminderLogs.Add(Log(user.Id, InsideWindow.AddDays(-1)));
        await db.SaveChangesAsync();

        var privacy = new PrivacyDataService(db, new Lookup(db), new RecordingMailer(), new Features("https://lobsy.nl"));
        var json = JsonSerializer.Serialize(await privacy.ExportAsync(Principal(user)));
        Assert.Contains("0612345678", json, StringComparison.Ordinal);
        Assert.Contains("ComebackReminders", json, StringComparison.OrdinalIgnoreCase);

        await privacy.DeleteOrAnonymizeAsync(Principal(user));
        Assert.False(await db.CandidateReminderPreferences.AnyAsync());
        var log = await db.ComebackReminderLogs.SingleAsync();
        Assert.Null(log.UserId);
    }

    private static ComebackReminderService CreateSut(
        JobsyDbContext db,
        RecordingMailer mail,
        bool whatsApp,
        RecordingHandler? handler = null)
    {
        handler ??= new RecordingHandler();
        IReminderChannel[] channels =
        [
            new EmailReminderChannel(mail),
            new WhatsAppReminderChannel(
                new HandlerFactory(handler),
                Options.Create(whatsApp
                    ? new WhatsAppReminderOptions { PhoneNumberId = "12345", AccessToken = "secret-token" }
                    : new WhatsAppReminderOptions()),
                new Flags(whatsApp),
                NullLogger<WhatsAppReminderChannel>.Instance)
        ];
        return new ComebackReminderService(
            db,
            channels,
            new EmailPreferenceService(db),
            new EmailLanguageResolver(db),
            new Features("https://acceptatie.lobsy.nl"),
            new Flags(whatsApp),
            Options.Create(whatsApp
                ? new WhatsAppReminderOptions { PhoneNumberId = "12345", AccessToken = "secret-token" }
                : new WhatsAppReminderOptions()),
            NullLogger<ComebackReminderService>.Instance);
    }

    private static async Task<User> SeedCandidate(
        JobsyDbContext db,
        DateTime signedUpUtc,
        bool emailOptIn,
        string email = "sanne@example.com")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FullName = "Sanne Tester",
            Role = UserRole.Candidate,
            IsActive = true,
            TermsAcceptedAt = signedUpUtc,
            EmailVerifiedAtUtc = signedUpUtc
        };
        db.Users.Add(user);
        if (emailOptIn)
        {
            db.CandidateReminderPreferences.Add(new CandidateReminderPreference
            {
                UserId = user.Id,
                EmailOptedInAtUtc = signedUpUtc,
                UpdatedAtUtc = signedUpUtc
            });
        }

        await db.SaveChangesAsync();
        return user;
    }

    private static async Task MarkTestsDone(JobsyDbContext db, Guid userId)
    {
        var now = InsideWindow.AddDays(-1);
        db.CandidateCompetencies.Add(new CandidateCompetency { Id = Guid.NewGuid(), UserId = userId, CompletedAtUtc = now });
        db.CandidateCareerInterests.Add(new CandidateCareerInterest { Id = Guid.NewGuid(), UserId = userId, CompletedAtUtc = now });
        db.CandidateCulturePersonalityProfiles.Add(new CandidateCulturePersonalityProfile { Id = Guid.NewGuid(), UserId = userId, CompletedAtUtc = now });
        db.CandidateValuesProfiles.Add(new CandidateValuesProfile { Id = Guid.NewGuid(), UserId = userId, CompletedAtUtc = now });
        await db.SaveChangesAsync();
    }

    private static ComebackReminderLog Log(Guid userId, DateTime sent)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Kind = ComebackReminderKinds.LookAgain,
            SentAtUtc = sent,
            Channels = "email"
        };

    private static ReminderDispatch Dispatch(bool sendWhatsApp, string? phone)
        => new(
            Guid.NewGuid(),
            "sanne@example.com",
            "Sanne",
            ComebackReminderKinds.BasicTests,
            EmailCulture.Nl,
            "https://lobsy.nl",
            InsideWindow,
            SendEmail: false,
            SendPush: false,
            SendWhatsApp: sendWhatsApp,
            WhatsAppPhone: phone);

    private static System.Security.Claims.ClaimsPrincipal Principal(User user)
    {
        var identity = new System.Security.Claims.ClaimsIdentity(
        [
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, user.Email)
        ], "test");
        return new System.Security.Claims.ClaimsPrincipal(identity);
    }

    private static JobsyDbContext CreateDb()
        => new(new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed class RecordingMailer : ITransactionalMailer
    {
        public List<(ComposedEmail Mail, string To)> Sent { get; } = [];

        public Task<EmailSendOutcome> SendAsync(
            ComposedEmail mail,
            string to,
            EmailSendOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Sent.Add((mail, to));
            return Task.FromResult(new EmailSendOutcome(true, false, null, EmailDeliveryKind.Stub));
        }
    }

    private sealed class CapturingEmail : IEmailService
    {
        public List<EmailMessage> Sent { get; } = [];

        public Task<EmailDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            Sent.Add(message);
            return Task.FromResult(EmailDeliveryResult.Stub);
        }
    }

    private sealed class Flags(bool whatsApp) : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(false, true, WhatsAppRemindersEnabled: whatsApp));

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(feature == PlatformFeature.WhatsAppReminders && whatsApp);

        public void Invalidate()
        {
        }
    }

    private sealed class Features(string url) : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(true, false, url, DateTime.UtcNow));

        public Task<PlatformFeatureSnapshot> UpdateAsync(PlatformFeatureUpdate update, CancellationToken cancellationToken = default)
            => GetAsync(cancellationToken);
    }

    private sealed class FakeUnsubscribe : IMailUnsubscribeTokenService
    {
        public string CreateToken(string email, string category, DateTime? issuedUtc = null) => "tok";

        public bool TryValidate(string? token, out string emailHash, out string category, out DateTime issuedUtc)
        {
            emailHash = "h";
            category = EmailOptionalCategories.ComebackReminder;
            issuedUtc = DateTime.UtcNow;
            return true;
        }

        public string BuildUnsubscribeUrl(string publicWebBaseUrl, string email, string category)
            => publicWebBaseUrl.TrimEnd('/') + "/mail/afmelden?t=tok";
    }

    private sealed class Env(string name) : Microsoft.Extensions.Hosting.IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    private sealed class Lookup(JobsyDbContext db) : IUserLookupService
    {
        public Task<User?> FindByPrincipalAsync(System.Security.Claims.ClaimsPrincipal principal, CancellationToken cancellationToken = default)
        {
            var id = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(id, out var userId)
                ? db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                : Task.FromResult<User?>(null);
        }
    }

    private sealed class HandlerFactory(RecordingHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(handler, disposeHandler: false) { Timeout = TimeSpan.FromSeconds(5) };
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public string? LastUri { get; private set; }
        public string? LastBody { get; private set; }
        public AuthenticationHeaderValue? LastAuth { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            LastUri = request.RequestUri?.ToString();
            LastAuth = request.Headers.Authorization;
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"messages\":[{\"id\":\"wamid.test\"}]}")
            };
        }
    }
}
