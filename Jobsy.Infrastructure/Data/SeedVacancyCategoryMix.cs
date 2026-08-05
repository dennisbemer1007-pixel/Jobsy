using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

/// <summary>
/// Assigns the seven built-in vacancy categories across banenkaart mock vacancies
/// so filters, legend colors and map-popup type badges are all demonstrable.
/// </summary>
internal static class SeedVacancyCategoryMix
{
    public const string Marker = "Banenkaart vacancy category mix v1";

    public readonly record struct Mix(
        Guid CategoryId,
        VacancyKind Kind,
        bool SuitableFor65Plus,
        bool PreferHighlight);

    /// <summary>
    /// Cycles all seven categories. Every second Regulier slot also gets
    /// <see cref="Vacancy.SuitableFor65Plus"/> so the 65+ filter works outside the dedicated category.
    /// Highlight-category slots prefer an active Uitgelicht flag for carousel/pulse demos.
    /// </summary>
    public static Mix Resolve(int oneBasedIndex)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(oneBasedIndex, 1);

        var cycle = (oneBasedIndex - 1) / 7;
        var slot = (oneBasedIndex - 1) % 7;
        return slot switch
        {
            0 => new Mix(
                VacancyCategoryDefaults.RegulierId,
                VacancyKind.Regular,
                SuitableFor65Plus: cycle % 2 == 1,
                PreferHighlight: false),
            1 => new Mix(
                VacancyCategoryDefaults.UitzendbureauId,
                VacancyKind.Regular,
                SuitableFor65Plus: false,
                PreferHighlight: false),
            2 => new Mix(
                VacancyCategoryDefaults.HighlightId,
                VacancyKind.Regular,
                SuitableFor65Plus: false,
                PreferHighlight: true),
            3 => new Mix(
                VacancyCategoryDefaults.InclusiefId,
                VacancyKind.Regular,
                SuitableFor65Plus: false,
                PreferHighlight: false),
            4 => new Mix(
                VacancyCategoryDefaults.VolunteerId,
                VacancyKind.Volunteer,
                SuitableFor65Plus: false,
                PreferHighlight: false),
            5 => new Mix(
                VacancyCategoryDefaults.InternshipId,
                VacancyKind.Internship,
                SuitableFor65Plus: false,
                PreferHighlight: false),
            _ => new Mix(
                VacancyCategoryDefaults.SeniorLightId,
                VacancyKind.Regular,
                SuitableFor65Plus: false,
                PreferHighlight: false)
        };
    }

    public static void Apply(Vacancy vacancy, int oneBasedIndex, bool keepExistingHighlight = true)
    {
        var mix = Resolve(oneBasedIndex);
        vacancy.CategoryId = mix.CategoryId;
        vacancy.Kind = mix.Kind;
        vacancy.SuitableFor65Plus = mix.SuitableFor65Plus;

        if (mix.PreferHighlight)
        {
            vacancy.IsHighlighted = true;
            vacancy.HighlightedUntil ??= DateTime.UtcNow.AddDays(VacancyProductRules.HighlightDays);
        }
        else if (!keepExistingHighlight)
        {
            // leave flags as caller set them
        }
    }

    public static int? TrySeedIndex(Guid vacancyId)
    {
        // Westland a1000000-… / Haaglanden a{2|3|4}000000-… → last segment is 1-based index.
        var text = vacancyId.ToString("D");
        if (text.Length < 36
            || text[0] != 'a'
            || text[2..] is not { } rest
            || !rest.StartsWith("000000-0000-4000-8000-", StringComparison.Ordinal))
        {
            return null;
        }

        var tail = text[^12..];
        return int.TryParse(tail, System.Globalization.NumberStyles.None, null, out var n) && n > 0
            ? n
            : null;
    }

    /// <summary>
    /// Idempotent upgrade for environments that already seeded banenkaart vacancies
    /// without category diversity (backfill only assigns Regulier).
    /// </summary>
    public static async Task EnsureAsync(JobsyDbContext db, ILogger logger)
    {
        if (await db.PlatformLogs.AnyAsync(l =>
                l.Category == "Seed" && l.Message == Marker))
        {
            return;
        }

        await new VacancyCategoryService(db).EnsureDefaultsAsync();

        var vacancies = await db.Vacancies.ToListAsync();
        var updated = 0;
        foreach (var vacancy in vacancies)
        {
            if (TrySeedIndex(vacancy.Id) is int index)
            {
                Apply(vacancy, index, keepExistingHighlight: true);
                updated++;
                continue;
            }

            // Well-known demo vacancies (DemoCompanies + Sprint8).
            if (ApplyKnownDemo(vacancy))
            {
                updated++;
            }
        }

        db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "Seed",
            Message = Marker,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        logger.LogInformation(
            "Vacancy category mix applied to {Updated} mock vacancies (all seven types).",
            updated);
    }

    private static readonly Guid DemoOrderpickerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid DemoBaristaId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid DemoRetailId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid DemoIntermediaryVacancyId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid DemoDraftId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid DemoPendingId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
    private static readonly Guid DemoArchivedId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    private static bool ApplyKnownDemo(Vacancy vacancy)
    {
        if (vacancy.Id == DemoOrderpickerId)
        {
            vacancy.CategoryId = VacancyCategoryDefaults.RegulierId;
            vacancy.Kind = VacancyKind.Regular;
            vacancy.SuitableFor65Plus = true;
            return true;
        }

        if (vacancy.Id == DemoBaristaId)
        {
            vacancy.CategoryId = VacancyCategoryDefaults.InclusiefId;
            vacancy.Kind = VacancyKind.Regular;
            vacancy.SuitableFor65Plus = false;
            return true;
        }

        if (vacancy.Id == DemoRetailId)
        {
            vacancy.CategoryId = VacancyCategoryDefaults.InternshipId;
            vacancy.Kind = VacancyKind.Internship;
            vacancy.SuitableFor65Plus = false;
            return true;
        }

        if (vacancy.Id == DemoIntermediaryVacancyId)
        {
            vacancy.CategoryId = VacancyCategoryDefaults.UitzendbureauId;
            vacancy.Kind = VacancyKind.Regular;
            vacancy.SuitableFor65Plus = false;
            return true;
        }

        if (vacancy.Id == DemoDraftId)
        {
            vacancy.CategoryId = VacancyCategoryDefaults.VolunteerId;
            vacancy.Kind = VacancyKind.Volunteer;
            vacancy.SuitableFor65Plus = false;
            return true;
        }

        if (vacancy.Id == DemoPendingId)
        {
            vacancy.CategoryId = VacancyCategoryDefaults.HighlightId;
            vacancy.Kind = VacancyKind.Regular;
            vacancy.SuitableFor65Plus = false;
            return true;
        }

        if (vacancy.Id == DemoArchivedId)
        {
            vacancy.CategoryId = VacancyCategoryDefaults.SeniorLightId;
            vacancy.Kind = VacancyKind.Regular;
            vacancy.SuitableFor65Plus = false;
            return true;
        }

        return false;
    }
}
