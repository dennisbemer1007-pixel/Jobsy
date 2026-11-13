using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;

namespace Jobsy.Core.Rules;

/// <summary>
/// Bipolar 1–5 scale for G78 contrast items (g78-2): 1 = veel meer A, 5 = veel meer B, 3 = neutraal.
/// </summary>
public static class PupilG78ContrastLikertScorer
{
    /// <summary>Likert points credited to the A pole (high when answer leans A).</summary>
    public static int TowardA(int raw) => LikertAnswerJson.LikertMax + LikertAnswerJson.LikertMin - raw;

    /// <summary>Likert points credited to the B pole (high when answer leans B).</summary>
    public static int TowardB(int raw) => raw;

    public static int? ScoreCategory(
        IEnumerable<PupilQuestion> items,
        IReadOnlyDictionary<int, int> answers,
        string category)
    {
        var scored = new List<int>();
        foreach (var item in items)
        {
            if (!answers.TryGetValue(item.Id, out var raw) || !LikertAnswerJson.IsValidAnswer(raw))
            {
                continue;
            }

            if (string.Equals(item.Category, category, StringComparison.Ordinal))
            {
                scored.Add(TowardA(raw));
            }

            if (!string.IsNullOrEmpty(item.CategoryB)
                && string.Equals(item.CategoryB, category, StringComparison.Ordinal))
            {
                scored.Add(TowardB(raw));
            }
        }

        return scored.Count == 0 ? null : LikertAnswerJson.ToPercent(scored);
    }

    public static CompetencyScores? ScoreCompetence(
        IReadOnlyDictionary<int, int> answers,
        IReadOnlyList<PupilQuestion> items)
    {
        var competence = items.Where(q => q.Model == AssessmentKind.Competence).ToList();
        var samenwerken = ScoreCategory(competence, answers, CompetencyTestCatalog.Samenwerken);
        var resultaat = ScoreCategory(competence, answers, CompetencyTestCatalog.Resultaatgerichtheid);
        var stress = ScoreCategory(competence, answers, CompetencyTestCatalog.Stressbestendigheid);
        var innovatie = ScoreCategory(competence, answers, CompetencyTestCatalog.Innovatie);
        var extraversie = ScoreCategory(competence, answers, CompetencyTestCatalog.Extraversie);
        if (samenwerken is null && resultaat is null && stress is null && innovatie is null && extraversie is null)
        {
            return null;
        }

        return new CompetencyScores(samenwerken, resultaat, stress, innovatie, extraversie);
    }

    public static RiasecScores? ScoreCareer(
        IReadOnlyDictionary<int, int> answers,
        IReadOnlyList<PupilQuestion> items)
    {
        var career = items.Where(q => q.Model == AssessmentKind.Career).ToList();
        int? Realistic() => ScoreCategory(career, answers, CareerTestCatalog.Realistic);
        int? Investigative() => ScoreCategory(career, answers, CareerTestCatalog.Investigative);
        int? Artistic() => ScoreCategory(career, answers, CareerTestCatalog.Artistic);
        int? Social() => ScoreCategory(career, answers, CareerTestCatalog.Social);
        int? Enterprising() => ScoreCategory(career, answers, CareerTestCatalog.Enterprising);
        int? Conventional() => ScoreCategory(career, answers, CareerTestCatalog.Conventional);
        var scores = new RiasecScores(Realistic(), Investigative(), Artistic(), Social(), Enterprising(), Conventional());
        return scores.Realistic is null && scores.Investigative is null && scores.Artistic is null
               && scores.Social is null && scores.Enterprising is null && scores.Conventional is null
            ? null
            : scores;
    }

    public static SchwartzValuesScores? ScoreValues(
        IReadOnlyDictionary<int, int> answers,
        IReadOnlyList<PupilQuestion> items)
    {
        var values = items.Where(q => q.Model == AssessmentKind.Values).ToList();
        var autonomy = ScoreCategory(values, answers, SchwartzValuesCatalog.Autonomy);
        var connection = ScoreCategory(values, answers, SchwartzValuesCatalog.Connection);
        var achievement = ScoreCategory(values, answers, SchwartzValuesCatalog.Achievement);
        var stability = ScoreCategory(values, answers, SchwartzValuesCatalog.Stability);
        var impact = ScoreCategory(values, answers, SchwartzValuesCatalog.Impact);
        if (autonomy is null && connection is null && achievement is null && stability is null && impact is null)
        {
            return null;
        }

        return new SchwartzValuesScores(autonomy, connection, achievement, stability, impact);
    }

    public static CulturePersonalityScores? ScoreCulture(
        IReadOnlyDictionary<int, int> answers,
        IReadOnlyList<PupilQuestion> items)
    {
        var culture = items.Where(q => q.Model == AssessmentKind.Culture).ToList();
        var autonomy = ScoreCategory(culture, answers, CulturePersonalityCatalog.Autonomy);
        var informal = ScoreCategory(culture, answers, CulturePersonalityCatalog.Informal);
        var collaboration = ScoreCategory(culture, answers, CulturePersonalityCatalog.Collaboration);
        var flexibility = ScoreCategory(culture, answers, CulturePersonalityCatalog.Flexibility);
        var innovation = ScoreCategory(culture, answers, CulturePersonalityCatalog.Innovation);
        var peopleFirst = ScoreCategory(culture, answers, CulturePersonalityCatalog.PeopleFirst);
        if (autonomy is null && informal is null && collaboration is null
            && flexibility is null && innovation is null && peopleFirst is null)
        {
            return null;
        }

        return new CulturePersonalityScores(
            autonomy,
            informal,
            collaboration,
            flexibility,
            innovation,
            peopleFirst);
    }
}
