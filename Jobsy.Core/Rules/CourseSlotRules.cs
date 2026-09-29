using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>
/// Passport course block: at most one free + one partner offer. No free match → empty (D5).
/// </summary>
public static class CourseSlotRules
{
    public const int MaxSlots = 2;

    public sealed record Context(
        IReadOnlyList<string> DetectedFields,
        string SearchBlob,
        /// <summary>Extra search keys (e.g. learning goals). Gaps still rank higher via existing match rules.</summary>
        IReadOnlyList<string>? ExtraContextKeys = null);

    public sealed record Slot(
        TrainingOffer Offer,
        int Score,
        bool IsFree);

    /// <summary>
    /// Picks slot 1 = best free, slot 2 = best partner. Returns empty when no free match.
    /// </summary>
    public static IReadOnlyList<Slot> Pick(IEnumerable<TrainingOffer> offers, Context context)
    {
        var detected = context.DetectedFields ?? [];
        var extra = context.ExtraContextKeys?
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim())
            .ToList() ?? [];
        var blob = string.Join(' ',
            new[] { context.SearchBlob ?? "" }
                .Concat(extra)
                .Where(s => !string.IsNullOrWhiteSpace(s)));

        var eligible = offers
            .Where(o => o.IsActive
                        && o.Provider is { IsActive: true }
                        && o.ShowInPassport
                        && IsStructurallyValid(o)
                        && TrainingDeepLinkRules.TryCombine(o.Provider.BaseUrl, o.ExternalPath, out _))
            .Select(o => (
                Offer: o,
                Score: TrainingMatchRules.Score(
                    o.Provider.Kind,
                    TrainingMatchRules.SplitCsv(o.FieldsCsv),
                    TrainingMatchRules.SplitCsv(o.KeysCsv),
                    detected,
                    blob)))
            .Where(x => x.Score > 0)
            .ToList();

        var free = eligible
            .Where(x => x.Offer.IsFree && !x.Offer.IsPartner)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Offer.SortOrder)
            .FirstOrDefault();

        if (free.Offer is null)
        {
            return [];
        }

        var slots = new List<Slot>(MaxSlots)
        {
            new(free.Offer, free.Score, IsFree: true)
        };

        var partner = eligible
            .Where(x => x.Offer.IsPartner && !x.Offer.IsFree && x.Offer.Id != free.Offer.Id)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Offer.SortOrder)
            .FirstOrDefault();

        if (partner.Offer is not null)
        {
            slots.Add(new(partner.Offer, partner.Score, IsFree: false));
        }

        return slots;
    }

    /// <summary>Admin / upsert validation. Returns null when valid.</summary>
    public static string? ValidateFlags(bool isFree, bool isPartner, string? affiliateCode, string? deepLinkUrl)
    {
        if (isFree && isPartner)
        {
            return "IsFree and IsPartner cannot both be true.";
        }

        if (isPartner)
        {
            if (string.IsNullOrWhiteSpace(affiliateCode))
            {
                return "Partner offers require an AffiliateCode.";
            }

            if (string.IsNullOrWhiteSpace(deepLinkUrl) || !TrainingDeepLinkRules.IsCourseDeepLink(deepLinkUrl))
            {
                return "Partner offers require a valid course deep link.";
            }
        }

        return null;
    }

    public static bool IsStructurallyValid(TrainingOffer offer)
    {
        if (offer.IsFree && offer.IsPartner)
        {
            return false;
        }

        if (offer.IsPartner)
        {
            if (string.IsNullOrWhiteSpace(offer.AffiliateCode))
            {
                return false;
            }

            if (offer.Provider is null
                || !TrainingDeepLinkRules.TryCombine(offer.Provider.BaseUrl, offer.ExternalPath, out var url)
                || !TrainingDeepLinkRules.IsCourseDeepLink(url))
            {
                return false;
            }
        }

        // Paid non-partner never appears in the passport block.
        if (!offer.IsFree && !offer.IsPartner)
        {
            return false;
        }

        return true;
    }
}
