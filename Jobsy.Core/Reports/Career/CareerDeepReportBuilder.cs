using Jobsy.Core.Rules;

namespace Jobsy.Core.Reports.Career;

public static class CareerDeepReportBuilder
{
    public static CareerDeepReport Build(
        IReadOnlyList<DeepAnalysisDomainScore> domainScores,
        CareerCompassSnapshot? compass,
        IReadOnlyDictionary<string, double>? normMeans,
        DateTime utcNow)
    {
        var riasec = DeepAnalysisCatalog.ToRiasecScores(domainScores);
        var domains = CareerTestCatalog.RiasecCodes
            .Select(code =>
            {
                var score = riasec.Get(code);
                return new DeepDomainScore
                {
                    Domain = code,
                    Score = score,
                    LevelKey = DeepReportCatalog.LevelKey(score),
                    NormMean = normMeans is not null && normMeans.TryGetValue(code, out var m) ? m : null
                };
            })
            .ToList();

        var holland = CareerTestCatalog.HollandCode(riasec);
        var ranked = domains.OrderByDescending(d => d.Score).ThenBy(d => d.Domain).ToList();
        var top3 = ranked.Take(3).Select(d => d.Domain).ToList();

        var deepened = CareerCompassSanitize.EnsureDepth(
            compass ?? CareerCompassSnapshot.Empty(fromDeepAnalysis: true),
            riasec);
        var occupationList = deepened.AllOccupations
            .OrderByDescending(m => m.Percent)
            .Take(CareerCompassSanitize.MaxCatalogueJobs)
            .ToList();
        var prose = CandidateFactSheet.ForCareerProse(occupationList.Select(m => m.Title));
        var occupations = occupationList
            .Select(m =>
            {
                var why = CandidateFactGuard.WithoutInventedHistory(
                    m.Why,
                    prose,
                    $"Dit beroep sluit aan bij hoe jij scoort ({m.Percent}%).");
                return new DeepOccupationFit
                {
                    TitleNl = m.Title,
                    TitleEn = EnglishOccupation(m.Title),
                    MatchPercent = m.Percent,
                    ReasonNl = why,
                    ReasonEn = EnglishReason(why),
                    Band = m.Band
                };
            })
            .ToList();

        var comparisonAvailable = normMeans is { Count: > 0 };

        return new CareerDeepReport
        {
            ReportVersion = CareerDeepReportJson.CurrentReportVersion,
            GeneratedAtUtc = utcNow,
            FromOpenAi = false,
            Summary = LocalizedReportText.FromPair(
                $"Jouw beroepsletters zijn {holland}. Je scoort het sterkst op {string.Join(", ", top3.Select(c => DeepReportCatalog.RiasecLabel(c, "nl")))}. Daarna zie je beroepen die bij je passen, een actieplan en je sterke kanten.",
                $"Your job letters are {holland}. You score highest on {string.Join(", ", top3.Select(c => DeepReportCatalog.RiasecLabel(c, "en")))}. Next you see jobs that fit, an action plan and your strengths."),
            Domains = domains,
            HollandCode = holland,
            Occupations = occupations,
            ActionPlan = BuildActionPlan(top3, occupations),
            StrengthKeys = top3.Select(c => $"strength.{c}").ToList(),
            PitfallKeys = top3.Select(c => $"pitfall.{c}").ToList(),
            ComparisonAvailable = comparisonAvailable
        };
    }

    private static List<DeepActionStep> BuildActionPlan(
        IReadOnlyList<string> top3,
        IReadOnlyList<DeepOccupationFit> occupations)
    {
        var steps = new List<DeepActionStep>();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var index = 0;
        foreach (var code in top3.Take(3))
        {
            var nl = DeepReportCatalog.RiasecLabel(code, "nl");
            var en = DeepReportCatalog.RiasecLabel(code, "en");
            var job = PickJob(code, occupations, used);
            if (job is not null)
            {
                used.Add(job.TitleNl);
            }

            var jobNl = job?.Title("nl") ?? "een beroep uit je lijst";
            var jobEn = job?.Title("en") ?? "a job from your list";
            var (bodyNl, bodyEn) = ActionBody(index, jobNl, jobEn);
            steps.Add(new DeepActionStep
            {
                Title = LocalizedReportText.FromPair(
                    $"Verken werk rondom {nl}",
                    $"Explore work around {en}"),
                Body = LocalizedReportText.FromPair(bodyNl, bodyEn)
            });
            index++;
        }

        while (steps.Count < 3)
        {
            var filler = steps.Count;
            steps.Add(new DeepActionStep
            {
                Title = LocalizedReportText.FromPair(
                    filler == 1 ? "Vraag hoe een werkdag eruitziet" : "Noteer wat je energie geeft",
                    filler == 1 ? "Ask what a workday looks like" : "Note what gives you energy"),
                Body = LocalizedReportText.FromPair(
                    filler == 1
                        ? "Kies één beroep uit de beroepen die bij je passen. Vraag iemand die dit werk doet hoe een gewone dag gaat."
                        : "Houd een week bij welke taken je energie geven. Gebruik dat bij de beroepen die bij je passen.",
                    filler == 1
                        ? "Pick one job from the jobs that fit you. Ask someone who does this work what a normal day is like."
                        : "For one week, note which tasks give you energy. Use that with the jobs that fit you.")
            });
        }

        return steps;
    }

    private static (string Nl, string En) ActionBody(int index, string jobNl, string jobEn) => index switch
    {
        0 => (
            $"Kies {jobNl} uit de beroepen die bij je passen. Praat met iemand die dit werk doet.",
            $"Pick {jobEn} from the jobs that fit you. Talk to someone who does this work."),
        1 => (
            $"Vraag iemand die werkt als {jobNl} hoe een gewone werkdag eruitziet.",
            $"Ask someone who works as {jobEn} what a normal workday looks like."),
        _ => (
            $"Loop een keer mee met {jobNl}. Let op welke taken je energie geven.",
            $"Shadow someone in {jobEn} once. Notice which tasks give you energy.")
    };

    private static DeepOccupationFit? PickJob(
        string code,
        IReadOnlyList<DeepOccupationFit> occupations,
        HashSet<string> used)
    {
        foreach (var job in occupations)
        {
            if (!PrimaryCodeIs(job.TitleNl, code) || SkipHigherEducation(job.TitleNl, occupations))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(job.TitleNl) || used.Contains(job.TitleNl))
            {
                continue;
            }

            return job;
        }

        return null;
    }

    public static string TypicalPlaces(CareerDeepReport report, string lang)
    {
        if (report.Occupations.Count >= 3)
        {
            return string.Join(", ", report.Occupations.Take(3).Select(o => o.Title(lang)));
        }

        return CareerCompassBuilder.TypicalEnvironments(
            report.Domains.OrderByDescending(d => d.Score).Select(d => d.Domain),
            lang);
    }

    private static bool SkipHigherEducation(string titleNl, IReadOnlyList<DeepOccupationFit> occupations)
    {
        if (!CareerGoalFit.IsClearlyHigherEducation(titleNl))
        {
            return false;
        }

        var code = CareerCompassBuilder.PrimaryCode(titleNl);
        return occupations.Any(other =>
            !string.Equals(other.TitleNl, titleNl, StringComparison.OrdinalIgnoreCase)
            && !CareerGoalFit.IsClearlyHigherEducation(other.TitleNl)
            && string.Equals(CareerCompassBuilder.PrimaryCode(other.TitleNl), code, StringComparison.OrdinalIgnoreCase));
    }

    private static bool PrimaryCodeIs(string titleNl, string code)
        => string.Equals(CareerCompassBuilder.PrimaryCode(titleNl), code, StringComparison.OrdinalIgnoreCase);

    private static string EnglishOccupation(string nl) => nl switch
    {
        "Verpleegkundige" => "Nurse",
        "Docent" => "Teacher",
        "Softwareontwikkelaar" => "Software developer",
        "Accountant" => "Accountant",
        "Verkoper" => "Sales advisor",
        "Monteur" => "Technician",
        "Ontwerper" => "Designer",
        "Onderzoeker" => "Researcher",
        _ => nl // compass titles may already be mixed; keep as-is when unknown
    };

    private static string EnglishReason(string nl)
    {
        if (string.IsNullOrWhiteSpace(nl))
        {
            return "";
        }

        // Plain rewrite of common Dutch reason stems without machine calques.
        return nl
            .Replace("Past bij", "Fits", StringComparison.Ordinal)
            .Replace("je sterke", "your strong", StringComparison.Ordinal)
            .Replace("jouw", "your", StringComparison.OrdinalIgnoreCase);
    }
}
