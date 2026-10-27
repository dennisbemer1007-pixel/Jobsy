namespace Jobsy.Web.Werkgever;

internal static class WerkgeverApplicationsListSsrPaths
{
    internal static bool IsListHostPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var p = path.Split('?', '#')[0].TrimEnd('/');
        return string.Equals(p, "/werkgever/sollicitaties", StringComparison.OrdinalIgnoreCase);
    }
}
