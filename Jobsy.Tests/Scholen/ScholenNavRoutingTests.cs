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
}
