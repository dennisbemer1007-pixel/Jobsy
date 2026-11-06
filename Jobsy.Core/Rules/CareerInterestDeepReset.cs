using Jobsy.Core.Entities;

namespace Jobsy.Core.Rules;

/// <summary>
/// Drops a compass that was written by the deep career test so the basic test can be scored again.
/// </summary>
public static class CareerInterestDeepReset
{
    public static bool ClearDeepCompass(CandidateCareerInterest row, DateTime utcNow)
    {
        var parsed = CareerCompassJson.TryDeserialize(row.CompassJson);
        var scoresPreview = CareerTestCatalog.Score(CareerTestCatalog.ParseAnswersJson(row.AnswersJson));
        var currentKey = scoresPreview is { IsComplete: true }
            ? CareerCompassBuilder.ScoresKey(scoresPreview)
            : null;
        var fingerprintMismatch = parsed is { HasOccupations: true }
                                  && currentKey is not null
                                  && !string.Equals(parsed.ScoresFingerprint, currentKey, StringComparison.Ordinal);
        if (!MarksDeepAnalysis(row.CompassJson) && !fingerprintMismatch)
        {
            return false;
        }

        var scores = CareerTestCatalog.Score(CareerTestCatalog.ParseAnswersJson(row.AnswersJson));
        if (scores is { IsComplete: true })
        {
            var tags = CareerTestCatalog.DeriveRiasecTags(scores);
            row.RealisticPercent = scores.Realistic;
            row.InvestigativePercent = scores.Investigative;
            row.ArtisticPercent = scores.Artistic;
            row.SocialPercent = scores.Social;
            row.EnterprisingPercent = scores.Enterprising;
            row.ConventionalPercent = scores.Conventional;
            row.HollandCode = CareerTestCatalog.HollandCode(scores);
            row.RiasecTagsJson = CareerTestCatalog.SerializeTags(tags);
            row.MatchTagsJson = CareerTestCatalog.SerializeTags(tags);
        }

        row.CompassJson = CareerCompassJson.Serialize(CareerCompassSnapshot.Empty(fromDeepAnalysis: false));
        row.UpdatedAtUtc = utcNow;
        return true;
    }

    /// <summary>
    /// True when the stored compass came from the deep test, even if the job list can no longer be parsed.
    /// </summary>
    public static bool MarksDeepAnalysis(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        if (CareerCompassJson.TryDeserialize(json) is { FromDeepAnalysis: true })
        {
            return true;
        }

        return System.Text.RegularExpressions.Regex.IsMatch(
            json,
            "\"fromDeepAnalysis\"\\s*:\\s*true",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    }
}
