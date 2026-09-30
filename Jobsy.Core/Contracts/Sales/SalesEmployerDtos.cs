using Jobsy.Core.Enums;

namespace Jobsy.Core.Contracts.Sales;

/// <summary>Privacy-safe referred employer row for the sales portal (D4).</summary>
public sealed class SalesEmployerDto
{
    public Guid CompanyId { get; init; }
    public string DisplayName { get; init; } = "";
    public string? Place { get; init; }
    public int BranchCount { get; init; }
    public DateOnly AttributedOn { get; init; }
    public string Source { get; init; } = "";
    public SalesEmployerStatus Status { get; init; }
    public string StatusLabelKey { get; init; } = "";
    public int? QuietDays { get; init; }
    public int? CommissionYear { get; init; }
    public decimal? CurrentRate { get; init; }
    public double YearProgress { get; init; }
    public decimal CommissionThisYear { get; init; }
    public DateOnly? LastPurchaseOn { get; init; }
}

public sealed class SalesEmployerDetailDto
{
    public SalesEmployerDto Row { get; init; } = new();
    public decimal CommissionTotal { get; init; }
    public int PurchaseCount { get; init; }
    public IReadOnlyList<SalesEmployerYearSegmentDto> Years { get; init; } = [];
    public IReadOnlyList<SalesEmployerTimelineItemDto> Timeline { get; init; } = [];
    public IReadOnlyList<SalesEmployerCommissionLineDto> Lines { get; init; } = [];
}

public sealed class SalesEmployerYearSegmentDto
{
    public int Year { get; init; }
    public DateOnly Start { get; init; }
    public DateOnly End { get; init; }
    public decimal Rate { get; init; }
    public bool IsCurrent { get; init; }
    public double Progress { get; init; }
}

public sealed class SalesEmployerTimelineItemDto
{
    public string Key { get; init; } = "";
    public string Label { get; init; } = "";
    public DateOnly? On { get; init; }
    public bool IsFuture { get; init; }
}

public sealed class SalesEmployerCommissionLineDto
{
    public DateOnly On { get; init; }
    public string PackageLabel { get; init; } = "";
    public decimal PurchaseAmountExVat { get; init; }
    public decimal OwnCommissionExVat { get; init; }
    public string State { get; init; } = "";
    public string StateLabel { get; init; } = "";
}

public enum SalesEmployerStatus
{
    NoPurchase = 0,
    Active = 1,
    Quiet = 2,
    Ended = 3
}

public sealed class SalesEmployerPageDto
{
    public IReadOnlyList<SalesEmployerDto> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public SalesEmployerListKpisDto Kpis { get; init; } = new();
}

public sealed class SalesEmployerListKpisDto
{
    public int TotalEmployers { get; init; }
    public int ActiveEmployers { get; init; }
    public int FirstPurchaseCount { get; init; }
    public int QuietCount { get; init; }
    public decimal AverageCommissionPerActive { get; init; }
}
