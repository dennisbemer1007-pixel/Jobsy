using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

/// <summary>
/// Resolves OpenAI API key, model, and base URL for a feature (DB credentials first, then config, then defaults).
/// </summary>
public interface IOpenAiEndpointResolver
{
    Task<OpenAiEndpointResolution> ResolveAsync(
        OpenAiFeature feature,
        CancellationToken cancellationToken = default);
}

/// <summary>Resolved OpenAI endpoint settings for one feature call.</summary>
public sealed record OpenAiEndpointResolution(string? ApiKey, string Model, string BaseUrl);
