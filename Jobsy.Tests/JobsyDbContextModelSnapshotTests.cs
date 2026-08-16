using Jobsy.Infrastructure.Data.Migrations;

namespace Jobsy.Tests;

public class JobsyDbContextModelSnapshotTests
{
    [Fact]
    public void Model_snapshot_builds_without_throwing()
    {
        var snapshot = new JobsyDbContextModelSnapshot();
        Assert.NotNull(snapshot.Model);
        Assert.NotNull(snapshot.Model.FindEntityType(typeof(Jobsy.Core.Entities.TokenPurchaseCheckout)));
        Assert.NotNull(snapshot.Model.FindEntityType(typeof(Jobsy.Core.Entities.TokenPurchaseInvoice)));
        Assert.NotNull(snapshot.Model.FindEntityType(typeof(Jobsy.Core.Entities.PlatformFeedback)));
    }
}
