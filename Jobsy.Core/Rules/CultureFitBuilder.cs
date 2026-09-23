namespace Jobsy.Core.Rules;

/// <summary>
/// Local culture/team-fit engine. OpenAI may refine percent + why; this stays the fallback
/// and the map-ranking source so discover stays fast.
/// </summary>
public static class CultureFitBuilder
{
    public const double TotalScoreWeight = 0.12;
    public const int HighThreshold = 75;
    public const int MidThreshold = 55;

    public static bool HardCriteriaMatch(ProfileVacancyMatchInput input, MatchScoreBreakdown core)
    {
        if (!core.LegalEligible
            || core.TravelWithinPreference == false
            || core.TotalPercent < MatchScoreWeights.GuldenMiddenwegThreshold)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(input.RequiredDrivingLicense)
            && !DrivingLicenseLabels.CandidateMeetsRequirement(input.CandidateLicenses, input.RequiredDrivingLicense))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(input.RequiredEducation)
            && !EducationLevelLabels.CandidateMeetsRequirement(input.CandidateEducations, input.RequiredEducation))
        {
            return false;
        }

        if (input.MinimumEmployers is > 0 && input.CandidateEmployerCount < input.MinimumEmployers)
        {
            return false;
        }

        return true;
    }

    public static CultureFitResult? Evaluate(
        IEnumerable<string>? pillarIds,
        CompetencyScores? scores,
        DiscScores? disc = null)
    {
        if (scores is not { IsComplete: true })
        {
            return null;
        }

        var pillars = CulturePillarCatalog.Normalize(pillarIds)
            .Select(id => CulturePillarCatalog.TryGet(id, out var d) ? d : null)
            .OfType<CulturePillarDefinition>()
            .ToList();
        if (pillars.Count < CulturePillarCatalog.MinSelected)
        {
            return null;
        }

        var fits = pillars.Select(p => PillarFit01(p, scores)).ToList();
        var percent = (int)Math.Clamp(
            Math.Round(100 * fits.Average(), MidpointRounding.AwayFromZero),
            0,
            100);
        if (disc is { IsComplete: true })
        {
            var discAvg = pillars.Select(p => DiscFitRules.PillarFit01(p.Id, disc)).Average();
            percent = (int)Math.Clamp(
                Math.Round(0.82 * percent + 0.18 * 100 * discAvg, MidpointRounding.AwayFromZero),
                0,
                100);
        }
        var band = Band(percent);
        var best = pillars[fits.IndexOf(fits.Max())];
        var why = WhyFor(best, band);
        if (CareerCompassBuilder.ContainsForbiddenJargon(why))
        {
            why = "Dit past bij de sfeer die het team zoekt — of juist minder, kijk naar de kernwaarden van de vacature.";
        }

        return new CultureFitResult(percent, band, Label(band), why, FromOpenAi: false);
    }

    public static CultureFitResult ClampToLocal(CultureFitResult local, CultureFitResult ai)
    {
        var percent = Math.Clamp(ai.Percent, Math.Max(0, local.Percent - 12), Math.Min(100, local.Percent + 12));
        var why = string.IsNullOrWhiteSpace(ai.Why) || CareerCompassBuilder.ContainsForbiddenJargon(ai.Why)
            ? local.Why
            : ai.Why.Trim();
        var band = Band(percent);
        return new CultureFitResult(percent, band, Label(band), why, FromOpenAi: true);
    }

    public static string Band(int percent)
        => percent >= HighThreshold ? "high" : percent >= MidThreshold ? "mid" : "low";

    public static string Label(string band)
        => band switch
        {
            "high" => "Cultuur Fit: Hoog",
            "low" => "Cultuur Fit: Laag",
            _ => "Cultuur Fit: Midden"
        };

    public static string Label(int percent) => Label(Band(percent));

    private static double PillarFit01(CulturePillarDefinition pillar, CompetencyScores scores)
    {
        var extra = scores.Extraversie ?? 55;
        var pairs = new (int Candidate, int Target)[]
        {
            (scores.Samenwerken ?? 55, pillar.Samenwerken),
            (scores.Resultaatgerichtheid ?? 55, pillar.Resultaat),
            (scores.Stressbestendigheid ?? 55, pillar.Stress),
            (scores.Innovatie ?? 55, pillar.Innovatie),
            (extra, pillar.Extraversie)
        };

        var sum = 0d;
        foreach (var (candidate, target) in pairs)
        {
            var gap = Math.Abs(candidate - target) / 100d;
            sum += Math.Clamp(1 - gap, 0, 1);
        }

        return sum / pairs.Length;
    }

    private static string WhyFor(CulturePillarDefinition pillar, string band)
        => (pillar.Id, band) switch
        {
            ("informeel", "high") =>
                "Sluit goed aan bij een informele, actieve werkomgeving waar proactief handelen gewaardeerd wordt.",
            ("informeel", "low") =>
                "Dit team werkt informeel en snel; jij lijkt meer rust of structuur nodig te hebben om op te bloeien.",
            ("groei", "high") =>
                "Je zoekt graag nieuwe wegen. Dat past bij een team dat groeit en dingen beter wil maken.",
            ("groei", "low") =>
                "Hier wordt veel geëxperimenteerd. Jij lijkt beter tot je recht te komen als de werkwijze al vastligt.",
            ("stabiel", "high") =>
                "Je houdt van duidelijkheid en afmaken wat je belooft. Dat past bij een rustig, gestructureerd team.",
            ("stabiel", "low") =>
                "Dit team werkt met vaste kaders; jij lijkt meer ruimte of tempo te willen dan hier gebruikelijk is.",
            ("zelfstandig", "high") =>
                "Je pakt dingen zelf op en wilt resultaat zien. Dat past bij een team dat zelfstandig werken beloont.",
            ("zelfstandig", "low") =>
                "Hier verwachten ze dat je veel alleen oppakt; jij lijkt beter te groeien met meer overleg.",
            ("samen", "high") =>
                "Je werkt graag met mensen en houdt de sfeer prettig. Dat past bij een klantgericht team.",
            ("samen", "low") =>
                "Dit team draait om samen optrekken; jij lijkt meer tot je recht te komen met zelfstandig werk.",
            ("kalm", "high") =>
                "Jij blijft overzicht houden als het druk is. Dat past bij een team dat piekmomenten kent.",
            ("kalm", "low") =>
                "Hier kan het hectisch zijn; jij lijkt beter te floreren als het tempo wat rustiger is.",
            ("creatief", "high") =>
                "Je bedenkt graag andere routes. Dat past bij een nieuwsgierig team dat ruimte geeft om te proberen.",
            ("creatief", "low") =>
                "Hier wordt veel bedacht en veranderd; jij lijkt beter te passen bij voorspelbaar werk.",
            ("zorgvuldig", "high") =>
                "Je werkt netjes en houdt je aan afspraken. Dat past bij een team dat betrouwbaarheid vooropzet.",
            ("zorgvuldig", "low") =>
                "Hier telt precisie zwaar; jij lijkt meer energie te hebben voor tempo of nieuwe ideeën.",
            (_, "high") =>
                $"Sluit goed aan bij een werkomgeving die {pillar.Label} belangrijk vindt.",
            (_, "low") =>
                $"De sfeer ({pillar.Label}) botst nog met hoe jij het liefst werkt.",
            _ =>
                $"Redelijke aansluiting bij {pillar.Label}, met ruimte om te wennen aan de teamdynamiek."
        };
}

public sealed record CultureFitResult(
    int Percent,
    string Band,
    string Label,
    string Why,
    bool FromOpenAi);
