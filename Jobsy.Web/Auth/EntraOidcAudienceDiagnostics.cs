using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Jobsy.Web.Auth;

public static class EntraOidcAudienceDiagnostics
{
    public static void LogAudienceFailure(
        ILogger logger,
        Exception? failure,
        string? configuredClientId,
        TokenValidationParameters parameters)
    {
        var expected = ResolveExpectedAudiences(configuredClientId, parameters);
        var tokenAudiences = TryReadAudiencesFromFailure(failure);

        logger.LogWarning(
            "Entra token audience mismatch: expected={ExpectedAudiences}, tokenAudiences={TokenAudiences}, " +
            "validAudienceParam={ValidAudience}, validateAudience={ValidateAudience}",
            string.Join(", ", expected),
            tokenAudiences.Count == 0 ? "(unknown)" : string.Join(", ", tokenAudiences),
            parameters.ValidAudience ?? "(null)",
            parameters.ValidateAudience);
    }

    internal static IReadOnlyList<string> ResolveExpectedAudiences(
        string? configuredClientId,
        TokenValidationParameters parameters)
    {
        var list = new List<string>();
        if (!string.IsNullOrWhiteSpace(configuredClientId))
        {
            list.Add(configuredClientId.Trim());
        }

        if (!string.IsNullOrWhiteSpace(parameters.ValidAudience)
            && !list.Contains(parameters.ValidAudience, StringComparer.Ordinal))
        {
            list.Add(parameters.ValidAudience);
        }

        if (parameters.ValidAudiences is not null)
        {
            foreach (var aud in parameters.ValidAudiences)
            {
                if (!string.IsNullOrWhiteSpace(aud) && !list.Contains(aud, StringComparer.Ordinal))
                {
                    list.Add(aud);
                }
            }
        }

        return list;
    }

    private static List<string> TryReadAudiencesFromFailure(Exception? failure)
    {
        var audiences = new List<string>();
        for (var ex = failure; ex is not null; ex = ex.InnerException)
        {
            if (ex is SecurityTokenInvalidAudienceException invalid && !string.IsNullOrWhiteSpace(invalid.InvalidAudience))
            {
                audiences.Add(invalid.InvalidAudience);
            }

            if (ex.Data.Contains("token") && ex.Data["token"] is string jwt
                && TryReadJwtAudiences(jwt, audiences))
            {
                break;
            }

            var message = ex.Message;
            const string prefix = "Audience: '";
            var idx = message.IndexOf(prefix, StringComparison.Ordinal);
            if (idx >= 0)
            {
                var start = idx + prefix.Length;
                var end = message.IndexOf('\'', start);
                if (end > start)
                {
                    audiences.Add(message[start..end]);
                }
            }
        }

        return audiences;
    }

    private static bool TryReadJwtAudiences(string jwt, List<string> audiences)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(jwt))
            {
                return false;
            }

            var token = handler.ReadJwtToken(jwt);
            foreach (var aud in token.Audiences)
            {
                if (!string.IsNullOrWhiteSpace(aud) && !audiences.Contains(aud, StringComparer.Ordinal))
                {
                    audiences.Add(aud);
                }
            }

            if (token.Payload.TryGetValue("aud", out var audClaim))
            {
                switch (audClaim)
                {
                    case string single when !string.IsNullOrWhiteSpace(single):
                        if (!audiences.Contains(single, StringComparer.Ordinal))
                        {
                            audiences.Add(single);
                        }

                        break;
                    case IEnumerable<object> many:
                        foreach (var item in many)
                        {
                            if (item?.ToString() is { Length: > 0 } s
                                && !audiences.Contains(s, StringComparer.Ordinal))
                            {
                                audiences.Add(s);
                            }
                        }

                        break;
                }
            }

            return audiences.Count > 0;
        }
        catch
        {
            return false;
        }
    }
}
