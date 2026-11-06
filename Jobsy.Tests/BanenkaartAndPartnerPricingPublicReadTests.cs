using System.Net;
using System.Net.Http.Json;
using Jobsy.Core.Interfaces;

namespace Jobsy.Tests;

/// <summary>
/// Public banenkaart + partner pricing reads depend on <see cref="Jobsy.Core.Features.RequiresFeatureAttribute"/>
/// loading <c>PlatformFeatureSettings</c> (including PassportPdfDnaBlocksEnabled from #692).
/// </summary>
public sealed class BanenkaartAndPartnerPricingPublicReadTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private readonly RoleFunctionalWebAppFactory _factory;

    public BanenkaartAndPartnerPricingPublicReadTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Anonymous_pins_and_partner_catalog_return_ok()
    {
        var client = _factory.CreateClient();

        var pins = await client.GetAsync("api/vacancies/pins");
        Assert.Equal(HttpStatusCode.OK, pins.StatusCode);

        var catalog = await client.GetAsync("api/sales-commercial/catalog");
        Assert.Equal(HttpStatusCode.OK, catalog.StatusCode);
        var body = await catalog.Content.ReadFromJsonAsync<PartnerSalesCatalogDto>();
        Assert.NotNull(body);
        Assert.True(body!.BaseTokenValueEuro > 0);
    }
}
