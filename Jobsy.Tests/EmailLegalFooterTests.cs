using Jobsy.Core.Email;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

/// <summary>
/// Dependency B (emails stack) is present: footer comes from MailOptions filled by ILegalIdentity,
/// not the <!--lobsy:legal-footer--> marker path.
/// </summary>
public class EmailLegalFooterTests
{
    [Fact]
    public void PostConfigure_fills_mail_options_from_identity_and_escapes_in_brand_line()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILegalIdentity>(new StubLegalIdentity(new LegalIdentitySnapshot(
            Name: "Acme <b>BV</b>",
            TradeName: "Lobsy",
            Street: "Straat 1",
            PostalCode: "1234 AB",
            City: "Delft",
            Country: "Nederland",
            KvkNumber: "87654321",
            VatNumber: null,
            PrivacyEmail: null,
            SupportEmail: "support@lobsy.nl",
            SchoolsEmail: null)));
        services.AddSingleton<IPostConfigureOptions<MailOptions>, MailOptionsLegalIdentityPostConfigure>();
        var sp = services.BuildServiceProvider();

        var options = new MailOptions
        {
            LegalName = MailOptions.DefaultLegalName,
            LegalAddress = null,
            KvkNumber = null
        };
        sp.GetRequiredService<IPostConfigureOptions<MailOptions>>().PostConfigure(Options.DefaultName, options);

        Assert.Equal("Acme <b>BV</b>", options.LegalName);
        Assert.Equal("Straat 1, 1234 AB Delft", options.LegalAddress);
        Assert.Equal("87654321", options.KvkNumber);

        var brand = EmailBrand.From(options, "https://lobsy.nl");
        Assert.Contains("Acme <b>BV</b>", brand.LegalLine, StringComparison.Ordinal);
        Assert.Contains("KvK 87654321", brand.LegalLine, StringComparison.Ordinal);
        // Renderer HTML-encodes elsewhere; brand line itself keeps the text (no marker left).
        Assert.DoesNotContain("lobsy:legal-footer", brand.LegalLine, StringComparison.Ordinal);
    }

    [Fact]
    public void Empty_identity_keeps_trade_name_only_without_marker()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILegalIdentity>(new StubLegalIdentity(new LegalIdentitySnapshot(
            Name: null,
            TradeName: "Lobsy",
            Street: null,
            PostalCode: null,
            City: null,
            Country: "Nederland",
            KvkNumber: null,
            VatNumber: null,
            PrivacyEmail: null,
            SupportEmail: "support@lobsy.nl",
            SchoolsEmail: null)));
        services.AddSingleton<IPostConfigureOptions<MailOptions>, MailOptionsLegalIdentityPostConfigure>();
        var sp = services.BuildServiceProvider();

        var options = new MailOptions
        {
            LegalName = MailOptions.DefaultLegalName,
            LegalAddress = null,
            KvkNumber = null
        };
        sp.GetRequiredService<IPostConfigureOptions<MailOptions>>().PostConfigure(Options.DefaultName, options);

        var brand = EmailBrand.From(options, "https://lobsy.nl");
        Assert.Equal("Lobsy", brand.LegalLine);
        Assert.DoesNotContain("lobsy:legal-footer", brand.LegalLine, StringComparison.Ordinal);
    }

    [Fact]
    public void No_legal_footer_marker_in_email_layout_source()
    {
        var root = FindRepoRoot();
        var layout = File.ReadAllText(Path.Combine(root, "Jobsy.Core", "Email", "EmailLayout.cs"));
        Assert.DoesNotContain("lobsy:legal-footer", layout, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }

    private sealed class StubLegalIdentity : ILegalIdentity
    {
        private readonly LegalIdentitySnapshot _snap;
        public StubLegalIdentity(LegalIdentitySnapshot snap) => _snap = snap;
        public Task<LegalIdentitySnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_snap);
    }
}
