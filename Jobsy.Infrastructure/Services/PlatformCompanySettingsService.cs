using System.Reflection;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Jobsy.Infrastructure.Services;

public sealed class PlatformCompanySettingsService : IPlatformCompanySettingsService
{
    public static readonly Guid SingletonId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");
    public const string DefaultCompanyName = "Lobsy";
    public const string DefaultLegalName = "Dennis Bemer h.o.d.n. Lobsy";
    public const string DefaultTradeName = "Lobsy";
    public const string DefaultSlogan = "Dichtbij genoeg om het pantser te laten vallen";

    private static readonly Lazy<byte[]> LogoBytes = new(LoadEmbeddedLogo);
    private static readonly Lazy<byte[]> WatermarkLogoBytes = new(() => CreateWatermarkLogo(LogoBytes.Value));

    private readonly JobsyDbContext _db;
    private readonly IMemoryCache _cache;

    public PlatformCompanySettingsService(JobsyDbContext db)
        : this(db, new MemoryCache(new MemoryCacheOptions()))
    {
    }

    public PlatformCompanySettingsService(JobsyDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<PlatformCompanySnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        var row = await _db.PlatformCompanySettings.AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
        return ToSnapshot(row);
    }

    public async Task<PlatformCompanySnapshot> UpdateAsync(
        PlatformCompanyUpdate update,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.PlatformCompanySettings.FirstOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            row = new PlatformCompanySettings { Id = SingletonId };
            _db.PlatformCompanySettings.Add(row);
        }

        var legalName = NormalizeOptional(update.LegalName);
        var tradeName = NormalizeOptional(update.TradeName);
        var supportEmail = NormalizeOptional(update.SupportEmail) ?? NormalizeOptional(update.Email);
        var privacyEmail = NormalizeOptional(update.PrivacyEmail);
        var kvk = DigitsOrNull(update.KvkNumber);
        var vat = NormalizeVat(update.VatNumber);
        PlatformCompanyFieldRules.EnsureValid(kvk, vat, supportEmail, privacyEmail);

        row.LegalName = legalName;
        row.TradeName = tradeName;
        row.CompanyName = tradeName
            ?? (string.IsNullOrWhiteSpace(update.CompanyName) ? DefaultCompanyName : update.CompanyName.Trim());
        row.Slogan = NormalizeOptional(update.Slogan);
        row.Address = NormalizeOptional(update.Address);
        row.PostalCode = NormalizeOptional(update.PostalCode);
        row.City = NormalizeOptional(update.City);
        row.Country = NormalizeOptional(update.Country) ?? "NL";
        row.PostalStreet = NormalizeOptional(update.PostalStreet);
        row.PostalPostalCode = NormalizeOptional(update.PostalPostalCode);
        row.PostalCity = NormalizeOptional(update.PostalCity);
        row.KvkNumber = kvk;
        row.VatNumber = vat;
        row.Phone = NormalizeOptional(update.Phone);
        row.SupportEmail = supportEmail;
        row.PrivacyEmail = privacyEmail;
        row.Email = supportEmail;
        row.VatBufferIban = NormalizeIban(update.VatBufferIban);
        row.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        LegalIdentityService.EvictCache(_cache);
        return ToSnapshot(row);
    }

    public byte[] GetBrandLogoPng() => LogoBytes.Value;

    public byte[] GetBrandWatermarkPng() => WatermarkLogoBytes.Value;

    private static PlatformCompanySnapshot ToSnapshot(PlatformCompanySettings? row)
        => new(
            string.IsNullOrWhiteSpace(row?.CompanyName) ? DefaultCompanyName : row.CompanyName.Trim(),
            string.IsNullOrWhiteSpace(row?.Slogan) ? DefaultSlogan : row.Slogan.Trim(),
            NormalizeOptional(row?.Address),
            NormalizeOptional(row?.PostalCode),
            NormalizeOptional(row?.City),
            NormalizeOptional(row?.Country) ?? "NL",
            DigitsOrNull(row?.KvkNumber),
            NormalizeVat(row?.VatNumber),
            NormalizeOptional(row?.Phone),
            NormalizeOptional(row?.Email),
            NormalizeIban(row?.VatBufferIban),
            row?.UpdatedAtUtc,
            NormalizeOptional(row?.LegalName),
            NormalizeOptional(row?.TradeName),
            NormalizeOptional(row?.PostalStreet),
            NormalizeOptional(row?.PostalPostalCode),
            NormalizeOptional(row?.PostalCity),
            NormalizeOptional(row?.SupportEmail) ?? NormalizeOptional(row?.Email),
            NormalizeOptional(row?.PrivacyEmail));

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? DigitsOrNull(string? value)
    {
        var trimmed = NormalizeOptional(value);
        if (trimmed is null)
        {
            return null;
        }

        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        return digits.Length == 0 ? null : digits;
    }

    private static string? NormalizeVat(string? value)
    {
        var trimmed = NormalizeOptional(value);
        if (trimmed is null)
        {
            return null;
        }

        return new string(trimmed.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
    }

    /// <summary>Normalizes IBAN to uppercase without spaces; returns null when empty.</summary>
    internal static string? NormalizeIban(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var compact = new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
        return compact.Length == 0 ? null : compact;
    }

    private static byte[] LoadEmbeddedLogo()
    {
        var assembly = typeof(PlatformCompanySettingsService).Assembly;
        const string resourceName = "Jobsy.Infrastructure.Assets.lobsy.png";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded logo resource '{resourceName}' ontbreekt.");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    private static byte[] CreateWatermarkLogo(byte[] sourcePng)
    {
        using var image = Image.Load<Rgba32>(sourcePng);
        // Very transparent but still readable on white paper (~12% opacity).
        image.Mutate(ctx => ctx.Opacity(0.12f));
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }
}
