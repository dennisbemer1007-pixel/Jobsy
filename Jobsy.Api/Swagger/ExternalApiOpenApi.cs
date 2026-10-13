using Jobsy.Core.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Jobsy.Api.Swagger;

/// <summary>
/// Built-in OpenAPI document for the partner/ATS vacancy API (replaces Swashbuckle).
/// </summary>
internal static class ExternalApiOpenApi
{
    public const string DocumentName = "external";

    public static void Configure(OpenApiOptions options)
    {
        options.ShouldInclude = desc =>
        {
            var path = desc.RelativePath ?? string.Empty;
            return path.StartsWith("api/external/vacancies", StringComparison.OrdinalIgnoreCase);
        };

        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            document.Info = new OpenApiInfo
            {
                Title = "Lobsy externe vacature-API",
                Version = "v1",
                Description =
                    "Partner/ATS-API voor vacatures. Authenticatie via header " +
                    $"`{ApiKeyAuthDefaults.HeaderName}`. Nieuwe vacatures komen binnen als concept; " +
                    "publiceren (en tokenverbruik) gebeurt in Lobsy."
            };

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>(StringComparer.Ordinal);
            document.Components.SecuritySchemes[ApiKeyAuthDefaults.AuthenticationScheme] = new OpenApiSecurityScheme
            {
                Name = ApiKeyAuthDefaults.HeaderName,
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Description = "Bedrijfs-API-key (formaat lobsy_…), één keer zichtbaar bij genereren of e-mail."
            };

            document.Security ??= [];
            var requirement = new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference(ApiKeyAuthDefaults.AuthenticationScheme, document),
                    new List<string>()
                }
            };
            document.Security.Add(requirement);

            return Task.CompletedTask;
        });
    }
}
