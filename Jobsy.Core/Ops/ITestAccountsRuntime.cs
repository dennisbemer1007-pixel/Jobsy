namespace Jobsy.Core.Ops;

/// <summary>
/// Process-wide result of the acceptatie test-accounts guard (evaluated once at startup).
/// MFA exemption and claim issue only apply when <see cref="IsActive"/> is true.
/// </summary>
public interface ITestAccountsRuntime
{
    bool IsActive { get; }
    string StatusCodes { get; }
}
