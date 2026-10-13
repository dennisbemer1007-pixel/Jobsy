namespace Jobsy.Core.Rules;

/// <summary>
/// Browser storage shape for anonymous gratis werk-DNA (<c>jobsy.gratisDna.v1</c>).
/// </summary>
public sealed class GratisDnaStoragePayload
{
    public const int SchemaVersion = 1;
    public const string AgeBand16Plus = "16plus";
    public const int RetentionDays = 7;

    public int V { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime ExpiresAtUtc { get; init; }
    public GratisDnaStoredConsent Consent { get; init; } = new();
    public string AgeBand { get; init; } = "";
    public GratisDnaStoredAnswers Answers { get; init; } = new();
}

public sealed class GratisDnaStoredConsent
{
    public string Version { get; init; } = "";
    public DateTime AtUtc { get; init; }
}

public sealed class GratisDnaStoredAnswers
{
    public Dictionary<int, int> Competency { get; init; } = new();
    public Dictionary<int, int> Career { get; init; } = new();
    public Dictionary<int, int> Culture { get; init; } = new();
    public Dictionary<int, int> Values { get; init; } = new();
}
