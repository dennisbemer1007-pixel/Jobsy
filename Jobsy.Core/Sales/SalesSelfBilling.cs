using System.Security.Cryptography;
using System.Text;

namespace Jobsy.Core.Sales;

/// <summary>Self-billing consent version + text hash (D5). Separate from the partner agreement.</summary>
public static class SalesSelfBilling
{
    public const string CurrentVersion = "2026-10-self-billing-v1";

    /// <summary>
    /// Canonical Dutch consent text. Must match <c>Sales.SelfBilling.ConsentText</c> in UiStringsSales.
    /// Hash is taken of this exact string (UTF-8, no BOM).
    /// </summary>
    public const string ConsentText =
        "Lobsy maakt namens jou de facturen voor je commissie (self-billing / \"factuur uitgereikt door afnemer\"). "
        + "Je gaat akkoord met deze manier van factureren. Je controleert elke factuur en laat Lobsy binnen 14 dagen weten "
        + "als er iets niet klopt. Je laat Lobsy weten wanneer je btw-situatie verandert. Je kunt deze toestemming op elk "
        + "moment intrekken; daarna is een handmatige factuur via Lobsy-support nodig voor uitbetalingen.";

    public static string Sha256Of(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string CurrentTextSha256 => Sha256Of(ConsentText);
}
