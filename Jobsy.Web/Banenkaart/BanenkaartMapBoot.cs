using System.Globalization;
using System.Text.Json;
using Jobsy.Core.Contracts;
using System.Text.Json.Serialization;
using Jobsy.Core.Rules;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;
using Jobsy.Web.Models;

namespace Jobsy.Web.Banenkaart;

internal static class BanenkaartMapBoot
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    internal static string BuildJson(
        CultureState culture,
        BanenkaartSsrQuery.State query,
        VacancyMapView opening,
        IReadOnlyList<Dictionary<string, object?>> pins,
        string pinsUrl,
        bool preferFilledLocation,
        bool isAuthenticated,
        double? originLat,
        double? originLng,
        bool zoomToPinsOutsideTravel,
        uint highlightSeed)
    {
        var payload = new Dictionary<string, object?>
        {
            ["preferFilledLocation"] = preferFilledLocation,
            ["illustratedPopup"] = true,
            ["zoomToPinsOutsideTravel"] = zoomToPinsOutsideTravel,
            ["view"] = new Dictionary<string, object?>
            {
                ["lat"] = opening.CenterLat,
                ["lng"] = opening.CenterLng,
                ["zoom"] = opening.Zoom
            },
            ["pins"] = pins,
            ["pinsUrl"] = pinsUrl,
            ["mapInit"] = new Dictionary<string, object?>
            {
                ["illustratedPopup"] = true,
                ["preferFilledLocation"] = preferFilledLocation,
                ["pinsUrl"] = pinsUrl,
                ["highlightSeed"] = highlightSeed,
                ["travel"] = new Dictionary<string, object?>
                {
                    ["maxMinutes"] = query.MaxTravelMinutes,
                    ["transport"] = query.Transport,
                    ["radiusKm"] = query.RadiusKm
                },
                ["labels"] = BuildLabels(culture, isAuthenticated),
                ["illustratedTravelSpot"] = CandidateIllustratedImages.PopupTravelAccent,
                ["illustratedVacancySpots"] = CandidateIllustratedImages.BanenkaartIllustratedSpots
                    .ToDictionary(static kv => kv.Key, static kv => kv.Value, StringComparer.OrdinalIgnoreCase),
                ["view"] = new Dictionary<string, object?>
                {
                    ["lat"] = opening.CenterLat,
                    ["lng"] = opening.CenterLng,
                    ["zoom"] = opening.Zoom
                }
            }
        };

        if (originLat is double lat && originLng is double lng)
        {
            var origin = new Dictionary<string, object?>
            {
                ["lat"] = lat,
                ["lng"] = lng,
                ["zoom"] = VacancyMapViewCalculator.FilledLocationZoom
            };
            payload["origin"] = origin;
            if (payload["mapInit"] is Dictionary<string, object?> mapInit)
            {
                mapInit["origin"] = origin;
            }
        }

        return JsonSerializer.Serialize(payload, JsonOptions)
            .Replace("<", "\\u003c", StringComparison.Ordinal);
    }

    internal static string BuildPinsApiUrl(BanenkaartSsrQuery.State query, bool isCandidate)
    {
        var qs = new List<string>
        {
            $"transport={Uri.EscapeDataString(query.Transport)}",
            $"maxMinutes={query.MaxTravelMinutes}"
        };

        if (query.OriginLat is double lat && query.OriginLng is double lng)
        {
            qs.Add($"originLat={lat.ToString(CultureInfo.InvariantCulture)}");
            qs.Add($"originLng={lng.ToString(CultureInfo.InvariantCulture)}");
            qs.Add($"radiusKm={query.RadiusKm.ToString(CultureInfo.InvariantCulture)}");
        }

        foreach (var wt in query.WorkTypes)
        {
            qs.Add($"workType={Uri.EscapeDataString(wt)}");
        }

        if (!string.IsNullOrWhiteSpace(query.SearchQuery))
        {
            qs.Add($"q={Uri.EscapeDataString(query.SearchQuery.Trim())}");
        }

        foreach (var id in query.CategoryIds)
        {
            qs.Add($"categoryId={id:D}");
        }

        foreach (var id in query.CompanyIds)
        {
            qs.Add($"companyId={id:D}");
        }

        if (isCandidate && query.MinMatchPercent > 0)
        {
            qs.Add($"minMatchPercent={query.MinMatchPercent}");
        }

        qs.Add("scope=map");
        return BanenkaartClientPins.Path + "?" + string.Join("&", qs);
    }

    internal static List<Dictionary<string, object?>> ToBootPins(IEnumerable<VacancyPinBootItem> pins) =>
        pins.Select(ToBootPinDict).ToList();

    internal static List<Dictionary<string, object?>> ToBootPinsFromList(IEnumerable<VacancyListItem> source) =>
        source
            .Where(v => double.IsFinite(v.Latitude) && double.IsFinite(v.Longitude))
            .Select(v => ToBootPinDict(new VacancyPinBootItem
            {
                Id = v.Id,
                Lat = v.Latitude,
                Lng = v.Longitude,
                Colour = v.CategoryColorHex,
                WorkType = v.WorkTypes is { Length: > 0 } ? v.WorkTypes[0] : null,
                PublicMapRadiusKm = v.PublicMapRadiusKm
            }))
            .ToList();

    private static Dictionary<string, object?> ToBootPinDict(VacancyPinBootItem p)
    {
        var dict = new Dictionary<string, object?>
        {
            ["id"] = p.Id.ToString("D"),
            ["lat"] = p.Lat,
            ["lng"] = p.Lng
        };

        if (!string.IsNullOrWhiteSpace(p.Colour))
        {
            dict["colour"] = p.Colour;
        }

        if (!string.IsNullOrWhiteSpace(p.WorkType))
        {
            dict["workType"] = p.WorkType;
        }

        if (p.PublicMapRadiusKm is int radiusKm && radiusKm > 0)
        {
            dict["publicMapRadiusKm"] = radiusKm;
        }

        return dict;
    }

    private static Dictionary<string, object?> BuildLabels(CultureState culture, bool isAuthenticated) => new()
    {
        ["vacanciesAtPlace"] = culture["Kb.Map.JobsAtPlace"],
        ["vacancyAtPlace"] = culture["Kb.Map.JobAtPlace"],
        ["vacanciesAtPlaceWithPlace"] = culture["Kb.Map.JobsAtPlaceWithPlace"],
        ["vacancyAtPlaceWithPlace"] = culture["Kb.Map.JobAtPlaceWithPlace"],
        ["vacanciesInView"] = culture["Map.VacanciesInView"],
        ["prevVacancy"] = culture["Map.PrevVacancy"],
        ["nextVacancy"] = culture["Map.NextVacancy"],
        ["vacancyOf"] = culture["Map.VacancyOf"],
        ["close"] = culture["Common.Close"],
        ["apply"] = culture["Map.Apply"],
        ["featured"] = culture["Discovery.Featured"],
        ["unavailableTitle"] = culture["Map.UnavailableTitle"],
        ["unavailableHint"] = culture["Map.UnavailableHint"],
        ["retry"] = culture["Common.Retry"],
        ["view"] = culture["Common.Open"],
        ["viewJob"] = culture["Kb.Map.ViewJob"],
        ["whyPrefix"] = culture["Kb.Map.WhyPrefix"],
        ["travelFromHome"] = culture["Kb.Map.TravelFromHome"],
        ["save"] = culture["Kb.Map.Save"],
        ["saved"] = culture["Kb.Map.Saved"],
        ["hoursSingle"] = culture["Kb.Map.HoursSingle"],
        ["hoursRange"] = culture["Kb.Map.HoursRange"],
        ["passportHref"] = KbRoutes.PaspoortTests,
        ["pagerNav"] = culture["Map.PagerNav"],
        ["noVacancies"] = culture["Kb.Map.NoVacancies"],
        ["vacancyFallback"] = culture["Kb.Map.VacancyFallback"],
        ["transportBike"] = culture["Transport.Verb.Bike"],
        ["transportCar"] = culture["Transport.Verb.Car"],
        ["transportTransit"] = culture["Transport.Verb.Transit"],
        ["transportWalk"] = culture["Transport.Verb.Walk"],
        ["transportDefault"] = culture["Transport.Verb.Default"],
        ["noVacanciesInTravel"] = culture["Kb.Map.NoVacanciesInTravel"],
        ["canSave"] = isAuthenticated
    };
}
