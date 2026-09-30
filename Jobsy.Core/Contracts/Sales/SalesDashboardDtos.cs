namespace Jobsy.Core.Contracts.Sales;

public sealed class SalesDashboardDto
{
    public string FirstName { get; init; } = "";
    public string? TrackingCode { get; init; }
    public string Period { get; init; } = "year";
    public decimal Available { get; init; }
    public decimal Pending { get; init; }
    public decimal EarnedInPeriod { get; init; }
    public decimal? EarnedPrevComparable { get; init; }
    public int ActiveEmployers { get; init; }
    public int TotalEmployers { get; init; }
    public int NewEmployersThisMonth { get; init; }
    public DateOnly NextRunDate { get; init; }
    public int CommissionHoldDays { get; init; }
    public decimal PayoutMinimumEuro { get; init; }
    public bool CanRequestPayout { get; init; }
    public string? PayoutBlockedReason { get; init; }
    public IReadOnlyList<SalesMonthlyBarDto> Monthly { get; init; } = [];
    public SalesFunnelDto? Funnel { get; init; }
    public IReadOnlyList<SalesTodoDto> Todos { get; init; } = [];
    public IReadOnlyList<SalesTopEmployerDto> TopEmployers { get; init; } = [];
}

public sealed class SalesMonthlyBarDto
{
    public int Year { get; init; }
    public int Month { get; init; }
    public string Label { get; init; } = "";
    public decimal AmountExVat { get; init; }
    public bool IsCurrent { get; init; }
}

public sealed class SalesFunnelDto
{
    public int Visits { get; init; }
    public int Registered { get; init; }
    public int FirstPurchase { get; init; }
    public int ActiveNow { get; init; }
    public string PeriodLabel { get; init; } = "";
}

public sealed class SalesTodoDto
{
    public string Kind { get; init; } = "";
    public string TitleKey { get; init; } = "";
    public string[] TitleArgs { get; init; } = [];
    public string SubLineKey { get; init; } = "";
    public string[] SubLineArgs { get; init; } = [];
    public string ActionLabelKey { get; init; } = "";
    public string? ActionHref { get; init; }
    public Guid? CompanyId { get; init; }
    public Guid? InvoiceId { get; init; }
    public int Urgency { get; init; }
}

public sealed class SalesTopEmployerDto
{
    public Guid CompanyId { get; init; }
    public string DisplayName { get; init; } = "";
    public string? Place { get; init; }
    public string StatusLabelKey { get; init; } = "";
    public int? QuietDays { get; init; }
    public decimal CommissionThisYear { get; init; }
    public bool IsQuiet { get; init; }
}
