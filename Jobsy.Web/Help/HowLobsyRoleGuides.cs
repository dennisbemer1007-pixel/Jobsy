using Jobsy.Core.Authorization;

namespace Jobsy.Web.Help;

/// <summary>
/// Role-specific “Hoe werkt Lobsy” guides (localization keys + deep links).
/// </summary>
public static class HowLobsyRoleGuides
{
    public const string SharedPath = "/hoe-werkt-lobsy";
    public const string CandidatePath = "/candidate/hoe-werkt-lobsy";

    public sealed record LinkSlot(string Href, string LabelKey);

    public sealed record Step(string TitleKey, string BodyKey, LinkSlot[] Links);

    public sealed record Guide(
        string TitleKey,
        string LeadKey,
        Step[] Steps,
        LinkSlot Primary,
        LinkSlot? Secondary);

    public static Guide? ForRole(string? role) => role switch
    {
        JobsyRoles.Candidate => Candidate,
        JobsyRoles.BranchManager => Branch,
        JobsyRoles.RegionalManager => Regional,
        JobsyRoles.EnterpriseManager => Enterprise,
        JobsyRoles.Intermediary => Intermediary,
        JobsyRoles.SalesManager => Sales,
        JobsyRoles.Ambassadeur => Ambassadeur,
        _ => null
    };

    public static readonly Guide Candidate = new(
        "HowLobsy.Title",
        "HowLobsy.Lead",
        [
            new("HowLobsy.Step1Title", "HowLobsy.Step1Body", [new("/banenkaart", "Nav.JobMap")]),
            new("HowLobsy.Step2Title", "HowLobsy.Step2Body", [new("/profiel", "Nav.Profile")]),
            new("HowLobsy.Step3Title", "HowLobsy.Step3Body",
            [
                new("/candidate/liked", "Nav.Saved"),
                new("/candidate/shared", "Nav.Shared")
            ]),
            new("HowLobsy.Step4Title", "HowLobsy.Step4Body", [new("#", "Apply.Title")]),
            new("HowLobsy.Step5Title", "HowLobsy.Step5Body", [new("/candidate/applications", "Nav.MyApplications")]),
            new("HowLobsy.Step6Title", "HowLobsy.Step6Body", [])
        ],
        new("/banenkaart", "HowLobsy.ToMap"),
        new("/profiel", "HowLobsy.ToProfile"));

    public static readonly Guide Branch = new(
        "HowLobsy.Branch.Title",
        "HowLobsy.Branch.Lead",
        [
            new("HowLobsy.Branch.Step1Title", "HowLobsy.Branch.Step1Body", [new("/home", "Nav.Home")]),
            new("HowLobsy.Branch.Step2Title", "HowLobsy.Branch.Step2Body", [new("/werkgever/vacatures", "Nav.Vacancies")]),
            new("HowLobsy.Branch.Step3Title", "HowLobsy.Branch.Step3Body", [new("/werkgever/sollicitaties", "Nav.Applications")]),
            new("HowLobsy.Branch.Step4Title", "HowLobsy.Branch.Step4Body", [new("/werkgever/tokens", "Nav.MyTokens")]),
            new("HowLobsy.Branch.Step5Title", "HowLobsy.Branch.Step5Body",
            [
                new("/werkgever/organisatie/profiel", "Nav.CompanyDetails"),
                new("/werkgever/overnames", "Nav.Takeovers")
            ]),
            new("HowLobsy.Branch.Step6Title", "HowLobsy.Branch.Step6Body", [new("/banenkaart", "Nav.JobMap")])
        ],
        new("/werkgever/vacatures", "HowLobsy.Branch.PrimaryCta"),
        new("/home", "HowLobsy.Branch.SecondaryCta"));

    public static readonly Guide Regional = new(
        "HowLobsy.Regional.Title",
        "HowLobsy.Regional.Lead",
        [
            new("HowLobsy.Regional.Step1Title", "HowLobsy.Regional.Step1Body", [new("/home", "Nav.Home")]),
            new("HowLobsy.Regional.Step2Title", "HowLobsy.Regional.Step2Body", [new("/werkgever/vacatures", "Nav.Vacancies")]),
            new("HowLobsy.Regional.Step3Title", "HowLobsy.Regional.Step3Body", [new("/werkgever/organisatie/vestigingen", "Nav.MyBranches")]),
            new("HowLobsy.Regional.Step4Title", "HowLobsy.Regional.Step4Body", [new("/werkgever/tokens", "Nav.Tokens")]),
            new("HowLobsy.Regional.Step5Title", "HowLobsy.Regional.Step5Body", [new("/banenkaart", "Nav.JobMap")])
        ],
        new("/werkgever/organisatie/vestigingen", "HowLobsy.Regional.PrimaryCta"),
        new("/home", "HowLobsy.Regional.SecondaryCta"));

    public static readonly Guide Enterprise = new(
        "HowLobsy.Enterprise.Title",
        "HowLobsy.Enterprise.Lead",
        [
            new("HowLobsy.Enterprise.Step1Title", "HowLobsy.Enterprise.Step1Body", [new("/home", "Nav.Home")]),
            new("HowLobsy.Enterprise.Step2Title", "HowLobsy.Enterprise.Step2Body", [new("/werkgever/vacatures", "Nav.Vacancies")]),
            new("HowLobsy.Enterprise.Step3Title", "HowLobsy.Enterprise.Step3Body", [new("/werkgever/tokens", "Nav.Tokens")]),
            new("HowLobsy.Enterprise.Step4Title", "HowLobsy.Enterprise.Step4Body", [new("/werkgever/organisatie/team", "Nav.Users")]),
            new("HowLobsy.Enterprise.Step5Title", "HowLobsy.Enterprise.Step5Body", [new("/werkgever/organisatie/vestigingen", "Nav.Organization")]),
            new("HowLobsy.Enterprise.Step6Title", "HowLobsy.Enterprise.Step6Body", [new("/werkgever/organisatie/vestigingen", "Nav.Organization")])
        ],
        new("/werkgever/vacatures", "HowLobsy.Enterprise.PrimaryCta"),
        new("/home", "HowLobsy.Enterprise.SecondaryCta"));

    public static readonly Guide Intermediary = new(
        "HowLobsy.Intermediary.Title",
        "HowLobsy.Intermediary.Lead",
        [
            new("HowLobsy.Intermediary.Step1Title", "HowLobsy.Intermediary.Step1Body", [new("/home", "Nav.Home")]),
            new("HowLobsy.Intermediary.Step2Title", "HowLobsy.Intermediary.Step2Body", [new("/intermediary", "Nav.Clients")]),
            new("HowLobsy.Intermediary.Step3Title", "HowLobsy.Intermediary.Step3Body", [new("/werkgever/vacatures", "Nav.Vacancies")]),
            new("HowLobsy.Intermediary.Step4Title", "HowLobsy.Intermediary.Step4Body", [new("/werkgever/tokens", "Nav.Tokens")]),
            new("HowLobsy.Intermediary.Step5Title", "HowLobsy.Intermediary.Step5Body", [new("/banenkaart", "Nav.JobMap")])
        ],
        new("/werkgever/vacatures", "HowLobsy.Intermediary.PrimaryCta"),
        new("/home", "HowLobsy.Intermediary.SecondaryCta"));

    public static readonly Guide Sales = BuildSalesGuide(trackingCode: null);

    public static readonly Guide Ambassadeur = BuildAmbassadeurGuide(trackingCode: null);

    /// <summary>
    /// Sales guide with a personal <c>/partner/{trackingCode}</c> deep link when available;
    /// otherwise falls back to the toolkit where the coded partner URL is shown.
    /// </summary>
    public static Guide BuildSalesGuide(string? trackingCode)
    {
        var code = trackingCode?.Trim();
        var partnerHref = string.IsNullOrWhiteSpace(code)
            ? "/sales/link"
            : $"/partner/{Uri.EscapeDataString(code)}";

        return new(
            "HowLobsy.Sales.Title",
            "HowLobsy.Sales.Lead",
            [
                new("HowLobsy.Sales.Step1Title", "HowLobsy.Sales.Step1Body", [new("/sales/start", "Nav.Onboarding")]),
                new("HowLobsy.Sales.Step2Title", "HowLobsy.Sales.Step2Body", [new("/sales/link", "Nav.SalesToolkit")]),
                new("HowLobsy.Sales.Step3Title", "HowLobsy.Sales.Step3Body", [new(partnerHref, "HowLobsy.Sales.PartnerLabel")]),
                new("HowLobsy.Sales.Step4Title", "HowLobsy.Sales.Step4Body", [new("/home", "Nav.Home")]),
                new("HowLobsy.Sales.Step5Title", "HowLobsy.Sales.Step5Body", [new("/sales/wallet", "Nav.Invoices")])
            ],
            new("/sales/link", "HowLobsy.Sales.PrimaryCta"),
            new("/home", "HowLobsy.Sales.SecondaryCta"));
    }

    public static Guide BuildAmbassadeurGuide(string? trackingCode)
    {
        var code = trackingCode?.Trim();
        var wervenHref = string.IsNullOrWhiteSpace(code)
            ? "/ambassadeur/toolkit"
            : $"/werven/{Uri.EscapeDataString(code)}";

        return new(
            "HowLobsy.Ambassadeur.Title",
            "HowLobsy.Ambassadeur.Lead",
            [
                new("HowLobsy.Ambassadeur.Step1Title", "HowLobsy.Ambassadeur.Step1Body", [new("/ambassadeur/onboarding", "Nav.Onboarding")]),
                new("HowLobsy.Ambassadeur.Step2Title", "HowLobsy.Ambassadeur.Step2Body", [new("/ambassadeur/toolkit", "Nav.AmbassadeurToolkit")]),
                new("HowLobsy.Ambassadeur.Step3Title", "HowLobsy.Ambassadeur.Step3Body", [new(wervenHref, "HowLobsy.Ambassadeur.PartnerLabel")]),
                new("HowLobsy.Ambassadeur.Step4Title", "HowLobsy.Ambassadeur.Step4Body", [new("/home", "Nav.Home")]),
                new("HowLobsy.Ambassadeur.Step5Title", "HowLobsy.Ambassadeur.Step5Body", [new("/ambassadeur/finance", "Nav.AmbassadeurFinance")])
            ],
            new("/ambassadeur/toolkit", "HowLobsy.Ambassadeur.PrimaryCta"),
            new("/home", "HowLobsy.Ambassadeur.SecondaryCta"));
    }
}
