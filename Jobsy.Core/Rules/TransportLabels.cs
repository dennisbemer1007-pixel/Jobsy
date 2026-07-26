using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

public static class TransportLabels
{
    public const string Bike = "Fiets";
    public const string Car = "Auto";
    public const string PublicTransport = "OV";
    public const string Walking = "Lopend";

    public static TransportMode Parse(string? label) => label?.Trim() switch
    {
        Car => TransportMode.Car,
        PublicTransport => TransportMode.PublicTransport,
        Walking => TransportMode.Walking,
        _ => TransportMode.Bike
    };

    public static string[] Expand(TransportMode mode)
    {
        var labels = new List<string>();
        if (mode.HasFlag(TransportMode.Walking)) labels.Add(Walking);
        if (mode.HasFlag(TransportMode.Bike)) labels.Add(Bike);
        if (mode.HasFlag(TransportMode.Car)) labels.Add(Car);
        if (mode.HasFlag(TransportMode.PublicTransport)) labels.Add(PublicTransport);
        return labels.ToArray();
    }

    public static bool MatchesRequired(string[] requiredTransport, string selectedLabel)
    {
        // No required modes ⇒ reachable by any transport the candidate chooses.
        if (requiredTransport is not { Length: > 0 })
        {
            return true;
        }

        return requiredTransport.Contains(selectedLabel, StringComparer.OrdinalIgnoreCase);
    }
}
