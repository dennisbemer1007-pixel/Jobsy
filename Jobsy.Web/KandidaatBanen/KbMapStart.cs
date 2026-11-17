using Jobsy.Core.Rules;

namespace Jobsy.Web.KandidaatBanen;

/// <summary>
/// Pure launch-origin precedence for the banenkaart (file 03 / D5).
/// Home/stored origins use candidate transport prefs when present; else Fiets · 20.
/// </summary>
public static class KbMapStart
{
    public const string DefaultTransport = "Fiets";
    public const int DefaultMaxTravelMinutes = 20;
    public const string SessionStorageKey = "jobsy.kb.origin";

    public sealed record OriginPoint(double Latitude, double Longitude, string? Label);

    public sealed record Input(
        OriginPoint? UrlOrStateOrigin,
        OriginPoint? SessionOrigin,
        double? HomeLatitude,
        double? HomeLongitude,
        string? HomeAddressLabel,
        OriginPoint? StoredOrigin,
        bool IsLoggedInCandidate,
        string? ProfilePreferredTransport = null,
        int? ProfileMaxTravelMinutes = null);

    public sealed record Result(
        OriginPoint? Origin,
        string Transport,
        int MaxTravelMinutes,
        bool ShowLocationPrompt,
        string Source);

    public static Result Resolve(Input input)
    {
        if (IsUsable(input.UrlOrStateOrigin))
        {
            return Apply(input.UrlOrStateOrigin!, "url", input);
        }

        if (IsUsable(input.SessionOrigin))
        {
            return Apply(input.SessionOrigin!, "session", input);
        }

        if (input.IsLoggedInCandidate
            && input.HomeLatitude is double lat
            && input.HomeLongitude is double lng
            && IsFinite(lat)
            && IsFinite(lng))
        {
            var label = string.IsNullOrWhiteSpace(input.HomeAddressLabel)
                ? null
                : input.HomeAddressLabel.Trim();
            return Apply(new OriginPoint(lat, lng, label), "home", input);
        }

        if (IsUsable(input.StoredOrigin))
        {
            return Apply(input.StoredOrigin!, "stored", input);
        }

        return new Result(
            Origin: null,
            Transport: DefaultTransport,
            MaxTravelMinutes: DefaultMaxTravelMinutes,
            ShowLocationPrompt: true,
            Source: "prompt");
    }

    private static Result Apply(OriginPoint origin, string source, Input input)
    {
        var useProfilePrefs = source is "home" or "stored";
        var transport = useProfilePrefs && !string.IsNullOrWhiteSpace(input.ProfilePreferredTransport)
            ? TransportLabels.Normalize(input.ProfilePreferredTransport)
            : DefaultTransport;
        var minutes = useProfilePrefs
                       && input.ProfileMaxTravelMinutes is int m
                       && m is >= 5 and <= 90
            ? m
            : DefaultMaxTravelMinutes;
        return new Result(origin, transport, minutes, ShowLocationPrompt: false, source);
    }

    private static bool IsUsable(OriginPoint? origin) =>
        origin is not null
        && IsFinite(origin.Latitude)
        && IsFinite(origin.Longitude);

    private static bool IsFinite(double value) =>
        !double.IsNaN(value) && !double.IsInfinity(value);
}
