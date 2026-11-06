using Microsoft.AspNetCore.Mvc.Testing;

namespace Jobsy.Tests.TestSupport;

/// <summary>Minimal base for illustrated capture host (no xUnit IAsyncLifetime).</summary>
public abstract class WebWebApplicationFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>;
