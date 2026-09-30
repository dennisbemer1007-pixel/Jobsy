namespace Jobsy.Web.KandidaatBanen;

/// <summary>
/// Candidate job route constants for the kandidaat-banen stack.
/// Prefer these over string literals in stack-touched files.
/// </summary>
public static class KbRoutes
{
    // KB-FALLBACK(E): PublicRoutes.Banenkaart (landing 04)
    public const string Map = "/";

    public const string Applications = "/candidate/applications";
    public const string Saved = "/candidate/liked";
    public const string Shared = "/candidate/shared";

    /// <summary>
    /// Entry for completing culture/values tests ("Maak je paspoort af").
    /// Paspoort 02 not landed — hub is today's <c>/profiel</c>; culture/values stay at
    /// <c>/candidate/culture</c> and <c>/candidate/values</c>.
    /// </summary>
    public const string PaspoortTests = "/profiel";

    public const string CultureTest = "/candidate/culture";
    public const string ValuesTest = "/candidate/values";
}
