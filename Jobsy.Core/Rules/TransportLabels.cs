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

    /// <summary>Known modes in <paramref name="stored"/>, in selectable order. Empty when nothing matches.</summary>
    public static IReadOnlyList<string> SplitMany(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
        {
            return [];
        }

        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in stored.Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var canonical = TryCanonical(part);
            if (canonical is not null)
            {
                found.Add(canonical);
            }
        }

        return Selectable.Where(found.Contains).ToArray();
    }

    /// <summary>Stable stored form, for example "Fiets, Auto". Empty when nothing is selected.</summary>
    public static string JoinMany(IEnumerable<string>? labels)
        => string.Join(", ", SplitMany(labels is null ? null : string.Join(", ", labels)));

    public static string Normalize(string? stored) => JoinMany(SplitMany(stored));

    public static string Toggle(string? stored, string label)
    {
        var canonical = TryCanonical(label);
        var current = SplitMany(stored).ToList();
        if (canonical is null)
        {
            return JoinMany(current);
        }

        if (!current.Remove(canonical))
        {
            current.Add(canonical);
        }

        return JoinMany(current);
    }

    /// <summary>Flags for every selected mode. A single label stays one flag; empty falls back to bike.</summary>
    public static TransportMode ParseMany(string? stored)
    {
        var parts = SplitMany(stored);
        if (parts.Count == 0)
        {
            return TransportMode.Bike;
        }

        var mode = TransportMode.None;
        foreach (var part in parts)
        {
            mode |= Parse(part);
        }

        return mode == TransportMode.None ? TransportMode.Bike : mode;
    }

    /// <summary>Known label, or null when the text is not a transport mode.</summary>
    public static string? TryCanonical(string? label)
    {
        var value = label?.Trim() ?? "";
        if (value.Length == 0)
        {
            return null;
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

        return null;
    }

    /// <summary>Stored UI label: E-bike stays E-bike; unknown values fall back to Fiets.</summary>
    public static string Canonical(string? label) => TryCanonical(label) ?? Bike;

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
