using Jobsy.Core.Contracts;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Careers;

/// <summary>
/// Calm "Goed om te weten" tip for a confirmed current job.
/// Adjacent jobs must pass every sourced filter. The list is never padded.
/// </summary>
public static class CurrentJobOutlook
{
    public const string NoAdjacentSentence =
        "We hebben nu geen beroep gevonden dat dichtbij ligt en een betere vooruitblik heeft.";

    public static bool IsCurrent(CandidateEmployerHistoryDto entry)
        => entry.IsCurrent == true
           || (!string.IsNullOrWhiteSpace(entry.StartMonth) && string.IsNullOrWhiteSpace(entry.EndMonth));

    public static CurrentJobTip? TryCreate(
        CandidateEmployerHistoryDto? entry,
        RiasecScores? scores,
        string? education)
    {
        if (entry is null || string.IsNullOrWhiteSpace(entry.EscoId) || !IsCurrent(entry))
        {
            return null;
        }

        var current = OccupationCatalog.Shared.Get(entry.EscoId);
        if (current is null)
        {
            return null;
        }

        var outlook = OccupationOutlook.Shared.Get(current.Id);
        var adjacent = Adjacent(current, scores, education);
        var skills = SkillsToLearn(current.Id, adjacent);
        return new CurrentJobTip(
            current.Id,
            current.Nl,
            outlook,
            adjacent,
            skills,
            adjacent.Count == 0 ? NoAdjacentSentence : null);
    }

    public static IReadOnlyList<string> QuoteLines(CurrentJobTip? tip)
    {
        if (tip is null)
        {
            return [];
        }

        var lines = new List<string>();
        Add(lines, tip.Outlook.DemandLine);
        Add(lines, tip.Outlook.AiLine);
        foreach (var task in tip.Outlook.ChangeTasks)
        {
            Add(lines, task);
        }

        foreach (var task in tip.Outlook.HumanTasks)
        {
            Add(lines, task);
        }

        foreach (var job in tip.Adjacent)
        {
            Add(lines, job.Title);
        }

        foreach (var skill in tip.SkillsToLearn)
        {
            Add(lines, skill);
        }

        Add(lines, tip.NoAdjacentMessage);
        foreach (var source in tip.Outlook.Sources)
        {
            Add(lines, source);
        }

        return lines;
    }

    private static RiasecScores? _rankScores;
    private static List<(Occupation Job, decimal Percent)>? _ranked;

    private static List<AdjacentOccupation> Adjacent(Occupation current, RiasecScores? scores, string? education)
    {
        var picked = new List<AdjacentOccupation>();
        if (scores is not { IsComplete: true })
        {
            return picked;
        }

        if (!OccupationOutlook.Shared.TryGetComparison(current.Id, out var currentIlo, out var currentItkb))
        {
            return picked;
        }

        var skills = OccupationSkills.Shared;
        var currentEssential = skills.Essential(current.Id);
        var gate = CareerEducationGate.MaxIscoLevel(education);
        var allowLead = CareerCompassBuilder.EnterprisingInTop3(scores);
        foreach (var (job, percent) in Ranked(scores))
        {
            if (string.Equals(job.Id, current.Id, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!IsClose(current, job, currentEssential, skills))
            {
                continue;
            }

            if (!IsBetterOutlook(job.Id, currentIlo, currentItkb))
            {
                continue;
            }

            if (job.IscoLevel is not int level || current.IscoLevel is not int currentLevel || level > currentLevel + 1)
            {
                continue;
            }

            if (!CareerEducationGate.Passes(job.IscoLevel, gate))
            {
                continue;
            }

            if (!allowLead && OccupationCatalog.IsLeadership(job))
            {
                continue;
            }

            picked.Add(new AdjacentOccupation(job.Id, job.Nl, percent));
            if (picked.Count == 3)
            {
                break;
            }
        }

        return picked;
    }

    private static IReadOnlyList<(Occupation Job, decimal Percent)> Ranked(RiasecScores scores)
    {
        if (_rankScores == scores && _ranked is not null)
        {
            return _ranked;
        }

        _ranked = OccupationCatalog.Shared.Listable
            .Select(job => (Job: job, Percent: CareerCompassBuilder.ProfileMatch(job.Oi!, scores)))
            .OrderByDescending(item => item.Percent)
            .ThenBy(item => item.Job.Nl, StringComparer.OrdinalIgnoreCase)
            .ToList();
        _rankScores = scores;
        return _ranked;
    }

    private static bool IsClose(
        Occupation current,
        Occupation other,
        IReadOnlyList<int> currentEssential,
        OccupationSkills skills)
    {
        if (current.Isco.Length >= 3
            && other.Isco.Length >= 3
            && string.Equals(current.Isco[..3], other.Isco[..3], StringComparison.Ordinal))
        {
            return true;
        }

        if (currentEssential.Count == 0)
        {
            return false;
        }

        var otherSkills = skills.AllOf(other.Id);
        var shared = 0;
        foreach (var skill in currentEssential)
        {
            if (otherSkills.Contains(skill))
            {
                shared++;
            }
        }

        return (decimal)shared / currentEssential.Count >= 0.20m;
    }

    private static bool IsBetterOutlook(string escoId, int currentIlo, int currentItkb)
    {
        if (!OccupationOutlook.Shared.TryGetComparison(escoId, out var ilo, out var itkb))
        {
            return false;
        }

        var iloNotHigher = ilo <= currentIlo;
        var demandNotLower = itkb >= currentItkb;
        var strictlyBetter = ilo < currentIlo || itkb > currentItkb;
        return iloNotHigher && demandNotLower && strictlyBetter;
    }

    private static IReadOnlyList<string> SkillsToLearn(string currentId, IReadOnlyList<AdjacentOccupation> adjacent)
    {
        if (adjacent.Count == 0)
        {
            return [];
        }

        var skills = OccupationSkills.Shared;
        var already = skills.AllOf(currentId);
        var counts = new Dictionary<int, int>();
        foreach (var job in adjacent)
        {
            foreach (var skill in skills.Essential(job.EscoId))
            {
                if (already.Contains(skill))
                {
                    continue;
                }

                counts.TryGetValue(skill, out var seen);
                counts[skill] = seen + 1;
            }
        }

        return counts
            .Select(pair => (Index: pair.Key, Count: pair.Value, Label: skills.Label(pair.Key)))
            .Where(item => !string.IsNullOrWhiteSpace(item.Label))
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Label, StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .Select(item => item.Label!)
            .ToList();
    }

    private static void Add(List<string> lines, string? text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            lines.Add(text.Trim());
        }
    }
}

public sealed record AdjacentOccupation(string EscoId, string Title, decimal Percent);

public sealed record CurrentJobTip(
    string EscoId,
    string Title,
    OccupationOutlookResult Outlook,
    IReadOnlyList<AdjacentOccupation> Adjacent,
    IReadOnlyList<string> SkillsToLearn,
    string? NoAdjacentMessage);
