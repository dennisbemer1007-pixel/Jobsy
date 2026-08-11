using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class MarketingFlyerSettingsService : IMarketingFlyerSettingsService
{
    public static readonly Guid SingletonId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");

    public const string DefaultHeadline = "Lobsy. Vacatures die écht gezien worden.";
    public const string DefaultSubheadline = "Nieuw platform — samen met het Westland groot maken.";
    public const string DefaultIntro =
        "Hyper-lokaal werven zonder logge vacaturesites. Dichtbij, snel en betaalbaar — voor elke ondernemer.";

    public static readonly string DefaultBulletPoints = """
        Vacatures zijn vele malen zichtbaarder dan op bestaande logge vacaturesites
        Stages en vrijwilligersbanen plaatsen is altijd gratis
        Vacatures plaatsen via een overzichtelijk tokensysteem — betaalbaar, óók voor de kleine ondernemer
        Duidelijk dashboard: zie direct waar successen en uitdagingen liggen
        Behulpzame Lobsy-bot die je onderweg ondersteunt
        Een vacature is zo geplaatst: handmatig, via CSV of via de API
        Tokens verdienen? Dat kan
        Matchen en afwijzen gaat makkelijker dan ooit
        Eigen flyers met QR-code naar jullie bedrijvenpagina — al je vacatures op één plek
        """.Trim();

    public const string DefaultPromoFreeText =
        "T/m 18 november 2026 is het gratis om vacatures te plaatsen.";

    public const string DefaultPromoDiscountText =
        "Tot 31 december 2026 krijg je 50% korting.";

    public const string DefaultCtaTitle = "Groei mee met Lobsy";
    public const string DefaultCtaBody =
        "Scan de QR-code en start vandaag — of ga naar lobsy.nl. Jouw bedrijvenpagina met alle vacatures is zo live.";
    public const string DefaultQrCaption = "Start op Lobsy";
    public const string DefaultQrPath = "/register";
    public const string DefaultFooterNote = "Lobsy · hyper-lokaal matchen in Westland & omgeving";

    private readonly JobsyDbContext _db;

    public MarketingFlyerSettingsService(JobsyDbContext db)
    {
        _db = db;
    }

    public async Task<MarketingFlyerSnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        var row = await _db.MarketingFlyerSettings.AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
        return ToSnapshot(row);
    }

    public async Task<MarketingFlyerSnapshot> UpdateAsync(
        MarketingFlyerUpdate update,
        CancellationToken cancellationToken = default)
    {
        var row = await EnsureRowAsync(cancellationToken);
        Apply(row, update);
        row.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return ToSnapshot(row);
    }

    public async Task<MarketingFlyerSnapshot> ResetToDefaultsAsync(CancellationToken cancellationToken = default)
    {
        var row = await EnsureRowAsync(cancellationToken);
        Apply(row, DefaultsUpdate());
        row.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return ToSnapshot(row);
    }

    private async Task<MarketingFlyerSettings> EnsureRowAsync(CancellationToken cancellationToken)
    {
        var row = await _db.MarketingFlyerSettings.FirstOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            row = new MarketingFlyerSettings { Id = SingletonId };
            Apply(row, DefaultsUpdate());
            _db.MarketingFlyerSettings.Add(row);
        }

        return row;
    }

    private static void Apply(MarketingFlyerSettings row, MarketingFlyerUpdate update)
    {
        row.Headline = Clamp(update.Headline, DefaultHeadline, 200);
        row.Subheadline = Clamp(update.Subheadline, DefaultSubheadline, 300);
        row.Intro = Clamp(update.Intro, DefaultIntro, 600);
        row.BulletPoints = Clamp(NormalizeBullets(update.BulletPoints), DefaultBulletPoints, 4000);
        row.PromoFreeText = Clamp(update.PromoFreeText, DefaultPromoFreeText, 400);
        row.PromoDiscountText = Clamp(update.PromoDiscountText, DefaultPromoDiscountText, 400);
        row.CtaTitle = Clamp(update.CtaTitle, DefaultCtaTitle, 200);
        row.CtaBody = Clamp(update.CtaBody, DefaultCtaBody, 500);
        row.QrCaption = Clamp(update.QrCaption, DefaultQrCaption, 200);
        row.QrPath = NormalizePath(update.QrPath);
        row.FooterNote = Clamp(update.FooterNote, DefaultFooterNote, 300);
    }

    public static MarketingFlyerUpdate DefaultsUpdate() => new(
        DefaultHeadline,
        DefaultSubheadline,
        DefaultIntro,
        DefaultBulletPoints,
        DefaultPromoFreeText,
        DefaultPromoDiscountText,
        DefaultCtaTitle,
        DefaultCtaBody,
        DefaultQrCaption,
        DefaultQrPath,
        DefaultFooterNote);

    private static MarketingFlyerSnapshot ToSnapshot(MarketingFlyerSettings? row)
    {
        if (row is null)
        {
            return ToSnapshot(new MarketingFlyerSettings
            {
                Headline = DefaultHeadline,
                Subheadline = DefaultSubheadline,
                Intro = DefaultIntro,
                BulletPoints = DefaultBulletPoints,
                PromoFreeText = DefaultPromoFreeText,
                PromoDiscountText = DefaultPromoDiscountText,
                CtaTitle = DefaultCtaTitle,
                CtaBody = DefaultCtaBody,
                QrCaption = DefaultQrCaption,
                QrPath = DefaultQrPath,
                FooterNote = DefaultFooterNote
            });
        }

        return new MarketingFlyerSnapshot(
            string.IsNullOrWhiteSpace(row.Headline) ? DefaultHeadline : row.Headline.Trim(),
            string.IsNullOrWhiteSpace(row.Subheadline) ? DefaultSubheadline : row.Subheadline.Trim(),
            string.IsNullOrWhiteSpace(row.Intro) ? DefaultIntro : row.Intro.Trim(),
            ParseBullets(string.IsNullOrWhiteSpace(row.BulletPoints) ? DefaultBulletPoints : row.BulletPoints),
            string.IsNullOrWhiteSpace(row.PromoFreeText) ? DefaultPromoFreeText : row.PromoFreeText.Trim(),
            string.IsNullOrWhiteSpace(row.PromoDiscountText) ? DefaultPromoDiscountText : row.PromoDiscountText.Trim(),
            string.IsNullOrWhiteSpace(row.CtaTitle) ? DefaultCtaTitle : row.CtaTitle.Trim(),
            string.IsNullOrWhiteSpace(row.CtaBody) ? DefaultCtaBody : row.CtaBody.Trim(),
            string.IsNullOrWhiteSpace(row.QrCaption) ? DefaultQrCaption : row.QrCaption.Trim(),
            NormalizePath(row.QrPath),
            string.IsNullOrWhiteSpace(row.FooterNote) ? DefaultFooterNote : row.FooterNote.Trim(),
            row.UpdatedAtUtc == default ? null : row.UpdatedAtUtc);
    }

    public static IReadOnlyList<string> ParseBullets(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return ParseBullets(DefaultBulletPoints);
        }

        return raw
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.TrimStart('•', '-', '*', ' ').Trim())
            .Where(line => line.Length > 0)
            .Take(12)
            .ToList();
    }

    private static string NormalizeBullets(string? raw)
        => string.Join('\n', ParseBullets(raw));

    private static string NormalizePath(string? path)
    {
        var trimmed = string.IsNullOrWhiteSpace(path) ? DefaultQrPath : path.Trim();
        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            // Absolute URLs allowed for QR; keep as-is (clamped).
            return trimmed.Length > 500 ? trimmed[..500] : trimmed;
        }

        if (!trimmed.StartsWith('/'))
        {
            trimmed = "/" + trimmed;
        }

        return trimmed.Length > 200 ? trimmed[..200] : trimmed;
    }

    private static string Clamp(string? value, string fallback, int max)
    {
        var text = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return text.Length > max ? text[..max] : text;
    }
}
