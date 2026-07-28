using Jobsy.Web.Auth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Jobsy.Tests;

public class EntraOidcOptionsApplierTests
{
    [Fact]
    public void Apply_replaces_pending_client_and_refreshes_metadata()
    {
        var options = new OpenIdConnectOptions
        {
            Authority = "https://login.microsoftonline.com/common/v2.0",
            MetadataAddress = "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration",
            ClientId = "pending",
            ClientSecret = "pending"
        };

        EntraOidcOptionsApplier.Apply(
            options,
            new ExternalOAuthCredentials(
                "real-client-id",
                "real-client-secret",
                "11111111-2222-3333-4444-555555555555"));

        Assert.Equal("real-client-id", options.ClientId);
        Assert.Equal("real-client-secret", options.ClientSecret);
        Assert.Equal(
            "https://login.microsoftonline.com/11111111-2222-3333-4444-555555555555/v2.0",
            options.Authority);
        Assert.NotNull(options.ConfigurationManager);
        Assert.Null(options.Configuration);
    }

    [Theory]
    [InlineData("https://login.microsoftonline.com/11111111-2222-3333-4444-555555555555/v2.0")]
    [InlineData("https://sts.windows.net/11111111-2222-3333-4444-555555555555/")]
    public void ValidateMicrosoftIssuer_accepts_entra_issuers(string issuer)
    {
        var result = EntraOidcOptionsApplier.ValidateMicrosoftIssuer(
            issuer,
            new DummyToken(),
            new TokenValidationParameters());

        Assert.Equal(issuer, result);
    }

    [Fact]
    public void ValidateMicrosoftIssuer_rejects_unknown_issuer()
    {
        Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
            EntraOidcOptionsApplier.ValidateMicrosoftIssuer(
                "https://evil.example/issuer",
                new DummyToken(),
                new TokenValidationParameters()));
    }

    private sealed class DummyToken : SecurityToken
    {
        public override string Id => "dummy";
        public override string Issuer => "dummy";
        public override SecurityKey SecurityKey => null!;
        public override SecurityKey SigningKey { get; set; } = null!;
        public override DateTime ValidFrom => DateTime.UtcNow.AddMinutes(-5);
        public override DateTime ValidTo => DateTime.UtcNow.AddMinutes(5);
    }
}
