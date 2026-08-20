namespace Jobsy.Web.Seo;

/// <summary>Resolved head tags for the current URL (catalog + optional page override).</summary>
public sealed record PageSeoModel(
    string Title,
    string Description,
    string CanonicalUrl,
    string Robots,
    bool Indexable,
    string OgType,
    string? ImageUrl,
    string? JsonLd);
