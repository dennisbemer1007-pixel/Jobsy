namespace Jobsy.Web.Navigation;

/// <summary>
/// Single helper for employer deep-links so werkgever-redesign 301s keep working
/// when Dependencies C flips to Present (URLs stay stable via this helper).
/// Absent case: today's routes.
/// </summary>
public static class EmployerLinks
{
    public const string Home = "/home";
    public const string Verify = "/register/verifieren";
    public const string VerifyBrief = "/register/verifieren/brief";
    public const string AboutCompany = "/register/bedrijf";
    public const string AboutBranche = "/register/bedrijf?stap=branche";
    public const string AboutCultuur = "/register/bedrijf?stap=cultuur";
    public const string AboutBetrokkenheid = "/register/bedrijf?stap=betrokkenheid";
    public const string CultureProfile = "/employer/culture";
    public const string CompanyDetails = "/employer/company";
    public const string Vacancies = "/employer/vacancies";
    public const string BranchVacancies = "/branch/vacancies";
    public const string NewVacancy = "/branch/vacancies/new";
    public const string Users = "/employer/users";
    public const string Branches = "/employer/branches";
    public const string Tokens = "/employer/tokens";
    public const string Applicants = "/branch/applicants";
    public const string Talent = "/employer/talent";
    public const string CandidateInsights = "/employer/kandidaatinzichten";

    public static string VacanciesForRole(bool isBranchManagerOnly)
        => isBranchManagerOnly ? BranchVacancies : Vacancies;

    public static string TokensForRole(bool isBranchManagerOnly)
        => isBranchManagerOnly ? "/branch/tokens" : Tokens;
}
