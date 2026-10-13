using Jobsy.Core.Enums;
using Jobsy.Core.Reports.Career;
using Jobsy.Core.Reports.Culture;
using Jobsy.Core.Reports.Values;

namespace Jobsy.Core.Interfaces;

/// <summary>Builds and loads stored paid deep reports for Career, Culture and Values.</summary>
public interface IKindDeepReportService
{
    Task<CareerDeepReport?> GetCareerAsync(Guid userId, CancellationToken ct = default);
    Task<CultureDeepReport?> GetCultureAsync(Guid userId, CancellationToken ct = default);
    Task<ValuesDeepReport?> GetValuesAsync(Guid userId, CancellationToken ct = default);

    Task<object?> GetStoredAsync(Guid userId, AssessmentKind kind, CancellationToken ct = default);

    Task BuildAndStoreAsync(Guid userId, AssessmentKind kind, CancellationToken ct = default);
}
