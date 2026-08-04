using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class ExclusivitySettingService : IExclusivitySettingService
{
    private readonly JobsyDbContext _db;
    private readonly ILogger<ExclusivitySettingService> _logger;

    public ExclusivitySettingService(JobsyDbContext db, ILogger<ExclusivitySettingService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task EnsureSeededAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.ExclusivitySettings.AnyAsync(cancellationToken))
        {
            return;
        }

        var open = new ExclusivitySetting
        {
            Id = ExclusivityRules.DefaultOpenOptionId,
            Name = ExclusivityRules.DefaultOpenName,
            IsActive = true,
            IsOpenOption = true,
            SortOrder = 0,
            CreatedAt = DateTime.UtcNow
        };

        var seeds = new List<ExclusivitySetting>
        {
            open,
            School("Inholland", "student.inholland.nl", @"^\d{7,8}$", 10),
            School("Albeda", "student.albeda.nl", @"^\d{6,10}$", 20),
            School("Zadkine", "student.zadkine.nl", @"^\d{6,10}$", 30),
            School("ROC Mondriaan", "student.rocmondriaan.nl", @"^\d{6,10}$", 40),
            School("Hogeschool Rotterdam", "hr.nl", @"^\d{7,8}$", 50)
        };

        _db.ExclusivitySettings.AddRange(seeds);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded {Count} exclusivity settings.", seeds.Count);
    }

    public async Task<IReadOnlyList<ExclusivitySettingDto>> ListAsync(
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken);
        var query = _db.ExclusivitySettings
            .AsNoTracking()
            .Include(s => s.Educations)
            .AsQueryable();
        if (activeOnly)
        {
            query = query.Where(s => s.IsActive);
        }

        var items = await query
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Name)
            .ToListAsync(cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<ExclusivitySettingDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken);
        var item = await _db.ExclusivitySettings
            .AsNoTracking()
            .Include(s => s.Educations)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        return item is null ? null : Map(item);
    }

    public async Task<ExclusivitySettingDto> CreateAsync(
        ExclusivitySettingUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken);
        ValidateUpsert(request);

        var domain = ExclusivityRules.NormalizeDomain(request.SchoolDomain);
        if (request.IsOpenOption)
        {
            domain = null;
        }

        await EnsureUniqueConstraintsAsync(null, domain, request.IsOpenOption, cancellationToken);

        var entity = new ExclusivitySetting
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            SchoolDomain = domain,
            StudentNumberPattern = string.IsNullOrWhiteSpace(request.StudentNumberPattern)
                ? null
                : request.StudentNumberPattern.Trim(),
            IsActive = request.IsActive,
            IsOpenOption = request.IsOpenOption,
            SortOrder = request.SortOrder,
            CreatedAt = DateTime.UtcNow
        };

        ReplaceEducations(entity, request.Educations);
        _db.ExclusivitySettings.Add(entity);

        if (request.IsOpenOption)
        {
            await ClearOtherOpenFlagsAsync(entity.Id, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<ExclusivitySettingDto> UpdateAsync(
        Guid id,
        ExclusivitySettingUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken);
        ValidateUpsert(request);

        var entity = await _db.ExclusivitySettings
            .Include(s => s.Educations)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Exclusiviteitsinstelling niet gevonden.");

        var domain = ExclusivityRules.NormalizeDomain(request.SchoolDomain);
        if (request.IsOpenOption)
        {
            domain = null;
        }

        await EnsureUniqueConstraintsAsync(id, domain, request.IsOpenOption, cancellationToken);

        entity.Name = request.Name.Trim();
        entity.SchoolDomain = domain;
        entity.StudentNumberPattern = string.IsNullOrWhiteSpace(request.StudentNumberPattern)
            ? null
            : request.StudentNumberPattern.Trim();
        entity.IsActive = request.IsActive;
        entity.IsOpenOption = request.IsOpenOption;
        entity.SortOrder = request.SortOrder;
        entity.UpdatedAt = DateTime.UtcNow;

        ReplaceEducations(entity, request.Educations);

        if (request.IsOpenOption)
        {
            await ClearOtherOpenFlagsAsync(entity.Id, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ExclusivitySettings
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Exclusiviteitsinstelling niet gevonden.");

        if (entity.IsOpenOption)
        {
            throw new InvalidOperationException(
                "De open-optie (“Open voor alle studenten”) kan niet worden verwijderd. Deactiveer of hernoem deze.");
        }

        var inUse = await _db.Vacancies.AnyAsync(v => v.ExclusivitySettingId == id, cancellationToken);
        if (inUse)
        {
            throw new InvalidOperationException(
                "Deze instelling is gekoppeld aan stageplekken. Deactiveer de optie in plaats van te verwijderen.");
        }

        _db.ExclusivitySettings.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateUpsert(ExclusivitySettingUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Naam is verplicht.");
        }

        var patternError = ExclusivityRules.ValidatePatternSyntax(request.StudentNumberPattern);
        if (patternError is not null)
        {
            throw new ArgumentException(patternError);
        }

        if (request.IsOpenOption
            && (!string.IsNullOrWhiteSpace(request.SchoolDomain)
                || !string.IsNullOrWhiteSpace(request.StudentNumberPattern)))
        {
            throw new ArgumentException(
                "De open-optie mag geen schooldomein of studentnummerpatroon hebben.");
        }
    }

    private async Task EnsureUniqueConstraintsAsync(
        Guid? exceptId,
        string? domain,
        bool isOpenOption,
        CancellationToken cancellationToken)
    {
        if (isOpenOption)
        {
            var otherOpen = await _db.ExclusivitySettings.AnyAsync(
                s => s.IsOpenOption && (exceptId == null || s.Id != exceptId),
                cancellationToken);
            if (otherOpen)
            {
                throw new InvalidOperationException(
                    "Er mag maar één “Open voor iedereen”-optie bestaan. Haal de vlag eerst van de bestaande open-optie.");
            }
        }

        if (!string.IsNullOrWhiteSpace(domain))
        {
            var clash = await _db.ExclusivitySettings.AnyAsync(
                s => s.SchoolDomain == domain && (exceptId == null || s.Id != exceptId),
                cancellationToken);
            if (clash)
            {
                throw new InvalidOperationException("Dit schooldomein is al in gebruik bij een andere instelling.");
            }
        }
    }

    private async Task ClearOtherOpenFlagsAsync(Guid keepId, CancellationToken cancellationToken)
    {
        var others = await _db.ExclusivitySettings
            .Where(s => s.IsOpenOption && s.Id != keepId)
            .ToListAsync(cancellationToken);
        foreach (var other in others)
        {
            other.IsOpenOption = false;
            other.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static void ReplaceEducations(ExclusivitySetting entity, IReadOnlyList<string>? educations)
    {
        entity.Educations.Clear();
        if (educations is null || educations.Count == 0)
        {
            return;
        }

        var order = 0;
        foreach (var name in educations
                     .Select(e => e.Trim())
                     .Where(e => !string.IsNullOrWhiteSpace(e))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            entity.Educations.Add(new ExclusivityEducation
            {
                Id = Guid.NewGuid(),
                ExclusivitySettingId = entity.Id,
                Name = name,
                SortOrder = order++,
                IsActive = true
            });
        }
    }

    private static ExclusivitySetting School(string name, string domain, string pattern, int sort)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = $"Exclusief voor {name}",
            SchoolDomain = domain,
            StudentNumberPattern = pattern,
            IsActive = true,
            IsOpenOption = false,
            SortOrder = sort,
            CreatedAt = DateTime.UtcNow,
            Educations =
            [
                new ExclusivityEducation
                {
                    Id = Guid.NewGuid(),
                    Name = "Algemeen",
                    SortOrder = 0,
                    IsActive = true
                }
            ]
        };

    private static ExclusivitySettingDto Map(ExclusivitySetting s)
        => new(
            s.Id,
            s.Name,
            s.SchoolDomain,
            s.StudentNumberPattern,
            s.IsActive,
            s.IsOpenOption,
            s.SortOrder,
            s.Educations
                .OrderBy(e => e.SortOrder)
                .ThenBy(e => e.Name)
                .Select(e => new ExclusivityEducationDto(e.Id, e.Name, e.SortOrder, e.IsActive))
                .ToList());
}
