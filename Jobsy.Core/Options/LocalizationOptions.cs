namespace Jobsy.Core.Options;

/// <summary>
/// Which UI cultures are offered at runtime (pickers, cookies, API <c>lang</c>).
/// Translation catalogs for other codes stay in the repo; set
/// <c>Localization:EnabledCultures</c> (env <c>Localization__EnabledCultures</c>) to re-enable them.
/// </summary>
public sealed class LocalizationOptions
{
    public const string SectionName = "Localization";

    /// <summary>Comma-separated ISO 639-1 codes. Default at launch: Dutch and English only.</summary>
    public const string DefaultEnabledCultures = "nl,en";

    public string EnabledCultures { get; set; } = DefaultEnabledCultures;
}
