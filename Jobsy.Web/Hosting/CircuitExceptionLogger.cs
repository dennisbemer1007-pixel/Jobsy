using System.Net.Http.Json;
using Jobsy.Core.Diagnostics;
using Jobsy.Core.Security;
using Jobsy.Web.Diagnostics;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Sentry;

namespace Jobsy.Web.Hosting;

/// <summary>
/// Logs Blazor circuit connect/disconnect and unhandled inbound circuit exceptions so
/// mobile sessions that show “unhandled exception on the current circuit”
/// leave a server-side trail (and Sentry when configured).
/// </summary>
public sealed class CircuitExceptionLogger(
    ILogger<CircuitExceptionLogger> logger,
    IHttpClientFactory http,
    IConfiguration configuration) : CircuitHandler
{
    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        logger.LogInformation("Blazor circuit opened {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        logger.LogInformation("Blazor circuit closed {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        logger.LogWarning("Blazor circuit connection down {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        logger.LogInformation("Blazor circuit connection up {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
        Func<CircuitInboundActivityContext, Task> next)
    {
        return async context =>
        {
            try
            {
                await next(context);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var supportCode = SupportCode.Create();
                logger.LogError(
                    ex,
                    "Unhandled Blazor circuit exception {SupportCode} on {CircuitId}",
                    supportCode,
                    context.Circuit.Id);
                SentrySdk.CaptureException(ex);
                await ReportPlatformAsync(ex, supportCode, context.Circuit.Id);
                throw;
            }
        };
    }

    private async Task ReportPlatformAsync(Exception ex, string supportCode, string circuitId)
    {
        var baseUrl = configuration["ApiBaseUrl"];
        var secret = configuration[InternalClientIpHeaders.ConfigKey];
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(secret))
        {
            return;
        }

        try
        {
            var client = http.CreateClient();
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/platform-logs/errors")
            {
                Content = JsonContent.Create(new
                {
                    category = "Blazor",
                    message = ex.GetType().Name,
                    supportCode,
                    detail = PlatformErrorDetail.Format(ex, circuitId)
                })
            };
            request.Headers.TryAddWithoutValidation(InternalClientIpHeaders.InternalSecretHeader, secret.Trim());
            using var response = await client.SendAsync(request);
        }
        catch (Exception reportEx)
        {
            logger.LogWarning(reportEx, "Could not write circuit exception {SupportCode} to Systeemlogs", supportCode);
        }
    }
}
