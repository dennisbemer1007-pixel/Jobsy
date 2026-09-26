using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Security;
using Jobsy.Web.Security;
using Microsoft.AspNetCore.Http;

namespace Jobsy.Tests;

public class LoginProtectionTests
{
    [Fact]
    public void Fifth_failed_attempt_starts_fifteen_minute_lockout()
    {
        Assert.Equal(TimeSpan.Zero, LoginLockoutRules.LockoutDuration(4));
        Assert.Equal(TimeSpan.FromMinutes(15), LoginLockoutRules.LockoutDuration(5));
        Assert.Equal(TimeSpan.FromMinutes(30), LoginLockoutRules.LockoutDuration(6));
    }

    [Fact]
    public void Rate_limit_for_one_account_does_not_block_another_account()
    {
        var limiter = new LoginProtectionRateLimiter();
        for (var attempt = 0; attempt < 8; attempt++)
        {
            Assert.True(limiter.TryAcquire("login", "203.0.113.10", "first@example.test"));
        }

        Assert.False(limiter.TryAcquire("login", "203.0.113.10", "first@example.test"));
        Assert.True(limiter.TryAcquire("login", "203.0.113.11", "other@example.test"));
    }

    [Fact]
    public void Plaintext_passwords_are_not_accepted_and_old_pbkdf2_hashes_rehash()
    {
        const string password = "een lang veilig wachtwoord";
        var oldHash = CreateHash(password, 100_000);

        Assert.False(JobsyPasswordHasher.Verify(password, password));
        Assert.True(JobsyPasswordHasher.Verify(password, oldHash));
        Assert.True(JobsyPasswordHasher.NeedsRehash(oldHash));
    }

    [Fact]
    public async Task Admin_without_mfa_is_redirected_before_admin_page_renders()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/admin/users";
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Role, "Admin")
        ], "test"));
        var reachedNext = false;
        var middleware = new MfaEnforcementMiddleware(_ =>
        {
            reachedNext = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.False(reachedNext);
        Assert.StartsWith("/account/mfa?returnUrl=", context.Response.Headers.Location.ToString());
    }

    private static string CreateHash(string password, int iterations)
    {
        var salt = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
        var hash = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
            System.Text.Encoding.UTF8.GetBytes(password),
            salt,
            iterations,
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            32);
        return $"PBKDF2${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }
}
