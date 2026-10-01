namespace Jobsy.Core.Legal;

public enum ProcessorStatus
{
    /// <summary>Live today.</summary>
    Active,

    /// <summary>Contracted or planned, not processing personal data yet.</summary>
    Planned
}

/// <summary>
/// One processor / third party row of the privacy statement (03.3 fills the list).
/// <paramref name="TransferBasisKey"/> is only set for transfers outside the EEA (DPF / SCC).
/// </summary>
public sealed record LegalProcessor(
    string Id,
    string Name,
    string Region,
    string PurposeKey,
    string DataKey,
    ProcessorStatus Status,
    string? TransferBasisKey)
{
    /// <summary>Row-specific note for a planned row; falls back to the generic "not live yet" note.</summary>
    public string? PlannedNoteKey { get; init; }
}

/// <summary>
/// The processor catalog behind the privacy statement table. Rows live in code, never in copy,
/// so the page and reality cannot drift apart.
/// </summary>
public static class LegalProcessors
{
    /// <summary>Transfer basis keys (03.4). A row inside the EEA keeps <see cref="InsideEu"/>.</summary>
    public const string InsideEu = "Legal.Transfer.Eu";
    public const string DataPrivacyFramework = "Legal.Transfer.Dpf";
    public const string StandardClauses = "Legal.Transfer.Scc";
    public const string AdequacyDecision = "Legal.Transfer.Adequacy";

    public static readonly IReadOnlyList<LegalProcessor> All =
    [
        new(
            "render",
            "Render",
            "EU (Frankfurt); Render zelf is een Amerikaans bedrijf",
            "Legal.Processor.render.Purpose",
            "Legal.Processor.render.Data",
            ProcessorStatus.Active,
            StandardClauses),
        new(
            "cloudflare",
            "Cloudflare",
            "EU en Verenigde Staten",
            "Legal.Processor.cloudflare.Purpose",
            "Legal.Processor.cloudflare.Data",
            ProcessorStatus.Active,
            DataPrivacyFramework),
        new(
            "resend",
            "Resend",
            "Verenigde Staten",
            "Legal.Processor.resend.Purpose",
            "Legal.Processor.resend.Data",
            ProcessorStatus.Active,
            DataPrivacyFramework),
        new(
            "sentry",
            "Sentry",
            "EU of Verenigde Staten, afhankelijk van onze instelling",
            "Legal.Processor.sentry.Purpose",
            "Legal.Processor.sentry.Data",
            ProcessorStatus.Active,
            DataPrivacyFramework),
        new(
            "mollie",
            "Mollie",
            "Nederland",
            "Legal.Processor.mollie.Purpose",
            "Legal.Processor.mollie.Data",
            ProcessorStatus.Active,
            InsideEu),
        new(
            "pingen",
            "Pingen",
            "Zwitserland",
            "Legal.Processor.pingen.Purpose",
            "Legal.Processor.pingen.Data",
            ProcessorStatus.Active,
            AdequacyDecision)
        {
            PlannedNoteKey = "Legal.Processor.pingen.Planned"
        },
        new(
            "openai",
            "OpenAI",
            "Verenigde Staten",
            "Legal.Processor.openai.Purpose",
            "Legal.Processor.openai.Data",
            ProcessorStatus.Active,
            DataPrivacyFramework),
        new(
            "cursor",
            "Cursor",
            "Verenigde Staten",
            "Legal.Processor.cursor.Purpose",
            "Legal.Processor.cursor.Data",
            ProcessorStatus.Active,
            DataPrivacyFramework),
        new(
            "google-ms",
            "Google, Microsoft",
            "EU en Verenigde Staten",
            "Legal.Processor.google-ms.Purpose",
            "Legal.Processor.google-ms.Data",
            ProcessorStatus.Active,
            DataPrivacyFramework),
        new(
            "kvk",
            "KvK",
            "Nederland",
            "Legal.Processor.kvk.Purpose",
            "Legal.Processor.kvk.Data",
            ProcessorStatus.Active,
            InsideEu),
        new(
            "routing",
            "OSRM, Transitous, Valhalla (FOSSGIS / openstreetmap.de)",
            "EU",
            "Legal.Processor.routing.Purpose",
            "Legal.Processor.routing.Data",
            ProcessorStatus.Active,
            InsideEu),
        new(
            "maps",
            "OpenFreeMap, OpenStreetMap-kaarttegels",
            "EU",
            "Legal.Processor.maps.Purpose",
            "Legal.Processor.maps.Data",
            ProcessorStatus.Active,
            InsideEu),
        new(
            "push",
            "Pushdiensten van Google, Apple, Mozilla en Microsoft",
            "EU en Verenigde Staten",
            "Legal.Processor.push.Purpose",
            "Legal.Processor.push.Data",
            ProcessorStatus.Active,
            DataPrivacyFramework),
        new(
            "video",
            "YouTube, Vimeo",
            "EU en Verenigde Staten",
            "Legal.Processor.video.Purpose",
            "Legal.Processor.video.Data",
            ProcessorStatus.Active,
            DataPrivacyFramework)
    ];

    public static LegalProcessor ById(string id)
        => All.Single(p => string.Equals(p.Id, id, StringComparison.Ordinal));

    public static IReadOnlyList<LegalProcessor> Active
        => All.Where(p => p.Status == ProcessorStatus.Active).ToList();

    public static IReadOnlyList<LegalProcessor> Planned
        => All.Where(p => p.Status == ProcessorStatus.Planned).ToList();
}
