using Jobsy.Web.Diagnostics;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Services;

/// <summary>
/// The one place that turns an exception into something a visitor may read (E7, errors 04 §04.5).
/// Visitors never see <c>ex.Message</c>, a stack trace, a path or an internal id: they get a
/// catalog key in their own language plus, when there is one, the <c>LB-XXXX</c> support code.
/// <para>
/// The exception itself is logged once here (with the support code), so pages can drop their own
/// logging when they switch over. The <c>NoRawExceptionMessageTests</c> ratchet keeps new
/// <c>ex.Message</c> assignments out of the UI.
/// </para>
/// </summary>
public sealed class UserFacingError
{
    private readonly ILogger<UserFacingError> _logger;
    private readonly CultureState _culture;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public UserFacingError(
        ILogger<UserFacingError> logger,
        CultureState culture,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _logger = logger;
        _culture = culture;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>Catalog key + support code for an exception. Logs the exception once.</summary>
    public UserFacingMessage From(Exception ex)
    {
        var key = MessageKeyFor(ex);
        var supportCode = SupportCodeFor(ex);

        _logger.LogError(
            ex,
            "User-facing error {SupportCode} {MessageKey} {ExceptionType}",
            supportCode,
            key,
            ex.GetType().Name);

        return new UserFacingMessage(key, supportCode, UserMessageFor(ex));
    }

    /// <summary>The text to show. A validation message the API wrote for users wins over the key.</summary>
    public string Describe(Exception ex)
    {
        var message = From(ex);
        return message.ApiUserMessage ?? _culture[message.MessageKey];
    }

    /// <summary>
    /// Pure mapping, without logging, so the table is testable:
    /// known <see cref="ApiError.Code"/> → its key, network/timeout → <c>Common.Error.Network</c>,
    /// everything else → <c>Common.Error.TryAgain</c>.
    /// </summary>
    public static string MessageKeyFor(Exception ex) => ex switch
    {
        ApiError api => KeyForCode(api.Code),
        HttpRequestException => "Common.Error.Network",
        TaskCanceledException or TimeoutException => "Common.Error.Network",
        _ => "Common.Error.TryAgain"
    };

    private static string KeyForCode(string code) => code switch
    {
        ApiError.RateLimited => "Common.Error.RateLimited",
        ApiError.NotFound => "Common.Error.NotFound",
        ApiError.Forbidden => "Common.Error.Forbidden",
        ApiError.Validation => "Common.Error.Validation",
        ApiError.Maintenance => "Common.Error.Maintenance",
        _ => "Common.Error.TryAgain"
    };

    private static string? UserMessageFor(Exception ex)
        => ex is ApiError { UserMessage: { Length: > 0 } message } ? message : null;

    /// <summary>The code the API already minted, else one code per request so support can match.</summary>
    private string SupportCodeFor(Exception ex)
        => ex is ApiError { SupportCode: { Length: > 0 } fromApi }
            ? fromApi
            : SupportCode.GetOrCreate(_httpContextAccessor?.HttpContext);
}

/// <summary>What the UI may show: a catalog key, a support code, and an API user message.</summary>
public readonly record struct UserFacingMessage(
    string MessageKey,
    string? SupportCode,
    string? ApiUserMessage = null);
