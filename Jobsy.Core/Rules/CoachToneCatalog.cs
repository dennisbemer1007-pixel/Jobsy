namespace Jobsy.Core.Rules;

/// <summary>B1 coach-voorkeuren: nine questions (7 tone + 2 thinking style).</summary>
public static class CoachToneCatalog
{
    public const int QuestionCount = 9;

    public const string ThinkingFeiten = "feiten_cijfers";
    public const string ThinkingStappen = "stap_voor_stap";
    public const string ThinkingMensen = "via_mensen_gevoel";
    public const string ThinkingPlaatje = "grote_plaatje";

    public sealed record QuestionDef(
        string Key,
        string TitleKey,
        string? HintKey,
        string? SectionKey,
        bool MultiSelect,
        IReadOnlyList<OptionDef> Options);

    public sealed record OptionDef(string Code, string LabelKey, string Icon);

    private static readonly IReadOnlyList<OptionDef> ThinkingOptions =
    [
        new(ThinkingFeiten, "CoachTone.Thinking.Feiten", "chart"),
        new(ThinkingStappen, "CoachTone.Thinking.Stappen", "list"),
        new(ThinkingMensen, "CoachTone.Thinking.Mensen", "heart"),
        new(ThinkingPlaatje, "CoachTone.Thinking.Plaatje", "globe"),
    ];

    public static readonly IReadOnlyList<QuestionDef> Questions =
    [
        new(
            "certainty_worries",
            "CoachTone.Q1.Title",
            "CoachTone.Multi.Hint",
            MultiSelect: true,
            SectionKey: null,
            Options:
            [
                new("onbekende_toekomst", "CoachTone.Q1.O1", "chart"),
                new("bang_verkeerd", "CoachTone.Q1.O2", "alert"),
                new("vaste_uren_missen", "CoachTone.Q1.O3", "clock"),
                new("ken_mensen_niet", "CoachTone.Q1.O4", "users"),
                new("niks_hoeft_niet", "CoachTone.Q1.O5", "ok"),
            ]),
        new(
            "certainty_helps",
            "CoachTone.Q2.Title",
            "CoachTone.Multi.Hint",
            MultiSelect: true,
            SectionKey: null,
            Options:
            [
                new("duidelijke_volgende_stap", "CoachTone.Q2.O1", "list"),
                new("vaste_afspraken", "CoachTone.Q2.O2", "calendar"),
                new("iemand_uitlegt", "CoachTone.Q2.O3", "chat"),
                new("rustig_tempo", "CoachTone.Q2.O4", "pace"),
                new("overslaan", "CoachTone.Q2.O5", "skip"),
            ]),
        new(
            "autonomy_choose",
            "CoachTone.Q3.Title",
            "CoachTone.Multi.Hint",
            MultiSelect: true,
            SectionKey: null,
            Options:
            [
                new("welke_banen", "CoachTone.Q3.O1", "search"),
                new("wanneer_solliciteren", "CoachTone.Q3.O2", "calendar"),
                new("hoe_voorbereiden", "CoachTone.Q3.O3", "prep"),
                new("met_wie_praten", "CoachTone.Q3.O4", "chat"),
                new("rustig_vanuit_lobsy", "CoachTone.Q3.O5", "lobsy"),
            ]),
        new(
            "autonomy_too_much",
            "CoachTone.Q4.Title",
            "CoachTone.Multi.Hint",
            MultiSelect: true,
            SectionKey: null,
            Options:
            [
                new("te_veel_stappen", "CoachTone.Q4.O1", "stack"),
                new("veel_regels", "CoachTone.Q4.O2", "rules"),
                new("iemand_kiest", "CoachTone.Q4.O3", "hand"),
                new("te_snel", "CoachTone.Q4.O4", "fast"),
                new("overslaan", "CoachTone.Q4.O5", "skip"),
            ]),
        new(
            "status_seen_as",
            "CoachTone.Q5.Title",
            "CoachTone.Multi.Hint",
            MultiSelect: true,
            SectionKey: null,
            Options:
            [
                new("mijn_best_doen", "CoachTone.Q5.O1", "star"),
                new("dat_ik_leer", "CoachTone.Q5.O2", "learn"),
                new("betrouwbaar", "CoachTone.Q5.O3", "shield"),
                new("eerlijk_over_kunnen", "CoachTone.Q5.O4", "heart"),
                new("overslaan", "CoachTone.Q5.O5", "skip"),
            ]),
        new(
            "relatedness_support",
            "CoachTone.Q6.Title",
            "CoachTone.Multi.Hint",
            MultiSelect: true,
            SectionKey: null,
            Options:
            [
                new("alleen_uitleg", "CoachTone.Q6.O1", "solo"),
                new("collega_team", "CoachTone.Q6.O2", "team"),
                new("begeleider", "CoachTone.Q6.O3", "mentor"),
                new("familie_vrienden", "CoachTone.Q6.O4", "heart"),
                new("overslaan", "CoachTone.Q6.O5", "skip"),
            ]),
        new(
            "fairness_important",
            "CoachTone.Q7.Title",
            "CoachTone.Multi.Hint",
            MultiSelect: true,
            SectionKey: null,
            Options:
            [
                new("eerlijke_kansen", "CoachTone.Q7.O1", "scale"),
                new("geen_valse_beloftes", "CoachTone.Q7.O2", "alert"),
                new("gelijke_kans", "CoachTone.Q7.O3", "equal"),
                new("duidelijk_wat_doen", "CoachTone.Q7.O4", "list"),
                new("overslaan", "CoachTone.Q7.O5", "skip"),
            ]),
        new(
            "thinking_learn",
            "CoachTone.Q8.Title",
            "CoachTone.Single.Hint",
            MultiSelect: false,
            SectionKey: "CoachTone.Section.Thinking",
            Options: ThinkingOptions),
        new(
            "thinking_new_task",
            "CoachTone.Q9.Title",
            "CoachTone.Single.Hint",
            MultiSelect: false,
            SectionKey: "CoachTone.Section.Thinking",
            Options: ThinkingOptions),
    ];

    public static QuestionDef? TryQuestion(string key)
        => Questions.FirstOrDefault(q => string.Equals(q.Key, key, StringComparison.OrdinalIgnoreCase));

    public static string? TryOptionLabelKey(string questionKey, string code)
    {
        var q = TryQuestion(questionKey);
        if (q is null)
        {
            return null;
        }

        return q.Options.FirstOrDefault(o => string.Equals(o.Code, code, StringComparison.OrdinalIgnoreCase))?.LabelKey;
    }

    public static string SummaryGroupKey(string questionKey) => questionKey switch
    {
        "certainty_worries" => "CoachTone.Summary.CertaintyWorries",
        "certainty_helps" => "CoachTone.Summary.CertaintyHelps",
        "autonomy_choose" => "CoachTone.Summary.AutonomyChoose",
        "autonomy_too_much" => "CoachTone.Summary.AutonomyTooMuch",
        "status_seen_as" => "CoachTone.Summary.Status",
        "relatedness_support" => "CoachTone.Summary.Relatedness",
        "fairness_important" => "CoachTone.Summary.Fairness",
        "thinking_learn" or "thinking_new_task" => "CoachTone.Summary.Thinking",
        _ => "CoachTone.Summary.Other",
    };
}
