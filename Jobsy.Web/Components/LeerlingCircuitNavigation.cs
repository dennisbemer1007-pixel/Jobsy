namespace Jobsy.Web.Components;

/// <summary>Pupil routes must never use the staff <c>RedirectToLogin</c>.</summary>
public static class LeerlingCircuitNavigation
{
    public static bool IsPupilPath(string? absoluteUri)
    {
        if (string.IsNullOrWhiteSpace(absoluteUri)
            || !Uri.TryCreate(absoluteUri, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var path = uri.AbsolutePath;
        return path.Equals("/leerling", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/leerling/", StringComparison.OrdinalIgnoreCase);
    }
}
