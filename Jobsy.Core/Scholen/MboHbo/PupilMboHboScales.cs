namespace Jobsy.Core.Scholen.MboHbo;

public enum PupilMboHboScaleKind
{
    /// <summary>Klopt niet … Klopt helemaal (tests 2, 3, 5).</summary>
    Agreement = 1,

    /// <summary>Lijkt me niks … Lijkt me heel leuk (beroepentest).</summary>
    Appeal = 2,

    /// <summary>Hoe leuk? / Hoe goed ben ik erin? (vakkenpakket).</summary>
    SubjectLeukGoed = 3
}

public static class PupilMboHboScaleLabels
{
    public static readonly string[] Agreement =
    [
        "Klopt niet",
        "Klopt meestal niet",
        "Klopt deels",
        "Klopt meestal",
        "Klopt helemaal"
    ];

    public static readonly string[] Appeal =
    [
        "Lijkt me niks",
        "Lijkt me minder",
        "Weet ik niet",
        "Lijkt me leuk",
        "Lijkt me heel leuk"
    ];

    public static readonly string[] SubjectLeuk = ["1", "2", "3", "4", "5"];
    public static readonly string[] SubjectGoed = ["1", "2", "3", "4", "5"];

    public static string Label(PupilMboHboScaleKind kind, int value)
    {
        if (value is < 1 or > 5)
        {
            return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        var idx = value - 1;
        return kind switch
        {
            PupilMboHboScaleKind.Agreement => Agreement[idx],
            PupilMboHboScaleKind.Appeal => Appeal[idx],
            _ => value.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
    }

    public static PupilMboHboScaleKind ForBlock(PupilMboHboBlock block)
        => block switch
        {
            PupilMboHboBlock.Vakken => PupilMboHboScaleKind.SubjectLeukGoed,
            PupilMboHboBlock.Beroepen => PupilMboHboScaleKind.Appeal,
            _ => PupilMboHboScaleKind.Agreement
        };
}
