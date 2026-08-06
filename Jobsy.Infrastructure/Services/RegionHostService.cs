using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class RegionHostService : IRegionHostService
{
    private readonly JobsyDbContext _db;

    public RegionHostService(JobsyDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<RegionHost>> ListAsync(CancellationToken cancellationToken = default)
        => await _db.RegionHosts.AsNoTracking()
            .OrderBy(h => h.Hostname)
            .ToListAsync(cancellationToken);

    public async Task<RegionHost?> FindByHostnameAsync(string hostname, CancellationToken cancellationToken = default)
    {
        var normalized = RegionHostRules.NormalizeHostname(hostname);
        if (normalized is null)
        {
            return null;
        }

        return await _db.RegionHosts.AsNoTracking()
            .FirstOrDefaultAsync(h => h.IsActive && h.Hostname == normalized, cancellationToken);
    }

    public Task<RegionHost?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.RegionHosts.AsNoTracking().FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

    public async Task<RegionHost> CreateAsync(RegionHostUpsert upsert, CancellationToken cancellationToken = default)
    {
        var host = Apply(new RegionHost { Id = Guid.NewGuid(), CreatedAtUtc = DateTime.UtcNow }, upsert);
        await EnsureUniqueHostnameAsync(host.Hostname, excludeId: null, cancellationToken);
        _db.RegionHosts.Add(host);
        await _db.SaveChangesAsync(cancellationToken);
        return host;
    }

    public async Task<RegionHost?> UpdateAsync(Guid id, RegionHostUpsert upsert, CancellationToken cancellationToken = default)
    {
        var existing = await _db.RegionHosts.FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        Apply(existing, upsert);
        await EnsureUniqueHostnameAsync(existing.Hostname, excludeId: id, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _db.RegionHosts.FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        _db.RegionHosts.Remove(existing);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static RegionHost Apply(RegionHost entity, RegionHostUpsert upsert)
    {
        var hostname = RegionHostRules.NormalizeHostname(upsert.Hostname)
            ?? throw new ArgumentException("Hostname is verplicht.");
        if (!RegionHostRules.IsValidHostname(hostname))
        {
            throw new ArgumentException("Hostname is ongeldig. Gebruik bijv. westland.lobsy.nl");
        }

        var display = (upsert.DisplayName ?? string.Empty).Trim();
        if (display.Length is < 1 or > RegionHostRules.MaxDisplayNameLength)
        {
            throw new ArgumentException("Weergavenaam is verplicht (max. 128 tekens).");
        }

        entity.Hostname = hostname;
        entity.DisplayName = display;
        entity.Slogan = Clamp(upsert.Slogan, RegionHostRules.MaxSloganLength);
        entity.AddressLabel = Clamp(upsert.AddressLabel, RegionHostRules.MaxAddressLength);
        entity.Latitude = upsert.Latitude;
        entity.Longitude = upsert.Longitude;
        entity.BackgroundImageUrl = NormalizeBackgroundUrl(upsert.BackgroundImageUrl);
        entity.IsActive = upsert.IsActive;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        return entity;
    }

    private static string? NormalizeBackgroundUrl(string? value)
    {
        var trimmed = Clamp(value, RegionHostRules.MaxBackgroundUrlLength);
        if (trimmed is null)
        {
            return null;
        }

        if (trimmed.StartsWith("/images/", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        throw new ArgumentException("Achtergrond-URL moet https://… of /images/… zijn.");
    }

    private async Task EnsureUniqueHostnameAsync(string hostname, Guid? excludeId, CancellationToken cancellationToken)
    {
        var clash = await _db.RegionHosts.AnyAsync(
            h => h.Hostname == hostname && (excludeId == null || h.Id != excludeId),
            cancellationToken);
        if (clash)
        {
            throw new InvalidOperationException($"Hostname '{hostname}' bestaat al.");
        }
    }

    private static string? Clamp(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}
