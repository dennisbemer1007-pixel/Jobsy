using AngleSharp.Html.Parser;
using Jobsy.Core.Email;
using Jobsy.Core.Email.Localization;
using Jobsy.Core.Email.Model;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace Jobsy.Tests;

public class EmailRenderMatrixTests
{
    private readonly ITestOutputHelper _output;

    public EmailRenderMatrixTests(ITestOutputHelper output) => _output = output;

    public static IEnumerable<object[]> MatrixCases()
    {
        foreach (var def in EmailTemplateRegistry.All)
        {
            foreach (var lang in EmailStrings.Languages)
            {
                yield return new object[] { def.Key, lang };
            }
        }
    }

    [Theory]
    [MemberData(nameof(MatrixCases))]
    public async Task Every_key_language_passes_render_matrix(string key, string lang)
    {
        var ctx = EmailSampleContext.ForPreview("https://lobsy.nl");
        var culture = EmailCulture.ForLanguage(lang);
        var mail = TransactionalEmails.Compose(key, ctx, culture);
        var def = EmailTemplateRegistry.GetRequired(key);
        var parser = new HtmlParser();
        var doc = await parser.ParseDocumentAsync(mail.Html);

        var htmlEl = doc.DocumentElement;
        Assert.Equal(lang, htmlEl.GetAttribute("lang"));
        Assert.Equal(lang == "ar" ? "rtl" : "ltr", htmlEl.GetAttribute("dir"));

        Assert.Contains("charset", mail.Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("name=\"viewport\"", mail.Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("color-scheme", mail.Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("supported-color-schemes", mail.Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("format-detection", mail.Html, StringComparison.OrdinalIgnoreCase);

        Assert.Single(doc.QuerySelectorAll("h1"));

        var ctas = doc.QuerySelectorAll("[data-lobsy-cta]");
        if (def.Kind == EmailKind.Security)
        {
            Assert.Empty(ctas);
            Assert.Contains("data-lobsy-otp=", mail.Html, StringComparison.Ordinal);
        }
        else
        {
            Assert.Single(ctas);
            Assert.Contains("<!--[if mso]>", mail.Html, StringComparison.Ordinal);
            Assert.Contains("v:roundrect", mail.Html, StringComparison.Ordinal);
        }

        Assert.False(string.IsNullOrWhiteSpace(mail.Preheader));
        Assert.NotEqual(mail.Subject.Trim(), mail.Preheader.Trim());
        Assert.Contains("display:none", mail.Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("&#8199;&#65279;&#847;", mail.Html, StringComparison.Ordinal);

        var logos = doc.QuerySelectorAll("img[alt='Lobsy']");
        Assert.NotEmpty(logos);
        foreach (var logo in logos)
        {
            Assert.False(string.IsNullOrWhiteSpace(logo.GetAttribute("width")));
            Assert.False(string.IsNullOrWhiteSpace(logo.GetAttribute("height")));
        }

        var mascots = doc.QuerySelectorAll("img[src*='mascot']");
        if (def.GoodNews)
        {
            Assert.NotEmpty(mascots);
            Assert.All(mascots, m => Assert.Equal("", m.GetAttribute("alt") ?? ""));
        }
        else
        {
            Assert.Empty(mascots);
        }

        foreach (var a in doc.QuerySelectorAll("a[href]"))
        {
            var href = a.GetAttribute("href") ?? "";
            Assert.False(href.Contains("utm_", StringComparison.OrdinalIgnoreCase), href);
            Assert.False(href.Contains("redirect=", StringComparison.OrdinalIgnoreCase), href);
            Assert.True(
                href.StartsWith("https://lobsy.nl", StringComparison.OrdinalIgnoreCase)
                || href.StartsWith("https://api.voorbeeld.invalid", StringComparison.OrdinalIgnoreCase)
                || href.StartsWith("mailto:support@lobsy.nl", StringComparison.OrdinalIgnoreCase)
                || href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase),
                $"{key}/{lang} href={href}");
        }

        Assert.Empty(doc.QuerySelectorAll("script,form,iframe,link[rel=stylesheet]"));
        foreach (var img in doc.QuerySelectorAll("img"))
        {
            var w = img.GetAttribute("width");
            var h = img.GetAttribute("height");
            Assert.False(w == "1" && h == "1", "tracking pixel");
            var src = img.GetAttribute("src") ?? "";
            Assert.DoesNotContain("track", src, StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain("{", mail.Subject);
        Assert.DoesNotContain("}", mail.Subject);
        Assert.DoesNotContain("{", mail.Text);
        Assert.DoesNotContain("}", mail.Text);
        Assert.DoesNotContain("&amp;amp;", mail.Html, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("mailto:support@lobsy.nl", mail.Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/privacy", mail.Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Lobsy", mail.Html, StringComparison.Ordinal);
        if (def.Kind == EmailKind.Optional)
        {
            Assert.Contains("mail-instellingen", mail.Html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("data-lobsy-unsub", mail.Html, StringComparison.OrdinalIgnoreCase);
        }

        Assert.False(string.IsNullOrWhiteSpace(mail.Text));
        Assert.DoesNotContain("<html", mail.Text, StringComparison.OrdinalIgnoreCase);
        if (def.Kind == EmailKind.Security)
        {
            Assert.Contains(TransactionalEmails.SampleOtp, mail.Text, StringComparison.Ordinal);
        }
        else
        {
            Assert.Contains("https://", mail.Text, StringComparison.OrdinalIgnoreCase);
        }

        if (lang is "nl" or "en")
        {
            Assert.True(mail.Subject.Length <= 78, $"{key}/{lang} subject length {mail.Subject.Length}: {mail.Subject}");
            if (mail.Subject.Length > 60)
            {
                _output.WriteLine($"WARN subject>60 {key}/{lang}: {mail.Subject.Length}");
            }
        }
        else if (mail.Subject.Length > 60)
        {
            _output.WriteLine($"WARN subject>60 {key}/{lang}: {mail.Subject.Length}");
        }

        await AssertMailerHeadersAsync(mail, def);

        // Legal footer: default brand shows name only; configured address+KvK appear when set.
        var bare = EmailBrand.ForBaseUrl("https://lobsy.nl");
        Assert.Equal("Lobsy", bare.LegalLine);
        var full = EmailBrand.From(
            new MailOptions { LegalAddress = "Markt 1, Delft", KvkNumber = "12345678" },
            "https://lobsy.nl");
        Assert.Contains("Markt 1, Delft", full.LegalLine, StringComparison.Ordinal);
        Assert.Contains("KvK 12345678", full.LegalLine, StringComparison.Ordinal);

        _output.WriteLine($"PASS {key} × {lang}");
    }

    private static async Task AssertMailerHeadersAsync(ComposedEmail mail, EmailTemplateDefinition def)
    {
        await using var db = new JobsyDbContext(
            new DbContextOptionsBuilder<JobsyDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var email = new RecordingEmail();
        var tokens = new MailUnsubscribeTokenService(
            DataProtectionProvider.Create(Path.Combine(Path.GetTempPath(), "jobsy-matrix-" + Guid.NewGuid().ToString("N"))));
        var mailer = new TransactionalMailer(
            email,
            new AlwaysOnFlags(),
            new Features(),
            new EmailPreferenceService(db),
            tokens,
            Options.Create(new MailOptions()),
            db,
            new Env("Testing"),
            NullLogger<TransactionalMailer>.Instance);

        var outcome = await mailer.SendAsync(mail, "matrix@voorbeeld.invalid");
        Assert.True(outcome.Sent);
        var sent = Assert.Single(email.Sent);
        Assert.Equal("support@lobsy.nl", sent.ReplyTo);
        if (def.Kind == EmailKind.Optional)
        {
            Assert.Contains("List-Unsubscribe", sent.Headers!.Keys);
            Assert.Contains("List-Unsubscribe-Post", sent.Headers!.Keys);
        }
        else
        {
            Assert.DoesNotContain(sent.Headers!.Keys, k => k.Contains("List-Unsubscribe", StringComparison.OrdinalIgnoreCase));
        }

        Assert.DoesNotContain(sent.Headers!.Keys, k => k.Contains("track", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class RecordingEmail : IEmailService
    {
        public List<EmailMessage> Sent { get; } = [];

        public Task<EmailDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            Sent.Add(message);
            return Task.FromResult(EmailDeliveryResult.Stub);
        }
    }

    private sealed class AlwaysOnFlags : Jobsy.Core.Features.IFeatureFlags
    {
        public ValueTask<Jobsy.Core.Features.FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new Jobsy.Core.Features.FeatureFlagSnapshot(true, false));

        public ValueTask<bool> IsEnabledAsync(Jobsy.Core.Features.PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(true);

        public void Invalidate() { }
    }

    private sealed class Features : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(true, true, "https://lobsy.nl", DateTime.UtcNow,
                AmbassadorsEnabled: true));

        public Task<PlatformFeatureSnapshot> UpdateAsync(PlatformFeatureUpdate update, CancellationToken cancellationToken = default)
            => GetAsync(cancellationToken);
    }

    private sealed class Env(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
