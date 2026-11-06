using System.Text.Json;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class GratisDnaStorageValidatorTests
{
    private static readonly DateTime Now = new(2026, 9, 27, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Valid_payload_roundtrips_and_filters_unknown_ids()
    {
        var payload = new GratisDnaStoragePayload
        {
            V = 1,
            CreatedAtUtc = Now.AddDays(-1),
            ExpiresAtUtc = Now.AddDays(6),
            AgeBand = GratisDnaStoragePayload.AgeBand16Plus,
            Consent = new GratisDnaStoredConsent
            {
                Version = PrivacyConstants.CandidateProfilingConsentVersion,
                AtUtc = Now.AddDays(-1)
            },
            Answers = new GratisDnaStoredAnswers
            {
                Competency = new Dictionary<int, int> { [1] = 4, [99] = 5, [6] = 0 },
                Career = new Dictionary<int, int> { [10] = 5, [14] = 3 },
                Culture = new Dictionary<int, int> { [1] = 5 },
                Values = new Dictionary<int, int> { [21] = 6 }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        var validated = GratisDnaStorageValidator.TryParseAndValidate(json, Now);

        Assert.NotNull(validated);
        Assert.Equal(4, validated!.Answers.Competency[1]);
        Assert.False(validated.Answers.Competency.ContainsKey(99));
        Assert.False(validated.Answers.Competency.ContainsKey(6));
        Assert.False(validated.Answers.Career.ContainsKey(10));
        Assert.Equal(3, validated.Answers.Career[14]);
        Assert.False(validated.Answers.Values.ContainsKey(21));
    }

    [Fact]
    public void Wrong_version_expired_or_age_band_rejected()
    {
        Assert.Null(GratisDnaStorageValidator.TryParseAndValidate(
            JsonSerializer.Serialize(MakePayload(v: 2)), Now));

        Assert.Null(GratisDnaStorageValidator.TryParseAndValidate(
            JsonSerializer.Serialize(MakePayload(expiresAtUtc: Now.AddMinutes(-1))), Now));

        Assert.Null(GratisDnaStorageValidator.TryParseAndValidate(
            JsonSerializer.Serialize(MakePayload(ageBand: "under16")), Now));
    }

    private static GratisDnaStoragePayload MakePayload(
        int v = 1,
        DateTime? expiresAtUtc = null,
        string ageBand = GratisDnaStoragePayload.AgeBand16Plus)
        => new()
        {
            V = v,
            CreatedAtUtc = Now.AddDays(-1),
            ExpiresAtUtc = expiresAtUtc ?? Now.AddDays(6),
            AgeBand = ageBand,
            Consent = new GratisDnaStoredConsent { Version = "old", AtUtc = Now },
            Answers = new GratisDnaStoredAnswers()
        };
}
