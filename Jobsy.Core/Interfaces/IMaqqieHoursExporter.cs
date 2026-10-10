using Jobsy.Core.Entities;

namespace Jobsy.Core.Interfaces;

/// <summary>Pass-through export of approved hours to Maqqie (stub until API integration).</summary>
public interface IMaqqieHoursExporter
{
    Task ExportWeekAsync(MaqqieHoursWeek week, Application application, CancellationToken cancellationToken = default);
}
