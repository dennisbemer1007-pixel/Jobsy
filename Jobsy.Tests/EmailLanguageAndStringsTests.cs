using Jobsy.Core.Email;
using Jobsy.Core.Email.Localization;
using Jobsy.Core.Email.Model;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Tests;

public class EmailLanguageResolverTests
{
    [Fact]
    public async Task User_preference_wins()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "u@example.com",
            PreferencesJson = """{"language":"ro"}"""
        });
        await db.SaveChangesAsync();

        var resolver = new EmailLanguageResolver(db);
        var culture = await resolver.ResolveAsync(new EmailRecipient.User(userId));
        Assert.Equal("ro", culture.Language);
    }

    [Fact]
    public async Task Requester_header_language()
    {
        await using var db = CreateDb();
        var resolver = new EmailLanguageResolver(db);
        var culture = await resolver.ResolveAsync(new EmailRecipient.Requester("pl"));
        Assert.Equal("pl", culture.Language);
    }

    [Fact]
    public async Task ParentOf_uses_child_language()
    {
        await using var db = CreateDb();
        var childId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = childId,
            Email = "child@example.com",
            PreferencesJson = """{"language":"ar"}"""
        });
        await db.SaveChangesAsync();

        var resolver = new EmailLanguageResolver(db);
        var culture = await resolver.ResolveAsync(new EmailRecipient.ParentOf(childId));
        Assert.Equal("ar", culture.Language);
        Assert.True(culture.IsRightToLeft);
    }

    [Fact]
    public async Task Address_lookup_and_unsupported_to_nl()
    {
        await using var db = CreateDb();
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "boss@example.com",
            PreferencesJson = """{"language":"en"}"""
        });
        await db.SaveChangesAsync();

        var resolver = new EmailLanguageResolver(db);
        Assert.Equal("en", (await resolver.ResolveAsync(new EmailRecipient.Address("boss@example.com"))).Language);
        Assert.Equal("nl", (await resolver.ResolveAsync(new EmailRecipient.Address("nobody@example.com"))).Language);
        Assert.Equal("nl", (await resolver.ResolveAsync(new EmailRecipient.Requester("xx"))).Language);
    }

    [Fact]
    public async Task Employer_recipients_resolve_own_language_not_candidate()
    {
        await using var db = CreateDb();
        var candidateId = Guid.NewGuid();
        var employerId = Guid.NewGuid();
        db.Users.AddRange(
            new User { Id = candidateId, Email = "cand@example.com", PreferencesJson = """{"language":"pl"}""" },
            new User { Id = employerId, Email = "hr@example.com", PreferencesJson = """{"language":"en"}""" });
        await db.SaveChangesAsync();

        var resolver = new EmailLanguageResolver(db);
        var employerCulture = await resolver.ResolveAsync(new EmailRecipient.User(employerId));
        Assert.Equal("en", employerCulture.Language);
        var candidateCulture = await resolver.ResolveAsync(new EmailRecipient.User(candidateId));
        Assert.Equal("pl", candidateCulture.Language);
        Assert.NotEqual(employerCulture.Language, candidateCulture.Language);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }
}

public class EmailFormatTests
{
    [Fact]
    public void Amsterdam_summer_utc_2230_shows_next_day_local()
    {
        // 30 Sep 2026 22:30 UTC = 1 Oct 2026 00:30 Europe/Amsterdam (CEST, UTC+2)
        var utc = new DateTime(2026, 9, 30, 22, 30, 0, DateTimeKind.Utc);
        var text = EmailFormat.DateTimeWithoutZone(utc, EmailCulture.Nl);
        Assert.Contains("1 oktober 2026", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("00:30", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Arabic_uses_gregorian_and_latin_digits()
    {
        var utc = new DateTime(2026, 9, 30, 10, 0, 0, DateTimeKind.Utc);
        var text = EmailFormat.Date(utc, EmailCulture.Ar);
        Assert.Contains("30", text, StringComparison.Ordinal);
        Assert.Contains("2026", text, StringComparison.Ordinal);
        Assert.Contains("سبتمبر", text, StringComparison.Ordinal);
        Assert.DoesNotContain("هـ", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Money_and_km_per_culture()
    {
        Assert.Equal("€ 12,50", EmailFormat.Money(12.50m, EmailCulture.Nl));
        Assert.Contains("12.50", EmailFormat.Money(12.50m, EmailCulture.En), StringComparison.Ordinal);
        Assert.Equal("3,4 km", EmailFormat.Km(3.4, EmailCulture.Nl));
        Assert.Equal("3.4 km", EmailFormat.Km(3.4, EmailCulture.En));
    }
}

public class EmailRendererRtlTests
{
    [Fact]
    public void Arabic_render_has_rtl_bdi_and_ltr_code()
    {
        var mail = TransactionalEmails.ApplicationConfirmation(
            "https://lobsy.nl", "Alex", "Weekendhulp", "Bakkerij De Gouden Korrel", false, EmailCulture.Ar);
        Assert.Contains("dir=\"rtl\"", mail.Html, StringComparison.Ordinal);
        Assert.Contains("lang=\"ar\"", mail.Html, StringComparison.Ordinal);
        Assert.Contains("<bdi>", mail.Html, StringComparison.Ordinal);
        Assert.Contains(EmailBidi.FirstStrongIsolate, mail.Text);
        Assert.Contains(EmailBidi.PopDirectionalIsolate, mail.Text);

        var codeMail = TransactionalEmails.ApplicationVerificationCode(
            "https://lobsy.nl", "Alex", "Weekendhulp", Guid.NewGuid(), "123456", EmailCulture.Ar);
        Assert.Contains("data-lobsy-otp=\"123456\"", codeMail.Html, StringComparison.Ordinal);
        Assert.Contains("dir=\"ltr\"", codeMail.Html, StringComparison.Ordinal);
    }

    [Fact]
    public void Non_arabic_text_has_no_bidi_isolates()
    {
        var mail = TransactionalEmails.ApplicationConfirmation(
            "https://lobsy.nl", "Alex", "Weekendhulp", "Bakkerij", false, EmailCulture.En);
        Assert.DoesNotContain(EmailBidi.FirstStrongIsolate, mail.Text);
        Assert.Contains("dir=\"ltr\"", mail.Html, StringComparison.Ordinal);
        Assert.Contains("<bdi>", mail.Html, StringComparison.Ordinal); // user data still isolated in HTML
    }
}

public class EmailStringsParityTests
{
    private static readonly HashSet<string> AllowIdenticalToNl = new(StringComparer.Ordinal)
    {
        "Email.Common.Privacy",
        "Email.Common.SignOff",
        "Email.Common.Km",
        "Email.CompanyApiKeyCredentials.Fact.Endpoint",
        "Email.CompanyApiKeyCredentials.Fact.Swagger",
        "Email.CompanyReEngagement.Fact.Csv",
        "Email.SalesManagerInvite.NoteLink",
        "Email.AmbassadeurInvite.NoteLink",
        "Email.RegistrationActivation.SbiBit",
        "Email.SupportAccessRequested.ExpiresVal",
        "Email.CompanyApiKeyCredentials.Fact.Header"
    };

    [Fact]
    public void Every_key_exists_in_all_five_languages_with_matching_placeholders_and_no_html()
    {
        var nlKeys = EmailStrings.AllKeys().OrderBy(k => k, StringComparer.Ordinal).ToList();
        Assert.NotEmpty(nlKeys);
        foreach (var lang in EmailStrings.Languages)
        {
            var map = EmailStrings.All[lang];
            Assert.Equal(nlKeys.Count, map.Count);
            foreach (var key in nlKeys)
            {
                Assert.True(map.ContainsKey(key), $"Missing {key} in {lang}");
                var value = map[key];
                Assert.DoesNotContain('<', value);
                Assert.DoesNotContain('>', value);
                Assert.Equal(
                    EmailStrings.PlaceholderIndexes(EmailStrings.All["nl"][key]),
                    EmailStrings.PlaceholderIndexes(value));
            }
        }
    }

    [Fact]
    public void Untranslated_baseline_must_not_grow()
    {
        var baselinePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "docs", "i18n", "email-untranslated-baseline.txt");
        if (!File.Exists(baselinePath))
        {
            baselinePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "docs", "i18n", "email-untranslated-baseline.txt"));
        }

        var baseline = File.Exists(baselinePath)
            ? File.ReadAllLines(baselinePath).Where(l => !string.IsNullOrWhiteSpace(l)).ToHashSet(StringComparer.Ordinal)
            : new HashSet<string>(StringComparer.Ordinal);

        var current = new List<string>();
        foreach (var key in EmailStrings.AllKeys())
        {
            if (AllowIdenticalToNl.Contains(key))
            {
                continue;
            }

            var nl = EmailStrings.All["nl"][key];
            foreach (var lang in new[] { "en", "pl", "ro", "ar" })
            {
                if (string.Equals(EmailStrings.All[lang][key], nl, StringComparison.Ordinal))
                {
                    current.Add($"{lang}\t{key}");
                }
            }
        }

        var unexpected = current.Where(x => !baseline.Contains(x)).OrderBy(x => x).ToList();
        Assert.True(unexpected.Count == 0,
            "email-untranslated-baseline grew:\n" + string.Join('\n', unexpected));
    }

    [Fact]
    public void Compose_all_registry_keys_in_five_languages_with_zero_nl_fallbacks()
    {
        var ctx = EmailSampleContext.ForPreview("https://lobsy.nl");
        foreach (var lang in EmailStrings.Languages)
        {
            var culture = EmailCulture.ForLanguage(lang);
            EmailStrings.ResetFallbackHits();
            foreach (var def in EmailTemplateRegistry.All)
            {
                var mail = TransactionalEmails.Compose(def.Key, ctx, culture);
                Assert.Equal(lang, mail.Language);
                Assert.False(string.IsNullOrWhiteSpace(mail.Html));
                Assert.False(string.IsNullOrWhiteSpace(mail.Text));
            }

            Assert.Equal(0, EmailStrings.FallbackHitCount);
        }
    }

    [Fact]
    public void No_04_markers_remain_in_Jobsy_Core_Email()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Jobsy.Core", "Email"));
        if (!Directory.Exists(root))
        {
            root = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "Jobsy.Core", "Email"));
        }

        var hits = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .SelectMany(f => File.ReadAllLines(f).Select((line, i) => (f, i: i + 1, line)))
            .Where(x => x.line.Contains("// 04", StringComparison.Ordinal))
            .Select(x => $"{x.f}:{x.i}:{x.line.Trim()}")
            .ToList();
        Assert.True(hits.Count == 0, string.Join('\n', hits));
    }

    [Fact]
    public void Subject_length_nl_en_reasonable()
    {
        var ctx = EmailSampleContext.ForPreview("https://lobsy.nl");
        foreach (var lang in new[] { "nl", "en" })
        {
            var mail = TransactionalEmails.Compose("ApplicationConfirmation", ctx, EmailCulture.ForLanguage(lang));
            Assert.True(mail.Subject.Length <= 70, $"{lang} subject too long: {mail.Subject.Length}");
        }
    }
}

public class EmailLanguageComposeIntegrationTests
{
    [Fact]
    public void Anonymous_requester_pl_and_user_ro()
    {
        var ctx = EmailSampleContext.ForPreview("https://lobsy.nl");
        var pl = TransactionalEmails.Compose("ApplicationVerificationCode", ctx, EmailCulture.Pl);
        Assert.Equal("pl", pl.Language);
        Assert.Contains("kod", pl.Subject, StringComparison.OrdinalIgnoreCase);

        var ro = TransactionalEmails.Compose("ApplicationConfirmation", ctx, EmailCulture.Ro);
        Assert.Equal("ro", ro.Language);
        Assert.Contains("Candidatur", ro.Subject, StringComparison.OrdinalIgnoreCase);
    }
}
