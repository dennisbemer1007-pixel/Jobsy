namespace Jobsy.Core.Rules;

public static class JobSearchActivityAttachmentRules
{
    public const int MaxBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    public static bool TryNormalize(
        string? fileName,
        string? contentType,
        ReadOnlySpan<byte> content,
        out string safeName,
        out string normalizedType,
        out string? error)
    {
        safeName = string.Empty;
        normalizedType = string.Empty;
        error = null;

        if (content.IsEmpty)
        {
            error = "file_empty";
            return false;
        }

        if (content.Length > MaxBytes)
        {
            error = "file_too_large";
            return false;
        }

        var name = Path.GetFileName(string.IsNullOrWhiteSpace(fileName) ? "bijlage" : fileName.Trim());
        if (name.Length > 200)
        {
            name = name[..200];
        }

        var type = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim();
        if (!AllowedContentTypes.Contains(type))
        {
            error = "file_type";
            return false;
        }

        safeName = name;
        normalizedType = type;
        return true;
    }
}
