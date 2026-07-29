using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class VacancyDraftCreationService : IVacancyDraftCreationService
{
    private readonly JobsyDbContext _db;
    private readonly ISalaryService _salary;
    private readonly IVacancyContentModerationService _moderation;

    public VacancyDraftCreationService(
        JobsyDbContext db,
        ISalaryService salary,
        IVacancyContentModerationService moderation)
    {
        _db = db;
        _salary = salary;
        _moderation = moderation;
    }

    public async Task<VacancyDraftCreateResult> CreateDraftAsync(
        VacancyDraftInput input,
        VacancySource source,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Title))
        {
            return VacancyDraftCreateResult.Fail("Titel is verplicht.");
        }

        if (input.Title.Trim().Length > 256)
        {
            return VacancyDraftCreateResult.Fail("Titel mag maximaal 256 tekens zijn.");
        }

        if (string.IsNullOrWhiteSpace(input.Description))
        {
            return VacancyDraftCreateResult.Fail("Omschrijving is verplicht.");
        }

        if (input.Description.Trim().Length > 20_000)
        {
            return VacancyDraftCreateResult.Fail("Omschrijving mag maximaal 20000 tekens zijn.");
        }

        if (input.EndDate < input.StartDate)
        {
            return VacancyDraftCreateResult.Fail("Einddatum mag niet vóór de startdatum liggen.");
        }

        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == input.CompanyId, cancellationToken);
        if (company is null)
        {
            return VacancyDraftCreateResult.Fail("Bedrijf/vestiging niet gevonden.");
        }

        var organizationId = company.ParentCompanyId ?? company.Id;
        var salaryTable = await _db.CompanySalaryTables
            .Include(t => t.Rates)
            .Include(t => t.AllowedBranches)
            .FirstOrDefaultAsync(t => t.Id == input.SalaryTableId && t.IsActive, cancellationToken);
        var allowed = salaryTable is not null
            && (WmlSalaryTableService.IsAllowedForBranch(salaryTable, input.CompanyId, organizationId)
                || salaryTable.CompanyId == input.CompanyId);
        if (!allowed)
        {
            return VacancyDraftCreateResult.Fail("Salaristabel niet gevonden voor deze vestiging.");
        }

        if (salaryTable!.Rates.Count == 0)
        {
            return VacancyDraftCreateResult.Fail("Salaristabel heeft geen tarieven.");
        }

        var adultRate = salaryTable.Rates
            .Where(r => r.AgeYears >= 21)
            .OrderBy(r => r.AgeYears)
            .Select(r => r.HourlyRate)
            .FirstOrDefault();
        if (adultRate <= 0)
        {
            adultRate = salaryTable.Rates.Max(r => r.HourlyRate);
        }

        var hourlyWage = adultRate > 0 ? adultRate : input.HourlyWage;
        if (hourlyWage <= 0)
        {
            return VacancyDraftCreateResult.Fail("Uurloon kon niet worden bepaald uit de salaristabel.");
        }

        if (!_salary.MeetsMinimumWage(hourlyWage, ageYears: 21))
        {
            return VacancyDraftCreateResult.Fail("Uurloon ligt onder het wettelijk minimumloon (21+).");
        }

        var branchLabels = NormalizeBranchLabels(input.WorkTypes);
        if (branchLabels.Length is < 1 or > WorkTypeLabels.MaxPerVacancy)
        {
            return VacancyDraftCreateResult.Fail($"Kies 1 of {WorkTypeLabels.MaxPerVacancy} branches.");
        }

        if (!await AreBranchLabelsAllowedAsync(branchLabels, cancellationToken))
        {
            return VacancyDraftCreateResult.Fail("Een of meer branches zijn ongeldig of niet actief.");
        }

        string? imageUrl = null;
        if (!string.IsNullOrWhiteSpace(input.ImageUrl))
        {
            imageUrl = HtmlSanitize.NormalizeImageInput(input.ImageUrl, out var imageError);
            if (imageUrl is null)
            {
                return VacancyDraftCreateResult.Fail(imageError ?? "Ongeldige afbeelding.");
            }
        }

        string? videoUrl = null;
        if (!string.IsNullOrWhiteSpace(input.VideoUrl))
        {
            videoUrl = HtmlSanitize.NormalizeMediaUrl(input.VideoUrl);
            if (videoUrl is null)
            {
                return VacancyDraftCreateResult.Fail("Ongeldige video-URL (alleen http/https).");
            }
        }

        var moderation = await _moderation.CheckAsync(input.Title, input.Description, cancellationToken);
        if (!moderation.IsAllowed)
        {
            return VacancyDraftCreateResult.Fail(moderation.Warning ?? "Inhoud is niet toegestaan.");
        }

        var vacancy = new Vacancy
        {
            Id = Guid.NewGuid(),
            Title = input.Title.Trim(),
            Description = input.Description.Trim(),
            HourlyWage = hourlyWage,
            StartDate = input.StartDate,
            EndDate = input.EndDate,
            Status = VacancyStatus.Draft,
            CompanyId = input.CompanyId,
            CreatedVia = source,
            Location = company.Location,
            RequiredTransport = input.RequiredTransport == TransportMode.None
                ? TransportMode.Bike | TransportMode.PublicTransport
                : input.RequiredTransport,
            WorkTypes = WorkTypeLabels.Combine(branchLabels),
            WorkTypeLabels = WorkTypeLabels.CombineStored(branchLabels),
            ImageUrl = imageUrl,
            VideoUrl = videoUrl,
            SalaryTableId = input.SalaryTableId,
            RequiredDrivingLicense = string.IsNullOrWhiteSpace(input.RequiredDrivingLicense)
                ? null
                : input.RequiredDrivingLicense.Trim(),
            RequiredEducation = string.IsNullOrWhiteSpace(input.RequiredEducation)
                ? null
                : input.RequiredEducation.Trim(),
            MinimumEmployers = input.MinimumEmployers is > 0 ? input.MinimumEmployers : null
        };

        _db.Vacancies.Add(vacancy);
        await _db.SaveChangesAsync(cancellationToken);
        vacancy.Company = company;
        return VacancyDraftCreateResult.Ok(vacancy);
    }

    private static string[] NormalizeBranchLabels(IEnumerable<string>? labels) =>
        (labels ?? [])
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(WorkTypeLabels.MaxPerVacancy)
            .Cast<string>()
            .ToArray();

    private async Task<bool> AreBranchLabelsAllowedAsync(string[] labels, CancellationToken cancellationToken)
    {
        if (labels.Length == 0)
        {
            return false;
        }

        var allowed = await _db.MasterdataOptions.AsNoTracking()
            .Where(o => o.Category == MasterdataCategories.Branch && o.IsActive)
            .Select(o => o.Label)
            .ToListAsync(cancellationToken);

        if (allowed.Count == 0)
        {
            // Fallback when masterdata is empty (tests / early seed).
            return labels.All(l => WorkTypeLabels.All.Contains(l, StringComparer.OrdinalIgnoreCase));
        }

        return labels.All(l => allowed.Contains(l, StringComparer.OrdinalIgnoreCase));
    }
}
