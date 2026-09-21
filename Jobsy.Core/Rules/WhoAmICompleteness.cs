using System.Text.Json;
using Jobsy.Core.Contracts;

namespace Jobsy.Core.Rules;

/// <summary>
/// Four-step gate for the "Wie ben ik?" report: profile, competence, careers, DISC Quick-Scan or deep.
/// </summary>
public static class WhoAmICompleteness
{
    public static bool IsProfileFilled(string? fullName, CandidatePreferencesDto? prefs)
        => IsProfileFilled(fullName, null, null, prefs);

    /// <summary>
    /// Name may live on <paramref name="fullName"/> or on the separate first/last fields.
    /// Background is the CV section: about-me, motivation, employers, education, certificates,
    /// an uploaded CV file, or a reference.
    /// </summary>
    public static bool IsProfileFilled(
        string? fullName,
        string? firstName,
        string? lastName,
        CandidatePreferencesDto? prefs,
        bool hasUploadedCv = false,
        bool hasReferences = false)
    {
        if (string.IsNullOrWhiteSpace(CandidateNameRules.ComposeFullName(firstName, lastName, fullName)))
        {
            return false;
        }

        if (prefs is null
            || prefs.MaxTravelMinutes is not > 0
            || string.IsNullOrWhiteSpace(prefs.PreferredTransport))
        {
            return false;
        }

        return HasBackground(prefs) || hasUploadedCv || hasReferences;
    }

    /// <summary>
    /// Reads stored preference JSON field-by-field. A strict record deserialize drops the whole
    /// document when one value has an unexpected shape (for example an availability slot stored
    /// as text), which kept a filled profile looking empty on Wie ben ik.
    /// </summary>
    public static bool IsProfileFilled(
        string? fullName,
        string? firstName,
        string? lastName,
        string? preferencesJson,
        bool hasUploadedCv = false,
        bool hasReferences = false)
    {
        if (string.IsNullOrWhiteSpace(CandidateNameRules.ComposeFullName(firstName, lastName, fullName)))
        {
            return false;
        }

        if (!TryReadSignals(preferencesJson, out var travelMinutes, out var transport, out var background))
        {
            return false;
        }

        if (travelMinutes is not > 0 || string.IsNullOrWhiteSpace(transport))
        {
            return false;
        }

        return background || hasUploadedCv || hasReferences;
    }

    public static bool HasBackground(CandidatePreferencesDto prefs)
        => !string.IsNullOrWhiteSpace(prefs.AboutMe)
           || !string.IsNullOrWhiteSpace(prefs.DefaultMotivation)
           || prefs.Employers is { Count: > 0 }
           || prefs.Educations is { Count: > 0 }
           || prefs.Certificates is { Count: > 0 };

    private static bool TryReadSignals(
        string? json,
        out int? travelMinutes,
        out string? transport,
        out bool background)
    {
        travelMinutes = null;
        transport = null;
        background = false;
        if (string.IsNullOrWhiteSpace(json))
        {
            return true;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var root = doc.RootElement;
            if (TryProperty(root, "maxTravelMinutes", out var travelEl) && travelEl.ValueKind == JsonValueKind.Number)
            {
                if (travelEl.TryGetInt32(out var travel))
                {
                    travelMinutes = travel;
                }
                else if (travelEl.TryGetDecimal(out var travelDecimal) && travelDecimal is > 0 and <= 180)
                {
                    travelMinutes = (int)travelDecimal;
                }
            }

            if (TryProperty(root, "preferredTransport", out var transportEl) && transportEl.ValueKind == JsonValueKind.String)
            {
                transport = transportEl.GetString();
            }

            background = HasText(root, "aboutMe")
                || HasText(root, "defaultMotivation")
                || HasNamedObjects(root, "employers", "employerName")
                || HasNonEmptyStrings(root, "educations")
                || HasNamedObjects(root, "certificates", "name");
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool HasText(JsonElement root, string name)
        => TryProperty(root, name, out var element)
           && element.ValueKind == JsonValueKind.String
           && !string.IsNullOrWhiteSpace(element.GetString());

    private static bool HasNonEmptyStrings(JsonElement root, string name)
    {
        if (!TryProperty(root, name, out var element) || element.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasNamedObjects(JsonElement root, string arrayName, string nameProperty)
    {
        if (!TryProperty(root, arrayName, out var element) || element.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (TryProperty(item, nameProperty, out var nameEl)
                && nameEl.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(nameEl.GetString()))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryProperty(JsonElement root, string name, out JsonElement value)
    {
        if (root.TryGetProperty(name, out value))
        {
            return true;
        }

        foreach (var property in root.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    public static bool IsUnlocked(
        bool profileFilled,
        bool competencyCompleted,
        bool careerCompleted,
        bool discCompleted)
        => profileFilled && competencyCompleted && careerCompleted && discCompleted;

    public static string Fingerprint(CompetencyScores competency, RiasecScores career, DiscScores disc)
        => string.Join('|',
            competency.Samenwerken,
            competency.Resultaatgerichtheid,
            competency.Stressbestendigheid,
            competency.Innovatie,
            competency.Extraversie,
            career.Realistic,
            career.Investigative,
            career.Artistic,
            career.Social,
            career.Enterprising,
            career.Conventional,
            disc.Dominant,
            disc.Invloed,
            disc.Stabiel,
            disc.Nauwkeurig);
}
