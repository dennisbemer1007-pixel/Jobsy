namespace Jobsy.Core.Reminders;

/// <summary>Stored on <c>ComebackReminderLog.Kind</c>. Not a score.</summary>
public static class ComebackReminderKinds
{
    public const string BasicTests = "BasicTests";
    public const string LookAgain = "LookAgain";

    public static bool IsKnown(string? kind)
        => string.Equals(kind, BasicTests, StringComparison.Ordinal)
           || string.Equals(kind, LookAgain, StringComparison.Ordinal);
}
