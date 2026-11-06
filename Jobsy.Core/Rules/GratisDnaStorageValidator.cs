using System.Text.Json;
using Jobsy.Core.Privacy;

namespace Jobsy.Core.Rules;

public static class GratisDnaStorageValidator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static GratisDnaStoragePayload? TryParseAndValidate(string? json, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var raw = JsonSerializer.Deserialize<GratisDnaStoragePayload>(json, JsonOptions);
            return raw is null ? null : Validate(raw, utcNow);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static GratisDnaStoragePayload? Validate(GratisDnaStoragePayload raw, DateTime utcNow)
    {
        if (raw.V != GratisDnaStoragePayload.SchemaVersion)
        {
            return null;
        }

        if (raw.ExpiresAtUtc <= utcNow)
        {
            return null;
        }

        if (!string.Equals(raw.AgeBand, GratisDnaStoragePayload.AgeBand16Plus, StringComparison.Ordinal))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(raw.Consent.Version))
        {
            return null;
        }

        var competency = FilterAnswers(raw.Answers.Competency, OnboardingWizardCatalog.CompetencyQuestionIds);
        var career = FilterAnswers(raw.Answers.Career, OnboardingWizardCatalog.CareerQuestionIds);
        var culture = FilterAnswers(raw.Answers.Culture, OnboardingWizardCatalog.CultureQuestionIds);
        var values = FilterAnswers(raw.Answers.Values, OnboardingWizardCatalog.ValuesQuestionIds);

        return new GratisDnaStoragePayload
        {
            V = raw.V,
            CreatedAtUtc = raw.CreatedAtUtc,
            ExpiresAtUtc = raw.ExpiresAtUtc,
            Consent = raw.Consent,
            AgeBand = raw.AgeBand,
            Answers = new GratisDnaStoredAnswers
            {
                Competency = competency,
                Career = career,
                Culture = culture,
                Values = values
            }
        };
    }

    /// <summary>True when stored consent matches the current profiling consent version.</summary>
    public static bool StoredConsentMatchesCurrent(GratisDnaStoragePayload stored)
        => string.Equals(
            stored.Consent.Version,
            PrivacyConstants.CandidateProfilingConsentVersion,
            StringComparison.Ordinal);

    private static Dictionary<int, int> FilterAnswers(
        IReadOnlyDictionary<int, int>? answers,
        IReadOnlyList<int> allowedQuestionIds)
    {
        if (answers is null || answers.Count == 0)
        {
            return new Dictionary<int, int>();
        }

        var allowed = new HashSet<int>(allowedQuestionIds);
        var result = new Dictionary<int, int>();
        foreach (var (id, value) in answers)
        {
            if (!allowed.Contains(id))
            {
                continue;
            }

            if (value is < LikertAnswerJson.LikertMin or > LikertAnswerJson.LikertMax)
            {
                continue;
            }

            result[id] = value;
        }

        return result;
    }
}
