using Microsoft.AspNetCore.Components;

namespace Jobsy.Web.Components.Legal;

/// <summary>
/// One numbered section of a legal document. <see cref="Body"/> is the official Dutch text (D3);
/// <see cref="SummaryKey"/> is the "In het kort" block in the reader's language and may be null
/// while a section has no summary yet.
/// </summary>
public sealed class LegalSection
{
    public required string Id { get; init; }

    /// <summary>Visible number, e.g. "1" or "5a". Empty renders the title without a number.</summary>
    public string Number { get; init; } = "";

    public required string TitleKey { get; init; }

    public string? SummaryKey { get; init; }

    public required RenderFragment Body { get; init; }
}
