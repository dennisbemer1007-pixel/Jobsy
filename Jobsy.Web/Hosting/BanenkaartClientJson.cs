namespace Jobsy.Web.Hosting;

internal static class BanenkaartClientJson
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
