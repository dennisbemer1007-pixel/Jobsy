using System.Text.RegularExpressions;

namespace Jobsy.Web.Services;

/// <summary>Parsed Dutch home-address text for PDOK Locatieserver requests.</summary>
public sealed partial class PdokAddressQuery
{
    public required string Raw { get; init; }

    public string? PostcodeCompact { get; init; }

    public int? HouseNumber { get; init; }

    public string? CityHint { get; init; }

    public string? StreetHint { get; init; }

    public bool HasHouseNumber => HouseNumber is > 0;

    public static PdokAddressQuery Parse(string? query)
    {
        var raw = (query ?? string.Empty).Trim();
        if (raw.Length == 0)
        {
            return new PdokAddressQuery { Raw = raw };
        }

        var leading = LeadingPostcode().Match(raw);
        if (leading.Success && PostcodeMatch.Compact(leading.Groups["pc"].Value) is { } pc)
        {
            var rest = leading.Groups["rest"].Value.Trim().TrimStart(',', ' ');
            var (num, afterNum) = TryTakeHouseNumber(rest);
            var city = string.IsNullOrWhiteSpace(afterNum) ? null : afterNum.Trim();
            return new PdokAddressQuery
            {
                Raw = raw,
                PostcodeCompact = pc,
                HouseNumber = num,
                CityHint = city,
            };
        }

        var comma = raw.IndexOf(',', StringComparison.Ordinal);
        if (comma >= 0)
        {
            var streetPart = raw[..comma].Trim();
            var cityHint = raw[(comma + 1)..].Trim();
            var (street, num) = SplitStreetAndNumber(streetPart);
            return new PdokAddressQuery
            {
                Raw = raw,
                StreetHint = street,
                HouseNumber = num,
                CityHint = string.IsNullOrWhiteSpace(cityHint) ? null : cityHint,
            };
        }

        var (streetOnly, houseNum) = SplitStreetAndNumber(raw);
        return new PdokAddressQuery
        {
            Raw = raw,
            StreetHint = streetOnly,
            HouseNumber = houseNum,
        };
    }

    internal string BuildPdokQueryString()
    {
        if (PostcodeCompact is not null && HasHouseNumber && string.IsNullOrWhiteSpace(StreetHint))
        {
            return FormattableString.Invariant($"postcode:{PostcodeCompact} AND huisnummer:{HouseNumber}");
        }

        return Raw;
    }

    internal string BuildFilterQuery()
    {
        if (HasHouseNumber || (PostcodeCompact is not null && HasHouseNumber))
        {
            return "type:adres";
        }

        if (!string.IsNullOrWhiteSpace(StreetHint) && !HasHouseNumber)
        {
            return "type:(adres OR weg)";
        }

        return "type:(adres OR postcode OR weg OR woonplaats)";
    }

    private static (string? Street, int? Number) SplitStreetAndNumber(string streetPart)
    {
        var match = StreetHouse().Match(streetPart.Trim());
        if (!match.Success)
        {
            return (streetPart.Trim(), null);
        }

        var street = match.Groups["street"].Value.Trim();
        if (int.TryParse(match.Groups["num"].Value, out var num))
        {
            return (string.IsNullOrWhiteSpace(street) ? streetPart.Trim() : street, num);
        }

        return (streetPart.Trim(), null);
    }

    private static (int? Number, string Remainder) TryTakeHouseNumber(string rest)
    {
        if (string.IsNullOrWhiteSpace(rest))
        {
            return (null, string.Empty);
        }

        var match = LeadingHouse().Match(rest);
        if (!match.Success || !int.TryParse(match.Groups["num"].Value, out var num))
        {
            return (null, rest);
        }

        return (num, rest[match.Length..].Trim());
    }

    [GeneratedRegex(@"^(?<pc>[1-9][0-9]{3}\s?[A-Za-z]{2})(?:\s+|,\s*)(?<rest>.*)$", RegexOptions.CultureInvariant)]
    private static partial Regex LeadingPostcode();

    [GeneratedRegex(@"^(?<num>[1-9][0-9]{0,4}[A-Za-z]?)(?:\s+|$)", RegexOptions.CultureInvariant)]
    private static partial Regex LeadingHouse();

    [GeneratedRegex(@"^(?<street>.+?)\s+(?<num>[1-9][0-9]{0,4}[A-Za-z]?)$", RegexOptions.CultureInvariant)]
    private static partial Regex StreetHouse();
}
