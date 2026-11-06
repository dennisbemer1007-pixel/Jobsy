namespace Jobsy.Core.Options;

/// <summary>
/// Contact address for the Nominatim User-Agent. Env: <c>Geo__ContactEmail</c>.
/// </summary>
public sealed class GeoOptions
{
    public const string SectionName = "Geo";

    public const string DefaultContactEmail = "info@lobsy.nl";

    public string ContactEmail { get; set; } = DefaultContactEmail;

    public static string UserAgent(string? contactEmail)
    {
        var email = string.IsNullOrWhiteSpace(contactEmail) ? DefaultContactEmail : contactEmail.Trim();
        return $"Lobsy/1.0 ({email})";
    }
}
