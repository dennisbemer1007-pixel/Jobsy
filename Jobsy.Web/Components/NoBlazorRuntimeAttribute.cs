namespace Jobsy.Web.Components;

/// <summary>
/// Marks a static SSR page that must not load <c>blazor.web.js</c> / SignalR
/// (landing, /werkgevers, /scholen). Pair with <c>[ExcludeFromInteractiveRouting]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class NoBlazorRuntimeAttribute : Attribute;
