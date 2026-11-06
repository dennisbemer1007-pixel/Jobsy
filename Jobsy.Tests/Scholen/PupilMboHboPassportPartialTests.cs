using System.Net;
using System.Net.Http.Json;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.MboHbo;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Scholen;
using Jobsy.Tests.TestSupport;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests.Scholen;

public sealed class PupilMboHboPassportPartialTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private readonly RoleFunctionalWebAppFactory _factory;

    public PupilMboHboPassportPartialTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Partial_mbo_passport_api_returns_200_with_completed_sections_only()
    {
        await EnableSchoolsAsync(true);
        var (schoolId, classId, plainCode) = await SeedPartialMboCodeAsync();
        using var client = _factory.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = true });

        var login = await client.PostAsJsonAsync(
            "api/pupil/login",
            new PupilLoginRequest(schoolId, classId, plainCode));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var response = await client.GetAsync("api/pupil/mbo-hbo/passport");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<PupilMboHboPassportDto>();
        Assert.NotNull(dto);
        Assert.False(dto!.Unlocked);
        Assert.NotEmpty(dto.Sections);
        Assert.True(dto.Sections.Count < 5);
        Assert.NotNull(dto.IncompleteHint);
        Assert.NotNull(dto.MissingBlockTitles);
        Assert.NotEmpty(dto.MissingBlockTitles!);
    }

    [Fact]
    public void Paspoort_ssr_for_partial_progress_shows_incomplete_hint_and_hides_pdf()
    {
        var culture = NlCulture();
        var model = new PupilMboHboPassportDto(
            "M2",
            "ABC-123",
            "Demo School",
            Unlocked: false,
            [new PupilMboHboPassportSectionDto("Vakkenpakket", "<p>Je vakken.</p>")],
            "Dit is een tussentijds beeld.",
            "Hier zie je wat er al klaar is. Rond de rest van je testen af.",
            ["Beroepentest", "Werkwaarden & werkcultuur"]);
        var html = PupilMboHboSsrHtml.Paspoort(model, culture, "/leerling/pdf", pdfNotice: null);
        Assert.Contains("data-testid=\"mhbo-paspoort-incomplete\"", html, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"mhbo-paspoort-missing\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-testid=\"mhbo-download-pdf\"", html, StringComparison.Ordinal);
    }

    private async Task<(Guid SchoolId, Guid ClassId, string Plain)> SeedPartialMboCodeAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var codes = scope.ServiceProvider.GetRequiredService<IPupilCodeService>();
        var schoolId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        db.Schools.Add(new School
        {
            Id = schoolId,
            Name = "Partial Passport School",
            City = "Utrecht",
            AllowedEmailDomains = "[\"partial.test\"]",
            IsActive = true,
            ProcessorAgreementSignedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            ProcessorAgreementVersion = "1",
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = _factory.AdminId
        });
        db.SchoolClasses.Add(new SchoolClass
        {
            Id = classId,
            SchoolId = schoolId,
            Name = "M2PART",
            Level = SchoolLevel.Mbo2,
            Year = 2,
            QuestionSet = PupilQuestionSet.MboHbo,
            SchoolYearStart = SchoolYear.Current(DateOnly.FromDateTime(DateTime.UtcNow)),
            PupilCount = 1,
            TestWindow = TestWindowState.Open,
            ParentalInfoConfirmedAtUtc = DateTime.UtcNow,
            ParentalInfoConfirmedByUserId = _factory.AdminId,
            ParentalInfoTextVersion = "1",
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var cls = await db.SchoolClasses.SingleAsync(c => c.Id == classId);
        var generated = await codes.GenerateAsync(1, cls);
        var code = generated[0];
        var plain = codes.Unprotect(code.CodeProtected)!;

        var state = new PupilMboHboProgressState();
        var now = DateTime.UtcNow.ToString("O");
        state.BlocksCompleted[PupilMboHboProgressState.BlockKey(PupilMboHboBlock.Vakken)] = now;
        state.BlocksCompleted[PupilMboHboProgressState.BlockKey(PupilMboHboBlock.Interesse)] = now;

        db.PupilProgresses.Add(new PupilProgress
        {
            PupilCodeId = code.Id,
            MboHboProgressJson = state.ToJson(),
            StartedAtUtc = DateTime.UtcNow.AddHours(-1),
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return (schoolId, classId, plain);
    }

    private async Task EnableSchoolsAsync(bool enabled)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var row = await db.PlatformFeatureSettings.FirstOrDefaultAsync();
        if (row is null)
        {
            db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
            {
                Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                SchoolsEnabled = enabled
            });
        }
        else
        {
            row.SchoolsEnabled = enabled;
        }

        await db.SaveChangesAsync();
    }

    private static CultureState NlCulture()
    {
        var services = new ServiceCollection();
        services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuth());
        services.AddSingleton<IJSRuntime, NoJsRuntime>();
        services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        return services.BuildServiceProvider().GetRequiredService<CultureState>();
    }

    private sealed class AnonymousAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal()));
    }

    private sealed class NoJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => default;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => default;
    }
}
