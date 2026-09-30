using Jobsy.Core.Enums;

namespace Jobsy.Core.Contracts.Scholen;

public sealed record SchoolReportFilterDto(
    int SchoolYearStart,
    Guid? SchoolId,
    SchoolLevel? Level,
    int? Year);

public sealed record SchoolReportNamedCountDto(string Key, string Display, int? Count, bool Masked);

public sealed record SchoolReportLevelYearRowDto(
    SchoolLevel Level,
    int Year,
    int? ClassCount,
    int? PupilCount,
    int? StartedCount,
    int? CompletedCount,
    bool Masked);

public sealed record SchoolReportViewDto(
    int SchoolYearStart,
    string SchoolYearLabel,
    Guid? SchoolId,
    string? SchoolName,
    SchoolLevel? Level,
    int? Year,
    int? ActiveSchools,
    int? ClassCount,
    int? StartedCount,
    int? CompletedCount,
    double? CompletedPercent,
    IReadOnlyList<SchoolReportNamedCountDto> RiasecTop3,
    IReadOnlyList<SchoolReportNamedCountDto> TopValues,
    IReadOnlyList<SchoolReportNamedCountDto> TopCultures,
    IReadOnlyList<SchoolReportNamedCountDto> DreamJobsTop10,
    IReadOnlyList<SchoolReportLevelYearRowDto> PerLevelYear,
    string CsvHeaderHelp);

public sealed record SchoolRetentionRunDto(
    Guid Id,
    DateTime RanAtUtc,
    DateOnly CutoffDate,
    int ClassesDeleted,
    int CodesDeleted,
    int ResultsDeleted,
    int AggregatesWritten,
    string Outcome);

public sealed record SchoolRetentionStatusDto(
    DateOnly NextCutoff,
    string NextCutoffLabel,
    IReadOnlyList<SchoolRetentionRunDto> RecentRuns);

public sealed record SchoolRetentionDryRunDto(
    DateOnly TodayAmsterdam,
    DateOnly CutoffDate,
    int ClassesWouldDelete,
    int CodesWouldDelete,
    int ResultsWouldDelete,
    IReadOnlyList<SchoolRetentionDryRunSchoolDto> Schools);

public sealed record SchoolRetentionDryRunSchoolDto(
    Guid SchoolId,
    string SchoolName,
    int Classes,
    int Codes,
    int Results,
    IReadOnlyList<int> SchoolYearStarts);

public sealed record SchoolRetentionImpactDto(
    int CutoffMonth,
    int CutoffDay,
    int ClassesImpacted,
    string ImpactNote);

public sealed record ConfirmSchoolNameRequest(string ConfirmName);

public sealed record DeleteSchoolYearRequest(string ConfirmPhrase);

public sealed record SnapshotTotalsResultDto(int AggregatesWritten);
