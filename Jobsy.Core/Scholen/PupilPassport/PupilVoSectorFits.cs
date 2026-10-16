namespace Jobsy.Core.Scholen.PupilPassport;

public static class PupilVoSectorFits
{
    public static IReadOnlyList<PupilPassportSectorFit> Build(
        string? hollandCode,
        IReadOnlyList<string> likeKeys)
    {
        var letter = string.IsNullOrWhiteSpace(hollandCode) ? 'S' : char.ToUpperInvariant(hollandCode[0]);
        var primary = SectorFor(letter, 1, "Mijn eerste keus");
        var secondary = SectorFor(hollandCode?.Length > 1 ? hollandCode[1] : 'I', 2, "Ook interessant");
        var tertiary = SectorFor(LikeLetter(likeKeys), 3, "Wil ik in groeien");
        return [primary, secondary, tertiary];
    }

    private static char LikeLetter(IReadOnlyList<string> likes)
    {
        if (likes.Any(l => l is "dieren" or "natuur"))
        {
            return 'S';
        }

        if (likes.Any(l => l is "techniek" or "programmeren" or "computers"))
        {
            return 'I';
        }

        return 'A';
    }

    private static PupilPassportSectorFit SectorFor(char letter, int rank, string subtitle)
    {
        var key = char.ToUpperInvariant(letter);
        return new PupilPassportSectorFit(
            rank,
            PupilPassportCopy.Get($"LeerlingPassport.Vo.Sector.{key}.Title"),
            subtitle,
            [
                PupilPassportCopy.Get($"LeerlingPassport.Vo.Sector.{key}.B1"),
                PupilPassportCopy.Get($"LeerlingPassport.Vo.Sector.{key}.B2"),
                PupilPassportCopy.Get($"LeerlingPassport.Vo.Sector.{key}.B3"),
            ],
            [
                PupilPassportCopy.Get($"LeerlingPassport.Vo.Sector.{key}.Role1"),
                PupilPassportCopy.Get($"LeerlingPassport.Vo.Sector.{key}.Role2"),
            ]);
    }
}
