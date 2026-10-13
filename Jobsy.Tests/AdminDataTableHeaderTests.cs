using Bunit;
using Jobsy.Web.Components.Admin.Ui;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public class AdminDataTableHeaderTests : BunitContext
{
    public AdminDataTableHeaderTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new HeaderFakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public void Gegevensinzage_keeps_who_headers_while_the_log_is_still_loading()
    {
        var cut = Render<AdminDataTable>(parameters => parameters
            .Add(table => table.KeepHeader, true)
            .Add(table => table.Loading, true)
            .Add(table => table.Empty, true)
            .Add(table => table.EmptyText, "Geen inzage-logs gevonden.")
            .Add(table => table.Header, Header));

        var markup = cut.Markup;
        Assert.Contains("<thead", markup, StringComparison.Ordinal);
        Assert.Contains(">Wie<", markup, StringComparison.Ordinal);
        Assert.Contains(">Over wie<", markup, StringComparison.Ordinal);
        Assert.Contains("Laden", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Other_lists_still_hide_the_table_while_loading()
    {
        var cut = Render<AdminDataTable>(parameters => parameters
            .Add(table => table.Loading, true)
            .Add(table => table.Header, Header));

        Assert.DoesNotContain("<thead", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Laden", cut.Markup, StringComparison.Ordinal);
    }

    private static void Header(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "th");
        builder.AddAttribute(1, "scope", "col");
        builder.AddContent(2, "Wie");
        builder.CloseElement();
        builder.OpenElement(3, "th");
        builder.AddAttribute(4, "scope", "col");
        builder.AddContent(5, "Over wie");
        builder.CloseElement();
    }

    private sealed class HeaderFakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(
                    [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Admin")],
                    "test"))));
    }
}
