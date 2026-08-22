using System.Security.Cryptography;

namespace Jobsy.Web.Security;

/// <summary>
/// Per-request CSP nonce (hex, 128 bits). Stored on <see cref="HttpContext.Items"/>
/// so the header and <c>nonce</c> attributes on this response stay in lockstep.
/// </summary>
public static class CspNonce
{
    public const string HttpContextItemKey = "Jobsy.CspNonce";

    public static string Create() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

    public static string GetOrCreate(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (TryGet(context) is { Length: > 0 } existing)
        {
            return existing;
        }

        var nonce = Create();
        context.Items[HttpContextItemKey] = nonce;
        return nonce;
    }

    public static string Get(HttpContext? context) => TryGet(context) ?? string.Empty;

    public static string? TryGet(HttpContext? context)
        => context?.Items.TryGetValue(HttpContextItemKey, out var value) == true
           && value is string nonce
           && nonce.Length > 0
            ? nonce
            : null;
}
