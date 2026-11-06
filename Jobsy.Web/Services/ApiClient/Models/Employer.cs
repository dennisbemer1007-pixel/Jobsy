using Jobsy.Core.Enums;
using Jobsy.Web.Models;

namespace Jobsy.Web.Services;

public sealed class DashboardRefreshResult
{
    public DateTime GeneratedAtUtc { get; set; }
    public DateTime CachedUntilUtc { get; set; }
    public List<MetricCount>? Metrics { get; set; }
    public VacancyPerformanceBoard? VacancyPerformance { get; set; }
    public ClientPerformanceBoard? ClientPerformance { get; set; }
    public SalesManagerDashboard? Sales { get; set; }
    public AmbassadeurDashboard? Ambassadeur { get; set; }
}
