using Jobsy.Core.Contracts;

namespace Jobsy.Web.Services;

public sealed record JobSearchActivitiesClientResult(
    JobSearchActivityOverviewDto? Overview,
    int HttpStatus,
    string? ErrorCode,
    string? ExceptionType);
