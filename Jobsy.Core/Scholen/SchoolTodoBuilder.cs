using Jobsy.Core.Enums;

namespace Jobsy.Core.Scholen;

public enum SchoolTodoKind
{
    ParentalConfirmationMissing = 0,
    ClassWithoutTeacher = 1,
    TeacherInvitePending = 2,
    WindowClosingLowCompletion = 3,
    PupilLoginPaused = 4,
    RetentionCutoffSoon = 5
}

public sealed record SchoolTodoItem(
    SchoolTodoKind Kind,
    string Title,
    string? Href,
    Guid? ClassId = null,
    Guid? TeacherUserId = null,
    DateOnly? DueOn = null);

public sealed record SchoolTodoClassInput(
    Guid ClassId,
    string ClassName,
    bool HasTeacher,
    bool ParentalConfirmed,
    TestWindowState TestWindow,
    DateOnly? TestWindowClosesOn,
    int CodeCount,
    int CompletedCount,
    DateTime? LoginPausedUntilUtc);

public sealed record SchoolTodoInviteInput(
    Guid InviteId,
    Guid? UserId,
    string TeacherDisplayName,
    DateTime CreatedAtUtc);

public sealed record SchoolTodoInput(
    DateTime UtcNow,
    DateOnly TodayAmsterdam,
    DateOnly? RetentionCutoff,
    IReadOnlyList<SchoolTodoClassInput> Classes,
    IReadOnlyList<SchoolTodoInviteInput> PendingInvitesOlderThan3Days);

/// <summary>Builds the school "Te doen" list from class/invite/retention state (pure).</summary>
public static class SchoolTodoBuilder
{
    public static IReadOnlyList<SchoolTodoItem> Build(SchoolTodoInput input, int maxItems = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(input);
        var items = new List<SchoolTodoItem>();

        foreach (var c in input.Classes)
        {
            if (!c.ParentalConfirmed)
            {
                items.Add(new SchoolTodoItem(
                    SchoolTodoKind.ParentalConfirmationMissing,
                    $"Ouders van klas {c.ClassName} nog niet bevestigd.",
                    $"/school/klassen/{c.ClassId}",
                    ClassId: c.ClassId));
            }

            if (!c.HasTeacher)
            {
                items.Add(new SchoolTodoItem(
                    SchoolTodoKind.ClassWithoutTeacher,
                    $"Klas {c.ClassName} heeft nog geen leraar.",
                    $"/school/klassen/{c.ClassId}",
                    ClassId: c.ClassId));
            }

            if (c.LoginPausedUntilUtc is DateTime paused && paused > input.UtcNow)
            {
                items.Add(new SchoolTodoItem(
                    SchoolTodoKind.PupilLoginPaused,
                    $"Inloggen voor klas {c.ClassName} is tijdelijk gepauzeerd.",
                    $"/school/klassen/{c.ClassId}",
                    ClassId: c.ClassId));
            }

            if (c.TestWindow == TestWindowState.Open
                && c.TestWindowClosesOn is DateOnly closesOn)
            {
                var daysLeft = closesOn.DayNumber - input.TodayAmsterdam.DayNumber;
                var pct = c.CodeCount <= 0 ? 100d : 100d * c.CompletedCount / c.CodeCount;
                if (daysLeft is >= 0 and <= 3 && pct < 80d)
                {
                    items.Add(new SchoolTodoItem(
                        SchoolTodoKind.WindowClosingLowCompletion,
                        $"Klas {c.ClassName}: testvenster sluit binnen {daysLeft} dag(en), {pct:0}% afgerond.",
                        $"/school/klassen/{c.ClassId}",
                        ClassId: c.ClassId,
                        DueOn: closesOn));
                }
            }
        }

        foreach (var invite in input.PendingInvitesOlderThan3Days)
        {
            items.Add(new SchoolTodoItem(
                SchoolTodoKind.TeacherInvitePending,
                $"Uitnodiging voor {invite.TeacherDisplayName} openstaat langer dan 3 dagen.",
                "/school/leraren",
                TeacherUserId: invite.UserId));
        }

        if (input.RetentionCutoff is DateOnly cutoff)
        {
            var days = cutoff.DayNumber - input.TodayAmsterdam.DayNumber;
            if (days is >= 0 and <= 30)
            {
                var yearLabel = SchoolYear.Label(cutoff.Year - 1);
                items.Add(new SchoolTodoItem(
                    SchoolTodoKind.RetentionCutoffSoon,
                    $"Op {cutoff:d MMMM yyyy} worden de leerlinggegevens van {yearLabel} verwijderd. Download wat je nodig hebt.",
                    "/school/privacy",
                    DueOn: cutoff));
            }
        }

        if (maxItems < items.Count)
        {
            return items.Take(maxItems).ToList();
        }

        return items;
    }
}
