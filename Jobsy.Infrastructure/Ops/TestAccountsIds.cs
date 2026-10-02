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

    public static readonly Guid SalesProfile = Guid.Parse("b0000001-0000-4000-8000-000000000040");
    public static readonly Guid AmbassadeurProfile = Guid.Parse("b0000001-0000-4000-8000-000000000041");

    public static readonly Guid School = Guid.Parse("b0000001-0000-4000-8000-000000000050");
    public static readonly Guid SchoolClass = Guid.Parse("b0000001-0000-4000-8000-000000000051");

    // Reserved KvK range for test data (never call out for IsTestData companies).
    public const string RootKvk = "00000991";
    public const string HaagKvk = "00000992";
    public const string DelftKvk = "00000993";
    public const string IntermediaryKvk = "00000994";
}
