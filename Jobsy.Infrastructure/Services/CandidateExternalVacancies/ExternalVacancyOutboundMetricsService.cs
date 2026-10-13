using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services.CandidateExternalVacancies;

public interface IExternalVacancyOutboundMetricsService
{
    Task MarkEmployerAccountCreatedAsync(
        string contactEmail,
        Guid? outboundId = null,
        CancellationToken cancellationToken = default);

    Task MarkApplicationAcceptedAsync(Guid applicationId, CancellationToken cancellationToken = default);
}

/// <summary>Updates admin metrics columns on <see cref="Core.Entities.CandidateExternalVacancyOutbound"/>.</summary>
public sealed class ExternalVacancyOutboundMetricsService : IExternalVacancyOutboundMetricsService
{
    private readonly JobsyDbContext _db;

    public ExternalVacancyOutboundMetricsService(JobsyDbContext db)
    {
        _db = db;
    }

    public async Task MarkEmployerAccountCreatedAsync(
        string contactEmail,
        Guid? outboundId = null,
        CancellationToken cancellationToken = default)
    {
        if (!CandidateExternalVacancyRules.IsValidEmployerEmail(contactEmail))
        {
            return;
        }

        var email = CandidateExternalVacancyRules.NormalizeEmployerEmail(contactEmail);
        var now = DateTime.UtcNow;
        List<Core.Entities.CandidateExternalVacancyOutbound> rows;
        if (outboundId is Guid id)
        {
            rows = await _db.CandidateExternalVacancyOutbounds
                .Where(o => o.Id == id && o.EmployerAccountCreatedAtUtc == null)
                .ToListAsync(cancellationToken);
        }
        else
        {
            rows = await _db.CandidateExternalVacancyOutbounds
                .Where(o => o.EmployerEmailNormalized == email && o.EmployerAccountCreatedAtUtc == null)
                .ToListAsync(cancellationToken);
        }
        if (rows.Count == 0)
        {
            return;
        }

        foreach (var row in rows)
        {
            row.EmployerAccountCreatedAtUtc = now;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkApplicationAcceptedAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        var vacancy = await _db.CandidateExternalVacancies
            .FirstOrDefaultAsync(v => v.ApplicationId == applicationId, cancellationToken);
        if (vacancy is null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var outbounds = await _db.CandidateExternalVacancyOutbounds
            .Where(o => o.ExternalVacancyId == vacancy.Id && o.AcceptedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var row in outbounds)
        {
            row.AcceptedAtUtc = now;
        }

        if (outbounds.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
