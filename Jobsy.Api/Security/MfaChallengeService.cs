using System.Security.Cryptography;
using Jobsy.Core.Entities;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Api.Security;

/// <summary>Short-lived, opaque proof that the password (or external identity) was checked.</summary>
public sealed class MfaChallengeService
{
    public const int MaxFailedAttempts = 5;
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
    private readonly IMemoryCache _cache;

    public MfaChallengeService(IMemoryCache cache) => _cache = cache;

    public string Create(User user, bool rememberDevice, bool localPassword = false)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _cache.Set(Key(token), new MfaChallenge(user.Id, rememberDevice, localPassword), Lifetime);
        return token;
    }

    public bool TryGet(string? token, out MfaChallenge challenge)
    {
        challenge = default!;
        return !string.IsNullOrWhiteSpace(token)
               && _cache.TryGetValue(Key(token!), out challenge!);
    }

    /// <summary>Increments failures; at 5 removes the challenge and returns true (expired).</summary>
    public bool RegisterFailure(string? token)
    {
        if (!TryGet(token, out var challenge))
        {
            return true;
        }

        challenge.FailedAttempts++;
        if (challenge.FailedAttempts >= MaxFailedAttempts)
        {
            Consume(token);
            return true;
        }

        _cache.Set(Key(token!), challenge, Lifetime);
        return false;
    }

    public void Consume(string? token)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            _cache.Remove(Key(token));
        }
    }

    private static string Key(string token) => "mfa-challenge:" + token;
}

public sealed class MfaChallenge
{
    public MfaChallenge(Guid userId, bool rememberDevice, bool localPassword = false)
    {
        UserId = userId;
        RememberDevice = rememberDevice;
        LocalPassword = localPassword;
    }

    public Guid UserId { get; }
    public bool RememberDevice { get; }
    public bool LocalPassword { get; }
    public int FailedAttempts { get; set; }
}
