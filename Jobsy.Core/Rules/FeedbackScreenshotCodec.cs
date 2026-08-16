namespace Jobsy.Core.Rules;

public static class FeedbackScreenshotCodec
{
    public const int MaxDecodedBytes = 1_500_000;
    public const int MaxDescriptionLength = 4000;
    public const int MaxPageUrlLength = 2048;
    public const int MaxBrowserInfoLength = 512;
    public const int MaxDeviceInfoLength = 256;
    public const int MaxPromptLength = 16_000;

    public static bool TryDecodeDataUrl(string? dataUrl, out byte[] bytes, out string contentType, out string? error)
    {
        bytes = [];
        contentType = "image/png";
        error = null;

        if (string.IsNullOrWhiteSpace(dataUrl))
        {
            return true;
        }

        var raw = dataUrl.Trim();
        var payload = raw;
        if (raw.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var comma = raw.IndexOf(',');
            if (comma < 0)
            {
                error = "Ongeldige screenshot (data-URL).";
                return false;
            }

            var header = raw[..comma];
            payload = raw[(comma + 1)..];
            var mimeEnd = header.IndexOf(';');
            if (header.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                var mime = mimeEnd > 5 ? header[5..mimeEnd] : header[5..];
                if (!string.IsNullOrWhiteSpace(mime))
                {
                    contentType = mime.Trim().ToLowerInvariant();
                }
            }
        }

        if (contentType is not ("image/png" or "image/jpeg" or "image/jpg" or "image/webp"))
        {
            error = "Screenshot moet PNG, JPEG of WebP zijn.";
            return false;
        }

        if (contentType == "image/jpg")
        {
            contentType = "image/jpeg";
        }

        payload = payload.Trim();
        if (payload.Length == 0)
        {
            return true;
        }

        // Base64 expands ~4/3; reject obviously oversized payloads before allocating.
        var maxChars = (int)(MaxDecodedBytes * 4L / 3) + 8;
        if (payload.Length > maxChars)
        {
            error = "Screenshot is te groot (max. 1,5 MB).";
            return false;
        }

        try
        {
            bytes = Convert.FromBase64String(payload);
        }
        catch (FormatException)
        {
            error = "Ongeldige screenshot (base64).";
            return false;
        }

        if (bytes.Length > MaxDecodedBytes)
        {
            error = "Screenshot is te groot (max. 1,5 MB).";
            bytes = [];
            return false;
        }

        return true;
    }

    public static string ToDataUrl(byte[] bytes, string? contentType)
    {
        var mime = string.IsNullOrWhiteSpace(contentType) ? "image/png" : contentType.Trim();
        return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
    }

    public static string ToBase64(byte[] bytes) => Convert.ToBase64String(bytes);

    public static string Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}
