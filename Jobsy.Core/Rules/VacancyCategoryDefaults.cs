using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>Seed defaults for the seven built-in vacancy categories.</summary>
public static class VacancyCategoryDefaults
{
    public static readonly Guid UitzendbureauId = Guid.Parse("c1000001-0000-4000-8000-000000000001");
    public static readonly Guid RegulierId = Guid.Parse("c1000001-0000-4000-8000-000000000002");
    public static readonly Guid HighlightId = Guid.Parse("c1000001-0000-4000-8000-000000000003");
    public static readonly Guid InclusiefId = Guid.Parse("c1000001-0000-4000-8000-000000000004");
    public static readonly Guid VolunteerId = Guid.Parse("c1000001-0000-4000-8000-000000000005");
    public static readonly Guid InternshipId = Guid.Parse("c1000001-0000-4000-8000-000000000006");
    public static readonly Guid SeniorLightId = Guid.Parse("c1000001-0000-4000-8000-000000000007");

    /// <summary>Dark purple used for the 65+ category and the “Geschikt voor 65+” label.</summary>
    public const string SeniorPlusColorHex = "#5B21B6";

    /// <summary>Display name for the 65+ vacancy type (category + map badge).</summary>
    public const string SuitableFor65PlusLabel = "Geschikt voor 65+";

    public const string UitzendbureauLabel = "Uitzendbureau";
    public const string UitzendbureauColorHex = "#2563EB";

    public sealed record SeedCategory(
        Guid Id,
        string Slug,
        string Name,
        string ColorHex,
        decimal PublishCostTokens,
        bool HighlightAvailable,
        decimal HighlightCostTokens,
        bool PushBomAvailable,
        decimal? PushBomCostTokens,
        bool IsAlwaysFree,
        VacancyKind PlacementKind,
        int SortOrder,
        IReadOnlyList<string> ExtraFields);

    public static readonly IReadOnlyList<SeedCategory> All =
    [
        new(UitzendbureauId, "uitzendbureau", UitzendbureauLabel, UitzendbureauColorHex,
            1m, true, 2m, true, 3m, false, VacancyKind.Regular, 10,
            [VacancyCategoryExtraFields.ContractType, VacancyCategoryExtraFields.HoursPerWeek,
                VacancyCategoryExtraFields.ExperienceLevel]),

        new(RegulierId, "regulier", "Reguliere vacature", "#F54A1B",
            1m, true, 2m, true, null, false, VacancyKind.Regular, 20,
            [VacancyCategoryExtraFields.ContractType, VacancyCategoryExtraFields.HoursPerWeek,
                VacancyCategoryExtraFields.ExperienceLevel]),

        new(HighlightId, "highlight", "Highlight vacature", "#E8A317",
            1m, true, 2m, true, null, false, VacancyKind.Regular, 30,
            [VacancyCategoryExtraFields.ContractType, VacancyCategoryExtraFields.HoursPerWeek]),

        new(InclusiefId, "inclusief", "Inclusieve vacature", "#8B5CF6",
            0.5m, true, 1m, true, 2m, false, VacancyKind.Regular, 40,
            [VacancyCategoryExtraFields.TargetGroup, VacancyCategoryExtraFields.JobCoachAvailable,
                VacancyCategoryExtraFields.WorkplaceAdjustments]),

        new(VolunteerId, "vrijwilligerswerk", "Vrijwilligerswerk", "#10B981",
            0m, false, 0m, false, 0m, true, VacancyKind.Volunteer, 50,
            [VacancyCategoryExtraFields.OrganizationType, VacancyCategoryExtraFields.VogRequired,
                VacancyCategoryExtraFields.Frequency]),

        new(InternshipId, "stageplekken", "Stageplekken", "#0EA5E9",
            0m, true, 1m, true, 2m, false, VacancyKind.Internship, 60,
            [VacancyCategoryExtraFields.EducationLevel, VacancyCategoryExtraFields.InternshipDuration,
                VacancyCategoryExtraFields.HoursPerWeek, VacancyCategoryExtraFields.InternshipType]),

        new(SeniorLightId, "65plus", SuitableFor65PlusLabel, SeniorPlusColorHex,
            0.5m, true, 1m, true, 2m, false, VacancyKind.Regular, 70,
            [VacancyCategoryExtraFields.PhysicalLoad, VacancyCategoryExtraFields.HoursPerWeek,
                VacancyCategoryExtraFields.ContractType])
    ];

    /// <summary>True when the vacancy belongs to the dedicated 65+ category.</summary>
    public static bool IsSeniorLightCategory(Guid? categoryId)
        => categoryId == SeniorLightId;

    /// <summary>
    /// Matches the unified “Geschikt voor 65+” vacancy type:
    /// dedicated 65+ category, or any vacancy with the suitability flag.
    /// </summary>
    public static bool MatchesSuitableFor65PlusFilter(Guid? categoryId, bool suitableFor65Plus)
        => IsSeniorLightCategory(categoryId) || suitableFor65Plus;

    /// <summary>
    /// Discovery category multi-select. Selecting the 65+ type also includes
    /// Regulier vacancies marked <c>SuitableFor65Plus</c>.
    /// </summary>
    public static bool MatchesSelectedCategories(
        Guid? categoryId,
        bool suitableFor65Plus,
        IReadOnlyCollection<Guid>? selectedCategoryIds)
    {
        if (selectedCategoryIds is null || selectedCategoryIds.Count == 0)
        {
            return true;
        }

        if (categoryId is Guid cid && selectedCategoryIds.Contains(cid))
        {
            return true;
        }

        if (selectedCategoryIds.Contains(SeniorLightId)
            && MatchesSuitableFor65PlusFilter(categoryId, suitableFor65Plus))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Short map-popup type label (legend colors). Regulier / highlight get no label.
    /// “Uitgelicht” is rendered separately and always stacks above this badge when active.
    /// </summary>
    public static (string? Label, string? ColorHex) ResolveMapPopupTypeBadge(
        Guid? categoryId,
        bool suitableFor65Plus,
        VacancyKind? kind = null)
    {
        if (categoryId == InternshipId || kind == VacancyKind.Internship)
        {
            return ("Stageplek", "#0EA5E9");
        }

        if (categoryId == VolunteerId || kind == VacancyKind.Volunteer)
        {
            return ("Vrijwilligers", "#10B981");
        }

        if (categoryId == InclusiefId)
        {
            return ("Inclusieve vacature", "#8B5CF6");
        }

        if (categoryId == UitzendbureauId)
        {
            return (UitzendbureauLabel, UitzendbureauColorHex);
        }

        if (MatchesSuitableFor65PlusFilter(categoryId, suitableFor65Plus))
        {
            return (SuitableFor65PlusLabel, SeniorPlusColorHex);
        }

        return (null, null);
    }

    public static Guid ResolveDefaultId(VacancyKind kind) => kind switch
    {
        VacancyKind.Internship => InternshipId,
        VacancyKind.Volunteer => VolunteerId,
        _ => RegulierId
    };
}
