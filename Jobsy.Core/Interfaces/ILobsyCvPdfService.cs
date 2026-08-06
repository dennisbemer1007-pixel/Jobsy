using Jobsy.Core.Contracts;

namespace Jobsy.Core.Interfaces;

public interface ILobsyCvPdfService
{
    /// <summary>Renders a Lobsy-CV PDF from a prepared model (on-the-fly, no disk storage).</summary>
    Task<byte[]> RenderAsync(LobsyCvModel model, CancellationToken cancellationToken = default);

    /// <summary>Safe download filename without e-mail: Lobsy-CV-{initials}-{yyyyMMdd}.pdf</summary>
    string BuildFileName(LobsyCvModel model);
}
