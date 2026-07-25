using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

public static class WorkTypeLabels
{
    public const int MaxPerVacancy = 2;

    public const string Horeca = "Horeca";
    public const string Winkel = "Winkel";
    public const string Logistiek = "Logistiek";
    public const string Tuinbouw = "Tuinbouw";
    public const string Zorg = "Zorg";
    public const string Kantoor = "Kantoor";
    public const string Bouw = "Bouw";
    public const string Schoonmaak = "Schoonmaak";
    public const string Productie = "Productie";

    public static readonly string[] All =
    [
        Horeca, Winkel, Logistiek, Tuinbouw, Zorg, Kantoor, Bouw, Schoonmaak, Productie
    ];

    public static WorkType Parse(string? label) => label?.Trim() switch
    {
        Horeca or "horeca" => WorkType.Horeca,
        Winkel or "winkel" or "retail" => WorkType.Winkel,
        Logistiek or "logistiek" => WorkType.Logistiek,
        Tuinbouw or "tuinbouw" => WorkType.Tuinbouw,
        Zorg or "zorg" => WorkType.Zorg,
        Kantoor or "kantoor" => WorkType.Kantoor,
        Bouw or "bouw" => WorkType.Bouw,
        Schoonmaak or "schoonmaak" => WorkType.Schoonmaak,
        Productie or "productie" => WorkType.Productie,
        _ => WorkType.None
    };

    public static string[] Expand(WorkType types)
    {
        var labels = new List<string>();
        if (types.HasFlag(WorkType.Horeca)) labels.Add(Horeca);
        if (types.HasFlag(WorkType.Winkel)) labels.Add(Winkel);
        if (types.HasFlag(WorkType.Logistiek)) labels.Add(Logistiek);
        if (types.HasFlag(WorkType.Tuinbouw)) labels.Add(Tuinbouw);
        if (types.HasFlag(WorkType.Zorg)) labels.Add(Zorg);
        if (types.HasFlag(WorkType.Kantoor)) labels.Add(Kantoor);
        if (types.HasFlag(WorkType.Bouw)) labels.Add(Bouw);
        if (types.HasFlag(WorkType.Schoonmaak)) labels.Add(Schoonmaak);
        if (types.HasFlag(WorkType.Productie)) labels.Add(Productie);
        return labels.ToArray();
    }

    public static WorkType Combine(IEnumerable<string>? labels)
    {
        var combined = WorkType.None;
        if (labels is null)
        {
            return combined;
        }

        foreach (var label in labels)
        {
            combined |= Parse(label);
        }

        return combined;
    }

    public static int CountFlags(WorkType types)
    {
        var n = (uint)types;
        var count = 0;
        while (n != 0)
        {
            n &= n - 1;
            count++;
        }

        return count;
    }

    public static bool IsValidSelection(WorkType types)
        => types != WorkType.None && CountFlags(types) <= MaxPerVacancy;

    public static bool MatchesFilter(WorkType vacancyTypes, string? selectedLabel)
    {
        if (string.IsNullOrWhiteSpace(selectedLabel))
        {
            return true;
        }

        var required = Parse(selectedLabel);
        if (required == WorkType.None)
        {
            return true;
        }

        return vacancyTypes.HasFlag(required);
    }
}
