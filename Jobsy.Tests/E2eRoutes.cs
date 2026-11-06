using Jobsy.Web.KandidaatBanen;

namespace Jobsy.Tests;

/// <summary>Shared paths for Playwright / HTTP smoke that need candidate job surfaces.</summary>
public static class E2eRoutes
{
    /// <summary>Canonical banenkaart URL (landing 04+). Aligns with <see cref="KbRoutes.Map"/>.</summary>
    public const string Banenkaart = "/banenkaart";

    public static string Applications => KbRoutes.Applications;
    public static string Saved => KbRoutes.Saved;
    public const string Match = "/candidate/match";
}
