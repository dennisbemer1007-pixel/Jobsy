using Jobsy.Infrastructure.Security;

namespace Jobsy.Tests;

/// <summary>Test double that stores secrets as plaintext (no Data Protection keys needed).</summary>
internal sealed class PassthroughSecretProtector : ISecretProtector
{
    public string? Protect(string? plaintext) =>
        string.IsNullOrWhiteSpace(plaintext) ? null : plaintext.Trim();

    public string? Unprotect(string? protectedPayload) =>
        string.IsNullOrWhiteSpace(protectedPayload) ? null : protectedPayload.Trim();
}
