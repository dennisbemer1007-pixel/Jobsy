using System.Reflection;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Jobsy.Infrastructure.Services;

public sealed class PlatformCompanySettingsService : IPlatformCompanySettingsService
{
    public static readonly Guid SingletonId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");
    public const string DefaultCompanyName = "Lobsy";
    public const string DefaultSlogan = "Dichtbij genoeg om het pantser te laten vallen";

    private static readonly Lazy<byte[]> LogoBytes = new(LoadEmbeddedLogo);
    private static readonly Lazy<byte[]> WatermarkLogoBytes = new(() => CreateWatermarkLogo(LogoBytes.Value));

    private readonly JobsyDbContext _db;

    public PlatformCompanySettingsService(JobsyDbContext db)
    {
        _db = db;
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

        row.CompanyName = string.IsNullOrWhiteSpace(update.CompanyName)
            ? DefaultCompanyName
            : update.CompanyName.Trim();
        row.Slogan = NormalizeOptional(update.Slogan);
        row.Address = NormalizeOptional(update.Address);
        row.PostalCode = NormalizeOptional(update.PostalCode);
        row.City = NormalizeOptional(update.City);
        row.Country = NormalizeOptional(update.Country) ?? "NL";
        row.KvkNumber = NormalizeOptional(update.KvkNumber);
        row.VatNumber = NormalizeOptional(update.VatNumber);
        row.Phone = NormalizeOptional(update.Phone);
        row.Email = NormalizeOptional(update.Email);
        row.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
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
            NormalizeOptional(row?.KvkNumber),
            NormalizeOptional(row?.VatNumber),
            NormalizeOptional(row?.Phone),
            NormalizeOptional(row?.Email),
            row?.UpdatedAtUtc);

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
