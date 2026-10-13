using System.IO.Compression;

namespace Jobsy.Core.Rules;

public static class CandidateCvFileRules
{
    public const int MaxBytes = 5 * 1024 * 1024;
    public const int MaxFileNameLength = 180;

    public const string PdfContentType = "application/pdf";
    public const string DocxContentType =
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public const string EmptyMessage = "Het CV-bestand is leeg.";
    public const string TooLargeMessage = "Het CV mag maximaal 5 MB zijn.";
    public const string WrongTypeMessage = "Upload een PDF of Word-bestand (.docx).";
    public const string NotPdfMessage = "Dit bestand is geen PDF. Upload een PDF of Word-bestand (.docx).";
    public const string PdfTruncatedMessage = "Het PDF-bestand is niet compleet. Upload het CV opnieuw.";
    public const string NotDocxMessage = "Dit bestand is geen Word-bestand (.docx). Upload een PDF of Word-bestand (.docx).";
    public const string DocxTruncatedMessage = "Het Word-bestand is niet compleet. Upload het CV opnieuw.";
    public const string NotValidDocxMessage =
        "Dit Word-bestand is geen geldig .docx-bestand. Sla het op als .docx en upload het opnieuw.";

    public static bool TryNormalize(
        string? fileName,
        string? contentType,
        int sizeBytes,
        out string safeFileName,
        out string normalizedContentType,
        out string? error)
    {
        safeFileName = "cv.pdf";
        normalizedContentType = PdfContentType;
        error = null;

        if (sizeBytes <= 0)
        {
            error = EmptyMessage;
            return false;
        }

        if (sizeBytes > MaxBytes)
        {
            error = TooLargeMessage;
            return false;
        }

        var rawName = string.IsNullOrWhiteSpace(fileName) ? "cv" : Path.GetFileName(fileName.Trim());
        rawName = rawName.Replace('\0', '_').Trim();
        if (rawName.Length > MaxFileNameLength)
        {
            rawName = rawName[..MaxFileNameLength];
        }

        var ext = Path.GetExtension(rawName).ToLowerInvariant();
        var type = (contentType ?? string.Empty).Trim().ToLowerInvariant();
        if (type.Contains(';'))
        {
            type = type.Split(';')[0].Trim();
        }

        var isPdf = ext == ".pdf" || type == PdfContentType;
        var isDocx = ext == ".docx"
                     || type == DocxContentType
                     || type == "application/docx";

        if (!isPdf && !isDocx)
        {
            error = WrongTypeMessage;
            return false;
        }

        if (isPdf)
        {
            if (string.IsNullOrWhiteSpace(ext))
            {
                rawName += ".pdf";
            }

            safeFileName = rawName;
            normalizedContentType = PdfContentType;
            return true;
        }

        if (string.IsNullOrWhiteSpace(ext))
        {
            rawName += ".docx";
        }

        safeFileName = rawName;
        normalizedContentType = DocxContentType;
        return true;
    }

    /// <summary>
    /// Checks the bytes after they are read, before they are stored or sent to CV extraction.
    /// PDF must start with <c>%PDF-</c> and contain <c>%%EOF</c>.
    /// DOCX must be a ZIP local file (<c>PK\x03\x04</c>) with <c>[Content_Types].xml</c> and <c>word/document.xml</c>.
    /// </summary>
    public static bool TryValidateBytes(byte[]? content, string? normalizedContentType, out string? error)
    {
        error = null;
        if (content is null || content.Length == 0)
        {
            error = EmptyMessage;
            return false;
        }

        if (content.Length > MaxBytes)
        {
            error = TooLargeMessage;
            return false;
        }

        if (string.Equals(normalizedContentType, PdfContentType, StringComparison.OrdinalIgnoreCase))
        {
            return TryValidatePdf(content, out error);
        }

        if (string.Equals(normalizedContentType, DocxContentType, StringComparison.OrdinalIgnoreCase))
        {
            return TryValidateDocx(content, out error);
        }

        error = WrongTypeMessage;
        return false;
    }

    private static bool TryValidatePdf(byte[] content, out string? error)
    {
        error = null;
        if (!StartsWithPdfHeader(content))
        {
            error = NotPdfMessage;
            return false;
        }

        if (content.AsSpan().IndexOf("%%EOF"u8) < 0)
        {
            error = PdfTruncatedMessage;
            return false;
        }

        return true;
    }

    private static bool TryValidateDocx(byte[] content, out string? error)
    {
        error = null;
        if (!HasZipLocalHeader(content))
        {
            error = NotDocxMessage;
            return false;
        }

        if (content.Length < 30)
        {
            error = DocxTruncatedMessage;
            return false;
        }

        try
        {
            using var stream = new MemoryStream(content, writable: false);
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
            var hasTypes = false;
            var hasDocument = false;
            var seen = 0;
            foreach (var entry in zip.Entries)
            {
                seen++;
                if (seen > 2000)
                {
                    error = NotValidDocxMessage;
                    return false;
                }

                var name = entry.FullName.Replace('\\', '/').TrimStart('/');
                if (name.Equals("[Content_Types].xml", StringComparison.OrdinalIgnoreCase))
                {
                    hasTypes = true;
                }
                else if (name.Equals("word/document.xml", StringComparison.OrdinalIgnoreCase))
                {
                    hasDocument = true;
                }

                if (hasTypes && hasDocument)
                {
                    return true;
                }
            }

            error = NotValidDocxMessage;
            return false;
        }
        catch (InvalidDataException)
        {
            error = DocxTruncatedMessage;
            return false;
        }
        catch (IOException)
        {
            error = DocxTruncatedMessage;
            return false;
        }
    }

    private static bool StartsWithPdfHeader(ReadOnlySpan<byte> content)
        => content.Length >= 5
           && content[0] == (byte)'%'
           && content[1] == (byte)'P'
           && content[2] == (byte)'D'
           && content[3] == (byte)'F'
           && content[4] == (byte)'-';

    private static bool HasZipLocalHeader(ReadOnlySpan<byte> content)
        => content.Length >= 4
           && content[0] == 0x50
           && content[1] == 0x4B
           && content[2] == 0x03
           && content[3] == 0x04;
}
