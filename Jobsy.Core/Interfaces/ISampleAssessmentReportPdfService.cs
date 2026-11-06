using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

public interface ISampleAssessmentReportPdfService
{
    Task<AssessmentReportPdf> RenderSampleAsync(
        AssessmentKind kind,
        string? lang,
        CancellationToken ct = default);

    /// <summary>
    /// First two sample-PDF pages as PNG (for the preview dialog), plus total report page count.
    /// </summary>
    Task<SampleAssessmentReportPreview> RenderSamplePreviewAsync(
        AssessmentKind kind,
        string? lang,
        CancellationToken ct = default);
}

public sealed record SampleAssessmentReportPreview(
    string FileName,
    int TotalPages,
    IReadOnlyList<string> PagePngBase64);
