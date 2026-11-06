namespace Jobsy.Tests.Scholen;

public sealed class ScholenNavRoutingTests
{
    [Theory]
    [InlineData("/leraar/klas/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/codes", "/leraar", false)]
    [InlineData("/leraar/klas/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", "/leraar", false)]
    [InlineData("/leraar", "/leraar", true)]
    [InlineData("/school/klassen", "/school", false)]
    [InlineData("/school", "/school", true)]
    [InlineData("/leraar/klas/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/codes", "/leraar/klas/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", false)]
    [InlineData("/leraar/klas/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", "/leraar/klas/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", true)]
    [InlineData("/leraar/klas/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/codes", "/leraar/klas/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/codes", true)]
    public void IsNavActive_respects_root_and_class_overview_rules(string path, string href, bool expected)
    {
        var actual = Jobsy.Web.Navigation.ScholenNavRouting.IsNavActive(path, href);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("/school", "School.Nav.Dashboard", true)]
    [InlineData("/school", "School.Nav.More", false)]
    [InlineData("/school/te-doen", "School.Nav.Dashboard", false)]
    [InlineData("/school/te-doen", "School.Nav.More", true)]
    [InlineData("/school/leraren", "School.Nav.More", true)]
    [InlineData("/school/klassen", "School.Nav.Classes", true)]
    [InlineData("/school/klassen", "School.Nav.ClassesBottom", true)]
    public void IsSchoolBottomNavActive_separates_dashboard_and_meer(string path, string labelKey, bool expected)
    {
        var href = labelKey switch
        {
            "School.Nav.Dashboard" => "/school",
            "School.Nav.More" => "/school",
            "School.Nav.Classes" => "/school/klassen",
            "School.Nav.ClassesBottom" => "/school/klassen",
            _ => "/school"
        };
        var actual = Jobsy.Web.Navigation.ScholenNavRouting.IsSchoolBottomNavActive(path, labelKey, href);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("/leraar/klas/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/droombanen", "Leraar.Nav.More", true)]
    [InlineData("/leraar/klas/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/codes", "Leraar.Nav.More", false)]
    [InlineData("/leraar/klas/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/codes", "Leraar.Nav.Codes", true)]
    public void IsTeacherBottomNavActive_separates_meer_overflow_routes(string path, string labelKey, bool expected)
    {
        var classId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var href = labelKey switch
        {
            "Leraar.Nav.More" => $"/leraar/klas/{classId:D}/materiaal",
            "Leraar.Nav.Codes" => $"/leraar/klas/{classId:D}/codes",
            _ => $"/leraar/klas/{classId:D}"
        };
        var actual = Jobsy.Web.Navigation.ScholenNavRouting.IsTeacherBottomNavActive(path, labelKey, href, classId);
        Assert.Equal(expected, actual);
    }
}
