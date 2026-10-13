using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>
/// Phase-1 match calibration: no neutral full credit for missing data, separate fit vs confidence,
/// weighted layers per psychometrie doc §4.2, shrinkage when confidence is low.
/// </summary>
public static class MatchCalibrationV1Calculator
{
    public const double WeightRole = 0.30;
    public const double WeightPractical = 0.25;
    public const double WeightInterests = 0.20;
    public const double WeightCulture = 0.15;
    public const double WeightValues = 0.10;

    public const double ShrinkBase = 0.55;
    public const double ShrinkConfidenceScale = 0.45;

    public const double IndicativeFitCap = 0.65;

    public sealed record LayerScore(double Fit01, double WeightUsed, double WeightAvailable);

    public sealed record CalibrationResult(
        bool DealBreaker,
        double FitRaw01,
        int FitRawPercent,
        double Confidence01,
        int ConfidencePercent,
        int DisplayPercent);

    /// <summary>
    /// Applies V1 scoring onto a legacy match (keeps explain/rationale from legacy pipeline).
    /// </summary>
    public static ProfileVacancyMatch Apply(ProfileVacancyMatchInput input, ProfileVacancyMatch legacy)
    {
        var computed = Compute(input, legacy);
        var display = computed.DisplayPercent;
        var band = MatchCalibrationBands.ColorBand(display, computed.ConfidencePercent);
        var isBroad = !TransferableSkillRules.TitleLooksExact(
                          input.VacancyTitle, input.CandidateRoles, input.CareerOccupations)
                      && display >= MatchCalibrationBands.DisplayThreshold;

        return new ProfileVacancyMatch
        {
            VacancyId = legacy.VacancyId,
            VacancyTitle = legacy.VacancyTitle,
            TotalPercent = display,
            LegacyTotalPercent = legacy.TotalPercent,
            FitRawPercent = computed.FitRawPercent,
            ConfidencePercent = computed.ConfidencePercent,
            Core = legacy.Core,
            ExperienceScore01 = legacy.ExperienceScore01,
            CompetencyScore01 = legacy.CompetencyScore01,
            CompetencyDim01 = legacy.CompetencyDim01,
            InterestScore01 = legacy.InterestScore01,
            CultureDim01 = legacy.CultureDim01,
            ValuesFit01 = legacy.ValuesFit01,
            CultureFit = legacy.CultureFit,
            EngagementBonus = 0,
            IsBroadMatch = isBroad,
            MatchRationale = legacy.MatchRationale,
            Why = legacy.Why,
            Gaps = AppendConfidenceGap(legacy.Gaps, computed.ConfidencePercent),
            ColorBand = band,
            CalibrationV1 = true,
            DealBreaker = computed.DealBreaker
        };
    }

    public static CalibrationResult Compute(ProfileVacancyMatchInput input, ProfileVacancyMatch? legacy = null)
    {
        legacy ??= ProfileVacancyMatchCalculator.CalculateLegacy(input);
        var core = legacy.Core;

        if (!core.LegalEligible)
        {
            return LowResult(0, 0, dealBreaker: true);
        }

        if (core.TravelWithinPreference == false)
        {
            return LowResult(0.15, 0.35, dealBreaker: true);
        }

        if (core.HoursOverlapHours is 0
            && core.HoursCandidateMin is not null
            && core.HoursVacancyMin is not null)
        {
            return LowResult(0.12, 0.38, dealBreaker: true);
        }

        var layers = new List<(double Weight, LayerScore Score)>();

        var role = ScoreRoleLayer(input);
        if (role.WeightAvailable > 0)
        {
            layers.Add((WeightRole, role));
        }

        var practical = ScorePracticalLayer(input.Core, core);
        if (practical.WeightAvailable > 0)
        {
            layers.Add((WeightPractical, practical));
        }

        var interests = ScoreInterestsLayer(input, legacy);
        if (interests.WeightAvailable > 0)
        {
            layers.Add((WeightInterests, interests));
        }

        var culture = ScoreCultureLayer(input, legacy);
        if (culture.WeightAvailable > 0)
        {
            layers.Add((WeightCulture, culture));
        }

        var values = ScoreValuesLayer(input, legacy);
        if (values.WeightAvailable > 0)
        {
            layers.Add((WeightValues, values));
        }

        if (layers.Count == 0)
        {
            return LowResult(0.35, 0.10, dealBreaker: false);
        }

        var weightSum = layers.Sum(l => l.Weight);
        var fitRaw = layers.Sum(l => l.Weight * l.Score.Fit01) / weightSum;
        var confidence = layers.Sum(l => l.Score.WeightAvailable) / (
            WeightRole + WeightPractical + WeightInterests + WeightCulture + WeightValues);

        fitRaw = Math.Clamp(fitRaw, 0, 1);
        confidence = Math.Clamp(confidence, 0, 1);

        var fitRawPercent = ToPercent(fitRaw);
        var confidencePercent = ToPercent(confidence);
        var display = ShrinkDisplay(fitRaw, confidence);

        return new CalibrationResult(
            DealBreaker: false,
            FitRaw01: fitRaw,
            FitRawPercent: fitRawPercent,
            Confidence01: confidence,
            ConfidencePercent: confidencePercent,
            DisplayPercent: display);
    }

    private static CalibrationResult LowResult(double fitRaw, double confidence, bool dealBreaker)
    {
        var display = ShrinkDisplay(fitRaw, confidence);
        return new CalibrationResult(
            dealBreaker,
            fitRaw,
            ToPercent(fitRaw),
            confidence,
            ToPercent(confidence),
            display);
    }

    public static int ShrinkDisplay(double fitRaw01, double confidence01)
    {
        var factor = ShrinkBase + ShrinkConfidenceScale * Math.Clamp(confidence01, 0, 1);
        var display = 100 * Math.Clamp(fitRaw01, 0, 1) * factor;
        return (int)Math.Clamp(Math.Round(display, MidpointRounding.AwayFromZero), 0, 100);
    }

    private static LayerScore ScoreRoleLayer(ProfileVacancyMatchInput input)
    {
        double sum = 0;
        double weight = 0;

        var experience = ScoreExperienceCalibrated(input);
        if (experience is not null)
        {
            sum += 0.45 * experience.Value;
            weight += 0.45;
        }

        if (input.CandidateCompetencies is { IsComplete: true } cand)
        {
            var targets = input.VacancyCompetencies ?? VacancyCompetencyProfile.Infer(
                input.WorkTypes, input.VacancyTitle, input.VacancyDescription);
            var sym = VacancyCompetencyProfile.Fit01Symmetric(cand, targets);
            if (IsIndicativeCompetency(input))
            {
                sym = Math.Min(sym, IndicativeFitCap);
            }

            sum += 0.35 * sym;
            weight += 0.35;
        }

        var conscientiousness = Conscientiousness01(input);
        if (conscientiousness is not null)
        {
            sum += 0.20 * conscientiousness.Value;
            weight += 0.20;
        }

        if (weight <= 1e-9)
        {
            return new LayerScore(0, 0, 0);
        }

        var available = WeightRole * weight;
        return new LayerScore(sum / weight, available, available);
    }

    private static LayerScore ScorePracticalLayer(MatchScoreInput input, MatchScoreBreakdown core)
    {
        double sum = 0;
        double parts = 0;

        if (input.EstimatedTravelMinutes is not null && input.MaxTravelMinutes is > 0)
        {
            sum += Ratio(core.TravelScore, MatchScoreWeights.Travel);
            parts++;
        }

        if (input.CandidateHours is not null && input.VacancyHours is not null)
        {
            sum += Ratio(core.HoursScore, MatchScoreWeights.Hours);
            parts++;
        }

        if (input.VacancySchedule is { FlexibleTimes: false }
            && input.CandidateSchedule is not null
            && !core.DayPartsNeutral)
        {
            sum += Ratio(core.DayPartsScore, MatchScoreWeights.DayParts);
            parts++;
        }

        if (parts <= 0)
        {
            return new LayerScore(0, 0, 0);
        }

        var fit = sum / parts;
        var available = WeightPractical * (parts / 3.0);
        return new LayerScore(fit, available, available);
    }

    private static LayerScore ScoreInterestsLayer(ProfileVacancyMatchInput input, ProfileVacancyMatch legacy)
    {
        if (legacy.InterestScore01 is not double interest)
        {
            return new LayerScore(0, 0, 0);
        }

        var available = input.CandidateRiasecScores is { IsComplete: true }
                        || input.CandidateRiasecTags is { Count: > 0 }
                        || input.CareerOccupations is { Count: > 0 };
        if (!available)
        {
            return new LayerScore(0, 0, 0);
        }

        var w = WeightInterests;
        if (IsIndicativeCareer(input))
        {
            interest = Math.Min(interest, IndicativeFitCap);
            w *= 0.85;
        }

        return new LayerScore(interest, w, w);
    }

    private static LayerScore ScoreCultureLayer(ProfileVacancyMatchInput input, ProfileVacancyMatch legacyMatch)
    {
        double sum = 0;
        double weight = 0;

        if (legacyMatch.CultureDim01 is double culture01)
        {
            sum += 0.55 * culture01;
            weight += 0.55;
        }
        else if (legacyMatch.CultureFit is { } pillar)
        {
            sum += 0.55 * (pillar.Percent / 100.0);
            weight += 0.55;
        }

        var engagement = EngagementCatalog.ComputeBonus(
            input.CandidateValuesScores,
            input.CompanyEngagement);
        if (engagement > 0)
        {
            // Within layer 4: max +3 points on fit scale, not stacked on total.
            sum += 0.45 * Math.Min(engagement, 3) / 3.0;
            weight += 0.45;
        }

        if (weight <= 1e-9)
        {
            return new LayerScore(0, 0, 0);
        }

        return new LayerScore(sum / weight, WeightCulture * weight, WeightCulture);
    }

    private static LayerScore ScoreValuesLayer(ProfileVacancyMatchInput input, ProfileVacancyMatch legacy)
    {
        if (legacy.ValuesFit01 is not double values01
            || input.CandidateValuesScores is not { IsComplete: true })
        {
            return new LayerScore(0, 0, 0);
        }

        return new LayerScore(values01, WeightValues, WeightValues);
    }

    private static double? ScoreExperienceCalibrated(ProfileVacancyMatchInput input)
    {
        var parts = new List<double>();
        var vacancyTypes = input.WorkTypes ?? [];
        var candidateRoles = input.CandidateRoles ?? [];

        if (vacancyTypes.Count > 0 && candidateRoles.Count > 0)
        {
            var vac = vacancyTypes.Select(NormalizeLabel).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var cand = candidateRoles.Select(NormalizeLabel).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var hit = vac.Count(v => cand.Contains(v));
            var exact01 = vac.Count == 0 ? 0.5 : (double)hit / vac.Count;
            var transferable01 = TransferableSkillRules.Score01(
                candidateRoles, vacancyTypes, input.VacancyTitle, input.VacancyDescription);
            parts.Add(ProfileVacancyMatchCalculator.ExactWorkTypeWeight * exact01
                        + ProfileVacancyMatchCalculator.TransferableWeight * transferable01);
        }
        else if (vacancyTypes.Count > 0 || candidateRoles.Count > 0)
        {
            parts.Add(TransferableSkillRules.Score01(
                candidateRoles, vacancyTypes, input.VacancyTitle, input.VacancyDescription));
        }

        if (!string.IsNullOrWhiteSpace(input.RequiredDrivingLicense))
        {
            parts.Add(DrivingLicenseLabels.CandidateMeetsRequirement(
                input.CandidateLicenses, input.RequiredDrivingLicense)
                ? 1
                : 0.15);
        }

        if (!string.IsNullOrWhiteSpace(input.RequiredEducation))
        {
            parts.Add(EducationLevelLabels.CandidateMeetsRequirement(
                input.CandidateEducations, input.RequiredEducation)
                ? 1
                : 0.15);
        }
        else if ((input.CandidateEducations?.Count ?? 0) > 0)
        {
            parts.Add(0.75);
        }

        if (input.MinimumEmployers is > 0)
        {
            parts.Add(Math.Clamp(input.CandidateEmployerCount / (double)input.MinimumEmployers.Value, 0, 1));
        }

        return parts.Count == 0 ? null : parts.Average();
    }

    private static double? Conscientiousness01(ProfileVacancyMatchInput input)
    {
        if (input.CandidateCultureScores is { IsComplete: true, Conscientiousness: { } c })
        {
            return Math.Clamp(c / 100.0, 0, 1);
        }

        if (input.CandidateCompetencies is { IsComplete: true } comp)
        {
            return Math.Clamp((comp.Resultaatgerichtheid ?? 0) / 100.0, 0, 1);
        }

        return null;
    }

    private static bool IsIndicativeCompetency(ProfileVacancyMatchInput input)
        => input.CandidateCompetencies is { IsComplete: true }
           && !input.CareerDeepCompleted;

    private static bool IsIndicativeCareer(ProfileVacancyMatchInput input)
        => !input.CareerDeepCompleted
           && input.CandidateRiasecScores is { IsComplete: true };

    private static double Ratio(int points, int weight)
        => weight <= 0 ? 0 : Math.Clamp(points / (double)weight, 0, 1);

    private static int ToPercent(double score01)
        => (int)Math.Clamp(Math.Round(100 * score01, MidpointRounding.AwayFromZero), 0, 100);

    private static string NormalizeLabel(string value)
    {
        var parsed = WorkTypeLabels.Parse(value);
        return parsed == WorkType.None
            ? value.Trim()
            : WorkTypeLabels.Expand(parsed).FirstOrDefault() ?? value.Trim();
    }

    private static IReadOnlyList<ProfileMatchExplainPoint> AppendConfidenceGap(
        IReadOnlyList<ProfileMatchExplainPoint> gaps,
        int confidencePercent)
    {
        if (confidencePercent >= MatchCalibrationBands.LowConfidenceThreshold)
        {
            return gaps;
        }

        var list = gaps.ToList();
        list.Insert(0, new ProfileMatchExplainPoint(
            "confidence",
            "low",
            MatchConfidenceCopy.NlHint(confidencePercent)));
        return list;
    }
}

/// <summary>B1 Dutch copy for match confidence (API + UI).</summary>
public static class MatchConfidenceCopy
{
    public static string NlHint(int confidencePercent)
        => confidencePercent < MatchCalibrationBands.LowConfidenceThreshold
            ? "Indicatief — vul je profiel aan voor een betere match."
            : "Betrouwbaarheid van deze match: ongeveer {0}%.".Replace("{0}", confidencePercent.ToString(), StringComparison.Ordinal);

    public static string NlShort(int confidencePercent)
        => $"Betrouwbaarheid {confidencePercent}%";
}
