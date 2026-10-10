using System.Globalization;
using System.Text;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services.EmployerPhase2;

/// <summary>Writes approved weeks to a local folder (stub until Maqqie API exists).</summary>
public sealed class FileMaqqieHoursExporter : IMaqqieHoursExporter
{
    private readonly IHostEnvironment _env;
    private readonly ILogger<FileMaqqieHoursExporter> _logger;

    public FileMaqqieHoursExporter(IHostEnvironment env, ILogger<FileMaqqieHoursExporter> logger)
    {
        _env = env;
        _logger = logger;
    }

    public Task ExportWeekAsync(MaqqieHoursWeek week, Application application, CancellationToken cancellationToken = default)
    {
        var dir = Path.Combine(_env.ContentRootPath, "App_Data", "maqqie-hours-export");
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, $"{week.Id:N}.csv");
        var line = string.Join(
            ',',
            week.WeekStart.ToString("O", CultureInfo.InvariantCulture),
            application.Id.ToString("D"),
            week.TotalHours.ToString(CultureInfo.InvariantCulture),
            week.DailyHoursJson);
        File.WriteAllText(file, line, Encoding.UTF8);
        _logger.LogInformation("Maqqie hours stub export written to {Path}", file);
        return Task.CompletedTask;
    }
}
