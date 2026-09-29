namespace Jobsy.Web.Navigation;

/// <summary>
/// School + teacher navigation catalog (§IA). Items with <see cref="ScholenNavItem.IsAvailable"/> false
/// are present for later files but not rendered.
/// </summary>
public static class ScholenNav
{
    public sealed record ScholenNavItem(
        string LabelKey,
        string Href,
        bool IsAvailable,
        string? BadgeKey = null);

    public sealed record ScholenNavGroup(string LabelKey, IReadOnlyList<ScholenNavItem> Items);

    public static readonly IReadOnlyList<ScholenNavGroup> SchoolGroups =
    [
        new("School.Nav.Overview",
        [
            new("School.Nav.Dashboard", "/school", IsAvailable: true),
            new("School.Nav.Todo", "/school/te-doen", IsAvailable: true),
        ]),
        new("School.Nav.Pupils",
        [
            new("School.Nav.Classes", "/school/klassen", IsAvailable: true),
            new("School.Nav.Results", "/school/resultaten", IsAvailable: true),
        ]),
        new("School.Nav.Team",
        [
            new("School.Nav.Teachers", "/school/leraren", IsAvailable: true),
        ]),
        new("School.Nav.School",
        [
            new("School.Nav.Details", "/school/gegevens", IsAvailable: true),
            new("School.Nav.Privacy", "/school/privacy", IsAvailable: true),
            new("School.Nav.Materials", "/school/materiaal", IsAvailable: true),
        ]),
    ];

    public static readonly IReadOnlyList<ScholenNavGroup> TeacherGroups =
    [
        new("Leraar.Nav.MyClass",
        [
            new("Leraar.Nav.Overview", "/leraar", IsAvailable: true),
            new("Leraar.Nav.Codes", "/leraar/codes", IsAvailable: false),
            new("Leraar.Nav.Group", "/leraar/groep", IsAvailable: false),
            new("Leraar.Nav.DreamJobs", "/leraar/droombanen", IsAvailable: false),
        ]),
        new("Leraar.Nav.InClass",
        [
            new("Leraar.Nav.TestWindow", "/leraar/testvenster", IsAvailable: false),
            new("Leraar.Nav.Materials", "/leraar/materiaal", IsAvailable: false),
        ]),
        new("Leraar.Nav.MyClasses", []),
    ];

    public static readonly (string LabelKey, string Href)[] SchoolBottomNav =
    [
        ("School.Nav.Dashboard", "/school"),
        ("School.Nav.Classes", "/school/klassen"),
        ("School.Nav.Results", "/school/resultaten"),
        ("School.Nav.More", "/school"),
    ];

    public static readonly (string LabelKey, string Href)[] TeacherBottomNav =
    [
        ("Leraar.Nav.Overview", "/leraar"),
        ("Leraar.Nav.Codes", "/leraar/codes"),
        ("Leraar.Nav.Group", "/leraar/groep"),
        ("Leraar.Nav.More", "/leraar"),
    ];
}
