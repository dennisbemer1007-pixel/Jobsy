using Jobsy.Infrastructure.Ops;

namespace Jobsy.Tests.TestAccounts;

public sealed class AcceptatieSollicitatiesDemoStartupStateTests
{
    [Fact]
    public void RecordError_includes_type_and_truncates_without_email()
    {
        var ex = new InvalidOperationException(
            "The instance of entity type 'Company' cannot be tracked. Contact user@example.com for help.");
        AcceptatieSollicitatiesDemoStartupState.RecordError(ex);
        var snap = AcceptatieSollicitatiesDemoStartupState.Snapshot();
        Assert.NotNull(snap.Outcome);
        Assert.StartsWith("error:InvalidOperationException:", snap.Outcome, StringComparison.Ordinal);
        Assert.Contains("[email]", snap.Outcome, StringComparison.Ordinal);
        Assert.DoesNotContain("user@example.com", snap.Outcome, StringComparison.Ordinal);
        Assert.True(snap.Outcome!.Length <= 200);
    }
}
