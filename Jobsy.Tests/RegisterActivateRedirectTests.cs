using Microsoft.AspNetCore.Mvc.Testing;

namespace Jobsy.Tests;

public class RegisterActivateRedirectTests : IClassFixture<RegisterActivateWebFactory>
{
    private readonly RegisterActivateWebFactory _factory;

    public RegisterActivateRedirectTests(RegisterActivateWebFactory factory) => _factory = factory;

    [Fact]
    public async Task Activate_without_token_redirects_to_register()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/register/activate");
        Assert.True(
            response.StatusCode is System.Net.HttpStatusCode.Redirect
                or System.Net.HttpStatusCode.RedirectKeepVerb
                or System.Net.HttpStatusCode.MovedPermanently
                or System.Net.HttpStatusCode.SeeOther
                or System.Net.HttpStatusCode.Found,
            $"Expected redirect, got {(int)response.StatusCode}");
        Assert.Equal("/register", response.Headers.Location?.OriginalString);
    }
}

public sealed class RegisterActivateWebFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>;
