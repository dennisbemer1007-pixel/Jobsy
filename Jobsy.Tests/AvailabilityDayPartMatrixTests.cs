using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class AvailabilityDayPartMatrixTests
{
    [Fact]
    public void Matrix_uses_all_seven_weekdays_and_four_day_parts()
    {
        Assert.Equal(["Ma", "Di", "Wo", "Do", "Vr", "Za", "Zo"], DayPartMatrix.DayCodes);
        Assert.Equal(4, DayPartMatrix.DayPartCodes.Length);

        var root = Directory.GetParent(Directory.GetCurrentDirectory())!.Parent!.Parent!.FullName;
        var source = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Shared/AvailabilityDayPartMatrix.razor"));
        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/app.css"));

        Assert.Contains("DayPartMatrix.DayCodes", source, StringComparison.Ordinal);
        Assert.Contains(".availability-matrix__cell.is-on:not(.availability-matrix__cell--btn)", css, StringComparison.Ordinal);
        Assert.DoesNotContain("@(on ? \"✓\" : \"–\")", source, StringComparison.Ordinal);
    }
}
