using Jobsy.Core.Security;

namespace Jobsy.Tests;

public class JobsyLocalSessionTokenTests
{
    [Fact]
    public void Create_and_validate_roundtrip()
    {
        var userId = Guid.NewGuid();
        var token = JobsyLocalSessionToken.Create(
            "Boss@Example.COM",
            userId,
            "test-secret",
            TimeSpan.FromMinutes(30));

        Assert.True(JobsyLocalSessionToken.TryValidate(token, "test-secret", out var email, out var id));
        Assert.Equal("boss@example.com", email);
        Assert.Equal(userId, id);
    }

    [Fact]
    public void Validate_rejects_wrong_secret_and_tampering()
    {
        var userId = Guid.NewGuid();
        var token = JobsyLocalSessionToken.Create(
            "user@example.com",
            userId,
            "secret-a",
            TimeSpan.FromHours(1));

        Assert.False(JobsyLocalSessionToken.TryValidate(token, "secret-b", out _, out _));

        var tampered = token[..^4] + "xxxx";
        Assert.False(JobsyLocalSessionToken.TryValidate(tampered, "secret-a", out _, out _));
    }

    [Fact]
    public void Validate_rejects_expired_token_but_read_allows_refresh()
    {
        var userId = Guid.NewGuid();
        var token = JobsyLocalSessionToken.Create(
            "user@example.com",
            userId,
            "secret",
            TimeSpan.FromSeconds(-5));

        Assert.False(JobsyLocalSessionToken.TryValidate(token, "secret", out _, out _));
        Assert.True(JobsyLocalSessionToken.TryReadSignedPayload(
            token, "secret", ignoreExpiry: true, out var email, out var id, out _));
        Assert.Equal("user@example.com", email);
        Assert.Equal(userId, id);
    }

    [Fact]
    public void ResolveSigningKey_prefers_dedicated_key()
    {
        Assert.Equal(
            "dedicated",
            JobsyLocalSessionToken.ResolveSigningKey("dedicated", "dev-secret"));
        Assert.Equal(
            "dev-secret",
            JobsyLocalSessionToken.ResolveSigningKey(null, "dev-secret"));
        Assert.Null(JobsyLocalSessionToken.ResolveSigningKey(null, null));
    }
}
