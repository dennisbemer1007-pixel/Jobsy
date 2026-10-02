namespace Jobsy.Web.Navigation;

/// <summary>
/// Single helper for employer deep-links so werkgever-redesign 301s keep working
/// when Dependencies C flips to Present (URLs stay stable via this helper).
/// </summary>
public static class EmployerLinks
{
    public const string Home = "/werkgever";
    public const string Verify = "/register/verifieren";
    public const string VerifyBrief = "/register/verifieren/brief";
    public const string AboutCompany = "/register/bedrijf";
    public const string AboutBranche = "/register/bedrijf?stap=branche";
    public const string AboutCultuur = "/register/bedrijf?stap=cultuur";
    public const string AboutBetrokkenheid = "/register/bedrijf?stap=betrokkenheid";
    public const string CultureProfile = "/werkgever/organisatie/profiel?tab=cultuur";
    public const string CompanyDetails = "/werkgever/organisatie/profiel";
    public const string Vacancies = "/werkgever/vacatures";
    public const string BranchVacancies = "/werkgever/vacatures";
    public const string NewVacancy = "/werkgever/vacatures/nieuw";
    public const string Users = "/werkgever/organisatie/team";
    public const string Branches = "/werkgever/organisatie/vestigingen";
    public const string Tokens = "/werkgever/tokens";
    public const string Applicants = "/werkgever/sollicitaties";
    public const string Talent = "/werkgever/talentpool";
    public const string CandidateInsights = "/werkgever/kandidaatinzichten";

    public static string VacanciesForRole(bool isBranchManagerOnly)
    {
        _ = isBranchManagerOnly;
        return Vacancies; // unified /werkgever routes (BM/VM share)
    }

    public static string TokensForRole(bool isBranchManagerOnly)
    {
        _ = isBranchManagerOnly;
        return Tokens; // unified /werkgever/tokens
    }
}
