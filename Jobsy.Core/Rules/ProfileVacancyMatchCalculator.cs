using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>
/// Candidate-facing vacancy ranking for the profile Top 10 widget.
/// Multidimensional: travel/hours/day-parts + opleiding, competenties/drijfveren
/// (Wie ben ik? / cultuur &amp; persoonlijkheid), and transferable skills — not exact functietitel alone.
/// </summary>
public static class ProfileVacancyMatchCalculator
{
    public const int DisplayThreshold = 60;
    public const int MaxResults = 10;
    public const double InterestWeightQuickScan = 0.25;
    public const double InterestWeightDeepAnalysis = 0.32;
    public const double InterestWeightQuickScanOnly = 0.30;
    public const double InterestWeightDeepAnalysisOnly = 0.38;
    public const double OccupationFitWeight = 0.65;
    public const double RiasecFitWeight = 0.35;
    /// <summary>Within experience: exact branche vs transferable-domain blend.</summary>
    public const double ExactWorkTypeWeight = 0.55;
    public const double TransferableWeight = 0.45;

    public static ProfileVacancyMatch Calculate(ProfileVacancyMatchInput input)
    {
        var core = MatchScoreCalculator.Calculate(input.Core);
        var experience01 = ScoreExperience(input);
        var competency01 = input.CandidateCompetencies is { IsComplete: true } scores
            ? VacancyCompetencyProfile.Fit01(scores, input.VacancyCompetencies ?? VacancyCompetencyProfile.Infer(
                input.WorkTypes,
                input.VacancyTitle,
                input.VacancyDescription))
            : (double?)null;
        if (competency01 is not null
            && input.CandidateCultureScores is { IsComplete: true } cultureScores)
        {
            var personality01 = CulturePersonalityFitRules.PersonalityFit01(
                cultureScores, input.VacancyTitle, input.VacancyDescription);
            var blend = personality01;
            if (input.CompanyCultureScores is { } companyCulture
                && companyCulture.Autonomy is not null)
            {
                var culture01 = CulturePersonalityFitRules.CultureFit01(cultureScores, companyCulture);
                blend = 0.55 * personality01 + 0.45 * culture01;
            }

            competency01 = 0.75 * competency01.Value + 0.25 * blend;
        }

        if (input.CandidateValuesScores is { IsComplete: true } valuesScores)
        {
            var values01 = SchwartzValuesFitRules.Fit01(
                valuesScores, input.VacancyTitle, input.VacancyDescription);
            competency01 = competency01 is not null
                ? 0.78 * competency01.Value + 0.22 * values01
                : values01;
        }
        var interest01 = input.CandidateRiasecScores is { IsComplete: true } scored
            ? VacancyRiasecProfile.Fit01(
                scored,
                input.VacancyRiasecTags ?? VacancyRiasecProfile.InferTags(
                    input.WorkTypes,
                    input.VacancyTitle,
                    input.VacancyDescription))
            : input.CandidateRiasecTags is { Count: > 0 } tags
            ? VacancyRiasecProfile.Fit01(
                tags,
                input.VacancyRiasecTags ?? VacancyRiasecProfile.InferTags(
                    input.WorkTypes,
                    input.VacancyTitle,
                    input.VacancyDescription))
            : (double?)null;
        var occupationFit = VacancyOccupationMatch.TryFit(
            input.CareerOccupations,
            input.WorkTypes,
            input.VacancyTitle,
            input.VacancyDescription);
        if (occupationFit is { } occ)
        {
            interest01 = interest01 is { } riasecFit
                ? OccupationFitWeight * occ.Score01 + RiasecFitWeight * riasecFit
                : occ.Score01;
        }

        double total01;
        var interestWeight = InterestWeight(input.CareerDeepCompleted, competency01 is not null);
        if (competency01 is not null && interest01 is not null)
        {
            var rest = 1 - interestWeight;
            total01 = rest * 0.25 / 0.80 * Ratio(core.TravelScore, MatchScoreWeights.Travel)
                      + rest * 0.10 / 0.80 * Ratio(core.HoursScore, MatchScoreWeights.Hours)
                      + rest * 0.10 / 0.80 * Ratio(core.DayPartsScore, MatchScoreWeights.DayParts)
                      + rest * 0.15 / 0.80 * experience01
                      + rest * 0.20 / 0.80 * competency01.Value
                      + interestWeight * interest01.Value;
        }
        else if (competency01 is not null)
        {
            total01 = 0.30 * Ratio(core.TravelScore, MatchScoreWeights.Travel)
                      + 0.15 * Ratio(core.HoursScore, MatchScoreWeights.Hours)
                      + 0.15 * Ratio(core.DayPartsScore, MatchScoreWeights.DayParts)
                      + 0.20 * experience01
                      + 0.20 * competency01.Value;
        }
        else if (interest01 is not null)
        {
            var rest = 1 - interestWeight;
            total01 = rest * 0.30 / 0.75 * Ratio(core.TravelScore, MatchScoreWeights.Travel)
                      + rest * 0.15 / 0.75 * Ratio(core.HoursScore, MatchScoreWeights.Hours)
                      + rest * 0.15 / 0.75 * Ratio(core.DayPartsScore, MatchScoreWeights.DayParts)
                      + rest * 0.15 / 0.75 * experience01
                      + interestWeight * interest01.Value;
        }
        else
        {
            total01 = 0.40 * Ratio(core.TravelScore, MatchScoreWeights.Travel)
                      + 0.25 * Ratio(core.HoursScore, MatchScoreWeights.Hours)
                      + 0.20 * Ratio(core.DayPartsScore, MatchScoreWeights.DayParts)
                      + 0.15 * experience01;
        }

        var total = (int)Math.Clamp(Math.Round(100 * total01, MidpointRounding.AwayFromZero), 0, 100);
        CultureFitResult? culture = null;
        if (CultureFitBuilder.HardCriteriaMatch(input, core)
            && input.CandidateCompetencies is { IsComplete: true })
        {
            culture = CultureFitBuilder.Evaluate(
                input.CulturePillars,
                input.CandidateCompetencies,
                input.CandidateCultureScores);
            if (culture is not null)
            {
                total01 = (1 - CultureFitBuilder.TotalScoreWeight) * total01
                          + CultureFitBuilder.TotalScoreWeight * (culture.Percent / 100.0);
                total = (int)Math.Clamp(Math.Round(100 * total01, MidpointRounding.AwayFromZero), 0, 100);
            }
        }

        var titleExact = TransferableSkillRules.TitleLooksExact(
            input.VacancyTitle, input.CandidateRoles, input.CareerOccupations);
        var isBroadMatch = !titleExact && total >= DisplayThreshold;
        var rationale = BroadMatchRationaleBuilder.TryBuild(
            input, experience01, competency01, interest01, occupationFit, isBroadMatch);

        var (why, gaps) = BuildExplanation(
            input, core, experience01, competency01, interest01, occupationFit, culture, total);
        return new ProfileVacancyMatch
        {
            VacancyId = input.VacancyId,
            VacancyTitle = input.VacancyTitle,
            TotalPercent = total,
            Core = core,
            ExperienceScore01 = experience01,
            CompetencyScore01 = competency01,
            InterestScore01 = interest01,
            CultureFit = culture,
            IsBroadMatch = isBroadMatch,
            MatchRationale = rationale,
            Why = why,
            Gaps = gaps,
            ColorBand = total >= MatchScoreWeights.StrongMatchThreshold
                ? "green"
                : total >= DisplayThreshold
                    ? "orange"
                    : "red"
        };
    }

    public static IReadOnlyList<ProfileVacancyMatch> Rank(
        IEnumerable<ProfileVacancyMatchInput> inputs,
        int take = MaxResults,
        int minPercent = DisplayThreshold)
        => RankScored(inputs.Select(Calculate), take, minPercent);

    /// <summary>
    /// Live banenkaart scoring: every vacancy, including below the Top-10 threshold.
    /// Legal eligibility is left to the caller (hide only when age is known).
    /// </summary>
    public static IReadOnlyList<ProfileVacancyMatch> ScoreAll(IEnumerable<ProfileVacancyMatchInput> inputs)
        => inputs.Select(Calculate).ToList();

    public static IReadOnlyList<ProfileVacancyMatch> RankScored(
        IEnumerable<ProfileVacancyMatch> matches,
        int take = MaxResults,
        int minPercent = DisplayThreshold)
        => matches
            .Where(m => m.Core.LegalEligible && m.TotalPercent >= minPercent)
            .OrderByDescending(m => m.TotalPercent)
            .ThenBy(m => m.VacancyTitle, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Clamp(take, 1, MaxResults))
            .ToList();

    public static string WhyHeadline(ProfileMatchExplainPoint point) => point.Kind switch
    {
        "travel" => "Goede reistijd",
        "hours" => "Beschikbaarheid past",
        "dayparts" => "Dagdelen kloppen",
        "experience" when point.Code == "license" => "Rijbewijs klopt",
        "experience" when point.Code == "transferable" => "Overdraagbare ervaring",
        "experience" when point.Code == "education" => "Opleiding sluit aan",
        "experience" => "Branche sluit aan",
        "competency" => "Sterke competentie-match",
        "culture" => "Cultuur & teamfit",
        "occupation" => "Beroepen-kompas past",
        "interest" => "Beroepsinteresse past",
        "broad" => "Brede match",
        _ => point.Text
    };

    public static IReadOnlyList<string> WhyHeadlines(ProfileVacancyMatch match)
        => match.Why
            .Select(WhyHeadline)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();

    /// <summary>Card/popup one-liner: prefer AI-style rationale on broader matches.</summary>
    public static string? SummaryLine(ProfileVacancyMatch match)
    {
        if (match.IsBroadMatch && !string.IsNullOrWhiteSpace(match.MatchRationale))
        {
            return match.MatchRationale;
        }

        var headlines = WhyHeadlines(match);
        return headlines.Count == 0 ? null : string.Join(", ", headlines);
    }

    private static double InterestWeight(bool careerDeepCompleted, bool hasCompetency)
    {
        if (careerDeepCompleted)
        {
            return hasCompetency ? InterestWeightDeepAnalysis : InterestWeightDeepAnalysisOnly;
        }

        return hasCompetency ? InterestWeightQuickScan : InterestWeightQuickScanOnly;
    }

    private static double Ratio(int points, int weight)
        => weight <= 0 ? 0 : Math.Clamp(points / (double)weight, 0, 1);

    private static double ScoreExperience(ProfileVacancyMatchInput input)
    {
        var parts = new List<double>();

        var vacancyTypes = input.WorkTypes ?? [];
        var candidateRoles = input.CandidateRoles ?? [];
        double exact01;
        if (vacancyTypes.Count > 0)
        {
            if (candidateRoles.Count == 0)
            {
                exact01 = 0.4;
            }
            else
            {
                var vac = vacancyTypes.Select(NormalizeLabel).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var cand = candidateRoles.Select(NormalizeLabel).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var hit = vac.Count(v => cand.Contains(v));
                exact01 = vac.Count == 0 ? 1 : (double)hit / vac.Count;
            }
        }
        else
        {
            exact01 = candidateRoles.Count > 0 ? 0.8 : 0.55;
        }

        var transferable01 = TransferableSkillRules.Score01(
            candidateRoles, vacancyTypes, input.VacancyTitle, input.VacancyDescription);
        parts.Add(ExactWorkTypeWeight * exact01 + TransferableWeight * transferable01);

        if (!string.IsNullOrWhiteSpace(input.RequiredDrivingLicense))
        {
            parts.Add(DrivingLicenseLabels.CandidateMeetsRequirement(
                input.CandidateLicenses, input.RequiredDrivingLicense)
                ? 1
                : 0);
        }

        // Opleidingsachtergrond: niveau (hard) + richting via overdraagbare domeinen.
        var educationLevel01 = string.IsNullOrWhiteSpace(input.RequiredEducation)
            ? ((input.CandidateEducations?.Count ?? 0) > 0 ? 0.85 : 0.55)
            : EducationLevelLabels.CandidateMeetsRequirement(
                input.CandidateEducations, input.RequiredEducation)
                ? 1
                : 0.2;
        var educationDirection01 = TransferableSkillRules.Score01(
            input.CandidateEducations?.Concat(candidateRoles).ToList(),
            vacancyTypes,
            input.VacancyTitle,
            input.VacancyDescription);
        parts.Add(0.65 * educationLevel01 + 0.35 * educationDirection01);

        if (input.MinimumEmployers is > 0)
        {
            parts.Add(Math.Clamp(input.CandidateEmployerCount / (double)input.MinimumEmployers.Value, 0, 1));
        }
        else if (input.CandidateEmployerCount > 0)
        {
            parts.Add(0.85);
        }
        else
        {
            parts.Add(0.5);
        }

        return parts.Average();
    }

    private static (IReadOnlyList<ProfileMatchExplainPoint> Why, IReadOnlyList<ProfileMatchExplainPoint> Gaps)
        BuildExplanation(
            ProfileVacancyMatchInput input,
            MatchScoreBreakdown core,
            double experience01,
            double? competency01,
            double? interest01,
            VacancyOccupationMatch.Fit? occupationFit,
            CultureFitResult? culture,
            int total)
    {
        var why = new List<ProfileMatchExplainPoint>();
        var gaps = new List<ProfileMatchExplainPoint>();

        if (culture is not null)
        {
            var point = new ProfileMatchExplainPoint("culture", culture.Band, culture.Why);
            if (culture.Band == "low")
            {
                gaps.Add(point);
            }
            else
            {
                why.Add(point);
            }
        }

        if (core.TravelWithinPreference == true)
        {
            var minutes = core.TravelMinutesEstimated is int m ? $" (± {m} min)" : "";
            why.Add(new(
                "travel",
                "within",
                $"Je kunt hier binnen de reistijd die jij wilt komen{minutes}. Dat telt sterk mee."));
        }
        else if (core.TravelWithinPreference == false)
        {
            gaps.Add(new(
                "travel",
                "over",
                "De reistijd is langer dan jij nu wilt. Verhoog je max. reistijd of kies een andere vervoerswijze."));
        }

        if (core.HoursOverlapHours is > 0)
        {
            why.Add(new(
                "hours",
                "overlap",
                $"Jouw uren ({FmtHours(core.HoursCandidateMin, core.HoursCandidateMax)}) passen bij deze baan ({FmtHours(core.HoursVacancyMin, core.HoursVacancyMax)})."));
        }
        else if (core.HoursVacancyMin is not null)
        {
            gaps.Add(new(
                "hours",
                "gap",
                $"Deze baan wil {FmtHours(core.HoursVacancyMin, core.HoursVacancyMax)} uur per week, jij staat op {FmtHours(core.HoursCandidateMin, core.HoursCandidateMax)}. Daar zit nog een gat."));
        }

        if (core.DayPartsNeutral)
        {
            why.Add(new(
                "dayparts",
                "flexible",
                "De werktijden zijn in overleg. Jouw dagdelen maken hier geen verschil."));
        }
        else if (core.DayPartsMatched.Count > 0 && core.DayPartsMissing.Count == 0)
        {
            why.Add(new(
                "dayparts",
                "full",
                "Jouw beschikbare dagdelen komen goed overeen met deze baan."));
        }
        else if (core.DayPartsMissing.Count > 0)
        {
            var sample = string.Join(", ", core.DayPartsMissing.Take(2).Select(PrettySlot));
            gaps.Add(new(
                "dayparts",
                "missing",
                $"Je hebt {sample} nog niet aangevinkt, terwijl deze baan dat wel vraagt."));
        }

        var sharedTypes = SharedWorkTypes(input);
        if (sharedTypes.Count > 0)
        {
            why.Add(new(
                "experience",
                "worktype",
                $"Je hebt interesse of ervaring in {JoinNl(sharedTypes)}, en dat sluit aan bij deze vacature."));
        }
        else
        {
            var transferable = TransferableSkillRules.SharedDomainLabels(
                input.CandidateRoles, input.WorkTypes, input.VacancyTitle, input.VacancyDescription);
            if (transferable.Count > 0)
            {
                why.Add(new(
                    "experience",
                    "transferable",
                    $"Je hebt overdraagbare ervaring in {JoinNl(transferable)}. Die vaardigheden kun je meenemen naar deze rol, ook als de functietitel anders is."));
            }
            else if ((input.WorkTypes?.Count ?? 0) > 0 && experience01 < 0.5)
            {
                gaps.Add(new(
                    "experience",
                    "worktype",
                    $"Deze baan zit in {JoinNl(input.WorkTypes!)}. Zet dat bij je interesses of ervaring als het klopt — dan matcht het beter."));
            }
        }

        if (!string.IsNullOrWhiteSpace(input.RequiredDrivingLicense)
            && !DrivingLicenseLabels.CandidateMeetsRequirement(input.CandidateLicenses, input.RequiredDrivingLicense))
        {
            gaps.Add(new(
                "experience",
                "license",
                $"Deze baan vraagt rijbewijs {input.RequiredDrivingLicense}, en dat staat nog niet in je profiel."));
        }
        else if (!string.IsNullOrWhiteSpace(input.RequiredDrivingLicense))
        {
            why.Add(new(
                "experience",
                "license",
                $"Je hebt het gevraagde rijbewijs ({input.RequiredDrivingLicense})."));
        }

        if (!string.IsNullOrWhiteSpace(input.RequiredEducation)
            && !EducationLevelLabels.CandidateMeetsRequirement(input.CandidateEducations, input.RequiredEducation))
        {
            gaps.Add(new(
                "experience",
                "education",
                $"Deze baan vraagt opleidingsniveau {input.RequiredEducation}. Vul je opleiding aan als je die hebt."));
        }
        else if (!string.IsNullOrWhiteSpace(input.RequiredEducation)
                 && EducationLevelLabels.CandidateMeetsRequirement(input.CandidateEducations, input.RequiredEducation))
        {
            why.Add(new(
                "experience",
                "education",
                $"Je opleidingsniveau past bij wat deze vacature vraagt ({input.RequiredEducation})."));
        }

        if (input.CandidateCompetencies is { IsComplete: true } cand
            && (input.VacancyCompetencies ?? VacancyCompetencyProfile.Infer(
                input.WorkTypes, input.VacancyTitle, input.VacancyDescription)) is var targets)
        {
            string? bestFit = null;
            var bestFitDelta = int.MinValue;
            string? worstGap = null;
            var worstGapDelta = int.MinValue;
            foreach (var category in CompetencyTestCatalog.CategoryCodes)
            {
                var c = cand.Get(category);
                var t = targets.Get(category);
                var delta = c - t;
                if (delta >= -8 && delta > bestFitDelta)
                {
                    bestFitDelta = delta;
                    bestFit = category;
                }

                if (delta < -10 && -delta > worstGapDelta)
                {
                    worstGapDelta = -delta;
                    worstGap = category;
                }
            }

            if (bestFit is not null)
            {
                why.Add(new(
                    "competency",
                    bestFit,
                    $"Op {Label(bestFit)} scoor jij {cand.Get(bestFit)}%. Dat past goed bij wat deze baan vraagt (rond {targets.Get(bestFit)}%)."));
            }

            if (worstGap is not null)
            {
                gaps.Add(new(
                    "competency",
                    worstGap,
                    $"Hier ligt de uitdaging: de baan vraagt meer {Label(worstGap).ToLowerInvariant()} ({targets.Get(worstGap)}%) dan jouw test nu laat zien ({cand.Get(worstGap)}%). Dat is het stukje dat nog niet matcht."));
            }
            else if (bestFit is null && competency01 is >= 0.75)
            {
                why.Add(new(
                    "competency",
                    "overall",
                    "Jouw competenties uit de test sluiten in het algemeen goed aan bij deze functie."));
            }
        }
        else if (input.CandidateCompetencies is not { IsComplete: true }
                 && input.CandidateRiasecTags is not { Count: > 0 }
                 && input.CandidateRiasecScores is not { IsComplete: true })
        {
            gaps.Add(new(
                "competency",
                "missing",
                "Rond de competentietest of beroepentest af. Dan kunnen we nóg beter uitleggen waarom een baan bij je past."));
        }

        if (occupationFit is { } occFit)
        {
            why.Add(new(
                "occupation",
                occFit.Title,
                $"Deze vacature sluit aan bij {occFit.Title.ToLowerInvariant()} uit jouw beroepen-kompas."));
        }

        var candRiasec = input.CandidateRiasecTags is { Count: > 0 }
            ? input.CandidateRiasecTags
            : CareerTestCatalog.DeriveRiasecTags(input.CandidateRiasecScores);
        if (candRiasec.Count > 0)
        {
            var vacancyRiasec = input.VacancyRiasecTags ?? VacancyRiasecProfile.InferTags(
                input.WorkTypes, input.VacancyTitle, input.VacancyDescription);
            var overlap = candRiasec
                .Where(t => vacancyRiasec.Contains(t, StringComparer.OrdinalIgnoreCase))
                .ToList();
            if (overlap.Count > 0 && interest01 is >= 0.5)
            {
                why.Add(new(
                    "interest",
                    string.Join("+", overlap),
                    $"Wat jij leuk vindt aan werk ({JoinNl(overlap.Select(CareerCompassBuilder.TypeLabel).ToList())}) komt terug in deze vacature."));
            }
            else if (interest01 is < 0.4)
            {
                gaps.Add(new(
                    "interest",
                    "mismatch",
                    "Deze baan ligt inhoudelijk wat verder van wat jij leuk vindt. Kijk of de taken je toch aanspreken, of filter op een andere richting."));
            }
        }

        if (why.Count == 0 && total >= DisplayThreshold)
        {
            why.Add(new(
                "overall",
                "ok",
                $"Op reistijd, uren en ervaring kom je tot {total}% match. Kijk hieronder wat je nog kunt aanscherpen."));
        }

        if (gaps.Count == 0)
        {
            gaps.Add(new(
                "overall",
                "none",
                "Geen groot gat: kleine verschillen in uren of reistijd kunnen het percentage nog een tikje lager zetten dan 100%."));
        }

        return (PrioritizeExplain(why), PrioritizeExplain(gaps));
    }

    /// <summary>
    /// Card/popup summaries keep three lines; travel + competency + RIASEC beat generic day-part copy.
    /// </summary>
    private static IReadOnlyList<ProfileMatchExplainPoint> PrioritizeExplain(
        IReadOnlyList<ProfileMatchExplainPoint> points)
        => points
            .GroupBy(p => p.Kind, StringComparer.Ordinal)
            .Select(g => g.First())
            .OrderBy(p => p.Kind switch
            {
                "culture" => 0,
                "travel" => 1,
                "competency" => 2,
                "occupation" => 3,
                "interest" => 4,
                "hours" => 5,
                "experience" => 6,
                "dayparts" => 7,
                _ => 8
            })
            .Take(3)
            .ToList();

    private static List<string> SharedWorkTypes(ProfileVacancyMatchInput input)
    {
        var vac = (input.WorkTypes ?? []).Select(NormalizeLabel).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (vac.Count == 0)
        {
            return [];
        }

        return (input.CandidateRoles ?? [])
            .Select(NormalizeLabel)
            .Where(vac.Contains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizeLabel(string value)
    {
        var parsed = WorkTypeLabels.Parse(value);
        return parsed == WorkType.None
            ? value.Trim()
            : WorkTypeLabels.Expand(parsed).FirstOrDefault() ?? value.Trim();
    }

    private static string FmtHours(decimal? min, decimal? max)
    {
        if (min is null && max is null)
        {
            return "—";
        }

        if (min is null)
        {
            return $"max. {max}";
        }

        if (max is null)
        {
            return $"min. {min}";
        }

        return $"{min}–{max}";
    }

    private static string PrettySlot(string code)
    {
        var parts = code.Split(':', 2);
        if (parts.Length != 2)
        {
            return code.Replace(':', ' ');
        }

        var day = parts[0] switch
        {
            "Ma" => "maandag",
            "Di" => "dinsdag",
            "Wo" => "woensdag",
            "Do" => "donderdag",
            "Vr" => "vrijdag",
            "Za" => "zaterdag",
            "Zo" => "zondag",
            _ => parts[0]
        };
        var slot = parts[1].ToLowerInvariant();
        return $"{day} {slot}";
    }

    private static string JoinNl(IReadOnlyList<string> items)
        => items.Count switch
        {
            0 => "",
            1 => items[0].ToLowerInvariant(),
            2 => $"{items[0].ToLowerInvariant()} en {items[1].ToLowerInvariant()}",
            _ => string.Join(", ", items.Take(items.Count - 1).Select(i => i.ToLowerInvariant()))
                 + " en " + items[^1].ToLowerInvariant()
        };

    private static string Label(string category) => category switch
    {
        CompetencyTestCatalog.Samenwerken => "Samenwerken",
        CompetencyTestCatalog.Resultaatgerichtheid => "Resultaatgerichtheid",
        CompetencyTestCatalog.Stressbestendigheid => "Stressbestendigheid",
        CompetencyTestCatalog.Innovatie => "Innovatie / probleemoplossen",
        _ => category
    };
}

public sealed class ProfileVacancyMatchInput
{
    public Guid VacancyId { get; init; }
    public required MatchScoreInput Core { get; init; }
    public required string VacancyTitle { get; init; }
    public string? VacancyDescription { get; init; }
    public IReadOnlyList<string>? WorkTypes { get; init; }
    public IReadOnlyList<string>? CandidateRoles { get; init; }
    public IReadOnlyList<string>? CandidateLicenses { get; init; }
    public IReadOnlyList<string>? CandidateEducations { get; init; }
    public int CandidateEmployerCount { get; init; }
    public string? RequiredDrivingLicense { get; init; }
    public string? RequiredEducation { get; init; }
    public int? MinimumEmployers { get; init; }
    public CompetencyScores? CandidateCompetencies { get; init; }
    public CompetencyScores? VacancyCompetencies { get; init; }
    public IReadOnlyList<string>? CandidateRiasecTags { get; init; }
    public RiasecScores? CandidateRiasecScores { get; init; }
    public bool CareerDeepCompleted { get; init; }
    public IReadOnlyList<string>? VacancyRiasecTags { get; init; }
    public IReadOnlyList<CareerOccupationMatch>? CareerOccupations { get; init; }
    public IReadOnlyList<string>? CulturePillars { get; init; }
    public CulturePersonalityScores? CandidateCultureScores { get; init; }
    public CulturePersonalityScores? CompanyCultureScores { get; init; }
    public SchwartzValuesScores? CandidateValuesScores { get; init; }
}

public sealed class ProfileVacancyMatch
{
    public Guid VacancyId { get; init; }
    public string VacancyTitle { get; init; } = "";
    public int TotalPercent { get; init; }
    public required MatchScoreBreakdown Core { get; init; }
    public double ExperienceScore01 { get; init; }
    public double? CompetencyScore01 { get; init; }
    public double? InterestScore01 { get; init; }
    public CultureFitResult? CultureFit { get; init; }
    /// <summary>True when the vacancy fits holistically without an exact functietitel hit.</summary>
    public bool IsBroadMatch { get; init; }
    /// <summary>Short AI-style onderbouwing for broader matches (opleiding, drijfveren, transferable).</summary>
    public string? MatchRationale { get; init; }
    public IReadOnlyList<ProfileMatchExplainPoint> Why { get; init; } = [];
    public IReadOnlyList<ProfileMatchExplainPoint> Gaps { get; init; } = [];
    public string ColorBand { get; init; } = "orange";
}

public sealed record ProfileMatchExplainPoint(string Kind, string Code, string Text);
