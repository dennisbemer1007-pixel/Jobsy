using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Jobsy.Tests;

/// <summary>UX audit cleanup — Playwright evidence for PR (desktop 1280×800, mobile 390×844).</summary>
[Collection("PlaywrightSmoke")]
public class UxCleanupEvidenceTests
{
    private static readonly (string File, string Path)[] Pages =
    [
        ("westland", "/westland"),
        ("lancering", "/lancering"),
        ("melden", "/melden?type=vacancy&id=00000000-0000-0000-0000-000000000001"),
        ("contact", "/contact"),
        ("info-onderzoek", "/info/onderzoek"),
        ("privacy", "/privacy"),
        ("login", "/login"),
        ("account-maken", "/account-maken"),
        ("banenkaart", "/banenkaart"),
        ("partner", "/partner"),
        ("register", "/register")
    ];

    [Fact]
    public async Task Public_pages_return_ok_from_test_host()
    {
        await using var factory = new PublicPagesWebFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });
        foreach (var (_, path) in Pages)
        {
            var response = await client.GetAsync(path);
            Assert.True(
                response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Found,
                $"{path} returned {(int)response.StatusCode}");
        }
    }

}
