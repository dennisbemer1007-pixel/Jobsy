namespace Jobsy.Core.Ops;

/// <summary>Stable ids for acceptatie sollicitaties demo applications (shared by Web SSR and Infrastructure seed).</summary>
public static class AcceptatieSollicDemoCatalog
{
    public static readonly Guid AppSent = Guid.Parse("b0000001-0000-4000-8000-00000000002f");
    public static readonly Guid AppSeen = Guid.Parse("b0000001-0000-4000-8000-000000000033");
    public static readonly Guid AppPick = Guid.Parse("b0000001-0000-4000-8000-000000000034");
    public static readonly Guid AppScheduled = Guid.Parse("b0000001-0000-4000-8000-000000000035");
    public static readonly Guid AppMessages = Guid.Parse("b0000001-0000-4000-8000-000000000036");
    public static readonly Guid AppDone = Guid.Parse("b0000001-0000-4000-8000-000000000037");

    private static readonly HashSet<Guid> ApplicationIds =
    [
        AppSent,
        AppSeen,
        AppPick,
        AppScheduled,
        AppMessages,
        AppDone,
    ];

    public static bool IsDemoApplicationId(Guid id) => ApplicationIds.Contains(id);
}
