using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jobsy.Web.Auth;

/// <summary>Named HttpClient for anonymous auth calls to the API (trusted client IP attached).</summary>
public sealed class AuthApiClient
{
    public const string HttpClientName = "JobsyAuthApi";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IHttpClientFactory _httpClientFactory;

    public AuthApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public HttpClient CreateClient() => _httpClientFactory.CreateClient(HttpClientName);

    public async Task<LocalLoginOutcome> LocalLoginAsync(
        string email,
        string password,
        bool rememberDevice,
        string? mfaTrustToken = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = CreateClient();
            using var response = await client.PostAsJsonAsync(
                "api/auth/local-login",
                new { email, password, rememberDevice, mfaTrustToken },
                cancellationToken);
            return await MapLoginResponseAsync(response, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            return LocalLoginOutcome.Fail(LocalLoginFailureKind.Unavailable);
        }
        catch (HttpRequestException)
        {
            return LocalLoginOutcome.Fail(LocalLoginFailureKind.Unavailable);
        }
    }

    public async Task<MfaVerifyOutcome> MfaVerifyAsync(
        string challengeToken,
        string? code,
        string? recoveryCode,
        bool trustDevice = false,
        string? method = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = CreateClient();
            using var response = await client.PostAsJsonAsync(
                "api/auth/mfa/verify",
                new { challengeToken, code, recoveryCode, trustDevice, method },
                cancellationToken);
            return await MapMfaResponseAsync(response, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            return MfaVerifyOutcome.Fail(MfaVerifyFailureKind.Unavailable);
        }
        catch (HttpRequestException)
        {
            return MfaVerifyOutcome.Fail(MfaVerifyFailureKind.Unavailable);
        }
    }

    public async Task<HttpResponseMessage> PostJsonAsync(
        string relativeUrl,
        object body,
        CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        return await client.PostAsJsonAsync(relativeUrl, body, cancellationToken);
    }

    public async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        return await client.SendAsync(request, cancellationToken);
    }

    private static async Task<LocalLoginOutcome> MapLoginResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            var profile = await response.Content.ReadFromJsonAsync<LocalApiLoginProfile>(JsonOptions, cancellationToken);
            if (profile is null || string.IsNullOrWhiteSpace(profile.Email))
            {
                return LocalLoginOutcome.Fail(LocalLoginFailureKind.Unavailable);
            }

            return LocalLoginOutcome.Ok(profile);
        }

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return LocalLoginOutcome.Fail(
                LocalLoginFailureKind.TooMany,
                ReadRetryAt(response, await ReadBodyAsync(response, cancellationToken)));
        }

        var body = await ReadBodyAsync(response, cancellationToken);
        var code = ReadCode(body);
        if (response.StatusCode == HttpStatusCode.Forbidden
            && string.Equals(code, "locked_out", StringComparison.OrdinalIgnoreCase))
        {
            return LocalLoginOutcome.Fail(LocalLoginFailureKind.Locked, ReadRetryAt(response, body));
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized
            && string.Equals(code, "invalid_credentials", StringComparison.OrdinalIgnoreCase))
        {
            return LocalLoginOutcome.Fail(LocalLoginFailureKind.Invalid);
        }

        if ((int)response.StatusCode >= 500 || response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Unknown 401 shape or 5xx → unavailable (never map lockout/429 as invalid).
            return response.StatusCode == HttpStatusCode.Unauthorized
                ? LocalLoginOutcome.Fail(LocalLoginFailureKind.Invalid)
                : LocalLoginOutcome.Fail(LocalLoginFailureKind.Unavailable);
        }

        return LocalLoginOutcome.Fail(LocalLoginFailureKind.Unavailable);
    }

    private static async Task<MfaVerifyOutcome> MapMfaResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            var profile = await response.Content.ReadFromJsonAsync<LocalApiLoginProfile>(JsonOptions, cancellationToken);
            return profile is null
                ? MfaVerifyOutcome.Fail(MfaVerifyFailureKind.Unavailable)
                : MfaVerifyOutcome.Ok(profile);
        }

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return MfaVerifyOutcome.Fail(
                MfaVerifyFailureKind.TooMany,
                ReadRetryAt(response, await ReadBodyAsync(response, cancellationToken)));
        }

        var body = await ReadBodyAsync(response, cancellationToken);
        var code = ReadCode(body);
        if (response.StatusCode == HttpStatusCode.Forbidden
            && string.Equals(code, "mfa_locked", StringComparison.OrdinalIgnoreCase))
        {
            return MfaVerifyOutcome.Fail(MfaVerifyFailureKind.Locked, ReadRetryAt(response, body));
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            if (string.Equals(code, "challenge_expired", StringComparison.OrdinalIgnoreCase))
            {
                return MfaVerifyOutcome.Fail(MfaVerifyFailureKind.Expired);
            }

            return MfaVerifyOutcome.Fail(MfaVerifyFailureKind.Invalid);
        }

        return MfaVerifyOutcome.Fail(MfaVerifyFailureKind.Unavailable);
    }

    private static async Task<JsonDocument?> ReadBodyAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            if (stream.CanSeek && stream.Length == 0)
            {
                return null;
            }

            return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private static string? ReadCode(JsonDocument? body)
    {
        if (body is null)
        {
            return null;
        }

        if (body.RootElement.TryGetProperty("code", out var code) && code.ValueKind == JsonValueKind.String)
        {
            return code.GetString();
        }

        return null;
    }

    private static DateTime? ReadRetryAt(HttpResponseMessage response, JsonDocument? body)
    {
        if (body is not null
            && body.RootElement.TryGetProperty("retryAtUtc", out var retry)
            && retry.ValueKind == JsonValueKind.String
            && DateTime.TryParse(retry.GetString(), null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
        {
            return parsed.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
                : parsed.ToUniversalTime();
        }

        if (response.Headers.RetryAfter?.Delta is TimeSpan delta)
        {
            return DateTime.UtcNow.Add(delta);
        }

        if (response.Headers.RetryAfter?.Date is DateTimeOffset date)
        {
            return date.UtcDateTime;
        }

        return null;
    }
}

public enum LocalLoginFailureKind
{
    Invalid,
    Locked,
    TooMany,
    Unavailable
}

public sealed class LocalLoginOutcome
{
    public LocalApiLoginProfile? Profile { get; private init; }
    public LocalLoginFailureKind? Failure { get; private init; }
    public DateTime? RetryAtUtc { get; private init; }

    public static LocalLoginOutcome Ok(LocalApiLoginProfile profile)
        => new() { Profile = profile };

    public static LocalLoginOutcome Fail(LocalLoginFailureKind failure, DateTime? retryAtUtc = null)
        => new() { Failure = failure, RetryAtUtc = retryAtUtc };
}

public enum MfaVerifyFailureKind
{
    Invalid,
    Expired,
    Locked,
    TooMany,
    Unavailable
}

public sealed class MfaVerifyOutcome
{
    public LocalApiLoginProfile? Profile { get; private init; }
    public MfaVerifyFailureKind? Failure { get; private init; }
    public DateTime? RetryAtUtc { get; private init; }

    public static MfaVerifyOutcome Ok(LocalApiLoginProfile profile)
        => new() { Profile = profile };

    public static MfaVerifyOutcome Fail(MfaVerifyFailureKind failure, DateTime? retryAtUtc = null)
        => new() { Failure = failure, RetryAtUtc = retryAtUtc };
}

/// <summary>DTO shared by AuthApiClient and auth endpoints (kept public for tests).</summary>
public sealed class LocalApiLoginProfile
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "Candidate";
    public Guid? CompanyId { get; set; }
    public List<Guid>? CompanyIds { get; set; }
    public Guid? SchoolId { get; set; }
    public bool ShowCandidateHowTo { get; set; }
    public bool HasCandidateApplications { get; set; }
    public bool HasSalesReferral { get; set; }
    public bool IsNewUser { get; set; }
    public string? SessionToken { get; set; }
    public int SessionVersion { get; set; }
    public Guid? DeviceSessionId { get; set; }
    public string? DeviceRefreshToken { get; set; }
    public DateTime? DeviceExpiresAtUtc { get; set; }
    public string? HandoffCode { get; set; }
    public Guid? UserId { get; set; }
    public bool RequiresMfa { get; set; }
    public bool MfaEnrolled { get; set; }
    public string? MfaChallengeToken { get; set; }
    public bool MfaVerified { get; set; }
    public List<string>? RecoveryCodes { get; set; }
    public int? RecoveryCodesLeft { get; set; }
    public bool UsedRecoveryCode { get; set; }
    public string? MfaTrustToken { get; set; }
    public string? AuthMethod { get; set; }
    public bool IsTestAccount { get; set; }
}
