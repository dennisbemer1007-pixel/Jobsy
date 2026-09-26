using Microsoft.AspNetCore.Components.Server.Circuits;
using Sentry;

namespace Jobsy.Web.Hosting;

/// <summary>
/// Logs Blazor circuit connect/disconnect and unhandled inbound circuit exceptions so
/// mobile sessions that show “unhandled exception on the current circuit”
/// leave a server-side trail (and Sentry when configured).
/// </summary>
public sealed class CircuitExceptionLogger(ILogger<CircuitExceptionLogger> logger) : CircuitHandler
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
                logger.LogError(
                    ex,
                    "Unhandled Blazor circuit exception on {CircuitId}",
                    context.Circuit.Id);
                SentrySdk.CaptureException(ex);
                throw;
            }
        };
    }
}
