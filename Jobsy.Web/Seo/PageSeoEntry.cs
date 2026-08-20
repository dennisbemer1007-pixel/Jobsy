namespace Jobsy.Web.Seo;

/// <summary>Static SEO defaults for a route (title/description keys + indexability).</summary>
public sealed record PageSeoEntry(
    string TitleKey,
    string DescriptionKey,
    bool Indexable,
    string OgType = "website");
