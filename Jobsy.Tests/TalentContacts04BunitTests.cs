using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Bunit;
using Jobsy.Core.Features;
using Jobsy.Web.Components.Candidate.Contacts;
using Jobsy.Web.Components.Pages.Candidate;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests;

/// <summary>
/// Carrière 04 §2–§5: the contact-request page in the journey style. The h1 is visible, statuses
/// are words, the share dialog lists the exact preview and nothing is shared without a yes.
/// </summary>
public class TalentContacts04BunitTests : TestContext
{
    private readonly StubHandler _handler = new();
    private bool _employers = true;
    private bool _passport;

    public TalentContacts04BunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeCandidateAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IFeatureFlags>(new LazyFlags(() => _employers, () => _passport));
        Services.AddSingleton(new JobsyApiClient(new HttpClient(_handler)
        {
            BaseAddress = new Uri("http://localhost/")
        }));
        Services.AddLogging();
    }

    // ---------- page shell (T1, T2) ----------

    [Fact]
    public void Page_shows_a_visible_h1_outside_the_old_profile_header()
    {
        _handler.Rows = [Row(TalentStatus.Pending)];
        var cut = RenderComponent<CandidateTalentContacts>();

        var h1 = cut.Find("h1");
        Assert.Equal("Een werkgever wil je spreken", h1.TextContent.Trim());
        Assert.Null(h1.Closest(".profile-page__header"));
        Assert.Null(h1.Closest(".visually-hidden"));
        Assert.Contains("journey-page career-page", cut.Markup, StringComparison.Ordinal);
        Assert.Contains(
            "Werkgevers zien je naam, e-mail en telefoon pas als jij ja zegt.",
            cut.Markup,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Without_open_requests_the_heading_is_the_calm_variant()
    {
        _handler.Rows = [Row(TalentStatus.CandidateDeclined)];
        var cut = RenderComponent<CandidateTalentContacts>();
        Assert.Equal("Contactverzoeken", cut.Find("h1").TextContent.Trim());
    }

    // ---------- statuses (T4) ----------

    [Theory]
    [InlineData(TalentStatus.Pending, "Wacht op jou")]
    [InlineData(TalentStatus.RefundEligible, "Wacht op jou")]
    [InlineData(TalentStatus.ContactShared, "Je zei ja")]
    [InlineData(TalentStatus.CandidateDeclined, "Je zei nee")]
    [InlineData(TalentStatus.WithdrawnRefunded, "Gestopt")]
    public void Status_pills_are_words_not_enum_names(string status, string expected)
    {
        _handler.Rows = [Row(status)];
        var cut = RenderComponent<CandidateTalentContacts>();

        Assert.Contains(expected, cut.Find(".career-pill").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain(status, cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Refund_eligible_keeps_the_answer_open_without_scary_copy()
    {
        _handler.Rows = [Row(TalentStatus.RefundEligible)];
        var cut = RenderComponent<CandidateTalentContacts>();

        Assert.Contains("De tijd is om, maar je kunt nog reageren.", cut.Markup, StringComparison.Ordinal);
        Assert.Equal(3, cut.FindAll(".talent-req__actions .career-btn").Count);
    }

    [Fact]
    public void Declined_because_already_placed_adds_the_reason_line()
    {
        _handler.Rows = [Row(TalentStatus.CandidateDeclined, declineReason: "AlreadyPlaced")];
        var cut = RenderComponent<CandidateTalentContacts>();

        Assert.Contains("Er is niets gedeeld.", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Je had al werk", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Withdrawn_says_nothing_was_shared()
    {
        _handler.Rows = [Row(TalentStatus.WithdrawnRefunded)];
        var cut = RenderComponent<CandidateTalentContacts>();
        Assert.Contains(
            "De werkgever heeft het verzoek ingetrokken. Er is niets gedeeld.",
            cut.Markup,
            StringComparison.Ordinal);
    }

    // ---------- dates (T4, §3) ----------

    [Fact]
    public void The_deadline_uses_amsterdam_time_not_the_server_zone()
    {
        var respondBy = new DateTime(2026, 7, 10, 8, 30, 0, DateTimeKind.Utc);
        _handler.Rows = [Row(TalentStatus.Pending, respondByUtc: respondBy)];
        var cut = RenderComponent<CandidateTalentContacts>();

        var expected = LobsyTime.Deadline(respondBy, CultureInfo.GetCultureInfo("nl-NL"));
        Assert.Contains(expected, cut.Find(".talent-req__when").TextContent, StringComparison.Ordinal);
        Assert.Contains("10:30", expected, StringComparison.Ordinal);
    }

    // ---------- share dialog (T3, §4) ----------

    [Fact]
    public void The_dialog_lists_exactly_the_preview_fields()
    {
        _handler.Rows = [Row(TalentStatus.Pending)];
        _handler.Preview = new TalentContactSharePreviewModel
        {
            CompanyName = "Kwekerij De Klauw",
            Name = "Kandidaat Test",
            Email = "kandidaat@lobsy.local",
            Phone = null
        };

        var cut = RenderComponent<CandidateTalentContacts>();
        cut.Find(".talent-req__actions .career-btn--primary").Click();

        var values = cut.FindAll(".talent-dialog__list dd").Select(d => d.TextContent.Trim()).ToList();
        Assert.Equal(["Kandidaat Test", "kandidaat@lobsy.local", "niet ingevuld"], values);
        Assert.Contains("Je gegevens delen met Kwekerij De Klauw?", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Delen kun je niet terugdraaien.", cut.Markup, StringComparison.Ordinal);
        Assert.Equal(0, _handler.RespondCalls);
    }

    [Fact]
    public void Nog_niet_closes_the_dialog_and_sends_nothing()
    {
        _handler.Rows = [Row(TalentStatus.Pending)];
        var cut = RenderComponent<CandidateTalentContacts>();
        cut.Find(".talent-req__actions .career-btn--primary").Click();
        Assert.NotEmpty(cut.FindAll(".talent-dialog"));

        cut.Find(".career-dialog__actions .career-btn--secondary").Click();

        Assert.Empty(cut.FindAll(".talent-dialog"));
        Assert.Equal(0, _handler.RespondCalls);
    }

    [Fact]
    public void Confirming_shares_once_and_sends_the_confirmation_token()
    {
        _handler.Rows = [Row(TalentStatus.Pending)];
        var cut = RenderComponent<CandidateTalentContacts>();
        cut.Find(".talent-req__actions .career-btn--primary").Click();

        _handler.RowsAfterRespond = [Row(TalentStatus.ContactShared)];
        cut.Find(".talent-dialog__share").Click();

        Assert.Equal(1, _handler.RespondCalls);
        Assert.Contains("\"confirmedShare\":true", _handler.LastRespondBody, StringComparison.Ordinal);
        Assert.Contains("\"accept\":true", _handler.LastRespondBody, StringComparison.Ordinal);
        Assert.Contains("Je zei ja", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Je gegevens zijn gedeeld met", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void A_failing_preview_hides_the_primary_button()
    {
        _handler.Rows = [Row(TalentStatus.Pending)];
        _handler.PreviewFails = true;

        var cut = RenderComponent<CandidateTalentContacts>();
        cut.Find(".talent-req__actions .career-btn--primary").Click();

        Assert.Empty(cut.FindAll(".talent-dialog__share"));
        Assert.Contains("We kunnen nu niet laten zien wat er gedeeld wordt.", cut.Markup, StringComparison.Ordinal);
        Assert.Equal(0, _handler.RespondCalls);
    }

    // ---------- declines (D14) ----------

    [Fact]
    public void Declining_immediately_sends_the_reason_and_confirms_in_place()
    {
        _handler.Rows = [Row(TalentStatus.Pending)];
        var cut = RenderComponent<CandidateTalentContacts>();

        _handler.RowsAfterRespond = [Row(TalentStatus.CandidateDeclined, declineReason: "AlreadyPlaced")];
        cut.Find(".talent-req__actions .career-btn--secondary").Click();

        Assert.Equal(1, _handler.RespondCalls);
        Assert.Contains("\"accept\":false", _handler.LastRespondBody, StringComparison.Ordinal);
        Assert.Contains("\"alreadyPlaced\":true", _handler.LastRespondBody, StringComparison.Ordinal);
        Assert.Contains("Je zei nee. Er is niets gedeeld.", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Geen_interesse_declines_without_the_already_placed_reason()
    {
        _handler.Rows = [Row(TalentStatus.Pending)];
        var cut = RenderComponent<CandidateTalentContacts>();

        _handler.RowsAfterRespond = [Row(TalentStatus.CandidateDeclined)];
        cut.Find(".talent-req__actions .career-btn--text").Click();

        Assert.Contains("\"alreadyPlaced\":false", _handler.LastRespondBody, StringComparison.Ordinal);
    }

    [Fact]
    public void A_conflict_shows_a_localized_code_message_not_api_text()
    {
        _handler.Rows = [Row(TalentStatus.Pending)];
        _handler.RespondStatus = HttpStatusCode.Conflict;
        _handler.RespondBody = "{\"code\":\"cannot_respond\",\"message\":\"raw server text\"}";

        var cut = RenderComponent<CandidateTalentContacts>();
        cut.Find(".talent-req__actions .career-btn--text").Click();

        Assert.Contains("Op dit verzoek kun je niet meer reageren.", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("raw server text", cut.Markup, StringComparison.Ordinal);
    }

    // ---------- empty state and the gate ----------

    [Fact]
    public void Empty_state_invites_a_stronger_profile_when_the_passport_is_off()
    {
        _handler.Rows = [];
        var cut = RenderComponent<CandidateTalentContacts>();

        Assert.Contains("Nog geen contactverzoeken.", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("profiel", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("href=\"/candidate/profile\"", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Ik luister mee.", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void With_the_passport_on_the_empty_state_points_at_the_paspoort()
    {
        _handler.Rows = [];
        _passport = true;
        var cut = RenderComponent<CandidateTalentContacts>();

        Assert.Contains("paspoort", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("href=\"/candidate/paspoort\"", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void With_the_employers_gate_off_the_page_stays_calm_and_loads_nothing()
    {
        _employers = false;
        var cut = RenderComponent<CandidateTalentContacts>();

        Assert.Contains("Contactverzoeken staan nu uit.", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll(".talent-req"));
        Assert.Equal(0, _handler.ListCalls);
    }

    // ---------- rtl ----------

    [Fact]
    public async Task Arabic_renders_the_page_right_to_left()
    {
        var previous = CultureInfo.DefaultThreadCurrentUICulture;
        try
        {
            var culture = Services.GetRequiredService<CultureState>();
            await culture.SetLanguageAsync("ar");
            Assert.True(culture.IsRightToLeft);

            _handler.Rows = [Row(TalentStatus.Pending)];
            var cut = RenderComponent<CandidateTalentContacts>();

            Assert.Contains("طلبات التواصل", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("القرار لك، دائماً.", cut.Markup, StringComparison.Ordinal);

            var layout = await File.ReadAllTextAsync(Path.Combine(
                RepoRoot(), "Jobsy.Web", "Components", "Layout", "MainLayout.razor"));
            Assert.Contains("\"rtl\"", layout, StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.DefaultThreadCurrentUICulture = previous;
            CultureInfo.DefaultThreadCurrentCulture = previous;
            CultureInfo.CurrentUICulture = previous ?? CultureInfo.GetCultureInfo("nl-NL");
            CultureInfo.CurrentCulture = previous ?? CultureInfo.GetCultureInfo("nl-NL");
        }
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Repo root not found.");
    }

    // ---------- helpers ----------

    private static class TalentStatus
    {
        public const string Pending = nameof(Jobsy.Core.Enums.TalentContactStatus.Pending);
        public const string RefundEligible = nameof(Jobsy.Core.Enums.TalentContactStatus.RefundEligible);
        public const string ContactShared = nameof(Jobsy.Core.Enums.TalentContactStatus.ContactShared);
        public const string CandidateDeclined = nameof(Jobsy.Core.Enums.TalentContactStatus.CandidateDeclined);
        public const string WithdrawnRefunded = nameof(Jobsy.Core.Enums.TalentContactStatus.WithdrawnRefunded);
    }

    private static TalentContactRequestModel Row(
        string status,
        string? declineReason = null,
        DateTime? respondByUtc = null)
    {
        var created = new DateTime(2026, 7, 8, 8, 30, 0, DateTimeKind.Utc);
        return new TalentContactRequestModel
        {
            Id = Guid.Parse("aaaaaaaa-0000-0000-0000-00000000000a"),
            CompanyId = Guid.Parse("bbbbbbbb-0000-0000-0000-00000000000b"),
            Status = status,
            Message = "We zoeken iemand zoals jij.",
            CreatedAtUtc = created,
            RespondByUtc = respondByUtc ?? created.AddHours(48),
            RespondedAtUtc = status == TalentStatus.Pending ? null : created.AddHours(3),
            ContactSharedAtUtc = status == TalentStatus.ContactShared ? created.AddHours(3) : null,
            CompanyName = "Kwekerij De Klauw",
            CandidateDeclineReason = declineReason
        };
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        public List<TalentContactRequestModel> Rows { get; set; } = [];

        public List<TalentContactRequestModel>? RowsAfterRespond { get; set; }

        public TalentContactSharePreviewModel? Preview { get; set; }

        public bool PreviewFails { get; set; }

        public HttpStatusCode RespondStatus { get; set; } = HttpStatusCode.OK;

        public string RespondBody { get; set; } = "{}";

        public int ListCalls { get; private set; }

        public int RespondCalls { get; private set; }

        public string LastRespondBody { get; private set; } = "";

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;

            if (path.EndsWith("/share-preview", StringComparison.Ordinal))
            {
                return PreviewFails
                    ? new HttpResponseMessage(HttpStatusCode.NotFound)
                    {
                        Content = new StringContent("{\"code\":\"not_found\"}", Encoding.UTF8, "application/json")
                    }
                    : Ok(Preview ?? new TalentContactSharePreviewModel
                    {
                        CompanyName = "Kwekerij De Klauw",
                        Name = "Kandidaat Test",
                        Email = "kandidaat@lobsy.local",
                        Phone = "+31600000001"
                    });
            }

            if (path.EndsWith("/respond", StringComparison.Ordinal))
            {
                RespondCalls++;
                LastRespondBody = request.Content is null
                    ? ""
                    : await request.Content.ReadAsStringAsync(cancellationToken);
                if (RespondStatus != HttpStatusCode.OK)
                {
                    return new HttpResponseMessage(RespondStatus)
                    {
                        Content = new StringContent(RespondBody, Encoding.UTF8, "application/json")
                    };
                }

                if (RowsAfterRespond is not null)
                {
                    Rows = RowsAfterRespond;
                }

                return Ok(new { ok = true });
            }

            ListCalls++;
            return Ok(Rows);
        }

        private static HttpResponseMessage Ok(object payload)
            => new(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(payload, Json),
                    Encoding.UTF8,
                    "application/json")
            };
    }

    private sealed class LazyFlags(Func<bool> employers, Func<bool> passport) : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(employers(), passport()));

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(employers(), passport()).IsEnabled(feature));

        public void Invalidate()
        {
        }
    }

    private sealed class FakeCandidateAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, "kandidaat"), new Claim(ClaimTypes.Role, "Candidate")],
                "test");
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
        }
    }
}
