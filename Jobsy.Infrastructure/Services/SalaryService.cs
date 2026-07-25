using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Dutch statutory minimum wage check. Prefers current DB rates; falls back to hardcoded demo values.
/// </summary>
public class SalaryService : ISalaryService
{
    private const decimal AdultMinimumHourly = 14.06m;

    private readonly JobsyDbContext _db;
    private Dictionary<int, decimal>? _currentByAge;

    public SalaryService(JobsyDbContext db)
    {
        _db = db;
    }

    public bool MeetsMinimumWage(decimal hourlyWage, int ageYears)
        => hourlyWage >= GetMinimumHourlyWage(ageYears);

    public decimal GetMinimumHourlyWage(int ageYears)
    {
        EnsureCache();
        var age = Math.Clamp(ageYears, 15, 21);
        if (_currentByAge!.TryGetValue(age, out var rate))
        {
            return rate;
        }

        // Prefer nearest lower age band from DB, else hardcoded fallback.
        for (var a = age - 1; a >= 15; a--)
        {
            if (_currentByAge.TryGetValue(a, out rate))
            {
                return rate;
            }
        }

        return HardcodedMinimum(ageYears);
    }

    private void EnsureCache()
    {
        if (_currentByAge is not null)
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var rates = _db.MinimumWageRates.AsNoTracking()
            .Where(r => r.EffectiveFrom <= today)
            .ToList();

        _currentByAge = rates
            .GroupBy(r => r.AgeYears)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(r => r.EffectiveFrom).First().HourlyRate);
    }

    private static decimal HardcodedMinimum(int ageYears) => ageYears switch
    {
        >= 21 => AdultMinimumHourly,
        20 => AdultMinimumHourly * 0.80m,
        19 => AdultMinimumHourly * 0.60m,
        18 => AdultMinimumHourly * 0.50m,
        17 => AdultMinimumHourly * 0.395m,
        16 => AdultMinimumHourly * 0.345m,
        15 => AdultMinimumHourly * 0.30m,
        _ => AdultMinimumHourly * 0.30m
    };
}
