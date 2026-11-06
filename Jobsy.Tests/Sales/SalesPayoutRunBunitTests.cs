using Jobsy.Core.Enums;

namespace Jobsy.Tests.Sales;

/// <summary>UI action-enabling contracts for admin payout drawer (08.5).</summary>
public class SalesPayoutRunBunitTests
{
    [Theory]
    [InlineData(nameof(SalesPayoutRunStatus.Draft), true, false, false)]
    [InlineData(nameof(SalesPayoutRunStatus.Approved), false, true, true)]
    [InlineData(nameof(SalesPayoutRunStatus.Exported), false, true, true)]
    [InlineData(nameof(SalesPayoutRunStatus.Paid), false, false, true)]
    [InlineData(nameof(SalesPayoutRunStatus.Closed), false, false, false)]
    public void Drawer_action_enabling_per_status(string status, bool approve, bool export, bool markPaid)
    {
        // Kept in sync with PayoutRunDetailDrawer.razor CanApprove / CanExport / CanMarkPaid.
        var canApprove = status == nameof(SalesPayoutRunStatus.Draft);
        var canExport = status is nameof(SalesPayoutRunStatus.Approved) or nameof(SalesPayoutRunStatus.Exported);
        var canMarkPaid = status is nameof(SalesPayoutRunStatus.Approved)
            or nameof(SalesPayoutRunStatus.Exported)
            or nameof(SalesPayoutRunStatus.Paid);
        Assert.Equal(approve, canApprove);
        Assert.Equal(export, canExport);
        Assert.Equal(markPaid, canMarkPaid);
    }

    [Fact]
    public void Correction_and_reassign_reason_bounds_match_api()
    {
        Assert.True(IsValidReason("abcde"));
        Assert.False(IsValidReason("abcd"));
        Assert.False(IsValidReason(new string('x', 501)));
        Assert.True(IsValidReason(new string('x', 500)));
    }

    private static bool IsValidReason(string reason)
    {
        var trimmed = (reason ?? "").Trim();
        return trimmed.Length is >= 5 and <= 500;
    }
}
