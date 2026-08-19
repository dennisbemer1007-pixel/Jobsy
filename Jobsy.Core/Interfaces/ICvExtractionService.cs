using Jobsy.Core.Contracts;

namespace Jobsy.Core.Interfaces;

public interface ICvTextExtractor
{
    string Extract(byte[] content, string contentType, string fileName);
}

public interface ICvExtractionService
{
    /// <summary>
    /// Asks OpenAI for clearly present profile fields. Returns empty extraction when no key or on failure.
    /// </summary>
    Task<CvExtractedProfile> ExtractAsync(string cvText, CancellationToken cancellationToken = default);
}
