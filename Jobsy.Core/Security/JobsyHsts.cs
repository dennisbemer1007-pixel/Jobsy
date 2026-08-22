namespace Jobsy.Core.Security;

/// <summary>
/// Mozilla Observatory requires HSTS <c>max-age</c> ≥ 15_768_000 (six months).
/// Two years matches the common preload-list recommendation without opting into preload.
/// </summary>
public static class JobsyHsts
{
    public const long ObservatoryMinimumSeconds = 15_768_000;
    public const long MaxAgeSeconds = 63_072_000;
}
