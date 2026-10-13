using System.Security.Cryptography;
using System.Text;

namespace Jobsy.Core.Rules;

public static class ResearchRespondentHash
{
    public static string ForUser(Guid userId, DateTime utcNow)
    {
        var year = utcNow.Year.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var payload = $"{userId}:lobsy-psych-export-v1:{year}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    public static string RandomPilot()
        => Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
}
