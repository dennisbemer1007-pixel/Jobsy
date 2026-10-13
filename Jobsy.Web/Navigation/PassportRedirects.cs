using Microsoft.AspNetCore.WebUtilities;

namespace Jobsy.Web.Navigation;

/// <summary>
/// Passport ↔ classic profile redirect helpers.
/// </summary>
public static class PassportRedirects
{
    public const string PassportPath = "/candidate/paspoort";
    public const string ClassicProfilePath = "/candidate/profile";

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
    /// Flag ON: classic profile URL → passport (including <c>tab=profile</c> → <c>tab=data</c>).
    /// </summary>
    public static string? TryToPassportUrl(string absoluteOrRelativeUri)
    {
        var raw = ReadQuery(absoluteOrRelativeUri, "tab");
        var passportTab = MapProfileTab(raw);
        var returnUrl = ReadQuery(absoluteOrRelativeUri, "returnUrl");
        return BuildUrl(PassportPath, passportTab, returnUrl);
    }

    /// <summary>
    /// Dutch bookmarks <c>carriere</c> and <c>bewijzen</c> are passport tabs.
    /// English <c>career</c> is not that bookmark and keeps the Kompas mapping.
    /// </summary>
    private static string MapProfileTab(string? raw)
    {
        var value = (raw ?? "").Trim().TrimStart('#').ToLowerInvariant();
        if (value is "carriere" or "carrière" or "loopbaan")
        {
            return PassportTabs.Career;
        }

        if (value is "bewijzen" or "bewijs" or "proof")
        {
            return PassportTabs.Proof;
        }

        return PassportTabs.FromKompasTab(CandidateKompasTabs.Normalize(raw));
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
