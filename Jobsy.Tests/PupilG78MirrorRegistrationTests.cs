namespace Jobsy.Tests;

public class PupilG78MirrorRegistrationTests
{
    [Fact]
    public void PupilG78MirrorService_is_not_registered()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "Jobsy.Infrastructure",
            "DependencyInjection.cs"));
        var text = File.ReadAllText(path);
        Assert.DoesNotContain("IPupilG78MirrorRenderer", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PupilG78MirrorService", text, StringComparison.Ordinal);
    }
}