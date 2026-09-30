using Jobsy.Core.Rules;

namespace Jobsy.Core.Email;

/// <summary>
/// Absolute deep links for transactional mail. Requires a configured public web base URL.
/// </summary>
public sealed class EmailLinks
{
    private readonly string _origin;

    private EmailLinks(string origin)
        => _origin = origin;

    public static EmailLinks For(string publicWebBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(publicWebBaseUrl))
        {
            throw new ArgumentException("Public web base URL is required for email links.", nameof(publicWebBaseUrl));
        }

        return new EmailLinks(JobsyPublicUrl.NormalizeOrigin(publicWebBaseUrl));
    }

    public string Absolute(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return _origin;
        }

        if (relativePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
        {
            return relativePath;
        }

        var path = relativePath.StartsWith('/') ? relativePath : "/" + relativePath;
        return _origin.TrimEnd('/') + path;
    }

    public string CandidateApplications => Absolute("/candidate/applications");
    public string Vacancy(Guid id) => Absolute($"/vacancies/{id}");
    /// <summary>Dependencies A/C: PublicRoutes.Banenkaart / KbRoutes.Map = /banenkaart.</summary>
    public string Map => Absolute("/banenkaart");
    public string WithdrawOthers(Guid hiredApplicationId)
        => Absolute(CandidateActionPurposes.WithdrawOthersInAppPath(hiredApplicationId));
    public string WithdrawOthersToken(string token)
        => Absolute($"/candidate/actions/withdraw-others?t={Uri.EscapeDataString(token)}");
    public string SetUnavailable => Absolute(CandidateActionPurposes.SetUnavailableInAppPath);
    public string SetUnavailableToken(string token)
        => Absolute($"/candidate/actions/set-unavailable?t={Uri.EscapeDataString(token)}");
    public string Login => Absolute("/login");
    public string PrivacyData => Absolute("/privacy/data");
    public string Privacy => Absolute("/privacy");
    public string MailSettings => Absolute("/account/mail-instellingen");
    public string SetPassword(string token)
        => Absolute($"/account/wachtwoord-instellen?t={Uri.EscapeDataString(token)}");
    public string ApiKeyReveal(string token)
        => Absolute($"/koppeling/sleutel?t={Uri.EscapeDataString(token)}");
    public string ParentalConsent(string token)
        => Absolute($"/toestemming?t={Uri.EscapeDataString(token)}");

    // Dependencies B — WerkgeverNav targets
    public string EmployerHome => Absolute("/werkgever");
    public string EmployerApplications(Guid? applicationId = null)
        => applicationId is Guid id
            ? Absolute($"/werkgever/sollicitaties?id={id}")
            : Absolute("/werkgever/sollicitaties");
    public string EmployerVacancyEdit(Guid id)
        => Absolute($"/werkgever/vacatures/nieuw?edit={id}");
    public string EmployerVacancies => Absolute("/werkgever/vacatures");
    public string EmployerVacancyBoostHighlight(Guid id)
        => Absolute($"/werkgever/vacatures?boost=highlight&id={id}");
    public string EmployerVacancyBoostPushBom(Guid id)
        => Absolute($"/werkgever/vacatures?boost=pushbom&id={id}");
    public string EmployerTokens => Absolute("/werkgever/tokens");
    public string EmployerTakeovers => Absolute("/werkgever/overnames");
    public string EmployerApiSettings => Absolute("/werkgever/koppelingen?tab=api");
    public string EmployerTeam => Absolute("/werkgever/organisatie/team");
    public string EmployerProfile => Absolute("/werkgever/organisatie/profiel");

    public string RegisterActivate => Absolute("/register"); // auth 06: activate page removed
    public string RegisterVerify => Absolute("/register/verifieren");
    public string Register => Absolute("/register");
    public string RegisterAccess => Absolute("/register/toegang");
    public string AccountCreateCode => Absolute("/account-maken/code");

    /// <summary>Dependencies D — SalesLegacyRoutes onboarding → /sales/start.</summary>
    public string SalesOnboarding => Absolute("/sales/start");
    public string AmbassadeurOnboarding => Absolute("/ambassadeur/onboarding");

    /// <summary>Dependencies G — AdminNav emails page.</summary>
    public string AdminEmails => Absolute("/admin/content/emails");
    public string AdminPersonalDataAccessLog => Absolute("/admin/personal-data-access-log");
    public string AdminWerkgeverVerificatie(string? tab = null)
        => string.IsNullOrWhiteSpace(tab)
            ? Absolute("/admin/werkgeververificatie")
            : Absolute($"/admin/werkgeververificatie?tab={Uri.EscapeDataString(tab)}");

    public string HowLobsyWorks => Absolute("/hoe-werkt-lobsy");
    public string SupportMailto(string supportAddress)
        => $"mailto:{supportAddress}";
}
