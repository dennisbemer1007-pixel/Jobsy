using Jobsy.Web.Auth;
using Microsoft.IdentityModel.Tokens;

namespace Jobsy.Tests;

public class EntraOidcAudienceDiagnosticsTests
{
    [Fact]
    public void ResolveExpectedAudiences_prefers_configured_client_id()
    {
        var parameters = new TokenValidationParameters
        {
            ValidAudience = "pending",
            ValidAudiences = new[] { "pending" }
        };

        var expected = EntraOidcAudienceDiagnostics.ResolveExpectedAudiences(
            "070ee028-600b-4f3a-a555-f86ccabd343c",
            parameters);

        Assert.Contains("070ee028-600b-4f3a-a555-f86ccabd343c", expected);
    }

    [Fact]
    public void ApplyAudienceValidation_overwrites_pending_audience()
    {
        var parameters = new TokenValidationParameters
        {
            ValidAudience = "pending",
            ValidAudiences = new[] { "pending" }
        };

        EntraOidcOptionsApplier.ApplyAudienceValidation(parameters, "070ee028-600b-4f3a-a555-f86ccabd343c");

        Assert.Equal("070ee028-600b-4f3a-a555-f86ccabd343c", parameters.ValidAudience);
        Assert.Equal(new[] { "070ee028-600b-4f3a-a555-f86ccabd343c" }, parameters.ValidAudiences);
    }
}
