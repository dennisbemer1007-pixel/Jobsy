using System.Text.Json;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;

namespace Jobsy.Core.Rules;

/// <summary>
/// Validates a foreign-diploma evaluation the candidate typed in.
/// This type never maps free text or the optional pick list onto a Dutch education
/// level, a match score, or <see cref="EducationLevelLabels"/>.
/// </summary>
public static class DiplomaEvaluationRules
{
    public const bool MayInfluenceMatchOrEducationLevel = false;

    public const int MaxPerCandidate = 8;
    public const int MaxDiplomaTitleLength = 200;
    public const int MaxOtherBodyLength = 120;
    public const int MaxLevelTextLength = 200;
    public const int MaxReferenceLength = 80;
    public const int MaxDocumentBytes = 5 * 1024 * 1024;
    public const int MaxFileNameLength = 180;
    public const int MaxSnapshotJsonLength = 4000;

    public const string BodyNuffic = "nuffic";
    public const string BodySbb = "sbb";
    public const string BodyOther = "other";

    public static readonly string[] IssuingBodies = [BodyNuffic, BodySbb, BodyOther];

    public static readonly string[] LevelCodes =
    [
        "mbo-1",
        "mbo-2",
        "mbo-3",
        "mbo-4",
        "havo",
        "vwo",
        "hbo-bachelor",
        "hbo-master",
        "wo-bachelor",
        "wo-master"
    ];

    private static readonly JsonSerializerOptions SnapshotJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public sealed record Normalized(
        string? DiplomaTitle,
        string IssuingBody,
        string? IssuingBodyOther,
        string EquivalentLevelText,
        string? EquivalentLevelCode,
        DateOnly EvaluationDate,
        string ReferenceNumber);

    public static bool TryNormalize(
        string? diplomaTitle,
        string? issuingBody,
        string? issuingBodyOther,
        string? equivalentLevelText,
        string? equivalentLevelCode,
        DateOnly? evaluationDate,
        string? referenceNumber,
        DateOnly today,
        out Normalized? value,
        out string? errorCode)
    {
        value = null;
        errorCode = null;

        var body = (issuingBody ?? string.Empty).Trim().ToLowerInvariant();
        if (body.Length == 0)
        {
            errorCode = "body_required";
            return false;
        }

        if (!IssuingBodies.Contains(body, StringComparer.Ordinal))
        {
            errorCode = "body_invalid";
            return false;
        }

        string? other = null;
        if (body == BodyOther)
        {
            other = string.IsNullOrWhiteSpace(issuingBodyOther) ? null : issuingBodyOther.Trim();
            if (other is null)
            {
                errorCode = "other_name_required";
                return false;
            }

            if (other.Length > MaxOtherBodyLength)
            {
                errorCode = "other_name_too_long";
                return false;
            }
        }

        var title = string.IsNullOrWhiteSpace(diplomaTitle) ? null : CollapseSpaces(diplomaTitle);
        if (title is { Length: > MaxDiplomaTitleLength })
        {
            errorCode = "title_too_long";
            return false;
        }

        // Keep the candidate's wording. Do not trim internal spacing they typed, only ends.
        var level = equivalentLevelText?.Trim();
        if (string.IsNullOrEmpty(level))
        {
            errorCode = "level_required";
            return false;
        }

        if (level.Length > MaxLevelTextLength)
        {
            errorCode = "level_too_long";
            return false;
        }

        string? code = string.IsNullOrWhiteSpace(equivalentLevelCode)
            ? null
            : equivalentLevelCode.Trim().ToLowerInvariant();
        if (code is not null && !LevelCodes.Contains(code, StringComparer.Ordinal))
        {
            errorCode = "level_code_invalid";
            return false;
        }

        if (evaluationDate is null)
        {
            errorCode = "date_required";
            return false;
        }

        if (evaluationDate.Value > today)
        {
            errorCode = "date_future";
            return false;
        }

        if (evaluationDate.Value < new DateOnly(1950, 1, 1))
        {
            errorCode = "date_too_old";
            return false;
        }

        var reference = string.IsNullOrWhiteSpace(referenceNumber) ? null : CollapseSpaces(referenceNumber);
        if (reference is null)
        {
            errorCode = "reference_required";
            return false;
        }

        if (reference.Length > MaxReferenceLength)
        {
            errorCode = "reference_too_long";
            return false;
        }

        value = new Normalized(title, body, other, level, code, evaluationDate.Value, reference);
        return true;
    }

    public static bool TryNormalizeDocument(
        string? fileName,
        string? contentType,
        byte[]? content,
        out string safeFileName,
        out string normalizedContentType,
        out string? errorCode)
    {
        safeFileName = "waardering.pdf";
        normalizedContentType = "application/pdf";
        errorCode = null;

        if (content is null || content.Length == 0)
        {
            errorCode = "file_empty";
            return false;
        }

        if (content.Length > MaxDocumentBytes)
        {
            errorCode = "file_too_large";
            return false;
        }

        if (!TryDetect(content, out var extension, out normalizedContentType))
        {
            errorCode = "file_type";
            return false;
        }

        var rawName = string.IsNullOrWhiteSpace(fileName) ? "waardering" : Path.GetFileName(fileName.Trim());
        rawName = rawName.Replace('\0', '_').Trim();
        var givenExt = Path.GetExtension(rawName).ToLowerInvariant();
        if (givenExt.Length > 0 && !ExtensionMatches(givenExt, extension))
        {
            errorCode = "file_type";
            return false;
        }

        var stem = Path.GetFileNameWithoutExtension(rawName);
        if (string.IsNullOrWhiteSpace(stem))
        {
            stem = "waardering";
        }

        var safe = stem + extension;
        if (safe.Length > MaxFileNameLength)
        {
            var room = MaxFileNameLength - extension.Length;
            safe = (room > 0 ? stem[..Math.Min(stem.Length, room)] : "waardering") + extension;
        }

        safeFileName = safe;
        _ = contentType;
        return true;
    }

    /// <summary>Exact Dutch passport/PDF label. Official bodies share one phrase; others use the typed name.</summary>
    public static string DutchAttribution(string issuingBody, string? otherName)
    {
        if (string.Equals(issuingBody, BodyNuffic, StringComparison.Ordinal)
            || string.Equals(issuingBody, BodySbb, StringComparison.Ordinal))
        {
            return "volgens waardering van Nuffic/SBB";
        }

        var name = string.IsNullOrWhiteSpace(otherName) ? "de instantie" : otherName.Trim();
        return "volgens waardering van " + name;
    }

    public static string DutchLevelLabel(string? code)
        => code switch
        {
            "mbo-1" => "mbo 1",
            "mbo-2" => "mbo 2",
            "mbo-3" => "mbo 3",
            "mbo-4" => "mbo 4",
            "havo" => "havo",
            "vwo" => "vwo",
            "hbo-bachelor" => "hbo-bachelor",
            "hbo-master" => "hbo-master",
            "wo-bachelor" => "wo-bachelor",
            "wo-master" => "wo-master",
            _ => ""
        };

    public static DiplomaEvaluationSharedFact ToSharedFact(CandidateDiplomaEvaluation row)
        => new(
            row.DiplomaTitle,
            row.IssuingBody,
            row.IssuingBodyOther,
            row.EquivalentLevelText,
            row.EquivalentLevelCode,
            row.EvaluationDate,
            row.ReferenceNumber);

    public static DiplomaEvaluationFactDto ToFactDto(CandidateDiplomaEvaluation row)
        => new(
            row.Id,
            row.DiplomaTitle,
            row.IssuingBody,
            row.IssuingBodyOther,
            row.EquivalentLevelText,
            row.EquivalentLevelCode,
            row.EvaluationDate,
            row.ReferenceNumber,
            row.DocumentContent is { Length: > 0 },
            row.DocumentContent is { Length: > 0 } ? row.DocumentFileName : null);

    public static string? SerializeSnapshot(IReadOnlyList<DiplomaEvaluationSharedFact>? facts, int maxLength = MaxSnapshotJsonLength)
    {
        var items = (facts ?? []).ToList();
        if (items.Count == 0)
        {
            return null;
        }

        while (items.Count > 0)
        {
            var json = JsonSerializer.Serialize(items, SnapshotJson);
            if (json.Length <= maxLength)
            {
                return json;
            }

            items.RemoveAt(items.Count - 1);
        }

        return null;
    }

    public static IReadOnlyList<DiplomaEvaluationSharedFact> ParseSnapshot(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<DiplomaEvaluationSharedFact>>(json, SnapshotJson) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static bool IsErrorCode(string? value)
        => value is "body_required" or "body_invalid" or "other_name_required" or "other_name_too_long"
            or "title_too_long" or "level_required" or "level_too_long" or "level_code_invalid"
            or "date_required" or "date_future" or "date_too_old" or "reference_required" or "reference_too_long"
            or "too_many" or "file_empty" or "file_too_large" or "file_type" or "not_found";

    public static string DutchMessage(string? errorCode)
        => errorCode switch
        {
            "body_required" or "body_invalid" => "Kies de instantie die de waardering afgaf.",
            "other_name_required" => "Vul de naam van de instantie in.",
            "other_name_too_long" => "De naam van de instantie is te lang.",
            "title_too_long" => "De naam van het diploma is te lang.",
            "level_required" => "Vul het Nederlandse niveau in, letterlijk zoals op de waardering.",
            "level_too_long" => "Het niveau is te lang.",
            "level_code_invalid" => "Kies een niveau uit de lijst, of laat de lijst leeg.",
            "date_required" => "Vul de datum van de waardering in.",
            "date_future" => "De datum van de waardering kan niet in de toekomst liggen.",
            "date_too_old" => "De datum van de waardering is te oud.",
            "reference_required" => "Vul het kenmerk of referentienummer in.",
            "reference_too_long" => "Het kenmerk is te lang.",
            "too_many" => "Je kunt maximaal 8 waarderingen bewaren.",
            "file_empty" => "Het bestand is leeg.",
            "file_too_large" => "Het bestand mag maximaal 5 MB zijn.",
            "file_type" => "Upload een PDF of een afbeelding (PNG, JPEG, WEBP of GIF).",
            "not_found" => "Deze waardering is niet gevonden.",
            _ => "De waardering is niet geldig."
        };

    private static string CollapseSpaces(string value)
    {
        var flat = value.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ').Trim();
        return string.Join(' ', flat.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static bool TryDetect(byte[] content, out string extension, out string contentType)
    {
        extension = "";
        contentType = "";
        if (content.Length >= 5
            && content[0] == (byte)'%'
            && content[1] == (byte)'P'
            && content[2] == (byte)'D'
            && content[3] == (byte)'F'
            && content[4] == (byte)'-')
        {
            extension = ".pdf";
            contentType = "application/pdf";
            return true;
        }

        if (content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF)
        {
            extension = ".jpg";
            contentType = "image/jpeg";
            return true;
        }

        if (content.Length >= 8
            && content[0] == 0x89 && content[1] == 0x50 && content[2] == 0x4E && content[3] == 0x47
            && content[4] == 0x0D && content[5] == 0x0A && content[6] == 0x1A && content[7] == 0x0A)
        {
            extension = ".png";
            contentType = "image/png";
            return true;
        }

        if (content.Length >= 6
            && content[0] == (byte)'G' && content[1] == (byte)'I' && content[2] == (byte)'F'
            && content[3] == (byte)'8' && (content[4] == (byte)'7' || content[4] == (byte)'9')
            && content[5] == (byte)'a')
        {
            extension = ".gif";
            contentType = "image/gif";
            return true;
        }

        if (content.Length >= 12
            && content[0] == (byte)'R' && content[1] == (byte)'I' && content[2] == (byte)'F' && content[3] == (byte)'F'
            && content[8] == (byte)'W' && content[9] == (byte)'E' && content[10] == (byte)'B' && content[11] == (byte)'P')
        {
            extension = ".webp";
            contentType = "image/webp";
            return true;
        }

        return false;
    }

    private static bool ExtensionMatches(string given, string detected)
    {
        if (given == detected)
        {
            return true;
        }

        return detected == ".jpg" && given is ".jpeg" or ".jpg";
    }
}
