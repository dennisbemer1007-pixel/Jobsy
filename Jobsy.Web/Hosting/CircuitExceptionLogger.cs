using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Jobsy.Web.Hosting;

/// <summary>
/// Logs Blazor circuit connect/disconnect and unhandled circuit exceptions so
/// Lighthouse / mobile sessions that show “unhandled exception on the current circuit”
/// leave a server-side trail.
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
}
