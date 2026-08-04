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
        new(UitzendbureauId, "uitzendbureau", "Uitzendbureau", "#2563EB",
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
            0.5m, true, 1m, true, 2m, false, VacancyKind.Internship, 60,
            [VacancyCategoryExtraFields.EducationLevel, VacancyCategoryExtraFields.InternshipDuration,
                VacancyCategoryExtraFields.HoursPerWeek, VacancyCategoryExtraFields.InternshipType]),

        new(SeniorLightId, "65plus", "65+ lichte betaalde functies", "#64748B",
            0.5m, true, 1m, true, 2m, false, VacancyKind.Regular, 70,
            [VacancyCategoryExtraFields.PhysicalLoad, VacancyCategoryExtraFields.HoursPerWeek,
                VacancyCategoryExtraFields.ContractType])
    ];

    public static Guid ResolveDefaultId(VacancyKind kind) => kind switch
    {
        VacancyKind.Internship => InternshipId,
        VacancyKind.Volunteer => VolunteerId,
        _ => RegulierId
    };
}
