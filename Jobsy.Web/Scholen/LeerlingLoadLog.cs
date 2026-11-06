using System.Net;
using System.Net.Http.Json;
using Jobsy.Core.Diagnostics;
using Jobsy.Core.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Web.Scholen;

/// <summary>
/// Pupil page loads that fail in the circuit. Category <c>Leerling</c> so Systeemlogs can filter them.
/// No codes, names or answer text.
/// </summary>
public static class LeerlingLoadLog
{
    public const string Category = "Leerling";

    public static void Failed(
        IServiceProvider? services,
        string operation,
        Exception? exception,
        HttpStatusCode? status,
        bool giveUp)
    {
        if (services is null)
        {
            return;
        }

        var logger = services.GetService<ILoggerFactory>()?.CreateLogger(Category);
        logger?.LogWarning(
            exception,
            "Leerling {Operation} failed with status {Status} (giveUp {GiveUp})",
            operation,
            status?.ToString() ?? "none",
            giveUp);

        if (!giveUp)
        {
            return;
        }

        _ = ReportAsync(services, operation, exception, status);
    }

    private static async Task ReportAsync(
        IServiceProvider services,
        string operation,
        Exception? exception,
        HttpStatusCode? status)
    {
        try
        {
            var configuration = services.GetService<IConfiguration>();
            var http = services.GetService<IHttpClientFactory>();
            var baseUrl = configuration?["ApiBaseUrl"];
            var secret = configuration?[InternalClientIpHeaders.ConfigKey];
            if (http is null || string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(secret))
            {
                return;
            }

            var client = http.CreateClient();
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
            var message = $"Leerling {operation} failed with status {status?.ToString() ?? "none"}"
                          + (exception is null ? "" : $" ({exception.GetType().Name})");
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/platform-logs/errors")
            {
                Content = JsonContent.Create(new
                {
                    category = Category,
                    message,
                    supportCode = (string?)null,
                    detail = exception is null ? null : PlatformErrorDetail.Format(exception, operation)
                })
            };
            request.Headers.TryAddWithoutValidation(InternalClientIpHeaders.InternalSecretHeader, secret.Trim());
            using var response = await client.SendAsync(request);
        }
        catch (Exception reportEx)
        {
            var logger = services.GetService<ILoggerFactory>()?.CreateLogger(Category);
            logger?.LogWarning(reportEx, "Could not write Leerling load failure to Systeemlogs");
        }
    }
}
