using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

/// <summary>
/// Idempotent one-shot: re-save plaintext IBAN columns so the EF value converter encrypts them.
/// Detects already-protected DB values via raw SQL (<c>dp1:</c> prefix).
/// </summary>
public sealed class IbanEncryptionMigrationHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IbanEncryptionMigrationHostedService> _logger;

    public IbanEncryptionMigrationHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<IbanEncryptionMigrationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        try
        {
            await EncryptAsync(stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "IBAN encryption migration failed.");
        }
    }

    private async Task EncryptAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();

        var changed = 0;
        changed += await EncryptTableAsync(db, "SalesManagerProfiles", cancellationToken);
        changed += await EncryptTableAsync(db, "AmbassadeurProfiles", cancellationToken);
        changed += await EncryptTableAsync(db, "PartnerAffiliateProfiles", cancellationToken);

        if (changed > 0)
        {
            _logger.LogInformation("Triggered encryption for {Count} plaintext IBAN profile row(s).", changed);
        }
    }

    private static async Task<int> EncryptTableAsync(
        JobsyDbContext db,
        string table,
        CancellationToken cancellationToken)
    {
        // Bypass value converter: inspect stored ciphertext prefix.
        var sql = $"""
            SELECT "Id" AS "Value"
            FROM "{table}"
            WHERE "Iban" IS NOT NULL
              AND TRIM("Iban") <> ''
              AND "Iban" NOT LIKE '{IbanProtector.Prefix}%'
            """;

        List<Guid> ids;
        try
        {
            ids = await db.Database.SqlQueryRaw<Guid>(sql).ToListAsync(cancellationToken);
        }
        catch (Exception)
        {
            // Table may not exist yet during early migrate; ignore.
            return 0;
        }

        if (ids.Count == 0)
        {
            return 0;
        }

        foreach (var id in ids)
        {
            // Load via EF (Unprotect → plaintext), mark Iban modified, Save (Protect).
            if (table == "SalesManagerProfiles")
            {
                var row = await db.SalesManagerProfiles.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
                if (row?.Iban is null)
                {
                    continue;
                }

                db.Entry(row).Property(p => p.Iban).IsModified = true;
            }
            else if (table == "AmbassadeurProfiles")
            {
                var row = await db.AmbassadeurProfiles.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
                if (row?.Iban is null)
                {
                    continue;
                }

                db.Entry(row).Property(p => p.Iban).IsModified = true;
            }
            else
            {
                var row = await db.PartnerAffiliateProfiles.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
                if (row?.Iban is null)
                {
                    continue;
                }

                db.Entry(row).Property(p => p.Iban).IsModified = true;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return ids.Count;
    }
}
