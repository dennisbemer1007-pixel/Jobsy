using System.Security.Cryptography;
using System.Text;

namespace Jobsy.Core.Rules;

/// <summary>
/// Outbound tracking for opleidingen. Candidate GUIDs and e-mail never appear in the URL.
/// </summary>
public static class TrainingTracking
{
    public const string Ref = "lobsy";
    public const string CampaignFit = "functie_fit";
    public const string CampaignCompass = "career_compass";
    public const string CampaignCompetence = "competence";
    public const string CampaignCulture = "culture";
    public const string CampaignDisc = "culture"; // legacy alias

    public static string CandidateHash(Guid userId, string secret)
        => HmacHex(userId.ToString("N"), secret)[..16];

    public static string EmailHash(string? email, string secret)
    {
        var normalized = (email ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(normalized) || !normalized.Contains('@', StringComparison.Ordinal))
        {
            return "";
        }

        return HmacHex(normalized, secret);
    }

    public static bool EmailHashMatches(string? email, string? storedHash, string secret)
    {
        if (string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        var computed = EmailHash(email, secret);
        if (computed.Length != storedHash.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(computed),
            Encoding.ASCII.GetBytes(storedHash));
    }

    public static string AppendParameters(
        string baseUrl,
        string candidateHash,
        Guid clickId,
        string campaign,
        string medium)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return "/";
        }

        var campaignValue = string.IsNullOrWhiteSpace(campaign) ? CampaignFit : campaign.Trim();
        var mediumValue = string.IsNullOrWhiteSpace(medium) ? "affiliate" : medium.Trim();
        var builder = new UriBuilder(EnsureAbsolute(baseUrl));
        var query = builder.Query.TrimStart('?');
        var pairs = new List<string>();
        if (!string.IsNullOrWhiteSpace(query))
        {
            pairs.AddRange(query.Split('&', StringSplitOptions.RemoveEmptyEntries));
        }

        void Set(string key, string value)
        {
            pairs.RemoveAll(p => p.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase));
            pairs.Add(key + "=" + Uri.EscapeDataString(value));
        }

        Set("ref", Ref);
        Set("candidate_id", candidateHash);
        Set("campaign", campaignValue);
        Set("click_id", clickId.ToString("N"));
        Set("utm_source", Ref);
        Set("utm_medium", mediumValue);
        Set("utm_campaign", campaignValue);

        builder.Query = string.Join('&', pairs);
        return builder.Uri.ToString();
    }

    public static bool LooksSafeOutbound(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme is not ("https" or "http"))
        {
            return false;
        }

        var text = uri.ToString();
        return !text.Contains('@', StringComparison.Ordinal)
               && !text.Contains("mailto", StringComparison.OrdinalIgnoreCase);
    }

    private static string EnsureAbsolute(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            return url;
        }

        return "https://" + url.TrimStart('/');
    }

    private static string HmacHex(string payload, string secret)
        => Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload)))
            .ToLowerInvariant();
}
