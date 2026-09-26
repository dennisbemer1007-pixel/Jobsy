namespace Jobsy.Core.Entities;

/// <summary>Stored translation of vacancy title/description for one language.</summary>
public class VacancyTranslation
{
    public Guid Id { get; set; }
    public Guid VacancyId { get; set; }
    public Vacancy Vacancy { get; set; } = null!;

    /// <summary>Normalized language code (e.g. en, pl).</summary>
    public string Language { get; set; } = "";

    /// <summary>Hash of Dutch source title + description used to detect staleness.</summary>
    public string SourceHash { get; set; } = "";

    /// <summary>JSON {"title":"...","description":"..."}.</summary>
    public string TranslatedJson { get; set; } = "{}";
    public DateTime UpdatedAtUtc { get; set; }
}
