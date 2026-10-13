namespace Jobsy.Core.Ops;

public sealed class TestAccountsRuntimeState : ITestAccountsRuntime
{
    public bool IsActive { get; }
    public string StatusCodes { get; }

    public TestAccountsRuntimeState(TestAccountGuardResult result)
    {
        IsActive = result.Allowed;
        StatusCodes = result.Allowed ? "ok" : result.CodesSummary();
    }

    public static TestAccountsRuntimeState FromInput(TestAccountGuardInput input)
        => new(TestAccountEnvironmentGuard.Evaluate(input));
}
