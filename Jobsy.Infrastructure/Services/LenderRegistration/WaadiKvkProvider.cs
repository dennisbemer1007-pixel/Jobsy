using Jobsy.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Jobsy.Infrastructure.Services.LenderRegistration;

/// <summary>
/// KvK has no public Waadi API. Returns Unknown plus a deep link for admin verification.
/// </summary>
public sealed class WaadiKvkProvider : ILenderRegistrationProvider
{
    public const string ProviderName = LenderRegistrationSources.WaadiKvk;

    private readonly string _deepLinkTemplate;

    public WaadiKvkProvider(IConfiguration configuration)
    {
        _deepLinkTemplate = configuration["LenderRegistration:WaadiKvk:DeepLinkTemplate"]
            ?? "https://www.kvk.nl/zoeken/?kvknummer={0}";
    }

    public string Name => ProviderName;
    public bool IsEnabled => true;

    public Task<LenderProviderResult> CheckAsync(string kvkNumber, CancellationToken cancellationToken = default)
    {
        var digits = new string((kvkNumber ?? "").Where(char.IsDigit).ToArray());
        var url = string.Format(_deepLinkTemplate, Uri.EscapeDataString(digits));
        return Task.FromResult(new LenderProviderResult(
            LenderProviderOutcomes.Unknown,
            DeepLink: url,
            Note: "KvK heeft geen publieke Waadi-API; admin controleert via de KvK Waadi-check."));
    }

    public static string BuildDeepLink(string kvkNumber, string? template = null)
    {
        var digits = new string((kvkNumber ?? "").Where(char.IsDigit).ToArray());
        return string.Format(template ?? "https://www.kvk.nl/zoeken/?kvknummer={0}", Uri.EscapeDataString(digits));
    }
}
