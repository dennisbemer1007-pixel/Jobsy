using Jobsy.Core.Email;
using Jobsy.Core.Email.Model;
using Jobsy.Core.Options;

namespace Jobsy.Tests;

public class EmailRendererTests
{
    private static EmailBrand Brand() => EmailBrand.From(new MailOptions { AssetVersion = "t" }, "https://lobsy.nl");

    private static EmailDocument Sample(
        EmailKind kind = EmailKind.Essential,
        EmailCta? cta = null,
        bool mascot = false,
        string preheader = "Preheader apart",
        string? culture = null,
        params EmailBlock[] blocks)
        => new(
            mascot ? "EmployerReactionAccepted" : "MailTest",
            kind,
            EmailCulture.ForLanguage(culture),
            "Onderwerp",
            preheader,
            "Kop",
            blocks.Length == 0 ? [new ParagraphBlock(EmailText.Plain("Body"))] : blocks,
            "Je ontvangt deze mail van Lobsy.",
            "Team Lobsy",
            Cta: cta,
            ShowMascot: mascot,
            Greeting: "Hoi Alex,");

    [Fact]
    public void Html_has_doctype_lang_dir_meta_and_one_style()
    {
        var html = EmailRenderer.Render(Sample(), Brand()).Html;
        Assert.StartsWith("<!DOCTYPE html>", html);
        Assert.Contains("lang=\"nl\"", html);
        Assert.Contains("dir=\"ltr\"", html);
        Assert.Contains("charset=\"utf-8\"", html);
        Assert.Contains("name=\"viewport\"", html);
        Assert.Contains("x-apple-disable-message-reformatting", html);
        Assert.Contains("format-detection", html);
        Assert.Contains("color-scheme", html);
        Assert.Contains("supported-color-schemes", html);
        Assert.Equal(1, System.Text.RegularExpressions.Regex.Matches(html, "<style>").Count);
        Assert.Contains("data-lobsy-layout=\"2\"", html);
        Assert.Contains("<!--[if mso]><table role=\"presentation\" width=\"600\"", html);
        Assert.Contains("&#8199;&#65279;&#847;", html);
    }

    [Fact]
    public void Cta_has_vml_twin_and_data_marker()
    {
        var html = EmailRenderer.Render(
            Sample(cta: new EmailCta("Ga verder", "https://lobsy.nl/login")), Brand()).Html;
        Assert.Contains("data-lobsy-cta", html);
        Assert.Contains("v:roundrect", html);
        Assert.Contains("href=\"https://lobsy.nl/login\"", html);
        Assert.Equal(1, System.Text.RegularExpressions.Regex.Matches(html, "data-lobsy-cta").Count);
    }

    [Fact]
    public void Mascot_only_when_allowed()
    {
        var with = EmailRenderer.Render(Sample(mascot: true), Brand()).Html;
        Assert.Contains("mascot-celebrating-128.png", with);
        var without = EmailRenderer.Render(Sample(mascot: false), Brand()).Html;
        Assert.DoesNotContain("mascot-celebrating-128.png", without);
    }

    [Fact]
    public void Footer_mail_settings_only_for_optional_and_legal_omits_empty()
    {
        var optional = EmailRenderer.Render(
            Sample(EmailKind.Optional, cta: new EmailCta("Open", "https://lobsy.nl/x")), Brand()).Html;
        Assert.Contains("/account/mail-instellingen", optional);
        Assert.Contains("Hulp", optional);
        Assert.Contains("/privacy", optional);
        Assert.Contains(">Lobsy<", optional); // legal name only when address/kvk empty — in footer text
        Assert.DoesNotContain("KvK", optional);

        var brand = EmailBrand.From(new MailOptions
        {
            LegalAddress = "Voorbeeldstraat 1",
            KvkNumber = "12345678",
            AssetVersion = "t"
        }, "https://lobsy.nl");
        var withLegal = EmailRenderer.Render(Sample(), brand).Html;
        Assert.Contains("Voorbeeldstraat 1", withLegal);
        Assert.Contains("KvK 12345678", withLegal);

        var essential = EmailRenderer.Render(Sample(), Brand()).Html;
        Assert.DoesNotContain("/account/mail-instellingen", essential);
    }

    [Fact]
    public void Escaping_and_rtl_and_text_part()
    {
        var doc = Sample(
            blocks: [new ParagraphBlock(EmailText.Plain("Hallo <script>alert(1)</script>")),
                new CodeBlock("123456", "Geldig 10 minuten.")],
            kind: EmailKind.Security,
            culture: "ar");
        // Security + code: no CTA
        var result = EmailRenderer.Render(doc, Brand());
        Assert.Contains("&lt;script&gt;", result.Html);
        Assert.Contains("dir=\"rtl\"", result.Html);
        Assert.Contains("text-align:right", result.Html);
        Assert.Contains("123456", result.Text);
        Assert.DoesNotContain("<script>", result.Html);
        Assert.DoesNotContain("<p", result.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<html", result.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Hallo <script>alert(1)</script>", result.Text);
    }

    [Fact]
    public void Sanity_rules_throw_in_testing()
    {
        var bad = Sample(preheader: "Onderwerp"); // equals subject
        Assert.Throws<InvalidOperationException>(() => EmailRenderer.Render(bad, Brand()));

        var securityWithCta = Sample(
            EmailKind.Security,
            cta: new EmailCta("X", "https://lobsy.nl"),
            blocks: [new CodeBlock("123456", "ok")]);
        Assert.Throws<InvalidOperationException>(() => EmailRenderer.Render(securityWithCta, Brand()));
    }

    [Fact]
    public void Text_contains_cta_url()
    {
        var result = EmailRenderer.Render(
            Sample(cta: new EmailCta("Open", "https://lobsy.nl/login")), Brand());
        Assert.Contains("Open: https://lobsy.nl/login", result.Text);
        Assert.DoesNotContain("<html", result.Text, StringComparison.OrdinalIgnoreCase);
    }
}
