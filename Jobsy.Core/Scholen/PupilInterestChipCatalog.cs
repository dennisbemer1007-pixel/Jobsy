namespace Jobsy.Core.Scholen;

/// <summary>Hobby / dislike chips for the pauze-eiland (sc-p3).</summary>
public static class PupilInterestChipCatalog
{
    public sealed record Chip(string Key, string LabelKey);

    /// <summary>Positive chips ("Wat vind jij leuk?").</summary>
    public static readonly IReadOnlyList<Chip> LikeChips =
    [
        new("sport", "Leerling.Chip.Sport"),
        new("buiten", "Leerling.Chip.Buiten"),
        new("dieren", "Leerling.Chip.Dieren"),
        new("gamen", "Leerling.Chip.Gamen"),
        new("tekenen", "Leerling.Chip.Tekenen"),
        new("muziek", "Leerling.Chip.Muziek"),
        new("koken", "Leerling.Chip.Koken"),
        new("fietsen-repareren", "Leerling.Chip.FietsenRepareren"),
        new("bouwen", "Leerling.Chip.Bouwen"),
        new("techniek", "Leerling.Chip.Techniek"),
        new("lezen", "Leerling.Chip.Lezen"),
        new("dansen", "Leerling.Chip.Dansen"),
        new("theater", "Leerling.Chip.Theater"),
        new("filmpjes", "Leerling.Chip.Filmpjes"),
        new("mode", "Leerling.Chip.Mode"),
        new("kleine-kinderen", "Leerling.Chip.KleineKinderen"),
        new("natuur", "Leerling.Chip.Natuur"),
        new("autos", "Leerling.Chip.Autos"),
        new("computers", "Leerling.Chip.Computers"),
        new("puzzels", "Leerling.Chip.Puzzels"),
        new("rekenen", "Leerling.Chip.Rekenen"),
        new("talen", "Leerling.Chip.Talen"),
        new("reizen", "Leerling.Chip.Reizen"),
        new("programmeren", "Leerling.Chip.Programmeren"),
    ];

    /// <summary>Dislike chips = likes plus school-life extras.</summary>
    public static readonly IReadOnlyList<Chip> DislikeExtraChips =
    [
        new("voor-de-klas", "Leerling.Chip.VoorDeKlas"),
        new("lang-stilzitten", "Leerling.Chip.LangStilzitten"),
        new("hard-werken-kou", "Leerling.Chip.HardWerkenKou"),
        new("veel-lezen", "Leerling.Chip.VeelLezen"),
        new("alleen-werken", "Leerling.Chip.AlleenWerken"),
        new("druk-lawaai", "Leerling.Chip.DrukLawaai"),
        new("vies-worden", "Leerling.Chip.ViesWorden"),
    ];

    public static IReadOnlyList<Chip> DislikeChips { get; } =
        LikeChips.Concat(DislikeExtraChips).ToList();

    public static readonly System.Text.RegularExpressions.Regex OtherWordPattern =
        new(@"^[\p{L} \-]{1,24}$", System.Text.RegularExpressions.RegexOptions.CultureInvariant | System.Text.RegularExpressions.RegexOptions.Compiled);

    public const int OtherWordMaxLength = 24;
    public const string OtherChipKey = "__other__";
}
