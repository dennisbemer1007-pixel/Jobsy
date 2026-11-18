namespace Jobsy.Core.Rules;

/// <summary>PDF producer/creator strings must never surface as CV facts (certificates, employers, etc.).</summary>
public static class CvPdfMetadataGuard
{
    private static readonly string[] DeniedSubstrings =
    [
        "reportlab pdf library",
        "reportlab",
        "adobe pdf library",
        "adobe illustrator",
        "adobe indesign",
        "microsoft word",
        "microsoft® word",
        "skia/pdf",
        "skia",
        "qt ",
        "pdfsharp",
        "wkhtmltopdf",
        "libreoffice",
        "openoffice",
        "itext",
        "fpdf",
        "cairo",
        "ghostscript",
        "producer",
        "creator",
    ];

    public static bool IsPdfMetadataNoise(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var normalized = CvExtractionGrounding.Normalize(value);
        if (normalized.Length == 0)
        {
            return true;
        }

        foreach (var denied in DeniedSubstrings)
        {
            if (normalized.Contains(denied, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
