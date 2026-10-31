using Jobsy.Core.Entities.Scholen;

namespace Jobsy.Core.Scholen;

public interface IPupilG78MirrorRenderer
{
    Task<string> RenderMirrorBodyAsync(
        PupilResult result,
        PupilProgress? progress,
        string language,
        CancellationToken cancellationToken = default);
}
