using Jobsy.Core.Diagnostics;

namespace Jobsy.Web.Diagnostics;

/// <summary>
/// One support code per request (E2). The /Error page, inline error blocks and the 429 page
/// all read the same value so a visitor never sees two codes for one failure.
/// </summary>
public static class SupportCode
{
    public const string ItemsKey = "Jobsy.SupportCode";

    /// <summary>Sentry tag name support searches on (<c>support_code:LB-7Q3K</c>).</summary>
    public const string SentryTag = "support_code";

    public static string Create() => SupportCodeGenerator.Create();

    public static bool IsValid(string? code) => SupportCodeGenerator.IsValid(code);

    public static string GetOrCreate(HttpContext? http)
    {
        if (http is null)
        {
            return Create();
        }

        if (http.Items.TryGetValue(ItemsKey, out var existing) && existing is string code && code.Length > 0)
        {
            return code;
        }

        var created = Create();
        http.Items[ItemsKey] = created;
        return created;
    }

    /// <summary>The code already minted for this request, or null when nothing failed yet.</summary>
    public static string? Peek(HttpContext? http)
        => http?.Items.TryGetValue(ItemsKey, out var existing) == true && existing is string code
            ? code
            : null;
}
