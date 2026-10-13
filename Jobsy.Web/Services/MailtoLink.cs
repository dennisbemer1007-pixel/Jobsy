namespace Jobsy.Web.Services;

/// <summary>Builds mailto: URLs with a single Uri.EscapeDataString pass (real newlines in the body).</summary>
public static class MailtoLink
{
    public static string Build(string subject, string body)
    {
        var s = Uri.EscapeDataString(subject ?? string.Empty);
        var b = Uri.EscapeDataString(body ?? string.Empty);
        return $"mailto:?subject={s}&body={b}";
    }
}
