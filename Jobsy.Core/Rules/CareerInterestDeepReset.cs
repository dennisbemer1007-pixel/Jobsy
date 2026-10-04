using Jobsy.Core.Entities;

namespace Jobsy.Core.Rules;

/// <summary>
/// Drops a compass that was written by the deep career test so the basic test can be scored again.
/// </summary>
public static class CareerInterestDeepReset
{
    public static bool ClearDeepCompass(CandidateCareerInterest row, DateTime utcNow)
    {
        var stored = CareerCompassJson.TryDeserialize(row.CompassJson);
        if (stored is not { FromDeepAnalysis: true })
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
}
