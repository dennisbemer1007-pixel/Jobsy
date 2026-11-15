using System.Security.Cryptography;
using System.Text;
using Jobsy.Core.Entities.WerkgeverApk;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Scholen;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Infrastructure.WerkgeverApk;

public interface IWerkgeverApkEmployeeCodeService
{
    Task<IReadOnlyList<WerkgeverApkEmployeeCode>> GenerateAsync(
        int count,
        WerkgeverApkTeam team,
        CancellationToken cancellationToken = default);

    string LookupHash(string normalizedCode);

    string Protect(string normalizedCode);

    string? Unprotect(string protectedPayload);
}

public sealed class WerkgeverApkEmployeeCodeService : IWerkgeverApkEmployeeCodeService
{
    public const string ProtectPurpose = "WerkgeverApk.EmployeeCode";
    private const string DevKeyFileName = "werkgever-apk-code-hmac.key";

    private readonly JobsyDbContext _db;
    private readonly IDataProtector _protector;
    private readonly byte[] _hmacKey;

    public WerkgeverApkEmployeeCodeService(
        JobsyDbContext db,
        IDataProtectionProvider dataProtection,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _db = db;
        _protector = dataProtection.CreateProtector(ProtectPurpose);
        _hmacKey = ResolveHmacKey(configuration, environment);
    }

    public async Task<IReadOnlyList<WerkgeverApkEmployeeCode>> GenerateAsync(
        int count,
        WerkgeverApkTeam team,
        CancellationToken cancellationToken = default)
    {
        if (count is < 1 or > 40)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Aantal codes moet 1–40 zijn.");
        }

        ArgumentNullException.ThrowIfNull(team);

        var existingHashes = await _db.WerkgeverApkEmployeeCodes
            .AsNoTracking()
            .Where(c => c.TeamId == team.Id)
            .Select(c => c.CodeLookupHash)
            .ToListAsync(cancellationToken);
        var used = new HashSet<string>(existingHashes, StringComparer.Ordinal);
        var maxNumber = await _db.WerkgeverApkEmployeeCodes
            .Where(c => c.TeamId == team.Id)
            .Select(c => (int?)c.Number)
            .MaxAsync(cancellationToken) ?? 0;

        var created = new List<WerkgeverApkEmployeeCode>(count);
        var alphabet = PupilCodeFormat.Alphabet.ToCharArray();
        Span<char> chars = stackalloc char[PupilCodeFormat.Length];
        for (var i = 0; i < count; i++)
        {
            var code = PupilCodeService.NextUniquePlainCode(
                alphabet,
                chars,
                used,
                LookupHash,
                "Kon geen unieke medewerkercode genereren.");
            var hash = LookupHash(code);

            var row = new WerkgeverApkEmployeeCode
            {
                Id = Guid.NewGuid(),
                TeamId = team.Id,
                Number = ++maxNumber,
                CodeLookupHash = hash,
                CodeProtected = Protect(code),
                Status = WerkgeverApkEmployeeCodeStatus.NotStarted,
                SessionVersion = 0,
                CreatedAtUtc = DateTime.UtcNow
            };
            _db.WerkgeverApkEmployeeCodes.Add(row);
            created.Add(row);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return created;
    }

    public string LookupHash(string normalizedCode)
    {
        var code = PupilCodeFormat.Normalize(normalizedCode);
        var hash = HMACSHA256.HashData(_hmacKey, Encoding.UTF8.GetBytes(code));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public string Protect(string normalizedCode)
    {
        var code = PupilCodeFormat.Normalize(normalizedCode);
        return _protector.Protect(code);
    }

    public string? Unprotect(string protectedPayload)
    {
        if (string.IsNullOrWhiteSpace(protectedPayload))
        {
            return null;
        }

        try
        {
            return _protector.Unprotect(protectedPayload);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    private static byte[] ResolveHmacKey(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration["WerkgeverApk:CodeHmacKey"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Encoding.UTF8.GetBytes(configured.Trim());
        }

        if (environment.IsProduction())
        {
            throw new InvalidOperationException(
                "WerkgeverApk:CodeHmacKey ontbreekt. Stel een geheime HMAC-sleutel in voor productie.");
        }

        var path = Path.Combine(environment.ContentRootPath, DevKeyFileName);
        if (File.Exists(path))
        {
            return Convert.FromBase64String(File.ReadAllText(path).Trim());
        }

        var key = RandomNumberGenerator.GetBytes(32);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, Convert.ToBase64String(key));
        return key;
    }
}
