using System.Text.Encodings.Web;
using System.Text.Json;
using Jobsy.Core.Rules;

namespace Jobsy.Web.Seo;

/// <summary>schema.org JSON-LD for public pages (no candidate PII).</summary>
public static class StructuredData
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string WebsiteAndOrganization(string origin)
    {
        var root = origin.TrimEnd('/');
        var payload = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@graph"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["@type"] = "Organization",
                    ["@id"] = root + "/#organization",
                    ["name"] = "Lobsy",
                    ["url"] = root + "/",
                    ["logo"] = root + "/images/brand/lobsy-256.webp",
                    ["description"] =
                        "Hyperlokale banenkaart: vacatures en bijbanen op reistijd en vervoer."
                },
                new Dictionary<string, object?>
                {
                    ["@type"] = "WebSite",
                    ["@id"] = root + "/#website",
                    ["name"] = "Lobsy",
                    ["url"] = root + "/",
                    ["inLanguage"] = "nl-NL",
                    ["publisher"] = new Dictionary<string, object?> { ["@id"] = root + "/#organization" },
                    ["potentialAction"] = new Dictionary<string, object?>
                    {
                        ["@type"] = "SearchAction",
                        ["target"] = root + "/?q={search_term_string}",
                        ["query-input"] = "required name=search_term_string"
                    }
                }
            }
        };

        return Serialize(payload);
    }

    public static string Breadcrumb(string origin, IReadOnlyList<(string Name, string Path)> items)
    {
        var root = origin.TrimEnd('/');
        var payload = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "BreadcrumbList",
            ["itemListElement"] = items
                .Select((item, i) => new Dictionary<string, object?>
                {
                    ["@type"] = "ListItem",
                    ["position"] = i + 1,
                    ["name"] = item.Name,
                    ["item"] = root + (item.Path == "/" ? "/" : item.Path)
                })
                .ToList()
        };
        return Serialize(payload);
    }

    public static string? JobList(string origin, IEnumerable<(Guid Id, string Title, string CompanyName)> jobs, int take = 20)
    {
        var root = origin.TrimEnd('/');
        var items = jobs
            .Take(take)
            .Select((job, i) => new Dictionary<string, object?>
            {
                ["@type"] = "ListItem",
                ["position"] = i + 1,
                ["url"] = root + "/vacancies/" + job.Id.ToString("D"),
                ["name"] = job.Title
            })
            .ToList();

        if (items.Count == 0)
        {
            return null;
        }

        var payload = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "ItemList",
            ["name"] = "Vacatures op de Lobsy-banenkaart",
            ["numberOfItems"] = items.Count,
            ["itemListElement"] = items
        };
        return Serialize(payload);
    }

    public static string? Organization(string origin, string name, string? address, string? logoUrl, string pagePath)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var root = origin.TrimEnd('/');
        var payload = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Organization",
            ["name"] = name.Trim(),
            ["url"] = root + pagePath
        };
        if (!string.IsNullOrWhiteSpace(address))
        {
            payload["address"] = new Dictionary<string, object?>
            {
                ["@type"] = "PostalAddress",
                ["streetAddress"] = address.Trim(),
                ["addressCountry"] = "NL"
            };
        }

        if (!string.IsNullOrWhiteSpace(logoUrl))
        {
            if (logoUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || logoUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                payload["logo"] = logoUrl;
            }
            else if (logoUrl.StartsWith('/'))
            {
                payload["logo"] = root + logoUrl;
            }
        }

        return Serialize(payload);
    }

    public static string? JobPosting(
        Guid id,
        string title,
        string description,
        string companyName,
        string? companyLogoUrl,
        string companyAddress,
        double latitude,
        double longitude,
        DateOnly startDate,
        DateOnly endDate,
        string kind,
        decimal? hourlyWage,
        bool wageVisible,
        decimal? minHoursPerWeek,
        decimal? maxHoursPerWeek,
        string pageUrl,
        string origin)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(companyName))
        {
            return null;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (endDate < today)
        {
            return null;
        }

        var plain = HtmlSanitize.ToPlainPreview(description, 5_000);
        if (string.IsNullOrWhiteSpace(plain))
        {
            plain = title + " bij " + companyName;
        }

        var job = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "JobPosting",
            ["title"] = title.Trim(),
            ["description"] = plain,
            ["identifier"] = new Dictionary<string, object?>
            {
                ["@type"] = "PropertyValue",
                ["name"] = "Lobsy",
                ["value"] = id.ToString("D")
            },
            ["datePosted"] = startDate.ToString("yyyy-MM-dd"),
            ["validThrough"] = endDate.ToDateTime(TimeOnly.MaxValue).ToString("yyyy-MM-ddTHH:mm:ss+00:00"),
            ["hiringOrganization"] = HiringOrganization(origin, companyName, companyLogoUrl),
            ["jobLocation"] = JobLocation(companyAddress, latitude, longitude),
            ["employmentType"] = EmploymentType(kind, minHoursPerWeek, maxHoursPerWeek),
            ["directApply"] = true,
            ["url"] = pageUrl
        };

        if (wageVisible && hourlyWage is > 0)
        {
            job["baseSalary"] = new Dictionary<string, object?>
            {
                ["@type"] = "MonetaryAmount",
                ["currency"] = "EUR",
                ["value"] = new Dictionary<string, object?>
                {
                    ["@type"] = "QuantitativeValue",
                    ["value"] = decimal.Round(hourlyWage.Value, 2),
                    ["unitText"] = "HOUR"
                }
            };
        }

        return Serialize(job);
    }

    private static Dictionary<string, object?> HiringOrganization(string origin, string name, string? logoUrl)
    {
        var org = new Dictionary<string, object?>
        {
            ["@type"] = "Organization",
            ["name"] = name.Trim()
        };
        if (string.IsNullOrWhiteSpace(logoUrl))
        {
            return org;
        }

        if (logoUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || logoUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            org["logo"] = logoUrl;
        }
        else if (logoUrl.StartsWith('/'))
        {
            org["logo"] = origin.TrimEnd('/') + logoUrl;
        }

        return org;
    }

    private static Dictionary<string, object?> JobLocation(string address, double latitude, double longitude)
    {
        var location = new Dictionary<string, object?>
        {
            ["@type"] = "Place",
            ["address"] = new Dictionary<string, object?>
            {
                ["@type"] = "PostalAddress",
                ["streetAddress"] = string.IsNullOrWhiteSpace(address) ? "Nederland" : address.Trim(),
                ["addressCountry"] = "NL"
            }
        };

        if (double.IsFinite(latitude)
            && double.IsFinite(longitude)
            && !(latitude == 0 && longitude == 0)
            && Math.Abs(latitude) <= 90
            && Math.Abs(longitude) <= 180)
        {
            location["geo"] = new Dictionary<string, object?>
            {
                ["@type"] = "GeoCoordinates",
                ["latitude"] = latitude,
                ["longitude"] = longitude
            };
        }

        return location;
    }

    private static string EmploymentType(string kind, decimal? minHours, decimal? maxHours)
    {
        if (string.Equals(kind, "Internship", StringComparison.OrdinalIgnoreCase))
        {
            return "INTERN";
        }

        if (string.Equals(kind, "Volunteer", StringComparison.OrdinalIgnoreCase))
        {
            return "VOLUNTEER";
        }

        var hours = maxHours ?? minHours;
        return hours >= 32 ? "FULL_TIME" : "PART_TIME";
    }

    private static string Serialize(object payload)
        => JsonSerializer.Serialize(payload, JsonOptions)
            .Replace("<", "\\u003c", StringComparison.Ordinal);
}
