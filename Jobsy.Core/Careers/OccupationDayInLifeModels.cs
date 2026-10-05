using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jobsy.Core.Careers;

/// <summary>Candidate-facing read of a stored day. No model call.</summary>
public interface IOccupationDayInLifeReader
{
    Task<OccupationDayReadResult> GetAsync(
        string? escoId,
        string? language = null,
        CancellationToken cancellationToken = default);
}

public sealed record OccupationDayReadResult(bool KnownOccupation, bool Enabled, OccupationDayView? Day);

public sealed record OccupationDayView(
    string EscoId,
    string TitleNl,
    string Morning,
    string Midday,
    string Afternoon,
    string Closing,
    IReadOnlyList<string> Highlights,
    string VariesNote,
    bool ThinSource,
    IReadOnlyList<OccupationDayBlock>? Blocks = null,
    IReadOnlyList<string>? Tasks = null,
    IReadOnlyList<string>? Skills = null);

/// <summary>OpenAI completion for one occupation. The candidate path does not use this.</summary>
public interface IOccupationDayInLifeWriter
{
    Task<OccupationDayWriteResult> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);
}

public sealed record OccupationDayWriteResult(bool Ok, string? Json, string? Error, string Model)
{
    public bool KeyMissing => string.Equals(Error, OccupationDayWriteErrors.KeyMissing, StringComparison.Ordinal);
}

public static class OccupationDayWriteErrors
{
    public const string KeyMissing = "openai-key-missing";
    public const string Http = "openai-http";
    public const string Empty = "openai-empty";
}

public sealed record OccupationDayDraft(
    string TitleNl,
    string Morning,
    string Midday,
    string Afternoon,
    string Closing,
    IReadOnlyList<string> Highlights,
    string VariesNote,
    IReadOnlyList<OccupationDayBlock>? Blocks = null,
    IReadOnlyList<string>? Tasks = null,
    IReadOnlyList<string>? Skills = null);

public sealed record OccupationDayFailure(string EscoId, string Reason);

public sealed record OccupationDayGenerateResult(
    int Generated,
    int Failed,
    int SkippedExisting,
    int Remaining,
    bool KeyMissing,
    IReadOnlyList<OccupationDayFailure> Failures);

public sealed record OccupationDayImportResult(int Inserted, int Updated, int Unchanged, int Rejected);

public sealed record OccupationDayAdminStatus(
    int Stored,
    int Total,
    int Remaining,
    bool Running,
    int GeneratedThisRun,
    int FailedThisRun,
    string? LastError,
    string? LastEscoId);

public sealed class OccupationDayExportDocument
{
    public string Locale { get; set; } = "nl";
    public DateTime ExportedAtUtc { get; set; }
    public List<OccupationDayExportRow> Rows { get; set; } = [];
}

public sealed class OccupationDayExportRow
{
    public string EscoId { get; set; } = "";
    public string Uri { get; set; } = "";
    public string TitleNl { get; set; } = "";
    public string Morning { get; set; } = "";
    public string Midday { get; set; } = "";
    public string Afternoon { get; set; } = "";
    public string Closing { get; set; } = "";
    public List<string> Highlights { get; set; } = [];
    public string VariesNote { get; set; } = "";
    public string SourceModel { get; set; } = "";
    public DateTime GeneratedAtUtc { get; set; }
    public string ContentHash { get; set; } = "";
    public string Locale { get; set; } = "nl";
    public bool ThinSource { get; set; }
    public Dictionary<string, OccupationDayStoredTranslation> Translations { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<OccupationDayBlock> Blocks { get; set; } = [];
    public List<string> Tasks { get; set; } = [];
    public List<string> Skills { get; set; } = [];
}

public static class OccupationDayInLifeGate
{
    /// <summary>Missing or unreadable value means the page is on.</summary>
    public static bool IsEnabled(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        return !bool.TryParse(raw, out var enabled) || enabled;
    }
}

public static class OccupationDayInLifeHash
{
    public static string Compute(string escoId, OccupationDayDraft draft)
    {
        var text = string.Join('\n', new[]
        {
            (escoId ?? "").Trim().ToLowerInvariant(),
            "nl",
            Norm(draft.TitleNl),
            Norm(draft.Morning),
            Norm(draft.Midday),
            Norm(draft.Afternoon),
            Norm(draft.Closing),
            string.Join('\n', draft.Highlights.Select(Norm)),
            string.Join('\n', (draft.Blocks ?? []).Select(block => Norm(block.Key) + "|" + Norm(block.Label) + "|" + Norm(block.Text))),
            string.Join('\n', (draft.Tasks ?? []).Select(Norm)),
            string.Join('\n', (draft.Skills ?? []).Select(Norm)),
            Norm(draft.VariesNote)
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    }

    private static string Norm(string? value) => (value ?? "").Trim().Replace("\r\n", "\n", StringComparison.Ordinal);
}

public static class OccupationDayJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };
}
