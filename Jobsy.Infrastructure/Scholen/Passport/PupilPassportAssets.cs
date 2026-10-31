namespace Jobsy.Infrastructure.Scholen.Passport;

internal enum PupilPassportG78Art
{
    None,
    KreeftKijker,
    KreeftSchild,
    KreeftKaart
}

internal static class PupilPassportAssets
{
    internal static byte[] MascotPng()
    {
        using var stream = typeof(PupilPassportAssets).Assembly
            .GetManifestResourceStream("Jobsy.Infrastructure.Assets.Brand.mascot-128.png");
        if (stream is null)
        {
            return [];
        }

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    internal static byte[] LogoPng()
    {
        using var stream = typeof(PupilPassportAssets).Assembly
            .GetManifestResourceStream("Jobsy.Infrastructure.Assets.lobsy.png");
        if (stream is null)
        {
            return [];
        }

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    internal static string LogoHorizontalSvg()
        => ReadText("Jobsy.Infrastructure.Assets.lobsy-logo-horizontaal.svg");

    internal static byte[] Groep78ArtPng(PupilPassportG78Art art)
    {
        var name = art switch
        {
            PupilPassportG78Art.KreeftKijker => "Jobsy.Infrastructure.Assets.g78-kreeft-kijker.png",
            PupilPassportG78Art.KreeftSchild => "Jobsy.Infrastructure.Assets.g78-kreeft-schild.png",
            PupilPassportG78Art.KreeftKaart => "Jobsy.Infrastructure.Assets.g78-kreeft-kaart.png",
            _ => null
        };

        if (name is null)
        {
            return [];
        }

        using var stream = typeof(PupilPassportAssets).Assembly.GetManifestResourceStream(name);
        if (stream is null)
        {
            return [];
        }

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    private static string ReadText(string resourceName)
    {
        using var stream = typeof(PupilPassportAssets).Assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return "";
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
