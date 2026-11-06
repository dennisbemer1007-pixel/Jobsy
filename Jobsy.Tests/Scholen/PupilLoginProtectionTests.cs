using Jobsy.Core.Scholen;

namespace Jobsy.Tests.Scholen;

public class PupilLoginProtectionTests
{
    [Fact]
    public void Ten_failures_trigger_cooldown_for_class_partition()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var protection = new PupilLoginProtection(clock);
        var partition = protection.ClientPartition("1.2.3.4", "Mozilla/5.0 Chrome/120");
        var classId = Guid.NewGuid();

        for (var i = 0; i < PupilLoginProtection.ClassPartitionFailLimit; i++)
        {
            Assert.True(protection.TryAcquireClassPartition(classId, partition));
            protection.RecordFailure(classId, partition);
        }

        Assert.False(protection.TryAcquireClassPartition(classId, partition));
        Assert.True(protection.IsClassPartitionCoolingDown(classId, partition, out var remaining));
        Assert.True(remaining > TimeSpan.Zero);
    }

    [Fact]
    public void Client_partition_is_stable_and_not_raw_ip()
    {
        var protection = new PupilLoginProtection();
        var a = protection.ClientPartition("10.0.0.1", "Chrome/1");
        var b = protection.ClientPartition("10.0.0.1", "Chrome/1");
        var c = protection.ClientPartition("10.0.0.2", "Chrome/1");
        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
        Assert.DoesNotContain("10.0.0.1", a, StringComparison.Ordinal);
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
