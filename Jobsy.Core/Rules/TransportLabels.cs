using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

public static class TransportLabels
{
    public const string Bike = "Fiets";
    public const string EBike = "E-bike";
    public const string Car = "Auto";
    public const string PublicTransport = "OV";
    public const string Walking = "Lopend";

    /// <summary>Selectable modes in candidate UI (E-bike routes as bike).</summary>
    public static readonly string[] Selectable = [Bike, EBike, Car, PublicTransport, Walking];

    public static TransportMode Parse(string? label)
    {
        var canonical = Canonical(label);
        return canonical switch
        {
            Car => TransportMode.Car,
            PublicTransport => TransportMode.PublicTransport,
            Walking => TransportMode.Walking,
            _ => TransportMode.Bike
        };
    }

    /// <summary>Stored UI label: E-bike stays E-bike; unknown values fall back to Fiets.</summary>
    public static string Canonical(string? label)
    {
        var value = label?.Trim() ?? "";
        if (value.Length == 0)
        {
            return Bike;
        }

        if (value.Equals(Car, StringComparison.OrdinalIgnoreCase)
            || value.Equals("car", StringComparison.OrdinalIgnoreCase))
        {
            return Car;
        }

        if (value.Equals(PublicTransport, StringComparison.OrdinalIgnoreCase)
            || value.Equals("transit", StringComparison.OrdinalIgnoreCase)
            || value.Equals("public transport", StringComparison.OrdinalIgnoreCase))
        {
            return PublicTransport;
        }

        if (value.Equals(Walking, StringComparison.OrdinalIgnoreCase)
            || value.Equals("walk", StringComparison.OrdinalIgnoreCase)
            || value.Equals("walking", StringComparison.OrdinalIgnoreCase)
            || value.Equals("lopend", StringComparison.OrdinalIgnoreCase))
        {
            return Walking;
        }

        if (IsEBike(value))
        {
            return EBike;
        }

        if (value.Equals(Bike, StringComparison.OrdinalIgnoreCase)
            || value.Equals("bike", StringComparison.OrdinalIgnoreCase)
            || value.Equals("bicycle", StringComparison.OrdinalIgnoreCase)
            || value.Equals("fiets", StringComparison.OrdinalIgnoreCase))
        {
            return Bike;
        }

        return Bike;
    }

    public static bool IsEBike(string? label)
    {
        var value = label?.Trim() ?? "";
        if (value.Length == 0)
        {
            return false;
        }

        var compact = value.Replace(" ", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal);
        return compact.Equals("ebike", StringComparison.OrdinalIgnoreCase)
               || compact.Equals("ebikes", StringComparison.OrdinalIgnoreCase);
    }

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

        var selected = Parse(selectedLabel);
        return requiredTransport.Any(required => Parse(required) == selected);
    }
}
