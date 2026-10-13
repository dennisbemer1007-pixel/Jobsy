using Bunit;
using Jobsy.Web.Components.Account;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public class ComebackReminderBunitTests : BunitContext
{
    public ComebackReminderBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new Auth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public void WhatsApp_fields_stay_hidden_until_the_channel_is_available()
    {
        var hidden = Render<ComebackWhatsAppFields>(p => p.Add(c => c.Available, false));
        Assert.DoesNotContain("data-comeback-whatsapp", hidden.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("whatsapp_phone", hidden.Markup, StringComparison.Ordinal);

        var shown = Render<ComebackWhatsAppFields>(p => p
            .Add(c => c.Available, true)
            .Add(c => c.OptedIn, false)
            .Add(c => c.Phone, "0612345678"));
        Assert.Contains("data-comeback-whatsapp", shown.Markup, StringComparison.Ordinal);
        Assert.Contains("Herinnering via WhatsApp", shown.Markup, StringComparison.Ordinal);
        Assert.Contains("name=\"whatsapp_phone\"", shown.Markup, StringComparison.Ordinal);
        Assert.Contains("0612345678", shown.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_requires_an_audit_reason_to_change_the_whatsapp_flag()
    {
        var entry = Jobsy.Web.Admin.PlatformSettingsCatalog.Entries
            .Single(e => e.Key == "WhatsAppRemindersEnabled");
        Assert.True(entry.ConfirmOnChange);
        Assert.Equal("AdminSettings.WhatsAppReminders.Enabled.Title", entry.TitleKey);
    }

    private sealed class Auth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal()));
    }
}
