using Bunit;
using Microsoft.AspNetCore.Components;

namespace Jobsy.Tests.TestSupport;

/// <summary>
/// bUnit host for interactive server components. Call <see cref="EnableInteractiveServerRenderer"/>
/// at the end of the constructor after all <c>Services.Add*</c> calls (bUnit locks DI after renderer init).
/// </summary>
public class JobsyBunitContext : BunitContext
{
    protected void EnableInteractiveServerRenderer()
        => SetRendererInfo(new RendererInfo("Server", true));
}
