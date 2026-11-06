using System.Globalization;
using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Microsoft.EntityFrameworkCore;
using Jobsy.Infrastructure.Data;

namespace Jobsy.Infrastructure.Training;

public sealed class TrainingPartnerFeedImporter(JobsyDbContext db)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<int> ImportAsync(TrainingProvider provider, CancellationToken cancellationToken = default)
    {
        if (provider.FeedKind == TrainingPartnerFeedKind.None || string.IsNullOrWhiteSpace(provider.FeedUrl))
        {
            return 0;
        }

        var rows = provider.FeedKind switch
        {
            TrainingPartnerFeedKind.Json => await LoadJsonAsync(provider.FeedUrl, cancellationToken),
            TrainingPartnerFeedKind.Csv => await LoadCsvAsync(provider.FeedUrl, cancellationToken),
            _ => []
        };

        var upserted = 0;
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.ExternalId) || string.IsNullOrWhiteSpace(row.Title))
            {
                continue;
            }

            var offer = await db.TrainingOffers
                .FirstOrDefaultAsync(
                    o => o.ProviderId == provider.Id && o.PartnerExternalId == row.ExternalId,
                    cancellationToken);
            if (offer is null)
            {
                offer = new TrainingOffer
                {
                    Id = Guid.NewGuid(),
                    ProviderId = provider.Id,
                    PartnerExternalId = row.ExternalId.Trim(),
                    ShowInPassport = row.Section == TrainingOfferSection.Growth,
                    IsPartner = true
                };
                db.TrainingOffers.Add(offer);
            }

            ApplyRow(offer, row);
            offer.ImportedAtUtc = DateTime.UtcNow;
            offer.UpdatedAtUtc = DateTime.UtcNow;
            upserted++;
        }

        provider.LastImportedAtUtc = DateTime.UtcNow;
        provider.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return upserted;
    }

    private static void ApplyRow(TrainingOffer offer, PartnerFeedRow row)
    {
        offer.Title = row.Title.Trim();
        offer.ExternalPath = row.ExternalPath?.Trim();
        offer.FieldsCsv = TrainingMatchRules.JoinCsv(row.Fields);
        offer.KeysCsv = TrainingMatchRules.JoinCsv(row.Keys);
        offer.Type = row.Type;
        offer.DurationValue = row.DurationValue;
        offer.DurationUnit = row.DurationUnit;
        offer.Delivery = row.Delivery;
        offer.Location = row.Location;
        offer.IsFree = row.IsFree;
        offer.PriceEuro = row.PriceEuro;
        offer.PriceLabel = row.PriceLabel;
        offer.Section = row.Section;
        offer.CompetenciesCsv = TrainingMatchRules.JoinCsv(row.Competencies);
        offer.CareerStepsCsv = TrainingMatchRules.JoinCsv(row.CareerSteps);
        offer.ReasonNl = row.ReasonNl;
        offer.ReasonEn = row.ReasonEn;
        offer.LearnBulletsJson = row.LearnBullets.Count == 0
            ? null
            : JsonSerializer.Serialize(row.LearnBullets, JsonOptions);
        offer.StartsOn = row.StartsOn;
        offer.IsActive = true;
        offer.SortOrder = row.SortOrder;
    }

    private static async Task<IReadOnlyList<PartnerFeedRow>> LoadJsonAsync(string url, CancellationToken ct)
    {
        if (url.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith('/'))
        {
            var path = url.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
                ? new Uri(url).LocalPath
                : url;
            await using var fs = File.OpenRead(path);
            var list = await JsonSerializer.DeserializeAsync<List<PartnerFeedRow>>(fs, JsonOptions, ct);
            return list ?? [];
        }

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        await using var stream = await http.GetStreamAsync(url, ct);
        var remote = await JsonSerializer.DeserializeAsync<List<PartnerFeedRow>>(stream, JsonOptions, ct);
        return remote ?? [];
    }

    private static async Task<IReadOnlyList<PartnerFeedRow>> LoadCsvAsync(string url, CancellationToken ct)
    {
        string text;
        if (url.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith('/'))
        {
            var path = url.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
                ? new Uri(url).LocalPath
                : url;
            text = await File.ReadAllTextAsync(path, ct);
        }
        else
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            text = await http.GetStringAsync(url, ct);
        }

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length <= 1)
        {
            return [];
        }

        var rows = new List<PartnerFeedRow>();
        for (var i = 1; i < lines.Length; i++)
        {
            var cols = lines[i].Split(';');
            if (cols.Length < 3)
            {
                continue;
            }

            rows.Add(new PartnerFeedRow
            {
                ExternalId = cols[0],
                Title = cols[1],
                ExternalPath = cols.Length > 2 ? cols[2] : null,
                IsFree = cols.Length > 3 && bool.TryParse(cols[3], out var free) && free,
                Type = Enum.TryParse<TrainingOfferType>(cols.Length > 4 ? cols[4] : "", true, out var t)
                    ? t
                    : TrainingOfferType.Cursus,
                Section = TrainingOfferSection.General
            });
        }

        return rows;
    }

    internal sealed class PartnerFeedRow
    {
        public string ExternalId { get; set; } = "";
        public string Title { get; set; } = "";
        public string? ExternalPath { get; set; }
        public IReadOnlyList<string> Fields { get; set; } = [];
        public IReadOnlyList<string> Keys { get; set; } = [];
        public TrainingOfferType Type { get; set; } = TrainingOfferType.Cursus;
        public int? DurationValue { get; set; }
        public TrainingDurationUnit? DurationUnit { get; set; }
        public TrainingDeliveryMode Delivery { get; set; } = TrainingDeliveryMode.Online;
        public string? Location { get; set; }
        public bool IsFree { get; set; }
        public decimal? PriceEuro { get; set; }
        public string? PriceLabel { get; set; }
        public TrainingOfferSection Section { get; set; } = TrainingOfferSection.General;
        public IReadOnlyList<string> Competencies { get; set; } = [];
        public IReadOnlyList<string> CareerSteps { get; set; } = [];
        public string? ReasonNl { get; set; }
        public string? ReasonEn { get; set; }
        public IReadOnlyList<string> LearnBullets { get; set; } = [];
        public DateOnly? StartsOn { get; set; }
        public int SortOrder { get; set; }
    }
}
