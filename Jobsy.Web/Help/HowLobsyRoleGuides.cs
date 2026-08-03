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
            new("HowLobsy.Step1Title", "HowLobsy.Step1Body", [new("/", "Nav.JobMap")]),
            new("HowLobsy.Step2Title", "HowLobsy.Step2Body", [new("/candidate/profile", "Nav.Profile")]),
            new("HowLobsy.Step3Title", "HowLobsy.Step3Body",
            [
                new("/candidate/liked", "Nav.Saved"),
                new("/candidate/shared", "Nav.Shared")
            ]),
            new("HowLobsy.Step4Title", "HowLobsy.Step4Body", [new("#", "Apply.Title")]),
            new("HowLobsy.Step5Title", "HowLobsy.Step5Body", [new("/candidate/applications", "Nav.MyApplications")]),
            new("HowLobsy.Step6Title", "HowLobsy.Step6Body", [])
        ],
        new("/", "HowLobsy.ToMap"),
        new("/candidate/profile", "HowLobsy.ToProfile"));

    public static readonly Guide Branch = new(
        "HowLobsy.Branch.Title",
        "HowLobsy.Branch.Lead",
        [
            new("HowLobsy.Branch.Step1Title", "HowLobsy.Branch.Step1Body", [new("/home", "Nav.Home")]),
            new("HowLobsy.Branch.Step2Title", "HowLobsy.Branch.Step2Body", [new("/branch/vacancies", "Nav.Vacancies")]),
            new("HowLobsy.Branch.Step3Title", "HowLobsy.Branch.Step3Body", [new("/branch/applicants", "Nav.Applications")]),
            new("HowLobsy.Branch.Step4Title", "HowLobsy.Branch.Step4Body", [new("/branch/tokens", "Nav.MyTokens")]),
            new("HowLobsy.Branch.Step5Title", "HowLobsy.Branch.Step5Body",
            [
                new("/employer/company", "Nav.CompanyDetails"),
                new("/employer/takeovers", "Nav.Takeovers")
            ]),
            new("HowLobsy.Branch.Step6Title", "HowLobsy.Branch.Step6Body", [new("/", "Nav.JobMap")])
        ],
        new("/branch/vacancies", "HowLobsy.Branch.PrimaryCta"),
        new("/home", "HowLobsy.Branch.SecondaryCta"));

    public static readonly Guide Regional = new(
        "HowLobsy.Regional.Title",
        "HowLobsy.Regional.Lead",
        [
            new("HowLobsy.Regional.Step1Title", "HowLobsy.Regional.Step1Body", [new("/home", "Nav.Home")]),
            new("HowLobsy.Regional.Step2Title", "HowLobsy.Regional.Step2Body", [new("/employer/vacancies", "Nav.Vacancies")]),
            new("HowLobsy.Regional.Step3Title", "HowLobsy.Regional.Step3Body", [new("/regional/branches", "Nav.MyBranches")]),
            new("HowLobsy.Regional.Step4Title", "HowLobsy.Regional.Step4Body", [new("/employer/tokens", "Nav.Tokens")]),
            new("HowLobsy.Regional.Step5Title", "HowLobsy.Regional.Step5Body", [new("/", "Nav.JobMap")])
        ],
        new("/regional/branches", "HowLobsy.Regional.PrimaryCta"),
        new("/home", "HowLobsy.Regional.SecondaryCta"));

    public static readonly Guide Enterprise = new(
        "HowLobsy.Enterprise.Title",
        "HowLobsy.Enterprise.Lead",
        [
            new("HowLobsy.Enterprise.Step1Title", "HowLobsy.Enterprise.Step1Body", [new("/home", "Nav.Home")]),
            new("HowLobsy.Enterprise.Step2Title", "HowLobsy.Enterprise.Step2Body", [new("/employer/vacancies", "Nav.Vacancies")]),
            new("HowLobsy.Enterprise.Step3Title", "HowLobsy.Enterprise.Step3Body", [new("/employer/tokens", "Nav.Tokens")]),
            new("HowLobsy.Enterprise.Step4Title", "HowLobsy.Enterprise.Step4Body", [new("/employer/users", "Nav.Users")]),
            new("HowLobsy.Enterprise.Step5Title", "HowLobsy.Enterprise.Step5Body", [new("/employer/organization", "Nav.Organization")]),
            new("HowLobsy.Enterprise.Step6Title", "HowLobsy.Enterprise.Step6Body", [new("/employer/organization", "Nav.Organization")])
        ],
        new("/employer/vacancies", "HowLobsy.Enterprise.PrimaryCta"),
        new("/home", "HowLobsy.Enterprise.SecondaryCta"));

    public static readonly Guide Intermediary = new(
        "HowLobsy.Intermediary.Title",
        "HowLobsy.Intermediary.Lead",
        [
            new("HowLobsy.Intermediary.Step1Title", "HowLobsy.Intermediary.Step1Body", [new("/home", "Nav.Home")]),
            new("HowLobsy.Intermediary.Step2Title", "HowLobsy.Intermediary.Step2Body", [new("/intermediary", "Nav.Clients")]),
            new("HowLobsy.Intermediary.Step3Title", "HowLobsy.Intermediary.Step3Body", [new("/employer/vacancies", "Nav.Vacancies")]),
            new("HowLobsy.Intermediary.Step4Title", "HowLobsy.Intermediary.Step4Body", [new("/employer/tokens", "Nav.Tokens")]),
            new("HowLobsy.Intermediary.Step5Title", "HowLobsy.Intermediary.Step5Body", [new("/", "Nav.JobMap")])
        ],
        new("/employer/vacancies", "HowLobsy.Intermediary.PrimaryCta"),
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
            ? "/salesmanager/toolkit"
            : $"/partner/{Uri.EscapeDataString(code)}";

        return new(
            "HowLobsy.Sales.Title",
            "HowLobsy.Sales.Lead",
            [
                new("HowLobsy.Sales.Step1Title", "HowLobsy.Sales.Step1Body", [new("/salesmanager/onboarding", "Nav.Onboarding")]),
                new("HowLobsy.Sales.Step2Title", "HowLobsy.Sales.Step2Body", [new("/salesmanager/toolkit", "Nav.SalesToolkit")]),
                new("HowLobsy.Sales.Step3Title", "HowLobsy.Sales.Step3Body", [new(partnerHref, "HowLobsy.Sales.PartnerLabel")]),
                new("HowLobsy.Sales.Step4Title", "HowLobsy.Sales.Step4Body", [new("/home", "Nav.Home")]),
                new("HowLobsy.Sales.Step5Title", "HowLobsy.Sales.Step5Body", [new("/salesmanager/invoices", "Nav.Invoices")])
            ],
            new("/salesmanager/toolkit", "HowLobsy.Sales.PrimaryCta"),
            new("/home", "HowLobsy.Sales.SecondaryCta"));
    }

    public static Guide BuildAmbassadeurGuide(string? trackingCode)
    {
        var code = trackingCode?.Trim();
        var wervenHref = string.IsNullOrWhiteSpace(code)
            ? "/ambassadeur/toolkit"
            : $"/werven/{Uri.EscapeDataString(code)}";

        return new(
            "HowLobsy.Sales.Title",
            "HowLobsy.Sales.Lead",
            [
                new("HowLobsy.Sales.Step1Title", "HowLobsy.Sales.Step1Body", [new("/ambassadeur/onboarding", "Nav.Onboarding")]),
                new("HowLobsy.Sales.Step2Title", "HowLobsy.Sales.Step2Body", [new("/ambassadeur/toolkit", "Nav.AmbassadeurToolkit")]),
                new("HowLobsy.Sales.Step3Title", "HowLobsy.Sales.Step3Body", [new(wervenHref, "HowLobsy.Sales.PartnerLabel")]),
                new("HowLobsy.Sales.Step4Title", "HowLobsy.Sales.Step4Body", [new("/home", "Nav.Home")]),
                new("HowLobsy.Sales.Step5Title", "HowLobsy.Sales.Step5Body", [new("/ambassadeur/finance", "Nav.AmbassadeurFinance")])
            ],
            new("/ambassadeur/toolkit", "HowLobsy.Sales.PrimaryCta"),
            new("/home", "HowLobsy.Sales.SecondaryCta"));
    }
}
