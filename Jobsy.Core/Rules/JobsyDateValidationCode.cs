namespace Jobsy.Core.Rules;

public enum JobsyDateValidationCode
{
    None = 0,
    Incomplete,
    InvalidDate,
    BirthDateFuture,
    BirthDateTooYoung,
    BirthDateTooOld,
    NotFuture,
    NotPast,
    BeforeMin,
    AfterMax
}
