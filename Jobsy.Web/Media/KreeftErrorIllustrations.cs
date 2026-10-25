namespace Jobsy.Web.Media;

public static class KreeftErrorIllustrations
{
    public const string Version = "53863b26-kreeft-v4";

    private const string Base = "/images/kreeft/illustraties";

    public static string NotFoundScene => $"{Base}/fout-404.webp?v={Version}";

    public static string NotFoundCrab => $"{Base}/kreeft-kaart.webp?v={Version}";

    public static string GeneralFaultScene => $"{Base}/fout-algemeen.webp?v={Version}";

    public static string GeneralFaultCrab => $"{Base}/kreeft-wachten.webp?v={Version}";
}
