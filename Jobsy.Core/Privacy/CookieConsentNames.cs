namespace Jobsy.Core.Privacy;

/// <summary>
/// ePrivacy analytics consent. The web client stores the choice in localStorage and
/// a first-party cookie; API writes require the same value via cookie or header
/// (Blazor Server calls the API from the web host, so the header is the real signal).
/// </summary>
public static class CookieConsentNames
{
    public const string CookieName = "Jobsy.CookieConsent";
    public const string HeaderName = "X-Jobsy-Cookie-Consent";
    public const string AnalyticsValue = "analytics";
    public const string NecessaryValue = "necessary";
}
