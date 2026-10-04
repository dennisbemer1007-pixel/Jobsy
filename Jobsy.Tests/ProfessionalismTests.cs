using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Jobsy.Core.Admin;
using Jobsy.Core.Options;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Web.Localization;
using Jobsy.Web.Seo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

/// <summary>
/// Professionalism gate: company details are admin-only and audited, empty lines stay hidden,
/// security.txt and the acceptatie noindex switch behave, and the age rule is one story.
/// </summary>
public class ProfessionalismTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private static readonly Regex Placeholder = new(@"\[[A-Z][A-Z \-]+\]", RegexOptions.CultureInvariant);

    private readonly RoleFunctionalWebAppFactory _factory;

    public ProfessionalismTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Company_settings_are_admin_only()
    {
        using var anon = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("api/settings/company")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.PutAsJsonAsync("api/settings/company", new { companyName = "X" })).StatusCode);

        using var employer = _factory.CreateClient();
        JobsyTestAuth.Authorize(employer, _factory.EmployerId);
        Assert.Equal(HttpStatusCode.Forbidden, (await employer.GetAsync("api/settings/company")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await employer.PutAsJsonAsync("api/settings/company", new { companyName = "X" })).StatusCode);
    }

    [Fact]
    public async Task Saving_company_details_audits_each_field_and_masks_the_iban()
    {
        using var admin = _factory.CreateClient();
        JobsyTestAuth.Authorize(admin, _factory.AdminId);
        var before = await admin.GetFromJsonAsync<JsonElement>("api/settings/company", JsonOpts);
        var marker = "Proef " + Guid.NewGuid().ToString("N")[..8];
        const string iban = "NL91ABNA0417164300";

        var put = await admin.PutAsJsonAsync("api/settings/company", new
        {
            companyName = "Lobsy",
            legalName = marker,
            tradeName = "Lobsy",
            slogan = Read(before, "slogan"),
            address = "Teststraat 1",
            postalCode = "2671 AB",
            city = "Naaldwijk",
            country = "NL",
            kvkNumber = "12345678",
            vatNumber = "NL123456782B01",
            phone = "0612345678",
            email = "support@lobsy.nl",
            supportEmail = "support@lobsy.nl",
            privacyEmail = "privacy@lobsy.nl",
            vatBufferIban = iban
        });
        var body = await put.Content.ReadAsStringAsync();
        Assert.True(put.StatusCode == HttpStatusCode.OK, body);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var rows = await db.AdminAuditEvents.AsNoTracking()
            .Where(e => e.Action == AdminAuditKeys.SettingsCompanyUpdate)
            .ToListAsync();
        var legal = rows.Single(e => e.TargetId == "LegalName" && (e.DetailsJson ?? "").Contains(marker, StringComparison.Ordinal));
        Assert.Contains("\"from\"", legal.DetailsJson, StringComparison.Ordinal);
        Assert.Contains(marker, legal.DetailsJson, StringComparison.Ordinal);

        var ibanRow = rows.Single(e => e.TargetId == "VatBufferIban" && (e.DetailsJson ?? "").Contains("4300", StringComparison.Ordinal));
        Assert.DoesNotContain(iban, ibanRow.DetailsJson, StringComparison.Ordinal);
        Assert.Contains("NL**4300", ibanRow.DetailsJson, StringComparison.Ordinal);

        var legalPage = await admin.GetFromJsonAsync<JsonElement>("api/site/legal", JsonOpts);
        Assert.Equal(marker, legalPage.GetProperty("name").GetString());
        Assert.DoesNotMatch(Placeholder, legalPage.ToString());

        var cleared = await admin.PutAsJsonAsync("api/settings/company", new
        {
            companyName = "Lobsy",
            legalName = marker,
            tradeName = "Lobsy",
            address = (string?)null,
            postalCode = (string?)null,
            city = (string?)null,
            country = "NL",
            kvkNumber = (string?)null,
            phone = (string?)null,
            email = "support@lobsy.nl",
            supportEmail = "support@lobsy.nl",
            privacyEmail = "privacy@lobsy.nl"
        });
        Assert.Equal(HttpStatusCode.OK, cleared.StatusCode);
        var afterClear = await admin.GetFromJsonAsync<JsonElement>("api/site/legal", JsonOpts);
        Assert.False(afterClear.TryGetProperty("street", out var street) && street.ValueKind == JsonValueKind.String);
        Assert.False(afterClear.TryGetProperty("kvkNumber", out var kvk) && kvk.ValueKind == JsonValueKind.String);
        Assert.Equal(marker, afterClear.GetProperty("name").GetString());

        await RestoreAsync(admin, before);
    }

    [Fact]
    public async Task Company_field_errors_are_plain_dutch()
    {
        using var admin = _factory.CreateClient();
        JobsyTestAuth.Authorize(admin, _factory.AdminId);
        var badKvk = await admin.PutAsJsonAsync("api/settings/company", new
        {
            companyName = "Lobsy",
            kvkNumber = "123"
        });
        Assert.Equal(HttpStatusCode.BadRequest, badKvk.StatusCode);
        Assert.Contains(PlatformCompanyFieldRules.KvkMessage, await badKvk.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        var badMail = await admin.PutAsJsonAsync("api/settings/company", new
        {
            companyName = "Lobsy",
            supportEmail = "niet-een-mail"
        });
        Assert.Equal(HttpStatusCode.BadRequest, badMail.StatusCode);
        Assert.Contains(PlatformCompanyFieldRules.EmailMessage, await badMail.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        var badVat = await admin.PutAsJsonAsync("api/settings/company", new
        {
            companyName = "Lobsy",
            vatNumber = "NL123456789B01"
        });
        Assert.Equal(HttpStatusCode.BadRequest, badVat.StatusCode);
        Assert.Contains(PlatformCompanyFieldRules.VatMessage, await badVat.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Saving_company_details_drops_the_legal_identity_cache()
    {
        await using var db = new JobsyDbContext(new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var company = new PlatformCompanySettingsService(db, cache);
        var legal = new LegalIdentityService(
            new StaticOptions(new LegalOptions()),
            company,
            cache,
            NullLogger<LegalIdentityService>.Instance);

        await company.UpdateAsync(new Jobsy.Core.Interfaces.PlatformCompanyUpdate(
            "Lobsy", null, "Oud 1", null, null, "NL", null, null, null, null,
            LegalName: "Eerste BV"));
        var first = await legal.GetAsync();
        Assert.Equal("Oud 1", first.Street);

        await company.UpdateAsync(new Jobsy.Core.Interfaces.PlatformCompanyUpdate(
            "Lobsy", null, "Nieuw 2", null, null, "NL", null, null, null, null,
            LegalName: "Eerste BV"));
        var second = await legal.GetAsync();
        Assert.Equal("Nieuw 2", second.Street);
        Assert.DoesNotContain("KvK", second.FooterLine, StringComparison.Ordinal);
    }

    [Fact]
    public void Security_txt_omits_contact_when_no_mailbox_is_set_and_expires_later()
    {
        var now = new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc);
        var empty = SecurityTxt.Format(null, "https://lobsy.nl", now);
        Assert.DoesNotContain("Contact:", empty, StringComparison.Ordinal);
        Assert.Contains("Expires: 2027-10-04T12:00:00.000Z", empty, StringComparison.Ordinal);
        Assert.Contains("Preferred-Languages: nl, en", empty, StringComparison.Ordinal);
        Assert.Contains("Canonical: https://lobsy.nl/.well-known/security.txt", empty, StringComparison.Ordinal);

        var withMail = SecurityTxt.Format(" privacy@lobsy.nl ", "https://acceptatie.lobsy.nl", now);
        Assert.StartsWith("Contact: mailto:privacy@lobsy.nl\n", withMail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Security_txt_is_public_plain_text()
    {
        await using var factory = new Errors.ErrorPagesWebFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/.well-known/security.txt");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Expires:", body, StringComparison.Ordinal);
        Assert.Contains("Preferred-Languages: nl, en", body, StringComparison.Ordinal);
        Assert.Contains("Canonical:", body, StringComparison.Ordinal);
        var expires = Regex.Match(body, @"Expires: (\S+)").Groups[1].Value;
        Assert.True(DateTime.Parse(expires, null, System.Globalization.DateTimeStyles.AdjustToUniversal) > DateTime.UtcNow);
    }

    [Fact]
    public async Task Noindex_switch_off_leaves_robots_and_sitemap_open()
    {
        await using var factory = new Errors.ErrorPagesWebFactory { NoIndex = false };
        using var client = factory.CreateHtmlClient();
        var robots = await client.GetAsync("/robots.txt");
        var text = await robots.Content.ReadAsStringAsync();

        Assert.False(robots.Headers.Contains(SeoNoIndexMiddleware.HeaderName));
        Assert.Contains("Sitemap:", text, StringComparison.Ordinal);
        Assert.Contains("Allow: /", text, StringComparison.Ordinal);

        var sitemap = await client.GetAsync("/sitemap.xml");
        Assert.Equal(HttpStatusCode.OK, sitemap.StatusCode);
    }

    [Fact]
    public async Task Noindex_switch_on_blocks_crawlers_on_every_response()
    {
        await using var factory = new Errors.ErrorPagesWebFactory { NoIndex = true };
        using var client = factory.CreateHtmlClient();

        var robots = await client.GetAsync("/robots.txt");
        var text = await robots.Content.ReadAsStringAsync();
        Assert.Equal(SeoNoIndexMiddleware.HeaderValue, robots.Headers.GetValues(SeoNoIndexMiddleware.HeaderName).First());
        Assert.Contains("Disallow: /", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Sitemap:", text, StringComparison.Ordinal);

        var sitemap = await client.GetAsync("/sitemap.xml");
        Assert.Equal(HttpStatusCode.NotFound, sitemap.StatusCode);

        var page = await client.GetAsync("/hoe-werkt-lobsy");
        var html = await page.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Equal(SeoNoIndexMiddleware.HeaderValue, page.Headers.GetValues(SeoNoIndexMiddleware.HeaderName).First());
        Assert.Contains("name=\"robots\" content=\"noindex, nofollow\"", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/geen-pagina.xyz")]
    [InlineData("/.well-known/onbekend")]
    public async Task Unknown_dotted_paths_return_a_friendly_html_404(string path)
    {
        await using var factory = new Errors.ErrorPagesWebFactory();
        using var client = factory.CreateHtmlClient();
        var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Deze pagina bestaat niet", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/ontdek\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/melden\"", html, StringComparison.Ordinal);
        Assert.Contains("noindex", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch(Placeholder, html);
    }

    [Fact]
    public void Age_copy_that_says_16_also_says_13_in_every_language()
    {
        var catalog = (Dictionary<string, Dictionary<string, string>>)typeof(UiStrings)
            .GetField("Catalog", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!;
        var misses = new List<string>();
        foreach (var (lang, map) in catalog)
        {
            foreach (var (key, value) in map)
            {
                var lower = value.ToLowerInvariant();
                if ((lower.Contains("16 jaar", StringComparison.Ordinal) || lower.Contains("vanaf 16", StringComparison.Ordinal))
                    && !lower.Contains("13", StringComparison.Ordinal))
                {
                    misses.Add($"{lang}:{key}");
                }
            }
        }

        Assert.True(misses.Count == 0, string.Join(", ", misses));
        Assert.Equal(13, CandidateConsentRules.MinimumCandidateAge);
        Assert.Equal(16, CandidateConsentRules.ParentalConsentAge);
        Assert.True(CandidateConsentRules.IsBelowMinimumAge(new DateOnly(2014, 10, 4), new DateOnly(2026, 10, 4)));
        Assert.False(CandidateConsentRules.IsBelowMinimumAge(new DateOnly(2013, 10, 4), new DateOnly(2026, 10, 4)));
    }

    private static async Task RestoreAsync(HttpClient admin, JsonElement before)
    {
        var restore = await admin.PutAsJsonAsync("api/settings/company", new
        {
            companyName = Read(before, "companyName") ?? "Lobsy",
            slogan = Read(before, "slogan"),
            address = Read(before, "address"),
            postalCode = Read(before, "postalCode"),
            city = Read(before, "city"),
            country = Read(before, "country"),
            kvkNumber = Read(before, "kvkNumber"),
            vatNumber = Read(before, "vatNumber"),
            phone = Read(before, "phone"),
            email = Read(before, "email"),
            legalName = Read(before, "legalName"),
            tradeName = Read(before, "tradeName"),
            postalStreet = Read(before, "postalStreet"),
            postalPostalCode = Read(before, "postalPostalCode"),
            postalCity = Read(before, "postalCity"),
            supportEmail = Read(before, "supportEmail"),
            privacyEmail = Read(before, "privacyEmail")
        });
        Assert.Equal(HttpStatusCode.OK, restore.StatusCode);
    }

    private static string? Read(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private sealed class StaticOptions(LegalOptions current) : IOptionsMonitor<LegalOptions>
    {
        public LegalOptions CurrentValue { get; } = current;
        public LegalOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<LegalOptions, string?> listener) => null;
    }
}
