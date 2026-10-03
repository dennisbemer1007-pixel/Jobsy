using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Features;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/admin/passport-partners")]
[Authorize(Policy = JobsyPolicies.RequireAdmin)]
public class PassportPartnersAdminController : ControllerBase
{
    private readonly JobsyDbContext _db;
    private readonly PassportPartnerService _partners;

    public PassportPartnersAdminController(JobsyDbContext db, PassportPartnerService partners)
    {
        _db = db;
        _partners = partners;
    }

    [HttpGet]
    [RequiresFeature(PlatformFeature.PassportPartners)]
    public async Task<ActionResult> List(CancellationToken cancellationToken)
    {
        var rows = await _db.PassportPartners.AsNoTracking()
            .Include(p => p.Company)
            .OrderBy(p => p.DisplayName)
            .Select(p => new
            {
                p.Id,
                p.CompanyId,
                p.DisplayName,
                p.IsActive,
                p.MaxBranches,
                Type = p.Company == null ? null : PassportPartner.TypeFromCompany(p.Company.Type).ToString(),
                p.TermsVersion,
                HasLogo = p.LogoPng != null
            })
            .ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpPost]
    [RequiresFeature(PlatformFeature.PassportPartners)]
    public async Task<ActionResult> Create([FromBody] CreatePassportPartnerRequest request, CancellationToken cancellationToken)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == request.CompanyId, cancellationToken);
        if (company is null)
        {
            return NotFound(new { error = "company_not_found" });
        }

        if (await _db.PassportPartners.AnyAsync(p => p.CompanyId == company.Id, cancellationToken))
        {
            return Conflict(new { error = "partner_exists" });
        }

        var now = DateTime.UtcNow;
        var partner = new PassportPartner
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            IsActive = true,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? company.Name : request.DisplayName.Trim(),
            MaxBranches = request.MaxBranches is > 0 ? request.MaxBranches.Value : 1,
            TermsVersion = PassportPartnerTerms.CurrentVersion,
            TermsAcceptedAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        if (partner.DisplayName.Length > 80)
        {
            partner.DisplayName = partner.DisplayName[..80];
        }

        _db.PassportPartners.Add(partner);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new
        {
            partner.Id,
            Type = PassportPartner.TypeFromCompany(company.Type).ToString(),
            Terms = PassportPartnerTerms.Text
        });
    }

    [HttpPost("{id:guid}/active")]
    [RequiresFeature(PlatformFeature.PassportPartners)]
    public async Task<ActionResult> SetActive(Guid id, [FromBody] SetPartnerActiveRequest request, CancellationToken cancellationToken)
    {
        var partner = await _db.PassportPartners.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (partner is null)
        {
            return NotFound();
        }

        partner.IsActive = request.IsActive;
        partner.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { partner.Id, partner.IsActive });
    }

    [HttpPost("{id:guid}/logo")]
    [RequiresFeature(PlatformFeature.PassportPartners)]
    [RequestSizeLimit(PassportPartnerLogoRules.MaxBytes + 1024)]
    public async Task<ActionResult> Logo(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        var partner = await _db.PassportPartners.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (partner is null)
        {
            return NotFound();
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "logo_missing" });
        }

        await using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);
        try
        {
            partner.LogoPng = PassportPartnerLogoEncoder.ReencodePng(buffer.ToArray(), file.FileName, file.ContentType);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        partner.LogoContentType = "image/png";
        partner.LogoUpdatedAtUtc = DateTime.UtcNow;
        partner.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { partner.Id, bytes = partner.LogoPng.Length });
    }

    [HttpPost("{id:guid}/codes")]
    [RequiresFeature(PlatformFeature.PassportPartners)]
    public async Task<ActionResult> CreateCode(Guid id, [FromBody] CreatePartnerCodeRequest request, CancellationToken cancellationToken)
    {
        if (!await _db.PassportPartners.AnyAsync(p => p.Id == id, cancellationToken))
        {
            return NotFound();
        }

        try
        {
            var code = await _partners.CreateCodeAsync(id, request.BranchCompanyId, request.VanityCode, cancellationToken);
            return Ok(new { code.Id, code.CodeDisplay, code.BranchCompanyId });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("codes/{codeId:guid}/deactivate")]
    [RequiresFeature(PlatformFeature.PassportPartners)]
    public async Task<ActionResult> DeactivateCode(Guid codeId, CancellationToken cancellationToken)
    {
        var code = await _db.PassportPartnerCodes.FirstOrDefaultAsync(c => c.Id == codeId, cancellationToken);
        if (code is null)
        {
            return NotFound();
        }

        code.IsActive = false;
        code.DeactivatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { code.Id, code.IsActive });
    }

    [HttpGet("{id:guid}/codes.pdf")]
    [RequiresFeature(PlatformFeature.PassportPartners)]
    public async Task<ActionResult> CodesPdf(Guid id, CancellationToken cancellationToken)
    {
        var partner = await _db.PassportPartners.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (partner is null)
        {
            return NotFound();
        }

        var rows = await _db.PassportPartnerCodes.AsNoTracking()
            .Where(c => c.PassportPartnerId == id && c.IsActive)
            .Join(_db.Companies.AsNoTracking(), c => c.BranchCompanyId, company => company.Id, (c, company) => new { company.Name, c.CodeDisplay })
            .ToListAsync(cancellationToken);
        var pdf = PassportPartnerCodeListPdfService.Render(partner.DisplayName, rows.Select(r => (r.Name, r.CodeDisplay)).ToList());
        return File(pdf, "application/pdf", "partnercodes.pdf");
    }
}

public sealed record CreatePassportPartnerRequest(Guid CompanyId, string? DisplayName, int? MaxBranches);

public sealed record SetPartnerActiveRequest(bool IsActive);

public sealed record CreatePartnerCodeRequest(Guid BranchCompanyId, string? VanityCode);
