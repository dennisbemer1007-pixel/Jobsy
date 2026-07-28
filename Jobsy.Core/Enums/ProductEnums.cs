namespace Jobsy.Core.Enums;

public enum TokenTransactionKind
{
    Purchase = 0,
    Spend = 1,
    Grant = 2,
    Allocation = 3
}

public enum TokenSpendReason
{
    None = 0,
    Publish = 1,
    Highlight = 2,
    PushBom = 3,
    Extend = 4
}

public enum ApplicationStatus
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2,
    EmployerContacting = 3,
    Hired = 4,
    FilledElsewhere = 5
}

public enum CompanyType
{
    Employer = 0,
    Intermediary = 1
}

public enum PlatformLogLevel
{
    Info = 0,
    Warning = 1,
    Error = 2
}

public enum MetricsPeriod
{
    Day = 0,
    Week = 1,
    Month = 2,
    Quarter = 3,
    Year = 4
}

public enum ShareChannel
{
    WhatsApp = 0,
    Email = 1,
    Facebook = 2,
    LinkedIn = 3,
    Signal = 4,
    Other = 5,
    X = 6,
    Telegram = 7
}

public enum IntegrationKey
{
    Mollie = 0,
    Kvk = 1,
    MicrosoftEntra = 2,
    GoogleEntra = 3,
    Mail = 4,
    OpenAI = 6
}
