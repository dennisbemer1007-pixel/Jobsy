using Jobsy.Core.Enums;

namespace Jobsy.Core.Scholen;

/// <summary>Resolves pupil story copy for web/PDF when language is not Dutch.</summary>
public static class PupilVerhaalLocalized
{
    public static string Get(string key, string language)
        => Get(key, language, DefaultContext);

    public static string Get(string key, string language, PupilClassContext ctx)
    {
        if (!IsEnglish(language))
        {
            return PupilVerhaalCopy.Get(key, ctx);
        }

        var levelKey = key + LevelSuffix(ctx.Level);
        if (levelKey != key && PupilVerhaalEnglish.TryGet(levelKey, out var levelEn))
        {
            return levelEn;
        }

        if (ctx.Set == PupilQuestionSet.Vo)
        {
            var voKey = key + ".Vo";
            if (PupilVerhaalEnglish.TryGet(voKey, out var voEn))
            {
                return voEn;
            }
        }

        if (PupilVerhaalEnglish.TryGet(key, out var en))
        {
            return en;
        }

        if (key.StartsWith("LeerlingDroom.", StringComparison.Ordinal))
        {
            var nl = PupilVerhaalCopy.Get(key, ctx);
            return PupilVerhaalEnglishDreamFallback.FromDutch(nl);
        }

        return PupilVerhaalCopy.Get(key, ctx);
    }

    public static bool IsEnglish(string? language)
        => !string.IsNullOrWhiteSpace(language)
           && language.StartsWith("en", StringComparison.OrdinalIgnoreCase);

    private static readonly PupilClassContext DefaultContext =
        new(SchoolLevel.Groep78, 8, PupilQuestionSet.Groep78);

    private static string LevelSuffix(SchoolLevel level) => level switch
    {
        SchoolLevel.Havo => ".Havo",
        SchoolLevel.Vwo => ".Vwo",
        SchoolLevel.Groep78 => ".G78",
        SchoolLevel.Mavo => ".Mavo",
        SchoolLevel.VmboB or SchoolLevel.VmboK or SchoolLevel.VmboGt => ".Vmbo",
        _ => ""
    };
}
