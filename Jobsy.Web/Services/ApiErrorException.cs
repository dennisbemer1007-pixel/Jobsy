using System.Net;
using System.Text.Json;

namespace Jobsy.Web.Services;

/// <summary>
/// A failed API call in the shape the UI can act on (errors 04 §04.5): a machine-readable
/// <see cref="Code"/>, the <c>supportCode</c> the API minted, and — only when the API marked it
/// as written for users — a message that may be shown as-is.
/// <para>
/// Never put the raw response body in <see cref="UserMessage"/>: that is how internal detail
/// reaches visitors. Map <see cref="Code"/> to a catalog key via
/// <see cref="UserFacingError"/> instead.
/// </para>
/// </summary>
public sealed class ApiErrorException : Exception
{
    public const string RateLimited = "rate_limited";
    public const string NotFound = "not_found";
    public const string Forbidden = "forbidden";
    public const string Validation = "validation";
    public const string Maintenance = "maintenance";
    public const string Unknown = "unknown";

    public ApiErrorException(
        string code,
        int? statusCode = null,
        string? supportCode = null,
        int? retryAfterSeconds = null,
        string? userMessage = null)
        : base("API call failed with code " + code)
    {
        Code = code;
        StatusCode = statusCode;
        SupportCode = supportCode;
        RetryAfterSeconds = retryAfterSeconds;
        UserMessage = userMessage;
    }

    public string Code { get; }

    public int? StatusCode { get; }

    /// <summary>The <c>LB-XXXX</c> code support can look the failure up with (E2).</summary>
    public string? SupportCode { get; }

    public int? RetryAfterSeconds { get; }

    /// <summary>
    /// A validation message the API wrote for the visitor (ProblemDetails <c>detail</c> on a 400
    /// with <c>userMessage: true</c>). Null for everything else.
    /// </summary>
    public string? UserMessage { get; }

    public static ApiErrorException TooManyRequests(int retryAfterSeconds, string? supportCode = null)
        => new(RateLimited, 429, supportCode, retryAfterSeconds);

    /// <summary>Reads the ProblemDetails body of a failed response; never throws.</summary>
    public static async Task<ApiErrorException> FromResponseAsync(
        HttpResponseMessage response,
        CancellationToken ct = default)
    {
        var status = (int)response.StatusCode;
        var retryAfter = ReadRetryAfter(response);
        string? code = null;
        string? supportCode = null;
        string? userMessage = null;

        try
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!string.IsNullOrWhiteSpace(body))
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    code = ReadString(root, "code") ?? ReadString(root, "error");
                    if (string.Equals(ReadString(root, "type"), "feature_disabled", StringComparison.Ordinal))
                    {
                        code = "feature_disabled";
                    }

                    supportCode = ReadString(root, "supportCode");
                    if (retryAfter is null
                        && root.TryGetProperty("retryAfterSeconds", out var seconds)
                        && seconds.TryGetInt32(out var parsed))
                    {
                        retryAfter = parsed;
                    }

                    if (status == 400
                        && root.TryGetProperty("userMessage", out var flag)
                        && (flag.ValueKind == JsonValueKind.True
                            || (flag.ValueKind == JsonValueKind.String
                                && string.Equals(flag.GetString(), "true", StringComparison.OrdinalIgnoreCase))))
                    {
                        userMessage = ReadString(root, "detail") ?? ReadString(root, "message");
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Not ProblemDetails (an HTML error page, an empty body): fall back on the status.
        }

        return new ApiErrorException(
            code ?? CodeForStatus(status),
            status,
            supportCode,
            retryAfter,
            userMessage);
    }

    private static string? ReadString(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int? ReadRetryAfter(HttpResponseMessage response)
    {
        var delta = response.Headers.RetryAfter?.Delta;
        return delta is null ? null : Math.Max(1, (int)Math.Ceiling(delta.Value.TotalSeconds));
    }

    private static string CodeForStatus(int status) => status switch
    {
        (int)HttpStatusCode.NotFound or (int)HttpStatusCode.Gone => NotFound,
        (int)HttpStatusCode.Forbidden or (int)HttpStatusCode.Unauthorized => Forbidden,
        (int)HttpStatusCode.BadRequest or 422 => Validation,
        429 => RateLimited,
        (int)HttpStatusCode.ServiceUnavailable => Maintenance,
        _ => Unknown
    };
}
