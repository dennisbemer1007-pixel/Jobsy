using System.Reflection;
using Jobsy.Web.Components.Pages.Ambassadeur;
using Microsoft.AspNetCore.Components;

namespace Jobsy.Tests.Sales;

/// <summary>
/// Parked Ambassadeur pages must still compile and remain discoverable so cleanup (09)
/// does not break a future re-enable. Runtime access stays behind AmbassadorsEnabled.
/// </summary>
public class AmbassadeurParkedSmokeTests
{
    [Fact]
    public void Ambassadeur_page_components_still_exist_and_are_routable_types()
    {
        var asm = typeof(Dashboard).Assembly;
        var pages = asm.GetTypes()
            .Where(t => t.Namespace == "Jobsy.Web.Components.Pages.Ambassadeur"
                        && typeof(ComponentBase).IsAssignableFrom(t))
            .ToList();

        Assert.Contains(pages, t => t.Name == "Dashboard");
        Assert.Contains(pages, t => t.Name == "Toolkit");
        Assert.Contains(pages, t => t.Name == "Finance");
        Assert.Contains(pages, t => t.Name == "Onboarding");
        Assert.Contains(pages, t => t.Name == "Landing");
        Assert.Contains(pages, t => t.Name == "PayoutCheckoutStub");

        foreach (var page in pages)
        {
            var routes = page.GetCustomAttributes(typeof(RouteAttribute), inherit: true);
            Assert.NotEmpty(routes);
        }
    }
}
