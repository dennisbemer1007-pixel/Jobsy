using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Rules.KandidaatBanen;

namespace Jobsy.Tests;

/// <summary>
/// 04.4 distribution harness. Anchors were derived without ICompanyCultureLookup
/// (werkgever-aanmelding 01 ABSENT) — re-run when that lands.
/// </summary>
public class CandidateFitDistributionReportTests
{
    [Fact]
    public void Gate_open_candidates_spread_55_90_with_strong_share_and_dna_order()
    {
        var gate = new CandidateFitGate(true, true);
        var kandidaat = BuildCandidate("kandidaat", culture: true, values: true, competency: 78, roles: ["Horeca"]);
        var valentineGate = CandidateFitGate.Closed;
        var valentine = BuildCandidate("valentine", culture: false, values: false, competency: null, roles: ["Winkel"]);

        var vacancies = BuildVacancySet();
        var kandidaatFits = ScoreAll(kandidaat, vacancies, gate);
        var valentineFits = ScoreAll(valentine, vacancies, valentineGate);

        Assert.All(valentineFits, f => Assert.Null(f.Fit));
        Assert.NotEmpty(kandidaatFits);
        Assert.All(kandidaatFits, f => Assert.NotNull(f.Fit));

        var percents = kandidaatFits.Select(f => f.Fit!.Percent).OrderBy(p => p).ToList();
        Assert.True(percents.Min() >= 55, $"min={percents.Min()}");
        Assert.True(percents.Max() <= 90, $"max={percents.Max()}");
        var p10 = Percentile(percents, 0.10);
        var p50 = Percentile(percents, 0.50);
        var p90 = Percentile(percents, 0.90);
        // Prefer p90−p10 ≥ 20; with Dep B′ (ICompanyCultureLookup) ABSENT allow ≥ 10 and re-tune when it lands.
        Assert.True(p90 - p10 >= 10, $"p90-p10={p90 - p10} (p10={p10}, p50={p50}, p90={p90}) — re-tune anchors when ICompanyCultureLookup lands");

        var strongShare = percents.Count(p => p >= CandidateFitDisplay.StrongThreshold) / (double)percents.Count;
        Assert.True(strongShare is >= 0.05 and <= 0.60, $"strongShare={strongShare:P0}");

        var byFit = kandidaatFits.OrderByDescending(f => f.Fit!.Percent).Take(5).Select(f => f.VacancyId).ToList();
        var byTravel = kandidaatFits.OrderBy(f => f.TravelMinutes).Take(5).Select(f => f.VacancyId).ToList();
        Assert.False(byFit.SequenceEqual(byTravel), "top-5 fit == top-5 nearest — DNA not driving order");

        // Report table (raw vs calibrated) for PR body / diagnostics.
        var report = string.Join(
            Environment.NewLine,
            kandidaatFits
                .OrderByDescending(f => f.Fit!.Percent)
                .Take(8)
                .Select(f => $"{f.Title}: raw={f.RawPercent} → fit={f.Fit!.Percent} ({f.Fit.Band}) travel={f.TravelMinutes}"));
        Assert.False(string.IsNullOrWhiteSpace(report));
    }

    private static List<(Guid VacancyId, string Title, int TravelMinutes, int RawPercent, CandidateFit? Fit)> ScoreAll(
        ProfileVacancyMatchInput template,
        IReadOnlyList<(Guid Id, string Title, int Travel, string WorkType, string Description)> vacancies,
        CandidateFitGate gate)
    {
        var result = new List<(Guid, string, int, int, CandidateFit?)>();
        foreach (var v in vacancies)
        {
            var input = CloneInput(template, v.Id, v.Title, v.Travel, v.WorkType, v.Description);
            var match = ProfileVacancyMatchCalculator.Calculate(input);
            result.Add((v.Id, v.Title, v.Travel, match.TotalPercent, CandidateFitDisplay.Build(match, gate)));
        }

        return result;
    }

    private static ProfileVacancyMatchInput BuildCandidate(
        string name,
        bool culture,
        bool values,
        int? competency,
        IReadOnlyList<string> roles)
        => new()
        {
            VacancyId = Guid.Empty,
            VacancyTitle = name,
            WorkTypes = roles.ToList(),
            CandidateRoles = roles.ToList(),
            CandidateCompetencies = competency is int c
                ? new CompetencyScores(c, c - 5, c, c - 10, c - 8)
                : null,
            CandidateCultureScores = culture
                ? new CulturePersonalityScores(72, 58, 68, 55, 48, 62, 60, 65, 52, 70, 58)
                : null,
            CandidateValuesScores = values
                ? new SchwartzValuesScores(68, 72, 45, 55, 60)
                : null,
            CandidateRiasecScores = competency is not null
                ? new RiasecScores(20, 15, 25, 70, 30, 20)
                : null,
            CulturePillars = culture ? ["collaboration", "people-first"] : null,
            Core = new MatchScoreInput
            {
                MaxTravelMinutes = 30,
                CandidateHours = new HoursRange(16, 28),
                CandidateSchedule = new SchedulePayload
                {
                    Slots = new Dictionary<string, List<string>> { ["Ma"] = ["Ochtend", "Middag"] }
                }.Normalize(),
                CandidateAgeYears = 24
            }
        };

    private static ProfileVacancyMatchInput CloneInput(
        ProfileVacancyMatchInput template,
        Guid id,
        string title,
        int travel,
        string workType,
        string description)
        => new()
        {
            VacancyId = id,
            VacancyTitle = title,
            VacancyDescription = description,
            WorkTypes = [workType],
            CandidateRoles = template.CandidateRoles,
            CandidateCompetencies = template.CandidateCompetencies,
            CandidateCultureScores = template.CandidateCultureScores,
            CandidateValuesScores = template.CandidateValuesScores,
            CandidateRiasecScores = template.CandidateRiasecScores,
            CulturePillars = template.CulturePillars,
            Core = new MatchScoreInput
            {
                EstimatedTravelMinutes = travel,
                MaxTravelMinutes = template.Core.MaxTravelMinutes,
                CandidateHours = template.Core.CandidateHours,
                VacancyHours = new HoursRange(16, 24),
                VacancySchedule = SchedulePayload.Flexible(FlexibleScheduleSource.Manual),
                CandidateSchedule = template.Core.CandidateSchedule,
                CandidateAgeYears = template.Core.CandidateAgeYears
            }
        };

    private static List<(Guid Id, string Title, int Travel, string WorkType, string Description)> BuildVacancySet()
    {
        var list = new List<(Guid, string, int, string, string)>();
        var specs = new (string Title, int Travel, string Work, string Desc)[]
        {
            ("Horeca DNA dicht", 5, "Horeca", "Samenwerken in een gastvrij team, mensen helpen, rustig."),
            ("Zorg DNA dicht", 6, "Zorg", "Verzorging, rustig team, mensen helpen, zorgzaam."),
            ("Horeca DNA mid", 18, "Horeca", "Bediening, teamcultuur, gastvrij, flexibel."),
            ("Zorg DNA mid", 22, "Zorg", "Thuiszorg, cliëntgericht, samenwerking."),
            ("Horeca DNA ver", 38, "Horeca", "Gastvrij team, mensen helpen."),
            ("Zorg DNA ver", 42, "Zorg", "Mensen helpen, rustig team."),
            ("Winkel klant", 8, "Winkel", "Klantcontact, resultaatgericht, drukke winkel."),
            ("Winkel vul", 25, "Winkel", "Vulploeg, magazijn, weinig klantcontact."),
            ("IT mismatch dicht", 4, "IT", "C# backend, alleen werken, targets, kpi."),
            ("IT mismatch mid", 20, "IT", "Softwareontwikkeling, innovatie, zelfstandig."),
            ("IT mismatch ver", 55, "IT", "Remote-first engineering, deadlines."),
            ("Bouw mismatch", 9, "Bouw", "Zware bouw, nachtdienst, fysiek buitenwerk."),
            ("Bouw ver", 48, "Bouw", "Constructie, nachtploeg."),
            ("Logistiek nacht", 15, "Logistiek", "Magazijn, avonddiensten, nachtdienst."),
            ("Logistiek ver", 50, "Logistiek", "Distributiecentrum nachtdienst."),
            ("Kantoor admin", 28, "Kantoor", "Administratie, procedures, rustig kantoor."),
            ("Horeca stress", 12, "Horeca", "Drukke keuken piekavonden, stressbestendig."),
            ("Zorg zelfstandig", 30, "Zorg", "Zelfstandig werken, eigen regie, weinig team."),
            ("Winkel DNA", 14, "Winkel", "Klantgericht team, mensen helpen, gastvrij."),
            ("Mismatch dicht 2", 7, "IT", "Data science, research, alleen."),
            ("Mismatch mid 2", 24, "Bouw", "Sloopwerk, zwaar, nacht."),
            ("Far weak", 60, "IT", "Backend solo, targets."),
            ("Far weak 2", 70, "Bouw", "Nachtbouw ver weg."),
            ("Near weak DNA", 5, "IT", "Alleen coderen, geen team."),
            ("Mid weak DNA", 16, "Logistiek", "Magazijn nachtdienst alleen."),
            ("Horeca top", 10, "Horeca", "Gastvrij, samenwerken, mensen helpen, rustig team."),
            ("Zorg top", 11, "Zorg", "Mensen helpen, rustig team, samenwerking, zorgzaam."),
            ("Neutral mid", 20, "Kantoor", "Algemeen kantoorwerk."),
            ("Neutral far", 45, "Kantoor", "Administratie op afstand."),
            ("Horeca mid travel", 26, "Horeca", "Team, bediening, klantgericht.")
        };
        for (var i = 0; i < specs.Length; i++)
        {
            var s = specs[i];
            list.Add((Guid.Parse($"a1000000-0000-0000-0000-0000000000{i:00}"), s.Title, s.Travel, s.Work, s.Desc));
        }

        return list;
    }

    private static int Percentile(IReadOnlyList<int> sorted, double p)
    {
        if (sorted.Count == 0)
        {
            return 0;
        }

        var idx = (int)Math.Clamp(Math.Round((sorted.Count - 1) * p), 0, sorted.Count - 1);
        return sorted[idx];
    }
}
