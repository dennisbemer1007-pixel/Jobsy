namespace Jobsy.Core.Rules;

public static class CandidateCvFileRules
{
    public const int MaxBytes = 5 * 1024 * 1024;
    public const int MaxFileNameLength = 180;

    public const string PdfContentType = "application/pdf";
    public const string DocxContentType =
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

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
            error = "Het CV-bestand is leeg.";
            return false;
        }

        if (sizeBytes > MaxBytes)
        {
            error = "Het CV mag maximaal 5 MB zijn.";
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
            error = "Upload een PDF of Word-bestand (.docx).";
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
}
