namespace Jobsy.Api;

/// <summary>Marker type so tests can host Jobsy.Api via <c>WebApplicationFactory&lt;ApiAssemblyMarker&gt;</c>.
/// Required on .NET 10+: both Api and Web emit a public top-level <c>Program</c>, so bare <c>Program</c> is ambiguous in Jobsy.Tests.</summary>
public sealed class ApiAssemblyMarker;
