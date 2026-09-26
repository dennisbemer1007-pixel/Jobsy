using System.Collections.Concurrent;
using System.Security.Cryptography;
using Jobsy.Core.Entities;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Api.Security;

/// <summary>Short-lived, opaque proof that the password (or external identity) was checked.</summary>
public sealed class MfaChallengeService
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
    private readonly IMemoryCache _cache;

    public MfaChallengeService(IMemoryCache cache) => _cache = cache;

    public string Create(User user, bool rememberDevice)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _cache.Set(Key(token), new MfaChallenge(user.Id, rememberDevice), Lifetime);
        return token;
    }

    public bool TryGet(string? token, out MfaChallenge challenge)
    {
        challenge = default!;
        return !string.IsNullOrWhiteSpace(token)
               && _cache.TryGetValue(Key(token!), out challenge!);
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

public sealed record MfaChallenge(Guid UserId, bool RememberDevice);
