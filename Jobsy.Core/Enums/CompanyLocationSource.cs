namespace Jobsy.Core.Enums;

/// <summary>
/// How a company's map coordinates were obtained. Unknown rows are skipped on the public map.
/// </summary>
public enum CompanyLocationSource
{
    Unknown = 0,
    Kvk = 1,
    Pdok = 2
}
