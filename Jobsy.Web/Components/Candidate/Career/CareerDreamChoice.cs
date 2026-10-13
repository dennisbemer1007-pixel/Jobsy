namespace Jobsy.Web.Components.Candidate.Career;

/// <summary>
/// A picked dream job: either a catalog entry (<see cref="CatalogKey"/>) or sanitized
/// free text (D1). Free text is never sent to the AI as an instruction (01 §3).
/// </summary>
public sealed record CareerDreamChoice(string? CatalogKey, string? FreeText, string Title);
