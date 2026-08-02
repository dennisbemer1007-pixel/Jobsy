using Jobsy.Core.Exceptions;

namespace Jobsy.Core.Interfaces;

public interface IKvkService
{
    Task<KvkCompanyResult?> GetByKvkNumberAsync(
        string kvkNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns establishments for a KVK number.
    /// Throws <see cref="KvkServiceUnavailableException"/> on transient API failure.
    /// Empty list means the number is unknown / has no vestigingen (not an outage).
    /// </summary>
    Task<IReadOnlyList<KvkEstablishmentResult>> GetEstablishmentsAsync(
        string kvkNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lookup that distinguishes API outage from "not found" without throwing.
    /// </summary>
    Task<KvkEstablishmentsLookup> LookupEstablishmentsAsync(
        string kvkNumber,
        CancellationToken cancellationToken = default);
}

public enum KvkLookupStatus
{
    Ok = 0,
    NotFound = 1,
    Unavailable = 2
}

public sealed record KvkEstablishmentsLookup(
    KvkLookupStatus Status,
    IReadOnlyList<KvkEstablishmentResult> Establishments,
    string? Message = null)
{
    public static KvkEstablishmentsLookup Ok(IReadOnlyList<KvkEstablishmentResult> items)
        => new(KvkLookupStatus.Ok, items);

    public static KvkEstablishmentsLookup NotFound()
        => new(KvkLookupStatus.NotFound, Array.Empty<KvkEstablishmentResult>(),
            "Geen vestigingen gevonden voor dit KVK-nummer.");

    public static KvkEstablishmentsLookup Unavailable(string? message = null)
        => new(KvkLookupStatus.Unavailable, Array.Empty<KvkEstablishmentResult>(),
            message ?? "KVK-dienst is tijdelijk niet beschikbaar. Je kunt doorgaan; verificatie volgt later.");
}

public static class KvkServiceLookup
{
    public static async Task<KvkEstablishmentsLookup> FromGetEstablishmentsAsync(
        IKvkService kvk,
        string kvkNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var items = await kvk.GetEstablishmentsAsync(kvkNumber, cancellationToken);
            return items.Count == 0
                ? KvkEstablishmentsLookup.NotFound()
                : KvkEstablishmentsLookup.Ok(items);
        }
        catch (KvkServiceUnavailableException ex)
        {
            return KvkEstablishmentsLookup.Unavailable(ex.Message);
        }
    }
}

public record KvkCompanyResult(
    string KvkNumber,
    string Name,
    string Address,
    IReadOnlyList<string>? SbiCodes = null)
{
    public IReadOnlyList<string> EffectiveSbiCodes => SbiCodes ?? Array.Empty<string>();
}

public record KvkEstablishmentResult(
    string KvkNumber,
    string EstablishmentNumber,
    string KvkEstablishmentId,
    string Name,
    string Address,
    double Latitude,
    double Longitude,
    bool IsInUse,
    IReadOnlyList<string>? SbiCodes = null)
{
    public IReadOnlyList<string> EffectiveSbiCodes => SbiCodes ?? Array.Empty<string>();
}
