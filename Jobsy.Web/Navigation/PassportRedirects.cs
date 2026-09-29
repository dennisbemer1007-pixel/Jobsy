using Microsoft.AspNetCore.WebUtilities;

namespace Jobsy.Web.Navigation;

/// <summary>
/// Passport ↔ classic profile redirect helpers.
/// <see cref="ClassicTabsUntilPhase4"/> is deleted in file 05 when Bewijzen/Gegevens land.
/// </summary>
public static class PassportRedirects
{
    public const string PassportPath = "/candidate/paspoort";
    public const string ClassicProfilePath = "/candidate/profile";

    /// <summary>
    /// Until Phase 4, these classic <c>?tab=</c> values stay on <c>/candidate/profile</c>
    /// so candidates can still reach their forms.
    /// </summary>
    public static readonly HashSet<string> ClassicTabsUntilPhase4 =
        new(StringComparer.OrdinalIgnoreCase)
        {
            CandidateKompasTabs.Profile
        };

    public static bool IsClassicTabUntilPhase4(string? kompasTab)
        => ClassicTabsUntilPhase4.Contains(CandidateKompasTabs.Normalize(kompasTab));

    /// <summary>
    /// Flag OFF: passport URL → classic profile with tab mapped back to Kompas ids.
    /// </summary>
    public static string ToClassicProfileUrl(string absoluteOrRelativeUri)
    {
        var tab = ReadTab(absoluteOrRelativeUri);
        var kompas = PassportTabs.ToKompasTab(tab);
        var returnUrl = ReadQuery(absoluteOrRelativeUri, "returnUrl");
        return BuildUrl(ClassicProfilePath, kompas, returnUrl);
    }

    /// <summary>
    /// Flag ON: classic profile URL → passport, unless transitional classic tab.
    /// Returns null when the caller should stay on the classic profile.
    /// </summary>
    public static string? TryToPassportUrl(string absoluteOrRelativeUri)
    {
        var kompasTab = CandidateKompasTabs.Normalize(ReadQuery(absoluteOrRelativeUri, "tab"));
        if (IsClassicTabUntilPhase4(kompasTab))
        {
            return null;
        }

        var passportTab = PassportTabs.FromKompasTab(kompasTab);
        var returnUrl = ReadQuery(absoluteOrRelativeUri, "returnUrl");
        return BuildUrl(PassportPath, passportTab, returnUrl);
    }

    public static string BuildPassportUrl(string tab, string? returnUrl = null)
        => BuildUrl(PassportPath, PassportTabs.Normalize(tab), returnUrl);

    public static string BuildClassicProfileUrl(string tab, string? returnUrl = null)
        => BuildUrl(ClassicProfilePath, CandidateKompasTabs.Normalize(tab), returnUrl);

    private static string BuildUrl(string path, string tab, string? returnUrl)
    {
        var qs = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["tab"] = tab
        };
        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            qs["returnUrl"] = returnUrl;
        }

        return QueryHelpers.AddQueryString(path, qs!);
    }

    private static string ReadTab(string uri)
        => PassportTabs.Normalize(ReadQuery(uri, "tab"));

    private static string? ReadQuery(string uri, string key)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            return null;
        }

        var queryIndex = uri.IndexOf('?', StringComparison.Ordinal);
        if (queryIndex < 0 || queryIndex >= uri.Length - 1)
        {
            return null;
        }

        var query = QueryHelpers.ParseQuery(uri[(queryIndex + 1)..]);
        return query.TryGetValue(key, out var values) ? values.FirstOrDefault() : null;
    }
}
