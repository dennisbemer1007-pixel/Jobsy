namespace Jobsy.Web.Navigation;

/// <summary>Canonical public / candidate-funnel paths used by the landing stack.</summary>
public static class PublicRoutes
{
    public const string Landing = "/";
    public const string Test = "/ontdek";
    public const string CreateAccount = "/account-maken";
    public const string CreateAccountFromTest = "/account-maken?van=ontdek";
    public const string CreateAccountFromUnder16 = "/account-maken?van=onder16";
    public const string CreateAccountCode = "/account-maken/code";
    public const string Banenkaart = "/banenkaart";
    public const string Employers = "/werkgevers";
    public const string Schools = "/scholen";
    public const string Partner = "/partner";
    public const string CompanyRegister = "/register";
    public const string Login = "/login";
    public const string HowItWorks = "/hoe-werkt-lobsy";
    public const string Privacy = "/privacy";
    public const string PrivacyCookies = "/privacy#cookies";
    public const string Terms = "/algemene-voorwaarden";
    public const string UsageTerms = "/gebruiksvoorwaarden";
    public const string About = "/wie-zijn-wij";
    public const string Accessibility = "/toegankelijkheid";
    public const string Contact = "/melden";
    public const string PassportAnchor = "/#wat-je-krijgt";
    public const string DiscoveryAnchor = "/#ontdekkingsreis";
}
