using System.Text.Json.Serialization;

namespace Jobsy.Core.Careers;

/// <summary>Root document for dagteksten.json (990 texts, 3039 ESCO mappings).</summary>
public sealed class OccupationDayDagtekstenDocument
{
    [JsonPropertyName("versie")]
    public string Versie { get; set; } = "";

    [JsonPropertyName("model")]
    public string Model { get; set; } = "";

    [JsonPropertyName("aantal_teksten")]
    public int AantalTeksten { get; set; }

    [JsonPropertyName("aantal_beroepen")]
    public int AantalBeroepen { get; set; }

    [JsonPropertyName("teksten")]
    public List<OccupationDayDagtekstEntry> Teksten { get; set; } = [];

    [JsonPropertyName("mapping")]
    public List<OccupationDayDagtekstMapping> Mapping { get; set; } = [];
}

public sealed class OccupationDayDagtekstEntry
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public OccupationDayDagtekstLocale? Nl { get; set; }
    public OccupationDayDagtekstLocale? En { get; set; }
}

public sealed class OccupationDayDagtekstLocale
{
    [JsonPropertyName("titel")]
    public string Titel { get; set; } = "";

    [JsonPropertyName("intro")]
    public string Intro { get; set; } = "";

    [JsonPropertyName("blokken")]
    public List<OccupationDayDagtekstBlok> Blokken { get; set; } = [];

    [JsonPropertyName("slot")]
    public string Slot { get; set; } = "";
}

public sealed class OccupationDayDagtekstBlok
{
    [JsonPropertyName("tijd")]
    public string Tijd { get; set; } = "";

    [JsonPropertyName("kop")]
    public string Kop { get; set; } = "";

    [JsonPropertyName("tekst")]
    public string Tekst { get; set; } = "";
}

public sealed class OccupationDayDagtekstMapping
{
    [JsonPropertyName("beroep")]
    public string Beroep { get; set; } = "";

    [JsonPropertyName("esco_id")]
    public string EscoId { get; set; } = "";

    [JsonPropertyName("esco_uri")]
    public string EscoUri { get; set; } = "";

    [JsonPropertyName("tekst_id")]
    public string TekstId { get; set; } = "";

    [JsonPropertyName("tekst_type")]
    public string TekstType { get; set; } = "";
}

public sealed record OccupationDayDagtekstenImportResult(
    int Inserted,
    int Updated,
    int Unchanged,
    int Rejected,
    int Mapped);
