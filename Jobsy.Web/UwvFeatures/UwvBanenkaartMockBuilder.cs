using Jobsy.Web.Media;
using Jobsy.Web.Models;
using Jobsy.Web.UwvMockups;

namespace Jobsy.Web.UwvFeatures;

/// <summary>Sample kansberoep vacancies + map pins for UWV mock banenkaart layer.</summary>
public static class UwvBanenkaartMockBuilder
{
    private static readonly Guid CompanyId = Guid.Parse("c1000000-0000-0000-0000-000000000001");

    private static readonly (Guid Id, double LatOffset, double LngOffset, int Index)[] PinLayout =
    [
        (Guid.Parse("c1000001-0000-0000-0000-000000000001"), 0.004, -0.006, 0),
        (Guid.Parse("c1000002-0000-0000-0000-000000000002"), -0.003, 0.005, 1),
        (Guid.Parse("c1000003-0000-0000-0000-000000000003"), -0.005, -0.004, 2),
    ];

    public static IReadOnlyList<VacancyListItem> BuildVacancies(bool english, double originLat, double originLng)
    {
        var cards = UwvMockupData.Kansberoepen;
        var map = UwvMockupData.MapVacancies;
        var list = new List<VacancyListItem>(PinLayout.Length);
        for (var i = 0; i < PinLayout.Length; i++)
        {
            var layout = PinLayout[i];
            var card = cards[layout.Index];
            var sample = map[Math.Min(i, map.Count - 1)];
            var title = english ? card.TitleEn : card.TitleNl;
            if (layout.Index == 2 && !title.Contains("BBL", StringComparison.OrdinalIgnoreCase))
            {
                title += english ? " (apprenticeship)" : " BBL";
            }

            list.Add(new VacancyListItem
            {
                Id = layout.Id,
                Title = title,
                Description = english ? "Sample opportunity occupation (UWV mock)." : "Voorbeeld-kansberoep (UWV mock).",
                CompanyName = english ? sample.CompanyEn : sample.CompanyNl,
                CompanyAddress = english ? sample.LocationEn : sample.LocationNl,
                CompanyId = CompanyId,
                ImageUrl = sample.ImageUrl,
                Latitude = originLat + layout.LatOffset,
                Longitude = originLng + layout.LngOffset,
                TravelMinutes = 15 + i * 3,
                WorkTypes = ["BBL"],
                CategoryColorHex = "#c9a227",
                MatchPercent = 82,
                MatchColorBand = "good",
                FitWhyLine = english ? card.ProspectEn : card.ProspectNl,
                WageVisible = false,
                Status = "Active",
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddMonths(3)),
            });
        }

        return list;
    }

    public static List<Dictionary<string, object?>> BuildPinPayload(
        IEnumerable<VacancyListItem> vacancies)
    {
        const string kansGold = "#c9a227";
        return vacancies
            .Where(v => double.IsFinite(v.Latitude) && double.IsFinite(v.Longitude))
            .Select(v => new Dictionary<string, object?>
            {
                ["id"] = v.Id.ToString("D"),
                ["lat"] = v.Latitude,
                ["lng"] = v.Longitude,
                ["colour"] = kansGold,
                ["categoryColor"] = kansGold,
                ["highlighted"] = true,
                ["kansberoep"] = true,
                ["workType"] = v.WorkTypes is { Length: > 0 } ? v.WorkTypes[0] : "BBL",
                ["matchPercent"] = v.MatchPercent,
                ["matchColorBand"] = v.MatchColorBand ?? "good",
            })
            .ToList();
    }
}
