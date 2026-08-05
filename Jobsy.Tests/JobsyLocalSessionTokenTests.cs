using Jobsy.Infrastructure.Security;

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
    public void Validate_rejects_expired_token()
    {
        var userId = Guid.NewGuid();
        var token = JobsyLocalSessionToken.Create(
            "user@example.com",
            userId,
            "secret",
            TimeSpan.FromSeconds(-5));

        Assert.False(JobsyLocalSessionToken.TryValidate(token, "secret", out _, out _));
    }
}
