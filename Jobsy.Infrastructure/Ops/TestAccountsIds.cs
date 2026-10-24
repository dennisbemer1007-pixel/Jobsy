namespace Jobsy.Infrastructure.Ops;

/// <summary>Deterministic IDs for idempotent test-account sample data.</summary>
internal static class TestAccountsIds
{
    public static readonly Guid RootCompany = Guid.Parse("b0000001-0000-4000-8000-000000000001");
    public static readonly Guid VestigingDenHaag = Guid.Parse("b0000001-0000-4000-8000-000000000002");
    public static readonly Guid VestigingDelft = Guid.Parse("b0000001-0000-4000-8000-000000000003");
    public static readonly Guid IntermediaryCompany = Guid.Parse("b0000001-0000-4000-8000-000000000004");
    public static readonly Guid Region = Guid.Parse("b0000001-0000-4000-8000-000000000010");

    public static readonly Guid VacancyHaag1 = Guid.Parse("b0000001-0000-4000-8000-000000000021");
    public static readonly Guid VacancyHaag2 = Guid.Parse("b0000001-0000-4000-8000-000000000022");
    public static readonly Guid VacancyDelftDraft = Guid.Parse("b0000001-0000-4000-8000-000000000023");
    public static readonly Guid ClientVacancy = Guid.Parse("b0000001-0000-4000-8000-000000000024");

    public static readonly Guid TokenGrant = Guid.Parse("b0000001-0000-4000-8000-000000000030");
    public static readonly Guid Application = Guid.Parse("b0000001-0000-4000-8000-000000000031");
    public static readonly Guid OnboardingComplete = Guid.Parse("b0000001-0000-4000-8000-000000000032");

    // Acceptatie sollicitaties mockup demo (TestAccountsSollicitatiesDemoSeeder).
    public static readonly Guid SollicDemoCompanyHoreca = Guid.Parse("b0000001-0000-4000-8000-000000000060");
    public static readonly Guid SollicDemoCompanyRetail = Guid.Parse("b0000001-0000-4000-8000-000000000061");
    public static readonly Guid SollicDemoCompanyGroen = Guid.Parse("b0000001-0000-4000-8000-000000000062");
    public static readonly Guid SollicDemoCompanyBlauw = Guid.Parse("b0000001-0000-4000-8000-000000000063");
    public static readonly Guid SollicDemoCompanyGrijs = Guid.Parse("b0000001-0000-4000-8000-000000000064");

    public static readonly Guid SollicDemoVacancySent = Guid.Parse("b0000001-0000-4000-8000-000000000070");
    public static readonly Guid SollicDemoVacancySeen = Guid.Parse("b0000001-0000-4000-8000-000000000071");
    public static readonly Guid SollicDemoVacancyPick = Guid.Parse("b0000001-0000-4000-8000-000000000072");
    public static readonly Guid SollicDemoVacancyScheduled = Guid.Parse("b0000001-0000-4000-8000-000000000073");
    public static readonly Guid SollicDemoVacancyMessages = Guid.Parse("b0000001-0000-4000-8000-000000000074");
    public static readonly Guid SollicDemoVacancyDone = Guid.Parse("b0000001-0000-4000-8000-000000000075");

    /// <summary>Pending — Verstuurd (reuses legacy test application id).</summary>
    public static readonly Guid SollicDemoAppSent = Application;
    public static readonly Guid SollicDemoAppSeen = Guid.Parse("b0000001-0000-4000-8000-000000000033");
    public static readonly Guid SollicDemoAppPick = Guid.Parse("b0000001-0000-4000-8000-000000000034");
    public static readonly Guid SollicDemoAppScheduled = Guid.Parse("b0000001-0000-4000-8000-000000000035");
    public static readonly Guid SollicDemoAppMessages = Guid.Parse("b0000001-0000-4000-8000-000000000036");
    public static readonly Guid SollicDemoAppDone = Guid.Parse("b0000001-0000-4000-8000-000000000037");
    public static readonly Guid SollicDemoExternalVacancy = Guid.Parse("b0000001-0000-4000-8000-000000000038");
    public static readonly Guid SollicDemoInitiativeViewed = Guid.Parse("b0000001-0000-4000-8000-000000000039");
    public static readonly Guid SollicDemoInitiativeAccepted = Guid.Parse("b0000001-0000-4000-8000-00000000003a");

    public const string SollicDemoKvkBase = "00000995";

    public static readonly Guid SalesProfile = Guid.Parse("b0000001-0000-4000-8000-000000000040");
    public static readonly Guid AmbassadeurProfile = Guid.Parse("b0000001-0000-4000-8000-000000000041");

    public static readonly Guid School = Guid.Parse("b0000001-0000-4000-8000-000000000050");
    public static readonly Guid SchoolClass = Guid.Parse("b0000001-0000-4000-8000-000000000051");
    public static readonly Guid SchoolClassGroep78 = Guid.Parse("b0000001-0000-4000-8000-000000000052");

    // Reserved KvK range for test data (never call out for IsTestData companies).
    public const string RootKvk = "00000991";
    public const string HaagKvk = "00000992";
    public const string DelftKvk = "00000993";
    public const string IntermediaryKvk = "00000994";
}
