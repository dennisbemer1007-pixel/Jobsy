using Jobsy.Core.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Jobsy.Api.Swagger;

internal static class ExternalApiSwagger
{
    public const string DocumentName = "external";

    public static void Configure(SwaggerGenOptions options)
    {
        options.SwaggerDoc(DocumentName, new OpenApiInfo
        {
            Title = "Lobsy externe vacature-API",
            Version = "v1",
            Description =
                "Partner/ATS-API voor vacatures. Authenticatie via header " +
                $"`{ApiKeyAuthDefaults.HeaderName}`. Nieuwe vacatures komen binnen als concept; " +
                "publiceren (en tokenverbruik) gebeurt in Lobsy."
        });

        options.DocInclusionPredicate((docName, apiDesc) =>
        {
            if (!string.Equals(docName, DocumentName, StringComparison.Ordinal))
            {
                return false;
            }

            var path = apiDesc.RelativePath ?? string.Empty;
            return path.StartsWith("api/external/vacancies", StringComparison.OrdinalIgnoreCase);
        });

        options.AddSecurityDefinition(ApiKeyAuthDefaults.AuthenticationScheme, new OpenApiSecurityScheme
        {
            Name = ApiKeyAuthDefaults.HeaderName,
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Description = "Bedrijfs-API-key (formaat lobsy_…), één keer zichtbaar bij genereren of e-mail."
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = ApiKeyAuthDefaults.AuthenticationScheme
                    }
                },
                Array.Empty<string>()
            }
        });
    }
}
