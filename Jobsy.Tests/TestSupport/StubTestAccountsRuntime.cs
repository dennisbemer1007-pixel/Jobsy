using Jobsy.Core.Ops;

namespace Jobsy.Tests.TestSupport;

public sealed class StubTestAccountsRuntime : ITestAccountsRuntime
{
    public StubTestAccountsRuntime(bool isActive = false) => IsActive = isActive;

    public bool IsActive { get; }
    public string StatusCodes => IsActive ? "ok" : "switch_off";
}
