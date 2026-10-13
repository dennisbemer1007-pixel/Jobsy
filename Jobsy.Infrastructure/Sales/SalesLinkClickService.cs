using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Sales;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Sales;

public sealed class SalesLinkClickService : ISalesLinkClickService
{
    private readonly JobsyDbContext _db;

    public SalesLinkClickService(JobsyDbContext db) => _db = db;

    public async Task RecordClickAsync(
        Guid beneficiaryUserId,
        SalesLinkChannel channel,
        DateOnly? localDate = null,
        CancellationToken cancellationToken = default)
    {
        var date = localDate ?? SalesClock.Today();
        var existing = await _db.SalesLinkClickDailies
            .FirstOrDefaultAsync(
                r => r.BeneficiaryUserId == beneficiaryUserId
                     && r.Date == date
                     && r.Channel == channel,
                cancellationToken);

        if (existing is null)
        {
            _db.SalesLinkClickDailies.Add(new SalesLinkClickDaily
            {
                BeneficiaryUserId = beneficiaryUserId,
                Date = date,
                Channel = channel,
                Count = 1
            });
        }
        else
        {
            existing.Count += 1;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
