namespace Jobsy.Core.Options;

/// <summary>
/// Banenkaart discovery index behaviour. Env: <c>VacancyIndex__IncludeTestData</c>.
/// </summary>
public sealed class VacancyIndexOptions
{
    public const string SectionName = "VacancyIndex";

    /// <summary>
    /// When true, <see cref="IsTestData"/> vacancies are indexed for discover/pins/matching.
    /// Default false (production). Acceptatie sets true so seed and test-account jobs stay visible.
    /// </summary>
    public bool IncludeTestData { get; set; }
}
