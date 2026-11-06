namespace Jobsy.Web.Scholen;

internal static class PupilLeerlingClientJson
{
    internal static bool WantsJson(HttpContext http)
    {
        if (http.Request.Headers.TryGetValue("Accept", out var accept))
        {
            foreach (var value in accept)
            {
                if (!string.IsNullOrEmpty(value)
                    && value.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return string.Equals(http.Request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed record PupilReisAnswerSaveJson(
    bool Ok,
    string? RedirectPath,
    string? Error,
    int Status,
    string? AntiforgeryField = null,
    string? AntiforgeryToken = null);
