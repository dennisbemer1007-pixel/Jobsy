namespace Jobsy.Core.Rules;

/// <summary>Magic-byte sniffing for common upload MIME types (PDF, DOCX, PNG, JPEG, WebP).</summary>
public static class UploadFileMagicRules
{
    public static bool TryValidateContent(string? normalizedContentType, ReadOnlySpan<byte> content, out string? error)
    {
        error = null;
        if (content.IsEmpty)
        {
            error = "file_empty";
            return false;
        }

        if (string.Equals(normalizedContentType, CandidateCvFileRules.PdfContentType, StringComparison.OrdinalIgnoreCase))
        {
            return CandidateCvFileRules.TryValidateBytes(content.ToArray(), normalizedContentType, out error);
        }

        if (string.Equals(normalizedContentType, CandidateCvFileRules.DocxContentType, StringComparison.OrdinalIgnoreCase))
        {
            return CandidateCvFileRules.TryValidateBytes(content.ToArray(), normalizedContentType, out error);
        }

        if (string.Equals(normalizedContentType, "image/png", StringComparison.OrdinalIgnoreCase))
        {
            if (!LooksLikePng(content))
            {
                error = "file_type";
                return false;
            }

            return true;
        }

        if (normalizedContentType is "image/jpeg" or "image/jpg")
        {
            if (!LooksLikeJpeg(content))
            {
                error = "file_type";
                return false;
            }

            return true;
        }

        if (string.Equals(normalizedContentType, "image/webp", StringComparison.OrdinalIgnoreCase))
        {
            if (!LooksLikeWebp(content))
            {
                error = "file_type";
                return false;
            }

            return true;
        }

        error = "file_type";
        return false;
    }

    private static bool LooksLikePng(ReadOnlySpan<byte> content)
        => content.Length >= 8
           && content[0] == 0x89
           && content[1] == (byte)'P'
           && content[2] == (byte)'N'
           && content[3] == (byte)'G';

    private static bool LooksLikeJpeg(ReadOnlySpan<byte> content)
        => content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF;

    private static bool LooksLikeWebp(ReadOnlySpan<byte> content)
        => content.Length >= 12
           && content[0] == (byte)'R'
           && content[1] == (byte)'I'
           && content[2] == (byte)'F'
           && content[3] == (byte)'F'
           && content[8] == (byte)'W'
           && content[9] == (byte)'E'
           && content[10] == (byte)'B'
           && content[11] == (byte)'P';
}
