namespace Jobsy.Infrastructure.Ops;

/// <summary>
/// AsyncLocal scope set by API middleware (test principal) and the CLI seed/cleanup command.
/// When active, outbound mail to non-test recipients is dropped.
/// </summary>
public static class TestAccountScope
{
    private static readonly AsyncLocal<bool> Active = new();

    public static bool IsActive => Active.Value;

    public static IDisposable Enter()
    {
        var previous = Active.Value;
        Active.Value = true;
        return new Pop(previous);
    }

    private sealed class Pop(bool previous) : IDisposable
    {
        public void Dispose() => Active.Value = previous;
    }
}
