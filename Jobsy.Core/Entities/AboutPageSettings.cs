namespace Jobsy.Core.Entities;

/// <summary>
/// Singleton row for the public “Wie zijn wij” page (admin-editable).
/// </summary>
public class AboutPageSettings
{
    public Guid Id { get; set; }

    public string Title { get; set; } = "Wie zijn wij";
    public string? Lead { get; set; }
    public string BodyHtml { get; set; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
