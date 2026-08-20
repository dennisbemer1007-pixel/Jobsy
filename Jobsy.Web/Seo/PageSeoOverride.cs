namespace Jobsy.Web.Seo;

/// <summary>Per-page overlay on <see cref="PageSeoCatalog"/> (vacancy, vestiging, about, …).</summary>
public sealed record PageSeoOverride(
    string? Title = null,
    string? Description = null,
    bool? Indexable = null,
    string? CanonicalPath = null,
    string? ImageUrl = null,
    string? OgType = null,
    string? JsonLd = null);
