using Jobsy.Core.Dev;

namespace Jobsy.Web.Hosting;

/// <summary>Development-only helpers for Playwright E2E (never enabled on Render).</summary>
public static class E2eDevExtensions
{
    public const string LastEmailCodePath = "/__test/last-email-code";

    public static WebApplication UseE2eDevPaths(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment() || !DevEmailCodeCapture.Enabled)
        {
            return app;
        }

        app.MapGet(LastEmailCodePath, (string? email) =>
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Results.BadRequest(new { message = "email required" });
            }

            return DevEmailCodeCapture.TryGet(email, out var code)
                ? Results.Ok(new { email = email.Trim().ToLowerInvariant(), code })
                : Results.NotFound(new { message = "no code captured yet" });
        });

        return app;
    }
}
