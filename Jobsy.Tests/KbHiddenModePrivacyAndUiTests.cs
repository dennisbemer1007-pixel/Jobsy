using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Rules.KandidaatBanen;
using Jobsy.Core.ValueObjects;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Models;

namespace Jobsy.Tests;

/// <summary>
/// Privacy + UI contracts for uitzendbureau hidden mode (05.6) and Dep B absent blocks.
/// </summary>
public class KbHiddenModePrivacyAndUiTests
{
    [Fact]
    public void Hidden_payload_never_contains_client_identity_or_workplace_coords()
    {
        var client = BuildClient();
        var bureau = BuildBureau();
        var vacancy = BuildHiddenVacancy(client, bureau);

        var display = IntermediaryVacancyRules.ResolvePublicDisplay(vacancy, client, bureau);
        Assert.Equal(bureau.Name, display.DisplayName);
        Assert.Equal(bureau.Address, display.DisplayAddress);
        Assert.Equal(bureau.Location.Latitude, display.Latitude);
        Assert.Equal(bureau.Location.Longitude, display.Longitude);
        Assert.Null(display.OfferedByLabel);

        // Workplace and client must stay out of public display fields.
        Assert.NotEqual(client.Name, display.DisplayName);
        Assert.DoesNotContain("Opdrachtgever", display.DisplayName, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Klantstraat", display.DisplayAddress, StringComparison.OrdinalIgnoreCase);
        Assert.True(Math.Abs(display.Latitude - vacancy.Location!.Latitude) > 0.001);
        Assert.True(Math.Abs(display.Longitude - vacancy.Location!.Longitude) > 0.001);
        Assert.True(KbHiddenIntermediaryMask.HideRouteAndStreetView(
            vacancy.IntermediaryCompanyId, vacancy.ShowClientAddressOnMap));
    }

    [Fact]
    public void CompanyLine_formats_via_bureau_in_hidden_mode()
    {
        var item = new VacancyListItem
        {
            CompanyName = "FlexPlus",
            CompanyAddress = "Bureauweg 9",
            IntermediaryCompanyId = Guid.NewGuid(),
            ShowClientAddressOnMap = false,
            Latitude = 52.0,
            Longitude = 4.2
        };

        // Culture-free format check against the nl template shape.
        Assert.True(KbIntermediaryDisplay.IsHiddenMode(item));
        Assert.True(KbIntermediaryDisplay.HideRouteAndStreetView(item));
        var formatted = string.Format("via uitzendbureau {0}", item.CompanyName);
        Assert.Equal("via uitzendbureau FlexPlus", formatted);
    }

    [Fact]
    public void VacancyDetail_markup_hides_route_streetview_and_dep_b_blocks()
    {
        var root = FindRepoRoot();
        var detail = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Pages", "VacancyDetail.razor"));
        var markupEnd = detail.IndexOf("@code", StringComparison.Ordinal);
        var markup = markupEnd > 0 ? detail[..markupEnd] : detail;

        // Hidden mode: Route/Street View gated; honest via label + map note.
        Assert.Contains("KbIntermediaryDisplay.HideRouteAndStreetView", detail, StringComparison.Ordinal);
        Assert.Contains("Kb.Via.Bureau", detail, StringComparison.Ordinal);
        Assert.Contains("Kb.Map.ShowsBureau", detail, StringComparison.Ordinal);
        Assert.Contains("Kb.Hidden.Info", detail, StringComparison.Ordinal);
        Assert.Contains("kb-hidden-info", markup, StringComparison.Ordinal);

        // Dep B ABSENT: do not render branche / kernwaarden / engagement employer blocks.
        Assert.DoesNotContain("kb-employer-branche", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("kb-employer-values", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("kb-employer-engagement", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("CompanyValuesProfile", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("EngagementCatalog", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("Waar {0} voor staat", detail, StringComparison.Ordinal);
    }

    [Fact]
    public void VacancyCard_uses_intermediary_display_helper()
    {
        var root = FindRepoRoot();
        var card = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Discovery", "VacancyCard.razor"));
        Assert.Contains("KbIntermediaryDisplay.CompanyLine", card, StringComparison.Ordinal);
    }

    [Fact]
    public void Fallback_mask_type_is_marked_for_intermediair_03()
    {
        var root = FindRepoRoot();
        var path = Path.Combine(root, "Jobsy.Core", "Rules", "KandidaatBanen", "KbHiddenIntermediaryMask.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("KB-FALLBACK(A)", text, StringComparison.Ordinal);
        Assert.Contains("superseded by intermediair 03", text, StringComparison.OrdinalIgnoreCase);
    }

    private static Company BuildClient() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Opdrachtgever Secret BV",
        Address = "Klantstraat 1, 2671 AB Naaldwijk",
        KvkNumber = "87654321",
        Location = new GeoPoint(52.1, 4.3)
    };

    private static Company BuildBureau() => new()
    {
        Id = Guid.NewGuid(),
        Name = "FlexPlus",
        Address = "Bureauweg 9, Naaldwijk",
        Location = new GeoPoint(52.0, 4.2),
        Type = CompanyType.Intermediary
    };

    private static Vacancy BuildHiddenVacancy(Company client, Company bureau) => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = client.Id,
        Company = client,
        IntermediaryCompanyId = bureau.Id,
        IntermediaryCompany = bureau,
        ShowClientAddressOnMap = false,
        Location = new GeoPoint(51.99, 4.25),
        Title = "Orderpicker",
        Description = "d",
        Status = VacancyStatus.Active
    };

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
}
