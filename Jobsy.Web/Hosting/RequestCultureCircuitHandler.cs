using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Jobsy.Web.Hosting;

/// <summary>
/// Re-applies this circuit's culture on every inbound activity.
/// Culture does not flow across Blazor's thread-pool hops, and it must not
/// be stored on <see cref="System.Globalization.CultureInfo.DefaultThreadCurrentCulture"/>.
/// </summary>
public sealed class RequestCultureCircuitHandler(CultureState culture) : CircuitHandler
{
    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
        Func<CircuitInboundActivityContext, Task> next)
        => async context =>
        {
            culture.Reapply();
            await next(context);
        };
}
