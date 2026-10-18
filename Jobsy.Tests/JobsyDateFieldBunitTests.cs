using AngleSharp.Dom;
using Bunit;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Shared;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public sealed class JobsyDateFieldBunitTests : BunitContext
{
    public JobsyDateFieldBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth("Candidate"));
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public void Typing_digits_applies_mask_and_binds_value()
    {
        var current = default(DateOnly?);
        var cut = Render<JobsyDateField>(parameters => parameters
            .Add(p => p.Constraint, JobsyDateConstraint.CandidateBirthDate)
            .Add(p => p.Value, current)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<DateOnly?>(this, v => current = v))
            .Add(p => p.TestId, "birth"));

        var input = cut.Find(".jobsy-date-field__text");
        input.Input("18101985");
        Assert.Equal("18-10-1985", input.GetAttribute("value"));
        Assert.Equal(new DateOnly(1985, 10, 18), current);
    }

    [Fact]
    public void Invalid_date_shows_inline_error_after_blur()
    {
        var cut = Render<JobsyDateField>(parameters => parameters
            .Add(p => p.Constraint, JobsyDateConstraint.CandidateBirthDate)
            .Add(p => p.TestId, "birth"));

        var input = cut.Find(".jobsy-date-field__text");
        input.Input("31-02-2000");
        input.Blur();
        var error = cut.Find(".jobsy-date-field__error");
        Assert.Contains("geldige datum", error.TextContent, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("true", input.GetAttribute("aria-invalid"));
    }

    [Fact]
    public void Uses_numeric_inputmode_and_calendar_button()
    {
        var cut = Render<JobsyDateField>(parameters => parameters
            .Add(p => p.TestId, "date"));
        var input = cut.Find(".jobsy-date-field__text");
        Assert.Equal("numeric", input.GetAttribute("inputmode"));
        Assert.NotNull(cut.Find(".jobsy-date-field__picker-btn"));
    }

    [Fact]
    public void Month_mode_masks_typed_digits_and_emits_yyyy_mm()
    {
        string? stored = null;
        var cut = Render<JobsyDateField>(parameters => parameters
            .Add(p => p.Kind, JobsyDatePickerKind.Month)
            .Add(p => p.Constraint, JobsyDateConstraint.WorkHistoryMonth)
            .Add(p => p.MonthValue, stored)
            .Add(p => p.MonthValueChanged, EventCallback.Factory.Create<string?>(this, v => stored = v)));

        var input = cut.Find(".jobsy-date-field__text");
        input.Input("102022");
        Assert.Equal("10-2022", input.GetAttribute("value"));
        Assert.Equal("2022-10", stored);
    }

    [Fact]
    public void Month_end_before_start_shows_validation_on_blur()
    {
        var cut = Render<JobsyDateField>(parameters => parameters
            .Add(p => p.Kind, JobsyDatePickerKind.Month)
            .Add(p => p.Constraint, JobsyDateConstraint.WorkHistoryMonth)
            .Add(p => p.ReferenceMonth, "2022-06"));

        var input = cut.Find(".jobsy-date-field__text");
        input.Input("01-2022");
        input.Blur();
        var error = cut.Find(".jobsy-date-field__error");
        Assert.Contains("startmaand", error.TextContent, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeAuth(string role) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(
                    [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role)],
                    "test"))));
    }
}
