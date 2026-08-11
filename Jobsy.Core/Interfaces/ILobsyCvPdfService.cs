using Jobsy.Core.Contracts;

namespace Jobsy.Core.Interfaces;

public interface ILobsyCvPdfService
{
    /// <summary>Renders a Lobsy-CV PDF from a prepared model (on-the-fly, no disk storage).</summary>
    Task<byte[]> RenderAsync(LobsyCvModel model, CancellationToken cancellationToken = default);

    /// <summary>Safe download filename without e-mail: Lobsy-CV-{initials}-{yyyyMMdd}.pdf</summary>
    string BuildFileName(LobsyCvModel model);
}

/// <summary>Renders a small OSM tile map PNG for embedding in the Lobsy-CV.</summary>
public interface ICandidateMapImageService
{
    Task<byte[]?> RenderAsync(
        double latitude,
        double longitude,
        int width = 640,
        int height = 280,
        int zoom = 15,
        byte[]? markerLogoPng = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Workplace-centered map with a travel-reach circle. Zoom auto-fits the circle.
    /// </summary>
    Task<byte[]?> RenderWorkplaceReachAsync(
        double latitude,
        double longitude,
        double radiusMeters,
        int width = 640,
        int height = 280,
        byte[]? markerLogoPng = null,
        CancellationToken cancellationToken = default);
}
