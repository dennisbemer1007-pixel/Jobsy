namespace Jobsy.Core.Enums;

/// <summary>Kinds of rows in <c>ApplicationStatusHistory</c> (kandidaat-banen 07).</summary>
public enum ApplicationStatusEventKind
{
    Created = 0,
    StatusChanged = 1,
    EmployerViewed = 2
}
