namespace Jobsy.Web.Features;

/// <summary>
/// Local employers-on check for pages/endpoints until werkgevers-actief provides <c>RequiresFeature</c>.
/// Replace with <c>[RequiresFeature(PlatformFeature.Employers)]</c> when that stack lands
/// (see <c>docs/feature-flags-landing-followup.md</c>).
/// </summary>
public static class EmployersGate
{
    public static ValueTask<bool> IsEnabledAsync(IEmployersSwitch employers, CancellationToken ct = default)
        => employers.IsEnabledAsync(ct);

    /// <summary>Redirects to <paramref name="fallbackPath"/> when employers are OFF.</summary>
    public static async Task<bool> AllowOrRedirectAsync(
        HttpContext http,
        IEmployersSwitch employers,
        string fallbackPath = "/",
        CancellationToken ct = default)
    {
        if (await employers.IsEnabledAsync(ct))
        {
            return true;
        }

        http.Response.Redirect(string.IsNullOrWhiteSpace(fallbackPath) ? "/" : fallbackPath);
        return false;
    }
}
