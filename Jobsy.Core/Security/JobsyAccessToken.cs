using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Jobsy.Core.Security;

/// <summary>
/// Short-lived ES256 access tokens issued by the Web app and validated by the API.
/// Claims: <c>sub</c> = UserId, <c>sv</c> = SessionVersion (no e-mail identity).
/// </summary>
public static class JobsyAccessToken
{
    public const string SessionVersionClaim = "sv";
    public const string ClientIpClaim = "client_ip";
    public const string DefaultIssuer = "jobsy-web";
    public const string DefaultAudience = "jobsy-api";
    public static readonly TimeSpan DefaultLifetime = TimeSpan.FromMinutes(10);

    public static string Create(
        Guid userId,
        int sessionVersion,
        string privateKeyPem,
        string? issuer = null,
        string? audience = null,
        TimeSpan? lifetime = null,
        string? clientIp = null,
        bool mfaVerified = false,
        string? authMethod = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(privateKeyPem);
        var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(privateKeyPem);
        try
        {
            var key = new ECDsaSecurityKey(ecdsa) { KeyId = "jobsy-es256" };
            // Avoid IdentityModel caching a SignatureProvider over a disposed ECDsa.
            key.CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false };
            var credentials = new SigningCredentials(key, SecurityAlgorithms.EcdsaSha256);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userId.ToString("D")),
                new(SessionVersionClaim, sessionVersion.ToString(CultureInfo.InvariantCulture)),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            };
            if (!string.IsNullOrWhiteSpace(clientIp))
            {
                claims.Add(new Claim(ClientIpClaim, clientIp.Trim()));
            }

            if (mfaVerified)
            {
                claims.Add(new Claim(Jobsy.Core.Authorization.JobsyClaimTypes.MfaVerified, "1"));
            }

            if (!string.IsNullOrWhiteSpace(authMethod))
            {
                claims.Add(new Claim("auth_method", authMethod.Trim()));
            }

            var now = DateTime.UtcNow;
            var life = lifetime ?? DefaultLifetime;
            var notBefore = life < TimeSpan.Zero ? now.Add(life).AddMinutes(-1) : now.AddSeconds(-30);
            var expires = now.Add(life);
            if (expires <= notBefore)
            {
                expires = notBefore.AddSeconds(1);
            }

            var token = new JwtSecurityToken(
                issuer: issuer ?? DefaultIssuer,
                audience: audience ?? DefaultAudience,
                claims: claims,
                notBefore: notBefore,
                expires: expires,
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        finally
        {
            ecdsa.Dispose();
        }
    }

    public static string NormalizePem(string? pem)
    {
        if (string.IsNullOrWhiteSpace(pem))
        {
            return string.Empty;
        }

        return pem.Replace("\\n", "\n", StringComparison.Ordinal).Trim();
    }

    public static TokenValidationParameters CreateValidationParameters(
        string publicKeyPem,
        string? issuer = null,
        string? audience = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(publicKeyPem);
        var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(publicKeyPem);
        var key = new ECDsaSecurityKey(ecdsa) { KeyId = "jobsy-es256" };

        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateIssuer = true,
            ValidIssuer = issuer ?? DefaultIssuer,
            ValidateAudience = true,
            ValidAudience = audience ?? DefaultAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = ClaimTypes.Role
        };
    }

    /// <summary>Generates a Development/test ES256 key pair (PEM).</summary>
    public static (string PrivatePem, string PublicPem) GenerateDevelopmentKeyPair()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var privatePem = ecdsa.ExportPkcs8PrivateKeyPem();
        var publicPem = ecdsa.ExportSubjectPublicKeyInfoPem();
        return (privatePem, publicPem);
    }

    /// <summary>
    /// Fixed ES256 pair for local Development / tests when env PEMs are unset.
    /// Also used as a logged Production bootstrap when Render <c>sync:false</c> PEMs
    /// are still empty — replace with environment-unique keys in the Dashboard ASAP.
    /// </summary>
    public const string DevelopmentPrivateKeyPem =
        """
        -----BEGIN PRIVATE KEY-----
        MIGHAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBG0wawIBAQQgwo7mZ1Taj8RciFa0
        vn6pFyNebR0fsaw2DbyA6E0mBZGhRANCAARWODuQmwaGgCIsJo46CluHL+TEuMC1
        dfOYAuZrOBaCPdJ0uE4EWa7WmA7mbFu3fbgoX96lKfol3LThCt7ea7j+
        -----END PRIVATE KEY-----
        """;

    public const string DevelopmentPublicKeyPem =
        """
        -----BEGIN PUBLIC KEY-----
        MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEVjg7kJsGhoAiLCaOOgpbhy/kxLjA
        tXXzmALmazgWgj3SdLhOBFmu1pgO5mxbt324KF/epSn6Jdy04Qre3mu4/g==
        -----END PUBLIC KEY-----
        """;
}
