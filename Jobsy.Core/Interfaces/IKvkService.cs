namespace Jobsy.Core.Interfaces;

public interface IKvkService
{
    Task<KvkCompanyResult?> GetByKvkNumberAsync(
        string kvkNumber,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KvkEstablishmentResult>> GetEstablishmentsAsync(
        string kvkNumber,
        CancellationToken cancellationToken = default);
}

public record KvkCompanyResult(
    string KvkNumber,
    string Name,
    string Address);

public record KvkEstablishmentResult(
    string KvkNumber,
    string EstablishmentNumber,
    string KvkEstablishmentId,
    string Name,
    string Address,
    double Latitude,
    double Longitude,
    bool IsInUse);
