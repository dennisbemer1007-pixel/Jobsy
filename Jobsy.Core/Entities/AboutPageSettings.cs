namespace Jobsy.Core.Entities;

/// <summary>
/// Singleton row for the public “Wie zijn wij” page. Unused since public-pages 08: the page is a
/// static text in 5 languages (D10) and the admin editor is gone. The table stays until a cleanup
/// migration drops it (see <c>docs/public-pages-followups.md</c>).
/// </summary>
[Obsolete("Unused since public-pages 08; drop in a cleanup migration")]
public class AboutPageSettings
{
    public Guid Id { get; set; }

    public string Title { get; set; } = "Wie zijn wij";
    public string? Lead { get; set; }
    public string BodyHtml { get; set; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
