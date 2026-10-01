using System.Security.Claims;
using Jobsy.Core.Localization;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;

namespace Jobsy.Web.Hosting;

/// <summary>
/// Everything <c>ErrorLayout</c> needs to draw its header and footer, resolved without a single
/// API or database call: language from cookie/header, sign-in state from the auth cookie claims.
/// </summary>
public sealed record ErrorChrome(
    string Language,
    bool IsRightToLeft,
    bool IsSignedIn,
    string ReturnPath,
    string LegalName,
    string SupportEmail,
    int Year)
{
    public string HomeHref => PublicRoutes.Landing;

    public string LoginHref => PublicRoutes.Login;

    public string MyStartHref => "/home";

    public string PrivacyHref => PublicRoutes.Privacy;

    public string TermsHref => PublicRoutes.Terms;

    public string HelpHref => PublicRoutes.HowItWorks;

    public string Copyright => $"© {Year} {LegalName}";

    public string LanguageHref(string code)
        => $"/taal/{code}?returnUrl={Uri.EscapeDataString(ReturnPath)}";
}

/// <summary>Seam so tests can force a layout render failure (ErrorLayout self-protection).</summary>
public interface IErrorChromeProvider
{
    ErrorChrome Build(HttpContext? http);
}

public sealed class ErrorChromeProvider : IErrorChromeProvider
{
    /// <summary>
    /// Dependency B absent on acceptatie: there is no LegalIdentityProvider cache yet, so the
    /// footer shows the brand name only. See docs/errors-followups.md.
    /// </summary>
    public const string DefaultLegalName = "Lobsy";

    private readonly IConfiguration _configuration;

    public ErrorChromeProvider(IConfiguration configuration) => _configuration = configuration;

    public ErrorChrome Build(HttpContext? http)
    {
        var language = ErrorCulture.Resolve(http);
        return new ErrorChrome(
            language,
            JobsyLanguages.Get(language).IsRightToLeft,
            IsSignedIn(http),
            ReturnPath(http),
            DefaultLegalName,
            SupportContact.Email(_configuration),
            DateTime.UtcNow.Year);
    }

    private static bool IsSignedIn(HttpContext? http)
        => http?.User?.Identity is ClaimsIdentity { IsAuthenticated: true };

    /// <summary>Path (no query) of the page the visitor asked for, for the language links.</summary>
    private static string ReturnPath(HttpContext? http)
    {
        var original = http?.Features.Get<Microsoft.AspNetCore.Diagnostics.IStatusCodeReExecuteFeature>()
            ?.OriginalPath;
        if (string.IsNullOrEmpty(original))
        {
            original = http?.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>()?.Path;
        }

        if (string.IsNullOrEmpty(original))
        {
            original = http?.Request.Path.Value;
        }

        return string.IsNullOrEmpty(original) || original[0] != '/' ? "/" : original;
    }
}

/// <summary>Support mailbox shown on error pages (E9).</summary>
public static class SupportContact
{
    public const string Default = "support@lobsy.nl";

    public static string Email(IConfiguration? configuration)
        => configuration?["Support:Email"]
           ?? configuration?["Mail:SupportAddress"]
           ?? Default;

    /// <summary>A <c>mailto:</c> link with the subject escaped exactly once (E9).</summary>
    public static string MailtoLink(string email, string? subject = null)
        => string.IsNullOrWhiteSpace(subject)
            ? $"mailto:{email}"
            : $"mailto:{email}?subject={Uri.EscapeDataString(subject)}";
}
