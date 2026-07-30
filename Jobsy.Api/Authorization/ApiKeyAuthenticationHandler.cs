using System.Security.Claims;
using System.Text.Encodings.Web;
using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Jobsy.Api.Authorization;

/// <summary>
/// Authenticates external clients via <c>X-API-Key</c>. Resolves the owning company from the
/// hashed key row and never trusts a client-supplied company id for tenancy.
/// </summary>
public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ICompanyApiKeyService _apiKeys;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ICompanyApiKeyService apiKeys)
        : base(options, logger, encoder)
    {
        _apiKeys = apiKeys;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyAuthDefaults.HeaderName, out var values))
        {
            return AuthenticateResult.NoResult();
        }

        var plaintext = values.FirstOrDefault()?.Trim();
        if (string.IsNullOrWhiteSpace(plaintext))
        {
            return AuthenticateResult.Fail("Missing API key.");
        }

        var key = await _apiKeys.FindActiveByPlaintextAsync(plaintext, Context.RequestAborted);
        if (key is null)
        {
            return AuthenticateResult.Fail("Invalid or inactive API key.");
        }

        // Fire-and-forget friendly: update last-used without blocking auth on soft failures.
        try
        {
            await _apiKeys.TouchLastUsedAsync(key.Id, Context.RequestAborted);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to update LastUsedAt for API key {ApiKeyId}", key.Id);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, key.Id.ToString()),
            new(ClaimTypes.Name, $"api-key:{key.KeyPrefix}"),
            new(ApiKeyAuthDefaults.ApiKeyIdClaim, key.Id.ToString()),
            new(JobsyClaimTypes.CompanyId, key.CompanyId.ToString())
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }
}
