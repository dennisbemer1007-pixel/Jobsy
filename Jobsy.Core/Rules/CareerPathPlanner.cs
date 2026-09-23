using Jobsy.Core.Contracts;

namespace Jobsy.Core.Rules;

public sealed record CareerPathStep(
    int Order,
    string Title,
    int DurationMonths,
    string DurationLabel,
    string Detail);

public sealed record CareerPathPlan(
    int TotalMonths,
    string DurationLabel,
    string Summary,
    IReadOnlyList<CareerPathStep> Steps);

/// <summary>
/// Turns a vacancy or job title into a stepped route with a concrete duration.
/// </summary>
public static class CareerPathPlanner
{
    public static CareerPathPlan? ForTitle(string? title)
        => Personalize(title, prefs: null, formal: null);

    public static CareerPathPlan? Personalize(
        string? title,
        CandidatePreferencesDto? prefs,
        VacancyBarrierCheck? formal)
    {
        OccupationTaxonomy.TryGetTrack(title, out _, out var template);
        var blob = EvidenceBlob(prefs, formal);
        var remaining = new List<EducationStepTemplate>();
        foreach (var step in template)
        {
            if (!EvidenceMet(step, blob))
            {
                remaining.Add(step);
            }
        }

        foreach (var extra in MissingRequirements(formal, remaining))
        {
            remaining.Add(extra);
        }

        if (template.Count == 0 && remaining.Count == 0)
        {
            return null;
        }

        var steps = remaining
            .Select((step, index) => new CareerPathStep(
                index + 1,
                step.Title,
                step.DurationMonths,
                FormatDuration(step.DurationMonths),
                step.Detail))
            .ToList();
        var total = steps.Sum(s => s.DurationMonths);
        return new CareerPathPlan(total, FormatDuration(total), Summary(total), steps);
    }

    public static string FormatDuration(int months)
    {
        if (months <= 0)
        {
            return "nu";
        }

        if (months < 12)
        {
            return months == 1 ? "1 maand" : $"{months} maanden";
        }

        var years = months / 12;
        var rest = months % 12;
        var yearPart = years == 1 ? "1 jaar" : $"{years} jaar";
        if (rest == 0)
        {
            return yearPart;
        }

        var monthPart = rest == 1 ? "1 maand" : $"{rest} maanden";
        return $"{yearPart} en {monthPart}";
    }

    public static string Summary(int months)
        => months <= 0
            ? "Je voldoet al aan de opleidingen en harde eisen. Je kunt deze vacature nu bereiken."
            : $"Met de volgende opleidingen en cursussen kun je binnen {FormatDuration(months)} deze vacature bereiken.";

    public static bool IsSummary(string? text)
        => !string.IsNullOrWhiteSpace(text)
           && text.Contains("deze vacature bereiken", StringComparison.OrdinalIgnoreCase);

    public static IReadOnlyList<string> WithSummary(IReadOnlyList<string>? steps, string summary)
    {
        var list = (steps ?? [])
            .Where(s => !IsSummary(s))
            .ToList();
        list.Insert(0, summary);
        return list.Take(5).ToList();
    }

    public static int CertificationMonths(string cert)
    {
        var folded = CareerOccupationKeys.Fold(cert);
        if (folded.Contains("vlieg", StringComparison.Ordinal) || folded.Contains("atpl", StringComparison.Ordinal))
        {
            return 24;
        }

        if (folded.Contains("big", StringComparison.Ordinal))
        {
            return 2;
        }

        if (folded.Contains("vca", StringComparison.Ordinal)
            || folded.Contains("bhv", StringComparison.Ordinal)
            || folded.Contains("haccp", StringComparison.Ordinal)
            || folded.Contains("heftruck", StringComparison.Ordinal))
        {
            return 1;
        }

        return 3;
    }

    public static int DiplomaMonths(string diploma)
    {
        var folded = CareerOccupationKeys.Fold(diploma);
        if (folded.Contains("geneeskunde", StringComparison.Ordinal))
        {
            return 72;
        }

        if (folded.Contains("hbo", StringComparison.Ordinal) || folded.Contains("wo", StringComparison.Ordinal))
        {
            return 48;
        }

        if (folded.Contains("mbo", StringComparison.Ordinal))
        {
            return 36;
        }

        return 12;
    }

    private static IEnumerable<EducationStepTemplate> MissingRequirements(
        VacancyBarrierCheck? formal,
        IReadOnlyList<EducationStepTemplate> remaining)
    {
        if (formal is null)
        {
            yield break;
        }

        foreach (var item in formal.Items.Where(i => !i.Met && i.IsDealbreaker))
        {
            var label = StripPrefix(item.Label);
            if (remaining.Any(step => Covers(step, label)) || string.IsNullOrWhiteSpace(label))
            {
                continue;
            }

            var months = item.Key.StartsWith("cert:", StringComparison.Ordinal)
                ? CertificationMonths(label)
                : item.Key.StartsWith("hard:", StringComparison.Ordinal)
                    ? 1
                    : item.Key.StartsWith("diploma:", StringComparison.Ordinal)
                        ? DiplomaMonths(label)
                        : 1;
            var detail = item.Key.StartsWith("hard:", StringComparison.Ordinal)
                ? "Harde eis van deze vacature. Zonder dit bewijs kun je niet starten."
                : "Dit vraagt de vacature extra, bovenop het gebruikelijke pad.";
            yield return new EducationStepTemplate(label, months, detail, []);
        }
    }

    private static bool Covers(EducationStepTemplate step, string label)
    {
        var folded = CareerOccupationKeys.Fold($"{step.Title} {step.Detail}");
        var needle = CareerOccupationKeys.Fold(label);
        if (needle.Length < 4)
        {
            return false;
        }

        return CareerOccupationKeys.Hits(folded, needle)
               || folded.Contains(needle, StringComparison.Ordinal);
    }

    private static string StripPrefix(string label)
    {
        var idx = label.IndexOf(':');
        return (idx >= 0 ? label[(idx + 1)..] : label).Trim();
    }

    private static string EvidenceBlob(CandidatePreferencesDto? prefs, VacancyBarrierCheck? formal)
    {
        var parts = new List<string>();
        if (prefs?.Educations is not null)
        {
            parts.AddRange(prefs.Educations);
        }

        if (prefs?.Certificates is not null)
        {
            parts.AddRange(prefs.Certificates.Select(c => c.Name));
        }

        if (prefs?.DrivingLicenses is not null)
        {
            parts.AddRange(prefs.DrivingLicenses);
        }

        if (formal is not null)
        {
            parts.AddRange(formal.Items.Where(i => i.Met).Select(i => i.Label));
        }

        return CareerOccupationKeys.Fold(string.Join(' ', parts));
    }

    private static bool EvidenceMet(EducationStepTemplate step, string foldedBlob)
    {
        if (foldedBlob.Length == 0 || step.EvidenceKeys.Length == 0)
        {
            return false;
        }

        foreach (var key in step.EvidenceKeys)
        {
            var folded = CareerOccupationKeys.Fold(key);
            if (folded.Length >= 3
                && (CareerOccupationKeys.Hits(foldedBlob, folded)
                    || foldedBlob.Contains(folded, StringComparison.Ordinal)))
            {
                return true;
            }
        }

        return false;
    }
}
