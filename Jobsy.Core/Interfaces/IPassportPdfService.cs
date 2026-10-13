using Jobsy.Core.Passport;

namespace Jobsy.Core.Interfaces;

public interface IPassportPdfService
{
    Task<byte[]> RenderAsync(PassportPdfModel model, CancellationToken cancellationToken = default);

    string BuildFileName(PassportPdfModel model);
}

/// <summary>Reads completed tests and returns word labels only. Never a percent.</summary>
public interface IPassportDnaReader
{
    Task<IReadOnlyList<PassportDnaLayerFact>> ReadAsync(
        Guid userId,
        string language,
        CancellationToken cancellationToken = default);
}
