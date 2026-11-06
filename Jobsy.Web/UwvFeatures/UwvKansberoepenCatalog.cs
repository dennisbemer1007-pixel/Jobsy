using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Uwv;
using Microsoft.AspNetCore.Hosting;

namespace Jobsy.Web.UwvFeatures;

public interface IUwvKansberoepenCatalog
{
    UwvKansberoepenDataset GetDefault();
}

public sealed class UwvKansberoepenCatalog(IWebHostEnvironment env) : IUwvKansberoepenCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private UwvKansberoepenDataset? _cached;

    public UwvKansberoepenDataset GetDefault()
    {
        if (_cached is not null)
        {
            return _cached;
        }

        var path = Path.Combine(env.WebRootPath, "data", "uwv", "kansberoepen-haaglanden.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("UWV kansberoepen dataset missing.", path);
        }

        var json = File.ReadAllText(path);
        var dto = JsonSerializer.Deserialize<KansJsonDto>(json, JsonOptions)
                  ?? throw new InvalidOperationException("Invalid kansberoepen JSON.");
        _cached = new UwvKansberoepenDataset(
            dto.DatasetId ?? "kansberoepen",
            dto.RegionKeyNl ?? "",
            dto.RegionKeyEn ?? "",
            dto.SourceLineNl ?? "",
            dto.SourceLineEn ?? "",
            dto.Entries?.Select(e => new UwvKansberoepEntry(
                e.Id ?? "",
                e.TitleNl ?? "",
                e.TitleEn ?? "",
                e.ProspectNl ?? "",
                e.ProspectEn ?? "",
                e.RouteNl ?? "",
                e.RouteEn ?? "",
                e.SectorIcon ?? "",
                e.Baangarantie,
                e.VacancyCount)).ToList() ?? []);
        return _cached;
    }

    private sealed class KansJsonDto
    {
        public string? DatasetId { get; set; }
        public string? RegionKeyNl { get; set; }
        public string? RegionKeyEn { get; set; }
        public string? SourceLineNl { get; set; }
        public string? SourceLineEn { get; set; }
        public List<KansEntryDto>? Entries { get; set; }
    }

    private sealed class KansEntryDto
    {
        public string? Id { get; set; }
        public string? TitleNl { get; set; }
        public string? TitleEn { get; set; }
        public string? ProspectNl { get; set; }
        public string? ProspectEn { get; set; }
        public string? RouteNl { get; set; }
        public string? RouteEn { get; set; }
        public string? SectorIcon { get; set; }

        [JsonPropertyName("baangarantie")]
        public bool Baangarantie { get; set; }

        public int VacancyCount { get; set; }
    }
}
