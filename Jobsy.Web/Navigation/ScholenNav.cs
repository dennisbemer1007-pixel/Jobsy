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
        string? BadgeKey = null,
        string? FixedLabel = null);

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

    /// <summary>Template teacher groups (classId substituted at render time).</summary>
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

    public static IReadOnlyList<ScholenNavGroup> TeacherGroupsForClass(
        Guid classId,
        IReadOnlyList<(Guid Id, string Name)> assignedClasses)
    {
        var basePath = $"/leraar/klas/{classId:D}";
        var myClass = new ScholenNavGroup("Leraar.Nav.MyClass",
        [
            new("Leraar.Nav.Overview", basePath, IsAvailable: true),
            new("Leraar.Nav.Codes", $"{basePath}/codes", IsAvailable: true),
            new("Leraar.Nav.Group", $"{basePath}/groep", IsAvailable: true),
            new("Leraar.Nav.DreamJobs", $"{basePath}/droombanen", IsAvailable: true),
        ]);
        var inClass = new ScholenNavGroup("Leraar.Nav.InClass",
        [
            new("Leraar.Nav.TestWindow", $"{basePath}/testvenster", IsAvailable: true),
            new("Leraar.Nav.Materials", $"{basePath}/materiaal", IsAvailable: true),
        ]);
        var myClasses = new ScholenNavGroup(
            "Leraar.Nav.MyClasses",
            assignedClasses
                .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .Select(c => new ScholenNavItem(
                    "Leraar.Nav.ClassItem",
                    $"/leraar/klas/{c.Id:D}",
                    IsAvailable: true,
                    FixedLabel: $"Klas {c.Name}"))
                .ToList());
        return [myClass, inClass, myClasses];
    }

    public static readonly (string LabelKey, string Href)[] SchoolBottomNav =
    [
        ("School.Nav.Dashboard", "/school"),
        ("School.Nav.Classes", "/school/klassen"),
        ("School.Nav.Results", "/school/resultaten"),
        ("School.Nav.More", "/school"),
    ];

    public static (string LabelKey, string Href)[] TeacherBottomNavForClass(Guid classId) =>
    [
        ("Leraar.Nav.Overview", $"/leraar/klas/{classId:D}"),
        ("Leraar.Nav.Codes", $"/leraar/klas/{classId:D}/codes"),
        ("Leraar.Nav.Group", $"/leraar/klas/{classId:D}/groep"),
        ("Leraar.Nav.More", $"/leraar/klas/{classId:D}/materiaal"),
    ];

    public static readonly (string LabelKey, string Href)[] TeacherBottomNav =
    [
        ("Leraar.Nav.Overview", "/leraar"),
        ("Leraar.Nav.Codes", "/leraar"),
        ("Leraar.Nav.Group", "/leraar"),
        ("Leraar.Nav.More", "/leraar"),
    ];
}
