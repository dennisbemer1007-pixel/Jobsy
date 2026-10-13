using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

public interface IAssessmentReportPdfService
{
    Task<AssessmentReportPdf?> TryRenderAsync(
        Guid userId,
        AssessmentKind kind,
        CancellationToken cancellationToken = default);

    /// <param name="lang">UI language; report language follows <see cref="Jobsy.Core.Reports.ReportLanguage"/>.</param>
    Task<AssessmentReportPdf?> TryRenderAsync(
        Guid userId,
        AssessmentKind kind,
        string? lang,
        CancellationToken cancellationToken = default);
}

public sealed record AssessmentReportPdf(string FileName, byte[] Content);
