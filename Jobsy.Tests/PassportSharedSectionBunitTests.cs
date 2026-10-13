using System.Net;
using System.Security.Claims;
using System.Text;
using Bunit;
using Jobsy.Core.Features;
using Jobsy.Web.Components.Candidate.Passport;
using Jobsy.Web.Components.Candidate.ProfileSections;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public class PassportSharedSectionBunitTests : BunitContext
{
    private readonly MutableFlags _flags = new();
    private readonly ApiStub _api = new();

    public PassportSharedSectionBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IGeocodingClient>(new NullGeo());
        Services.AddSingleton(new HttpClient(_api) { BaseAddress = new Uri("http://localhost/") });
        Services.AddSingleton(sp => new JobsyApiClient(sp.GetRequiredService<HttpClient>()));
        Services.AddSingleton<CandidateProfileEditor>();
        Services.AddSingleton<IFeatureFlags>(_flags);
    }

    [Fact]
    public void Data_tab_hides_shared_section_when_pdf_v2_is_off()
    {
        _flags.PdfV2 = false;
        var cut = Render<PassportDataTab>(p => p.Add(x => x.Active, true));
        cut.WaitForAssertion(() => Assert.Contains("Persoonlijk", cut.Markup, StringComparison.Ordinal));
        Assert.DoesNotContain("Dit deel ik met werkgevers en bureaus", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("data-testid=\"phone-verify\"", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Chips_save_canonical_codes_and_phone_button_stays_hidden()
    {
        _flags.PdfV2 = true;
        _flags.Phone = false;
        var cut = Render<PassportDataTab>(p => p.Add(x => x.Active, true));
        cut.WaitForAssertion(() =>
            Assert.Contains("Dit deel ik met werkgevers en bureaus", cut.Markup, StringComparison.Ordinal));

        cut.FindAll("button").First(b => b.TextContent.Contains("Dit deel ik met werkgevers", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => Assert.Contains("data-testid=\"shared-with-employers\"", cut.Markup, StringComparison.Ordinal));
        Assert.DoesNotContain("data-testid=\"phone-verify\"", cut.Markup, StringComparison.Ordinal);

        cut.FindAll("button").First(b => b.TextContent.Contains("Liever binnen", StringComparison.Ordinal)).Click();
        var editor = Services.GetRequiredService<CandidateProfileEditor>();
        Assert.Equal("prefer", editor.WorkIndoor);

        await cut.InvokeAsync(() => editor.SaveAsync());
        Assert.NotNull(_api.LastPut);
        Assert.Contains("\"indoor\":\"prefer\"", _api.LastPut, StringComparison.Ordinal);
        Assert.DoesNotContain("Liever binnen", _api.LastPut, StringComparison.Ordinal);
    }

    [Fact]
    public void Phone_verify_line_shows_only_when_the_flag_is_on()
    {
        _flags.PdfV2 = true;
        _flags.Phone = true;
        var cut = Render<PassportDataTab>(p => p.Add(x => x.Active, true));
        cut.WaitForAssertion(() =>
            Assert.Contains("Dit deel ik met werkgevers en bureaus", cut.Markup, StringComparison.Ordinal));
        cut.FindAll("button").First(b => b.TextContent.Contains("Dit deel ik met werkgevers", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => Assert.Contains("data-testid=\"phone-verify\"", cut.Markup, StringComparison.Ordinal));
        Assert.Contains("Bevestig telefoon", cut.Markup, StringComparison.Ordinal);
    }

    private sealed class MutableFlags : IFeatureFlags
    {
        public bool PdfV2 { get; set; }
        public bool Phone { get; set; }

        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(
                true,
                true,
                PassportPdfV2Enabled: PdfV2,
                PhoneVerificationEnabled: Phone));

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(feature switch
            {
                PlatformFeature.Employers => true,
                PlatformFeature.CandidatePassport => true,
                PlatformFeature.PassportPdfV2 => PdfV2,
                _ => false
            });

        public void Invalidate()
        {
        }
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(
                new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.Name, "t"), new Claim(ClaimTypes.Role, "Candidate")], "t"))));
    }

    private sealed class NullGeo : IGeocodingClient
    {
        public Task<IReadOnlyList<AddressSuggestion>> SuggestAsync(string query, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AddressSuggestion>>([]);

        public Task<string?> ReverseAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);
    }

    private sealed class ApiStub : HttpMessageHandler
    {
        public string? LastPut { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (request.Method == HttpMethod.Put && path.Contains("profile", StringComparison.Ordinal))
            {
                LastPut = request.Content is null
                    ? null
                    : await request.Content.ReadAsStringAsync(cancellationToken);
                return Json(Profile());
            }

            if (path.Contains("private-preferences", StringComparison.Ordinal))
            {
                return Json("""{"dislikes":[],"customDislikes":[]}""");
            }

            if (path.Contains("masterdata", StringComparison.Ordinal))
            {
                return Json("[]");
            }

            if (path.Contains("profile", StringComparison.Ordinal))
            {
                return Json(Profile());
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private static HttpResponseMessage Json(string body)
            => new(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };

        private static string Profile() =>
            """
            {"id":"11111111-1111-1111-1111-111111111111","email":"sam@example.com","fullName":"Sam Tester","firstName":"Sam","lastName":"Tester","role":"Candidate","preferences":{"roles":[]},"emailVerified":true,"phoneVerified":false}
            """;
    }
}
