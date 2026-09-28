using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

public interface ISampleAssessmentReportPdfService
{
    Task<AssessmentReportPdf> RenderSampleAsync(
        AssessmentKind kind,
        string? lang,
        CancellationToken ct = default);
}
