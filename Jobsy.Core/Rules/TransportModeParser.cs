using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>Parse one or more transport labels into a flags enum (CSV / API helpers).</summary>
public static class TransportModeParser
{
    public static TransportMode ParseMany(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return TransportMode.Bike | TransportMode.PublicTransport;
        }

        var parts = VacancyCsvParser.SplitMultiValue(raw);
        if (parts.Length == 0)
        {
            return TransportMode.Bike | TransportMode.PublicTransport;
        }

        TransportMode mode = TransportMode.None;
        foreach (var part in parts)
        {
            mode |= ParseOne(part);
        }

        return mode == TransportMode.None
            ? TransportMode.Bike | TransportMode.PublicTransport
            : mode;
    }

    public static bool TryParseMany(string? raw, out TransportMode mode, out string? error)
    {
        error = null;
        mode = TransportMode.None;
        if (string.IsNullOrWhiteSpace(raw))
        {
            mode = TransportMode.Bike | TransportMode.PublicTransport;
            return true;
        }

        var parts = VacancyCsvParser.SplitMultiValue(raw);
        if (parts.Length == 0)
        {
            mode = TransportMode.Bike | TransportMode.PublicTransport;
            return true;
        }

        foreach (var part in parts)
        {
            if (!TryParseOne(part, out var flag))
            {
                error = $"Onbekend vervoermiddel '{part}'. Gebruik: Fiets, Auto, OV, Lopend.";
                mode = TransportMode.None;
                return false;
            }

            mode |= flag;
        }

        return true;
    }

    private static TransportMode ParseOne(string label) =>
        TryParseOne(label, out var mode) ? mode : TransportMode.Bike;

    private static bool TryParseOne(string label, out TransportMode mode)
    {
        switch (label.Trim().ToLowerInvariant())
        {
            case "fiets":
            case "bike":
            case "bicycle":
                mode = TransportMode.Bike;
                return true;
            case "auto":
            case "car":
                mode = TransportMode.Car;
                return true;
            case "ov":
            case "publictransport":
            case "public_transport":
            case "openbaar vervoer":
                mode = TransportMode.PublicTransport;
                return true;
            case "lopend":
            case "walking":
            case "walk":
            case "te voet":
                mode = TransportMode.Walking;
                return true;
            default:
                mode = TransportMode.None;
                return false;
        }
    }
}
