namespace Jobsy.Core.Legal;

/// <summary>
/// Builds the "where is your data" facts from the processor list.
/// Copy lives in the five locale strings; this type only picks the names.
/// </summary>
public static class DataLocationText
{
    public static IReadOnlyList<string> AmericanCompanyNames(IReadOnlyList<LegalProcessor> processors)
        => processors.Where(p => p.IsAmericanCompany).Select(p => p.Name).ToList();

    public static IReadOnlyList<string> SwissCompanyNames(IReadOnlyList<LegalProcessor> processors)
        => processors.Where(p => p.CompanyHq == ProcessorRegion.Switzerland).Select(p => p.Name).ToList();

    public static bool AllCompaniesAreOutsideTheUnitedStates(IReadOnlyList<LegalProcessor> processors)
        => processors.Count > 0 && processors.All(p => !p.IsAmericanCompany);

    /// <summary>"A, B en C" / "A, B and C" / Arabic comma. One name is returned as-is.</summary>
    public static string JoinNames(IReadOnlyList<string> names, string? language)
    {
        if (names.Count == 0)
        {
            return string.Empty;
        }

        if (names.Count == 1)
        {
            return names[0];
        }

        var lang = (language ?? "nl").Trim().ToLowerInvariant();
        var (separator, conjunction) = lang switch
        {
            "en" => (", ", "and"),
            "pl" => (", ", "i"),
            "ro" => (", ", "și"),
            "ar" => ("، ", "و"),
            _ => (", ", "en")
        };

        return string.Join(separator, names.Take(names.Count - 1))
            + " " + conjunction + " "
            + names[^1];
    }
}
