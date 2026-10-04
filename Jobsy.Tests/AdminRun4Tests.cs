using System.Net;
using System.Security.Claims;
using System.Text;
using Bunit;
using Bunit.TestDoubles;
using Jobsy.Core.Rules;
using Jobsy.Web.Admin;
using Jobsy.Web.Components.Admin.Sections;
using Jobsy.Web.Components.Admin.Shell;
using Jobsy.Web.Components.Admin.Ui;
using Jobsy.Web.Components.Pages.Admin;
using Jobsy.Web.Components.Shared;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public class AdminRun4LabelTests
{
    private static string Text(string key) => key;

    private static string AccessText(string key) => key switch
    {
        "AdminDataAccess.Reason.Page" => "Pagina {0}",
        "AdminDataAccess.Reason.Actor" => "wie {0}",
        "AdminDataAccess.Reason.Subject" => "over wie {0}",
        _ => key
    };

    [Fact]
    public void Token_and_audit_and_access_values_use_dutch_keys()
    {
        Assert.Equal("AdminToken.Kind.Grant", AdminActionLabels.TokenKind("Grant", Text));
        Assert.Equal("—", AdminActionLabels.TokenReason("None", Text));
        Assert.Equal("—", AdminActionLabels.TokenReason(null, Text));
        Assert.Equal("TokenSpendCost.Publish", AdminActionLabels.TokenReason("Publish", Text));
        Assert.Equal("AdminToken.Reason.Extend", AdminActionLabels.TokenReason("Extend", Text));
        Assert.Equal("AdminAudit.Target.Retention", AdminActionLabels.Target("Data retention", Text));
        Assert.Equal("AdminAudit.Target.AuditCsv", AdminActionLabels.Target("audit-csv", Text));
        Assert.Equal(
            "AdminDataAccess.Action.Viewed",
            AdminActionLabels.AccessAction("admin.personal_data_access_log.list", "list", Text));
        Assert.Equal("AdminAction.Resource.AccessLog", AdminActionLabels.Resource("admin.personal_data_access_log.list", Text));
        Assert.Equal("Pagina 1", AdminActionLabels.AccessReason("page=1;actor=;subject=", AccessText));
        Assert.Equal("Pagina 2 · wie aabbccdd", AdminActionLabels.AccessReason("page=2;actor=aabbccdd-1111-1111-1111-111111111111;subject=", AccessText));
        Assert.Equal("Bewaartermijn", AdminActionLabels.Target("Data retention", key => key == "AdminAudit.Target.Retention" ? "Bewaartermijn" : key));
    }

    [Fact]
    public void Closing_a_drawer_drops_open_and_keeps_other_query_keys()
    {
        var next = AdminOpenLink.WithoutOpen("https://acc.lobsy.nl/admin/gebruikers?open=11111111-1111-1111-1111-111111111111&tab=kandidaten");
        Assert.DoesNotContain("open=", next, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tab=kandidaten", next, StringComparison.Ordinal);
        Assert.False(AdminOpenLink.ShouldNavigate(
            "https://acc.lobsy.nl/admin/gebruikers?tab=kandidaten",
            next,
            interactive: true));
        Assert.Equal("/admin/vacatures", AdminOpenLink.WithoutOpen("/admin/vacatures?open=abc"));
    }

    [Fact]
    public void Navigation_titles_about_staff_and_search_are_not_vacancies()
    {
        Assert.False(AtsListingValidation.TryValidateForReview(
            "Dit zijn onze zorgverleners", "Acme", "Wat ga je doen? Solliciteer vandaag nog.", out _));
        Assert.False(AtsListingValidation.TryValidateForReview(
            "Waar bent u naar op zoek?", "Acme", "Bekijk de vacatures en solliciteer op een functie.", out _));
    }
}

public class AdminRun4UiTests : BunitContext
{
    private readonly ApiHandler _handler = new();

    public AdminRun4UiTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var admin = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "ffffffff-1111-1111-1111-111111111111"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Name, "Admin")
        ], "test"));
        Services.AddSingleton<AuthenticationStateProvider>(new AuthStub(admin));
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddLogging();
        Services.AddSingleton<UserFacingError>();
        Services.AddAuthorizationCore();
        this.AddAuthorization().SetAuthorized("admin").SetRoles("Admin");
        Services.AddSingleton(new JobsyApiClient(new HttpClient(_handler) { BaseAddress = new Uri("http://localhost") }));
    }

    [Fact]
    public async Task Row_menu_stays_open_after_focusout_and_an_action_can_be_chosen()
    {
        var cut = Render<MenuHost>();
        await cut.Find("button.row-actions-menu__toggle").ClickAsync();
        Assert.Equal("true", cut.Find("button.row-actions-menu__toggle").GetAttribute("aria-expanded"));

        await cut.Find(".row-actions-menu").TriggerEventAsync("onfocusout", new FocusEventArgs());
        await Task.Delay(400);

        Assert.Equal("true", cut.Find("button.row-actions-menu__toggle").GetAttribute("aria-expanded"));
        Assert.Contains("Bekijk", cut.Markup, StringComparison.Ordinal);
        await cut.Find(".row-actions-menu__item").ClickAsync();
        Assert.True(cut.Instance.Chosen);
    }

    [Fact]
    public async Task Row_menu_closes_only_when_focus_left_the_menu()
    {
        JSInterop.Setup<bool>("jobsyDialog.focusLeft", _ => true).SetResult(true);
        var cut = Render<MenuHost>();
        await cut.Find("button.row-actions-menu__toggle").ClickAsync();
        await cut.Find(".row-actions-menu").TriggerEventAsync("onfocusout", new FocusEventArgs());
        cut.WaitForAssertion(
            () => Assert.Equal("false", cut.Find("button.row-actions-menu__toggle").GetAttribute("aria-expanded")),
            TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Organisatie_search_is_not_the_dark_map_button()
    {
        var cut = Render<AdminFilterBar>(p => p
            .Add(x => x.Query, "")
            .Add(x => x.QueryChanged, _ => Task.CompletedTask));
        Assert.Contains("class=\"admin-filter-bar__search\"", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("filter-bar__search ", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Organisaties_search_rerenders_the_filtered_rows()
    {
        var cut = Render<CompaniesAdmin>();
        cut.WaitForAssertion(() => Assert.Contains("Andere Zaak", cut.Markup, StringComparison.Ordinal), TimeSpan.FromSeconds(3));
        await TypeSearchAsync(cut, ".admin-filter-bar__search input", "Binckhorst");
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Binckhorst", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Andere Zaak", cut.Markup, StringComparison.Ordinal);
        }, TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task Vacatures_search_rerenders_the_filtered_rows()
    {
        var cut = Render<VacanciesAdminSection>();
        cut.WaitForAssertion(() => Assert.Contains("Magazijn", cut.Markup, StringComparison.Ordinal), TimeSpan.FromSeconds(3));
        await TypeSearchAsync(cut, ".filter-bar__query input", "Kassamedewerker");
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Kassamedewerker", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Magazijn", cut.Markup, StringComparison.Ordinal);
        }, TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task Systeemlogs_search_rerenders_the_filtered_rows()
    {
        var cut = Render<LoggingAdmin>();
        cut.WaitForAssertion(() => Assert.Contains("Beta regel", cut.Markup, StringComparison.Ordinal), TimeSpan.FromSeconds(3));
        await TypeSearchAsync(cut, ".admin-filter-bar__search input", "Alpha");
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Alpha storing", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Beta regel", cut.Markup, StringComparison.Ordinal);
        }, TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task Gebruikers_search_rerenders_the_filtered_rows_and_sorts_on_the_header()
    {
        var cut = Render<UsersAdminSection>();
        cut.WaitForAssertion(() => Assert.Contains("Andere Zaak", cut.Markup, StringComparison.Ordinal), TimeSpan.FromSeconds(3));
        var nameHeader = cut.Find("th[aria-sort='ascending']");
        Assert.Contains("Gebruiker", nameHeader.TextContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("aria-sort", cut.Find("button.data-table__sort").OuterHtml, StringComparison.Ordinal);

        await TypeSearchAsync(cut, ".admin-filter-bar__search input", "Binckhorst");
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Binckhorst", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Andere Zaak", cut.Markup, StringComparison.Ordinal);
        }, TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task Topbar_search_keeps_every_character_at_40ms()
    {
        var cut = Render<AdminGlobalSearch>();
        var input = cut.Find("input.admin-search__input");
        const string typed = "binckhorstxyzab";
        Assert.Equal(15, typed.Length);
        var accumulated = "";
        foreach (var ch in typed)
        {
            accumulated += ch;
            await input.InputAsync(accumulated);
            await Task.Delay(40);
        }

        Assert.Equal(typed, cut.Find("input.admin-search__input").GetAttribute("value"));
        cut.WaitForAssertion(() => Assert.Equal(typed, _handler.LastSearch), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Mobile_topbar_css_stays_transparent_and_hides_the_label_under_480()
    {
        var css = File.ReadAllText(Path.Combine(FindRoot(), "Jobsy.Web/wwwroot/css/features/admin.css"));
        var marker = css.IndexOf(".admin-topbar__menu,\n    .admin-topbar__search-trigger {\n        display: inline-flex;\n        flex-shrink: 0;\n        background: transparent;", StringComparison.Ordinal);
        Assert.True(marker > 0, "Mobile menu button is not transparent.");
        var block = css[marker..css.IndexOf("@media (min-width: 480px)", marker, StringComparison.Ordinal)];
        Assert.Contains("display: none;", block, StringComparison.Ordinal);
        Assert.DoesNotContain("background: var(--surface, #fff)", block, StringComparison.Ordinal);
    }

    private static async Task TypeSearchAsync(IRenderedComponent<IComponent> cut, string selector, string text)
    {
        var input = cut.Find(selector);
        try
        {
            await input.TriggerEventAsync("onfocus", new FocusEventArgs());
        }
        catch (MissingEventHandlerException)
        {
            // Vacatures binds the field directly and has no focus handler.
        }

        await input.InputAsync(text);
    }

    private static string FindRoot()
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

        throw new InvalidOperationException("Repo root not found.");
    }

    private sealed class AuthStub(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(user));
    }

    private sealed class MenuHost : ComponentBase
    {
        public bool Chosen { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<RowActionsMenu>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(child =>
            {
                child.OpenElement(0, "button");
                child.AddAttribute(1, "type", "button");
                child.AddAttribute(2, "class", "row-actions-menu__item");
                child.AddAttribute(3, "role", "menuitem");
                child.AddAttribute(4, "onclick", EventCallback.Factory.Create(this, () => Chosen = true));
                child.AddContent(5, "Bekijk");
                child.CloseElement();
            }));
            builder.CloseComponent();
        }
    }

    private sealed class ApiHandler : HttpMessageHandler
    {
        public string? LastSearch { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri ?? new Uri("http://localhost/");
            var path = uri.AbsolutePath;
            var q = QueryValue(uri.Query, "q");
            if (path.Contains("/api/admin/search", StringComparison.Ordinal))
            {
                LastSearch = q;
                return Task.FromResult(Json("{\"users\":[],\"organisations\":[],\"vacancies\":[],\"invoices\":[],\"correlations\":[]}"));
            }

            if (path.Contains("/api/admin/companies", StringComparison.Ordinal))
            {
                var items = Filter(
                    q,
                    """{"id":"11111111-1111-1111-1111-111111111111","name":"Binckhorst","kvkNumber":"12345678","type":"Employer","status":"active"}""",
                    """{"id":"22222222-2222-2222-2222-222222222222","name":"Andere Zaak","kvkNumber":"87654321","type":"Employer","status":"active"}""");
                return Task.FromResult(Json($"{{\"items\":[{items}],\"page\":1,\"pageSize\":50,\"totalCount\":{Count(items)}}}"));
            }

            if (path.Contains("/api/admin/vacancies", StringComparison.Ordinal))
            {
                var items = Filter(
                    q,
                    Vacancy("33333333-3333-3333-3333-333333333333", "Kassamedewerker", "Binckhorst"),
                    Vacancy("44444444-4444-4444-4444-444444444444", "Magazijn", "Andere Zaak"));
                var response = Json($"[{items}]");
                response.Headers.TryAddWithoutValidation("X-Total-Count", Count(items).ToString());
                response.Headers.TryAddWithoutValidation("X-Active-Regular", Count(items).ToString());
                return Task.FromResult(response);
            }

            if (path.Contains("/api/admin/users", StringComparison.Ordinal))
            {
                var items = Filter(
                    q,
                    User("55555555-5555-5555-5555-555555555555", "Binckhorst Beheer", "binck@jobsy.local"),
                    User("66666666-6666-6666-6666-666666666666", "Andere Zaak", "andere@jobsy.local"));
                var count = Count(items);
                return Task.FromResult(Json(
                    $"{{\"aggregates\":{{\"byRole\":{{}},\"topCompanies\":[],\"activeCount\":{count},\"inactiveCount\":0,\"byRegistrationWeek\":[]}},\"items\":[{items}],\"page\":1,\"pageSize\":50,\"totalCount\":{count},\"masked\":true}}"));
            }

            if (path.Contains("/api/platform-logs", StringComparison.Ordinal))
            {
                var items = Filter(
                    q,
                    """{"id":"77777777-7777-7777-7777-777777777777","level":"Info","category":"App","message":"Alpha storing","createdAt":"2026-10-01T10:00:00Z"}""",
                    """{"id":"88888888-8888-8888-8888-888888888888","level":"Info","category":"App","message":"Beta regel","createdAt":"2026-10-01T11:00:00Z"}""");
                var response = Json($"[{items}]");
                response.Headers.TryAddWithoutValidation("X-Total-Count", Count(items).ToString());
                return Task.FromResult(response);
            }

            if (path.Contains("/api/admin/todo", StringComparison.Ordinal))
            {
                return Task.FromResult(Json("{}"));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private static string Filter(string? q, string first, string second)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return first + "," + second;
            }

            var hits = new List<string>();
            if (first.Contains(q, StringComparison.OrdinalIgnoreCase))
            {
                hits.Add(first);
            }

            if (second.Contains(q, StringComparison.OrdinalIgnoreCase))
            {
                hits.Add(second);
            }

            return string.Join(',', hits);
        }

        private static int Count(string items)
            => string.IsNullOrWhiteSpace(items) ? 0 : items.Split('{').Length - 1;

        private static string Vacancy(string id, string title, string company)
            => $$"""{"id":"{{id}}","title":"{{title}}","status":"Active","companyId":"11111111-1111-1111-1111-111111111111","companyName":"{{company}}","companyType":"Employer","startDate":"2026-01-01","endDate":"2026-02-01","createdVia":"Manual"}""";

        private static string User(string id, string name, string email)
            => $$"""{"id":"{{id}}","email":"{{email}}","fullName":"{{name}}","role":"Admin","isActive":true,"mfaStatus":"enrolled"}""";

        private static string? QueryValue(string query, string key)
        {
            foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var bits = part.Split('=', 2);
                if (string.Equals(Uri.UnescapeDataString(bits[0]), key, StringComparison.OrdinalIgnoreCase))
                {
                    return bits.Length == 2 ? Uri.UnescapeDataString(bits[1]) : "";
                }
            }

            return null;
        }

        private static HttpResponseMessage Json(string body)
            => new(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
    }
}
