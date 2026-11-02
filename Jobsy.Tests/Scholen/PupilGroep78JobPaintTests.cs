using Jobsy.Web.Scholen;

namespace Jobsy.Tests.Scholen;

public sealed class PupilGroep78JobPaintTests
{
    [Fact]
    public void Fotograaf_and_grafisch_ontwerper_use_distinct_job_paintings()
    {
        var fotograaf = PupilGroep78JobPaint.PaintImagePath("fotograaf");
        var grafisch = PupilGroep78JobPaint.PaintImagePath("grafisch-ontwerper");
        Assert.NotEqual(fotograaf, grafisch);
        Assert.Contains("creatief", fotograaf, StringComparison.Ordinal);
        Assert.Contains("creatief", grafisch, StringComparison.Ordinal);
        Assert.EndsWith("-hero.jpg", fotograaf, StringComparison.Ordinal);
        Assert.DoesNotContain("-hero", grafisch, StringComparison.Ordinal);
    }
}
