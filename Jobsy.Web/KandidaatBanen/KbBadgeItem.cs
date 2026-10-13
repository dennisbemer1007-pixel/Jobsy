namespace Jobsy.Web.KandidaatBanen;

/// <summary>One badge in a <c>KbBadgeRow</c> (fit pill counts toward MaxVisible).</summary>
public sealed record KbBadgeItem(string Key, string Label, string? CssModifier = null, bool IsFitPill = false);
