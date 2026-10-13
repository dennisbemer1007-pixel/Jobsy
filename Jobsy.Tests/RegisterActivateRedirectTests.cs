using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Jobsy.Tests;

public class RegisterActivateRedirectTests : IClassFixture<RegisterActivateWebFactory>
{
    private readonly RegisterActivateWebFactory _factory;

    public RegisterActivateRedirectTests(RegisterActivateWebFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/register/activate")]
    [InlineData("/register/activate?token=abc")]
    [InlineData("/register/activate?token=abc&x=1")]
    public async Task Activate_urls_permanently_redirect_to_register(string path)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("/register", response.Headers.Location?.ToString());
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("abc", body, StringComparison.Ordinal);
        Assert.DoesNotContain("token=", body, StringComparison.OrdinalIgnoreCase);
    }
}

public class RegisterActivateRemovedTests
{
    [Fact]
    public void No_blazor_route_for_register_activate()
    {
        var root = FindRepoRoot();
        var pages = Path.Combine(root, "Jobsy.Web", "Components", "Pages");
        Assert.False(File.Exists(Path.Combine(pages, "RegisterActivate.razor")));
        foreach (var file in Directory.GetFiles(pages, "*.razor", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("@page \"/register/activate\"", text, StringComparison.Ordinal);
        }

        var service = File.ReadAllText(Path.Combine(root, "Jobsy.Infrastructure", "Services", "CompanyRegistrationService.cs"));
        Assert.DoesNotContain("BuildActivationUrl", service, StringComparison.Ordinal);

        var controller = File.ReadAllText(Path.Combine(root, "Jobsy.Api", "Controllers", "RegistrationController.cs"));
        Assert.DoesNotContain("StubActivation", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("stub-activation", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpPost(\"activate\")", controller, StringComparison.Ordinal);
    }

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

public sealed class RegisterActivateWebFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>;
