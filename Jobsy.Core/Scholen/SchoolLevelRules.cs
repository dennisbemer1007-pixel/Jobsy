using Jobsy.Core.Enums;

namespace Jobsy.Core.Scholen;

/// <summary>
/// Pure rules: class level → question set, leerjaar validation, and display labels.
/// The class level is the only source of which test every pupil code of that class gets.
/// </summary>
public static class SchoolLevelRules
{
    public static IReadOnlyList<SchoolLevel> VoLevels { get; } =
    [
        SchoolLevel.VmboB,
        SchoolLevel.VmboK,
        SchoolLevel.VmboGt,
        SchoolLevel.Mavo,
        SchoolLevel.Havo,
        SchoolLevel.Vwo,
        SchoolLevel.Mix,
        SchoolLevel.Anders
    ];

    public static IReadOnlyList<SchoolLevel> MboHboLevels { get; } =
    [
        SchoolLevel.Mbo1,
        SchoolLevel.Mbo2,
        SchoolLevel.Mbo3,
        SchoolLevel.Mbo4,
        SchoolLevel.Hbo
    ];

    public static PupilQuestionSet QuestionSetFor(SchoolLevel level)
    {
        if (level == SchoolLevel.Groep78)
        {
            return PupilQuestionSet.Groep78;
        }

        if (IsMboHbo(level))
        {
            return PupilQuestionSet.MboHbo;
        }

        return PupilQuestionSet.Vo;
    }

    public static bool IsPrimary(SchoolLevel level) => level == SchoolLevel.Groep78;

    public static bool IsMboHbo(SchoolLevel level)
        => level is SchoolLevel.Mbo1 or SchoolLevel.Mbo2 or SchoolLevel.Mbo3 or SchoolLevel.Mbo4
            or SchoolLevel.Hbo;

    public static bool IsHbo(SchoolLevel level) => level == SchoolLevel.Hbo;

    /// <summary>
    /// True when changing level would switch the question set (G78 ↔ VO).
    /// Within the same set (havo→vwo, groep 7→8) this is false.
    /// </summary>
    public static bool StartedCodesBlockChange(SchoolLevel from, SchoolLevel to)
        => QuestionSetFor(from) != QuestionSetFor(to);

    public static string? ValidateYear(SchoolLevel level, int year)
    {
        if (IsPrimary(level))
        {
            return year is 7 or 8 ? null : "Kies groep 7 of groep 8.";
        }

        if (IsMboHbo(level))
        {
            return year is >= 1 and <= 4 ? null : "Leerjaar moet 1–4 zijn.";
        }

        return year is >= 1 and <= 6 ? null : "Leerjaar moet 1–6 zijn.";
    }

    public static string LabelKey(SchoolLevel level) => $"School.Level.{level}";

    /// <summary>Dutch year label for lists and scope chips: "Groep 7" / "Klas 2".</summary>
    public static string YearLabel(SchoolLevel level, int year)
        => IsPrimary(level) ? $"Groep {year}" : $"Klas {year}";

    public static string QuestionSetPillLabel(PupilQuestionSet set)
        => set switch
        {
            PupilQuestionSet.Groep78 => "Groep 7/8",
            PupilQuestionSet.MboHbo => "MBO/HBO",
            _ => "VO"
        };
}
