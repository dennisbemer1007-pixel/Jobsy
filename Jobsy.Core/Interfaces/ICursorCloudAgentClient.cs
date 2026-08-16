namespace Jobsy.Core.Interfaces;

public sealed record CursorAgentImage(string Base64Data, int? Width = null, int? Height = null);

public sealed record CursorAgentLaunchRequest(
    string Prompt,
    string BranchName,
    IReadOnlyList<CursorAgentImage>? Images = null);

public sealed record CursorAgentLaunchResult(
    string AgentId,
    string? Status,
    string? AgentUrl);

public sealed record CursorAgentStatusResult(
    string AgentId,
    string Status,
    string? PullRequestUrl,
    string? BranchName,
    string? Summary);

public interface ICursorCloudAgentClient
{
    bool IsConfigured { get; }

    Task<CursorAgentLaunchResult> LaunchAsync(
        CursorAgentLaunchRequest request,
        CancellationToken cancellationToken = default);

    Task<CursorAgentStatusResult?> GetAsync(
        string agentId,
        CancellationToken cancellationToken = default);
}
