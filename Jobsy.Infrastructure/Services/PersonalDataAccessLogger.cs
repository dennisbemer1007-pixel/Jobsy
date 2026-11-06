using System.Security.Cryptography;
using System.Text;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class PersonalDataAccessLogger : IPersonalDataAccessLogger
{
    private readonly JobsyDbContext _db;
    private readonly ILogger<PersonalDataAccessLogger> _logger;
    private readonly string _ipSalt;

    public PersonalDataAccessLogger(
        JobsyDbContext db,
        IConfiguration configuration,
        ILogger<PersonalDataAccessLogger> logger)
    {
        _db = db;
        _logger = logger;
        _ipSalt = configuration["Privacy:IpHashSalt"]
                  ?? configuration["VerificationCodes:Pepper"]
                  ?? configuration["JobsyAuth:DevelopmentAuthSecret"]
                  ?? "jobsy-ip-hash-fallback";
    }

    public async Task LogAsync(PersonalDataAccessEntry entry, CancellationToken cancellationToken = default)
    {
        try
        {
            var row = new PersonalDataAccessLog
            {
                Id = Guid.NewGuid(),
                OccurredAt = DateTime.UtcNow,
                ActorUserId = entry.ActorUserId,
                ActorRole = string.IsNullOrWhiteSpace(entry.ActorRole) ? "Unknown" : entry.ActorRole.Trim(),
                SubjectUserId = entry.SubjectUserId,
                SubjectCompanyId = entry.SubjectCompanyId,
                SubjectPupilCodeId = entry.SubjectPupilCodeId,
                Resource = entry.Resource.Trim(),
                Action = entry.Action.Trim(),
                Reason = string.IsNullOrWhiteSpace(entry.Reason) ? null : entry.Reason.Trim(),
                SupportAccessGrantId = entry.SupportAccessGrantId,
                CorrelationId = string.IsNullOrWhiteSpace(entry.CorrelationId)
                    ? Guid.NewGuid().ToString("N")
                    : entry.CorrelationId.Trim(),
                IpHash = HashIp(entry.IpAddress)
            };

            _db.PersonalDataAccessLogs.Add(row);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Failed to persist PersonalDataAccessLog for resource {Resource} action {Action} actor {ActorUserId}",
                entry.Resource,
                entry.Action,
                entry.ActorUserId);
        }
    }

    private string? HashIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            return null;
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(_ipSalt + "|" + ip.Trim()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
