using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Contracts;

namespace Jobsy.Core.Rules;

/// <summary>Vacature werkcontext (module E spiegel) — V1–V4 from doc §3.3.</summary>
public sealed record VacancyWorkContextSnapshot(
    string? Pace = null,
    string? Physical = null,
    string? IndoorOutdoor = null,
    string? CustomerContact = null);

/// <summary>Explicit competency targets (V5–V8) when employer overrides infer.</summary>
public sealed record VacancyCompetencyTargetsSnapshot(
    int? Samenwerken = null,
    int? Resultaatgerichtheid = null,
    int? Stressbestendigheid = null,
    int? Innovatie = null);

/// <summary>Delta on culture dimensions when team differs from company (V9).</summary>
public sealed record VacancyCultureDeltaSnapshot(
    bool DiffersFromCompany = false,
    IReadOnlyDictionary<string, int>? DimensionPercents = null);

public sealed record VacancyMatchExtrasSnapshot(
    VacancyWorkContextSnapshot? WorkContext = null,
    VacancyCompetencyTargetsSnapshot? CompetencyTargets = null,
    VacancyCultureDeltaSnapshot? CultureDelta = null,
    bool UseInferredCompetencies = true)
{
    public static VacancyMatchExtrasSnapshot Empty { get; } = new();
}

public static class VacancyMatchExtrasJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    public static VacancyMatchExtrasSnapshot? Parse(
        string? workContextJson,
        string? competencyTargetsJson,
        string? cultureDeltaJson,
        bool useInferredCompetencies)
    {
        VacancyWorkContextSnapshot? work = null;
        VacancyCompetencyTargetsSnapshot? comp = null;
        VacancyCultureDeltaSnapshot? culture = null;

        if (!string.IsNullOrWhiteSpace(workContextJson))
        {
            work = JsonSerializer.Deserialize<VacancyWorkContextSnapshot>(workContextJson, Options);
            work = NormalizeWork(work);
        }

        if (!string.IsNullOrWhiteSpace(competencyTargetsJson))
        {
            comp = JsonSerializer.Deserialize<VacancyCompetencyTargetsSnapshot>(competencyTargetsJson, Options);
            comp = NormalizeCompetencies(comp);
        }

        if (!string.IsNullOrWhiteSpace(cultureDeltaJson))
        {
            culture = JsonSerializer.Deserialize<VacancyCultureDeltaSnapshot>(cultureDeltaJson, Options);
        }

        if (work is null && comp is null && culture is null && useInferredCompetencies)
        {
            return null;
        }

        return new VacancyMatchExtrasSnapshot(work, comp, culture, useInferredCompetencies);
    }

    public static string? SerializeWork(VacancyWorkContextSnapshot? work)
        => work is null ? null : JsonSerializer.Serialize(NormalizeWork(work), Options);

    public static string? SerializeCompetencies(VacancyCompetencyTargetsSnapshot? targets)
        => targets is null ? null : JsonSerializer.Serialize(NormalizeCompetencies(targets), Options);

    public static string? SerializeCultureDelta(VacancyCultureDeltaSnapshot? delta)
        => delta is null ? null : JsonSerializer.Serialize(delta, Options);

    private static VacancyWorkContextSnapshot? NormalizeWork(VacancyWorkContextSnapshot? raw)
    {
        if (raw is null)
        {
            return null;
        }

        return new VacancyWorkContextSnapshot(
            WorkPreferenceCatalogs.CanonicalVacancyPace(raw.Pace),
            WorkPreferenceCatalogs.CanonicalPhysicalWork(raw.Physical),
            WorkPreferenceCatalogs.CanonicalIndoorOutdoor(raw.IndoorOutdoor),
            WorkPreferenceCatalogs.CanonicalCustomerContact(raw.CustomerContact));
    }

    private static VacancyCompetencyTargetsSnapshot? NormalizeCompetencies(VacancyCompetencyTargetsSnapshot? raw)
    {
        if (raw is null)
        {
            return null;
        }

        static int? Clamp(int? v) => v is null ? null : Math.Clamp(v.Value, 0, 100);

        return new VacancyCompetencyTargetsSnapshot(
            Clamp(raw.Samenwerken),
            Clamp(raw.Resultaatgerichtheid),
            Clamp(raw.Stressbestendigheid),
            Clamp(raw.Innovatie));
    }
}

/// <summary>Candidate module E — extends shareable work preferences.</summary>
public static class CandidateWorkContextProfile
{
    public static CandidateWorkContextSnapshot FromPreferences(SharedWorkPreferences? prefs)
    {
        if (prefs is null)
        {
            return CandidateWorkContextSnapshot.Empty;
        }

        return new CandidateWorkContextSnapshot(
            WorkPreferenceCatalogs.CanonicalIndoor(prefs.Indoor),
            WorkPreferenceCatalogs.CanonicalOutdoor(prefs.Outdoor),
            WorkPreferenceCatalogs.CanonicalPhysicalWork(prefs.PhysicalWork),
            WorkPreferenceCatalogs.CanonicalPace(prefs.Pace),
            WorkPreferenceCatalogs.CanonicalTeamMode(prefs.TeamMode),
            WorkPreferenceCatalogs.CanonicalStructure(prefs.Structure),
            WorkPreferenceCatalogs.CanonicalCustomerContact(prefs.CustomerContact),
            WorkPreferenceCatalogs.CanonicalShiftsPreference(prefs.ShiftsPreference));
    }

    public static bool HasAny(CandidateWorkContextSnapshot ctx)
        => ctx.Indoor is not null
           || ctx.Outdoor is not null
           || ctx.PhysicalWork is not null
           || ctx.Pace is not null
           || ctx.TeamMode is not null
           || ctx.Structure is not null
           || ctx.CustomerContact is not null
           || ctx.ShiftsPreference is not null;
}

public sealed record CandidateWorkContextSnapshot(
    string? Indoor = null,
    string? Outdoor = null,
    string? PhysicalWork = null,
    string? Pace = null,
    string? TeamMode = null,
    string? Structure = null,
    string? CustomerContact = null,
    string? ShiftsPreference = null)
{
    public static CandidateWorkContextSnapshot Empty { get; } = new();
}
