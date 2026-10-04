using Jobsy.Core.Email;

namespace Jobsy.Core.Legal;

public enum ProcessorStatus
{
    /// <summary>Live today.</summary>
    Active,

    /// <summary>Contracted or planned, not processing personal data yet.</summary>
    Planned
}

/// <summary>
/// One processor / third party row of the privacy statement.
/// <paramref name="TransferBasisKey"/> is only set for transfers outside the EEA (DPF / SCC).
/// <see cref="CompanyHq"/> is where the company itself sits. <see cref="DataRegion"/> is where it processes data.
/// An American company is <see cref="ProcessorRegion.UnitedStates"/> headquarters, even when the servers are in the EU.
/// </summary>
public sealed record LegalProcessor(
    string Id,
    string Name,
    ProcessorRegion CompanyHq,
    ProcessorRegion DataRegion,
    string PurposeKey,
    string DataKey,
    ProcessorStatus Status,
    string? TransferBasisKey)
{
    /// <summary>Row-specific note for a planned row; falls back to the generic "not live yet" note.</summary>
    public string? PlannedNoteKey { get; init; }

    /// <summary>
    /// When set, the row is listed only while this mail provider is the one that actually sends.
    /// Null means the row is always listed.
    /// </summary>
    public string? WhenMailProvider { get; init; }

    /// <summary>Dutch "where" cell for the official table: company headquarters and data region.</summary>
    public string Region => ProcessorRegionText.DutchWhere(CompanyHq, DataRegion);

    public bool IsAmericanCompany => CompanyHq == ProcessorRegion.UnitedStates;
}

/// <summary>
/// The processor catalog behind the privacy statement table. Rows live in code, never in copy,
/// so the page and reality cannot drift apart.
/// </summary>
public static class LegalProcessors
{
    /// <summary>Transfer basis keys. A row inside the EEA keeps <see cref="InsideEu"/>.</summary>
    public const string InsideEu = "Legal.Transfer.Eu";
    public const string DataPrivacyFramework = "Legal.Transfer.Dpf";
    public const string StandardClauses = "Legal.Transfer.Scc";
    public const string AdequacyDecision = "Legal.Transfer.Adequacy";

    public static readonly IReadOnlyList<LegalProcessor> All =
    [
        new(
            "render",
            "Render",
            ProcessorRegion.UnitedStates,
            ProcessorRegion.EuFrankfurt,
            "Legal.Processor.render.Purpose",
            "Legal.Processor.render.Data",
            ProcessorStatus.Active,
            StandardClauses),
        new(
            "cloudflare",
            "Cloudflare",
            ProcessorRegion.UnitedStates,
            ProcessorRegion.EuAndUnitedStates,
            "Legal.Processor.cloudflare.Purpose",
            "Legal.Processor.cloudflare.Data",
            ProcessorStatus.Active,
            DataPrivacyFramework),
        new(
            "resend",
            "Resend",
            ProcessorRegion.UnitedStates,
            ProcessorRegion.UnitedStates,
            "Legal.Processor.resend.Purpose",
            "Legal.Processor.resend.Data",
            ProcessorStatus.Active,
            DataPrivacyFramework)
        {
            WhenMailProvider = MailProviderNames.Resend
        },
        new(
            "lettermint",
            "Lettermint",
            ProcessorRegion.Netherlands,
            ProcessorRegion.EuropeanUnion,
            "Legal.Processor.lettermint.Purpose",
            "Legal.Processor.lettermint.Data",
            ProcessorStatus.Active,
            InsideEu)
        {
            WhenMailProvider = MailProviderNames.Lettermint
        },
        new(
            "sentry",
            "Sentry",
            ProcessorRegion.UnitedStates,
            ProcessorRegion.EuOrUnitedStates,
            "Legal.Processor.sentry.Purpose",
            "Legal.Processor.sentry.Data",
            ProcessorStatus.Active,
            DataPrivacyFramework),
        new(
            "mollie",
            "Mollie",
            ProcessorRegion.Netherlands,
            ProcessorRegion.Netherlands,
            "Legal.Processor.mollie.Purpose",
            "Legal.Processor.mollie.Data",
            ProcessorStatus.Active,
            InsideEu),
        new(
            "pingen",
            "Pingen",
            ProcessorRegion.Switzerland,
            ProcessorRegion.Switzerland,
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
            ProcessorRegion.UnitedStates,
            ProcessorRegion.UnitedStates,
            "Legal.Processor.openai.Purpose",
            "Legal.Processor.openai.Data",
            ProcessorStatus.Active,
            DataPrivacyFramework),
        new(
            "cursor",
            "Cursor",
            ProcessorRegion.UnitedStates,
            ProcessorRegion.UnitedStates,
            "Legal.Processor.cursor.Purpose",
            "Legal.Processor.cursor.Data",
            ProcessorStatus.Active,
            DataPrivacyFramework),
        new(
            "google-ms",
            "Google, Microsoft",
            ProcessorRegion.UnitedStates,
            ProcessorRegion.EuAndUnitedStates,
            "Legal.Processor.google-ms.Purpose",
            "Legal.Processor.google-ms.Data",
            ProcessorStatus.Active,
            DataPrivacyFramework),
        new(
            "kvk",
            "KvK",
            ProcessorRegion.Netherlands,
            ProcessorRegion.Netherlands,
            "Legal.Processor.kvk.Purpose",
            "Legal.Processor.kvk.Data",
            ProcessorStatus.Active,
            InsideEu),
        new(
            "routing",
            "OSRM, Transitous, Valhalla (FOSSGIS / openstreetmap.de)",
            ProcessorRegion.EuropeanUnion,
            ProcessorRegion.EuropeanUnion,
            "Legal.Processor.routing.Purpose",
            "Legal.Processor.routing.Data",
            ProcessorStatus.Active,
            InsideEu),
        new(
            "maps",
            "OpenFreeMap, OpenStreetMap-kaarttegels",
            ProcessorRegion.EuropeanUnion,
            ProcessorRegion.EuropeanUnion,
            "Legal.Processor.maps.Purpose",
            "Legal.Processor.maps.Data",
            ProcessorStatus.Active,
            InsideEu),
        new(
            "push",
            "Pushdiensten van Google, Apple, Mozilla en Microsoft",
            ProcessorRegion.UnitedStates,
            ProcessorRegion.EuAndUnitedStates,
            "Legal.Processor.push.Purpose",
            "Legal.Processor.push.Data",
            ProcessorStatus.Active,
            DataPrivacyFramework),
        new(
            "video",
            "YouTube, Vimeo",
            ProcessorRegion.UnitedStates,
            ProcessorRegion.EuAndUnitedStates,
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
