using QuestPDF.Drawing;

namespace Jobsy.Infrastructure.Scholen.Passport;

internal static class PupilPassportFonts
{
    internal const string Nunito = "Nunito";
    internal const string Fredoka = "Fredoka SemiBold";
    internal const string Caveat = "Caveat";

    private static int _registered;

    internal static void EnsureRegistered()
    {
        if (Interlocked.Exchange(ref _registered, 1) == 1)
        {
            return;
        }

        Register("Jobsy.Infrastructure.Assets.Fonts.Nunito-Regular.ttf");
        Register("Jobsy.Infrastructure.Assets.Fonts.Nunito-Bold.ttf");
        Register("Jobsy.Infrastructure.Assets.Fonts.Fredoka-SemiBold.ttf");
        Register("Jobsy.Infrastructure.Assets.Fonts.Caveat-Regular.ttf");
    }

    private static void Register(string resource)
    {
        using var stream = typeof(PupilPassportFonts).Assembly.GetManifestResourceStream(resource);
        if (stream is null)
        {
            return;
        }

        FontManager.RegisterFont(stream);
    }
}
