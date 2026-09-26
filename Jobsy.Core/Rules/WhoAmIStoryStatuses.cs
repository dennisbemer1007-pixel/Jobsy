namespace Jobsy.Core.Rules;

/// <summary>Read-only WhoAmI story status on GET api/me/kompas (never triggers AI).</summary>
public static class WhoAmIStoryStatuses
{
    public const string Ready = "Ready";
    public const string Updating = "Updating";
    public const string Empty = "Empty";
}
