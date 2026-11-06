namespace Jobsy.Core.Rules;

/// <summary>
/// Work-context hard filters (module E) and light candidate-facing explanations. Phase 1: filter only, no score weight.
/// </summary>
public static class WorkContextMatchRules
{
    public sealed record Result(
        bool HardMismatch,
        bool CandidateAnswered,
        bool VacancyAnswered,
        IReadOnlyList<ProfileMatchExplainPoint> Notes);

    public static Result Evaluate(
        CandidateWorkContextSnapshot candidate,
        VacancyWorkContextSnapshot? vacancy)
    {
        var notes = new List<ProfileMatchExplainPoint>();
        var candAnswered = CandidateWorkContextProfile.HasAny(candidate);
        var vacAnswered = vacancy is not null && HasVacancyAnswer(vacancy);

        if (!candAnswered || vacancy is null || !vacAnswered)
        {
            return new Result(false, candAnswered, vacAnswered, notes);
        }

        if (IsOutdoorHardMismatch(candidate.Outdoor, vacancy.IndoorOutdoor))
        {
            notes.Add(new ProfileMatchExplainPoint(
                "workcontext",
                "outdoor",
                "Buitenwerk past niet bij je voorkeur."));
            return new Result(true, true, true, notes);
        }

        if (IsIndoorHardMismatch(candidate.Indoor, vacancy.IndoorOutdoor))
        {
            notes.Add(new ProfileMatchExplainPoint(
                "workcontext",
                "indoor",
                "Binnenwerk past niet bij je voorkeur."));
            return new Result(true, true, true, notes);
        }

        if (IsPhysicalHardMismatch(candidate.PhysicalWork, vacancy.Physical))
        {
            notes.Add(new ProfileMatchExplainPoint(
                "workcontext",
                "physical",
                "Fysieke belasting wijkt sterk af van je voorkeur."));
            return new Result(true, true, true, notes);
        }

        if (IsPaceHardMismatch(candidate.Pace, vacancy.Pace))
        {
            notes.Add(new ProfileMatchExplainPoint(
                "workcontext",
                "pace",
                "Tempo op deze functie past niet bij je voorkeur."));
            return new Result(true, true, true, notes);
        }

        if (IsCustomerHardMismatch(candidate.CustomerContact, vacancy.CustomerContact))
        {
            notes.Add(new ProfileMatchExplainPoint(
                "workcontext",
                "customer",
                "Klantcontact op deze functie past niet bij je voorkeur."));
            return new Result(true, true, true, notes);
        }

        if (notes.Count == 0 && candAnswered && vacAnswered)
        {
            notes.Add(new ProfileMatchExplainPoint(
                "workcontext",
                "align",
                "Werkcontext sluit aan bij je voorkeuren."));
        }

        return new Result(false, true, true, notes);
    }

    private static bool HasVacancyAnswer(VacancyWorkContextSnapshot v)
        => v.Pace is not null
           || v.Physical is not null
           || v.IndoorOutdoor is not null
           || v.CustomerContact is not null;

    private static bool IsOutdoorHardMismatch(string? candidateOutdoor, string? vacancyPlace)
    {
        if (candidateOutdoor != "rather-not" || vacancyPlace is null)
        {
            return false;
        }

        return string.Equals(vacancyPlace, "prefer-outdoor", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsIndoorHardMismatch(string? candidateIndoor, string? vacancyPlace)
    {
        if (candidateIndoor != "rather-not" || vacancyPlace is null)
        {
            return false;
        }

        return string.Equals(vacancyPlace, "prefer-indoor", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPhysicalHardMismatch(string? candidatePhysical, string? vacancyPhysical)
    {
        if (candidatePhysical is null || vacancyPhysical is null)
        {
            return false;
        }

        var candRank = PhysicalRank(candidatePhysical);
        var vacRank = PhysicalRank(vacancyPhysical);
        return vacRank - candRank >= 2;
    }

    private static int PhysicalRank(string code) => code switch
    {
        "light" => 0,
        "standing" => 1,
        "lifting-15" => 2,
        "lifting-25" => 3,
        _ => 1
    };

    private static bool IsPaceHardMismatch(string? candidatePace, string? vacancyPace)
    {
        if (candidatePace is null || vacancyPace is null)
        {
            return false;
        }

        return string.Equals(candidatePace, "calm", StringComparison.OrdinalIgnoreCase)
               && string.Equals(vacancyPace, "piek", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCustomerHardMismatch(string? candidateContact, string? vacancyContact)
    {
        if (candidateContact is null || vacancyContact is null)
        {
            return false;
        }

        return string.Equals(candidateContact, "none", StringComparison.OrdinalIgnoreCase)
               && string.Equals(vacancyContact, "much", StringComparison.OrdinalIgnoreCase);
    }
}
