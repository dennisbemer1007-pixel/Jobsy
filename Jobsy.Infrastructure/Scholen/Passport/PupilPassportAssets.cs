namespace Jobsy.Infrastructure.Scholen.Passport;

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
}
