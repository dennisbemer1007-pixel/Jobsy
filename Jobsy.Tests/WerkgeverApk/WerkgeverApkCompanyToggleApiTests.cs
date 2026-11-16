using System.Net;
using System.Net.Http.Json;
using Jobsy.Api.Models;

namespace Jobsy.Tests.WerkgeverApk;

public sealed class WerkgeverApkCompanyToggleApiTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private readonly RoleFunctionalWebAppFactory _factory;

    public WerkgeverApkCompanyToggleApiTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Enterprise_manager_cannot_enable_loopbaan_apk_for_company()
    {
        var client = _factory.CreateClient();
        JobsyTestAuth.Authorize(client, _factory.EnterpriseId);

        var response = await client.PutAsJsonAsync(
            $"api/companies/{_factory.CompanyId:D}/loopbaan-apk",
            new UpdateLoopbaanApkCompanyRequest(true));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
