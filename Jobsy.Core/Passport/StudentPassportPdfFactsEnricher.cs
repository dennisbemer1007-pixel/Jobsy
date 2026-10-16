using Jobsy.Core.Contracts;
using Jobsy.Core.Interfaces;

namespace Jobsy.Core.Passport;

/// <summary>Applies student/stagiair layout fields on top of a standard passport facts record.</summary>
public static class StudentPassportPdfFactsEnricher
{
    public static PassportPdfFacts Apply(
        PassportPdfFacts facts,
        CandidatePreferencesDto preferences,
        IReadOnlyList<CandidateMatchedVacancyDto> matches)
    {
        var stageMatches = matches
            .Take(3)
            .Select(m => new PassportJobFit(
                m.Title,
                m.Why.Count > 0 ? string.Join(" · ", m.Why.Take(3)) : m.CompanyName))
            .ToList();

        return facts with
        {
            StudentPassportLayout = true,
            StudentStudyChip = StudyChip(preferences),
            StudentInternshipChip = InternshipChip(preferences),
            StageVacancyMatches = stageMatches,
            StudyDirectionSteps = StudyDirection(facts, preferences),
            ShareEmployerPreferences = false
        };
    }

    private static string? StudyChip(CandidatePreferencesDto preferences)
    {
        var ed = preferences.Educations ?? [];
        if (ed.Count == 0)
        {
            return null;
        }

        var line = ed[0].Trim();
        if (ed.Count > 1 && ed[1].Contains("jaar", StringComparison.OrdinalIgnoreCase))
        {
            return $"{line} · {ed[1].Trim()}";
        }

        return line;
    }

    private static string? InternshipChip(CandidatePreferencesDto preferences)
    {
        foreach (var line in preferences.Educations ?? [])
        {
            if (line.Contains("stage", StringComparison.OrdinalIgnoreCase))
            {
                return line.Trim();
            }
        }

        return null;
    }

    private static IReadOnlyList<PassportDirectionStep> StudyDirection(
        PassportPdfFacts facts,
        CandidatePreferencesDto preferences)
    {
        var steps = new List<PassportDirectionStep>();
        var ed = preferences.Educations ?? [];
        if (ed.Count > 0)
        {
            steps.Add(new PassportDirectionStep(
                StudyChip(preferences) ?? ed[0],
                "Nu"));
        }

        var internship = InternshipChip(preferences);
        if (!string.IsNullOrWhiteSpace(internship))
        {
            steps.Add(new PassportDirectionStep(internship, "Stageperiode"));
        }

        if (!string.IsNullOrWhiteSpace(facts.EducationDirection))
        {
            steps.Add(new PassportDirectionStep(facts.EducationDirection.Trim(), "Daarna"));
        }

        var dream = facts.DreamTitle ?? facts.Motivation;
        if (!string.IsNullOrWhiteSpace(dream))
        {
            steps.Add(new PassportDirectionStep(dream.Trim(), "Doel"));
        }

        return steps.Count >= 2 ? steps.Take(4).ToArray() : [];
    }
}
