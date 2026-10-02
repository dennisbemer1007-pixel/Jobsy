using System.Security.Cryptography;
using System.Text;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Infrastructure.Scholen;

public interface IPupilCodeService
{
    Task<IReadOnlyList<PupilCode>> GenerateAsync(int count, SchoolClass schoolClass, CancellationToken cancellationToken = default);

    string LookupHash(string normalizedCode);

    string Protect(string normalizedCode);

    string? Unprotect(string protectedPayload);

    Task<PupilCode> ReplaceAsync(PupilCode existing, CancellationToken cancellationToken = default);
}

public sealed class PupilCodeService : IPupilCodeService
{
    public const string ProtectPurpose = "Scholen.PupilCode";
    private const string DevKeyFileName = "scholen-code-hmac.key";

    private readonly JobsyDbContext _db;
    private readonly IDataProtector _protector;
    private readonly byte[] _hmacKey;

    public PupilCodeService(
        JobsyDbContext db,
        IDataProtectionProvider dataProtection,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _db = db;
        _protector = dataProtection.CreateProtector(ProtectPurpose);
        _hmacKey = ResolveHmacKey(configuration, environment);
    }

    public async Task<IReadOnlyList<PupilCode>> GenerateAsync(
        int count,
        SchoolClass schoolClass,
        CancellationToken cancellationToken = default)
    {
        if (count is < 1 or > 40)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Aantal codes moet 1–40 zijn.");
        }

        ArgumentNullException.ThrowIfNull(schoolClass);

        var existingHashes = await _db.PupilCodes
            .AsNoTracking()
            .Where(c => c.SchoolClassId == schoolClass.Id)
            .Select(c => c.CodeLookupHash)
            .ToListAsync(cancellationToken);
        var used = new HashSet<string>(existingHashes, StringComparer.Ordinal);
        var maxNumber = await _db.PupilCodes
            .Where(c => c.SchoolClassId == schoolClass.Id)
            .Select(c => (int?)c.Number)
            .MaxAsync(cancellationToken) ?? 0;

        var created = new List<PupilCode>(count);
        var alphabet = PupilCodeFormat.Alphabet.ToCharArray();
        // Allocate once per batch — never stackalloc inside the collision retry loop (CA2014).
        Span<char> chars = stackalloc char[PupilCodeFormat.Length];
        for (var i = 0; i < count; i++)
        {
            var code = NextUniquePlainCode(alphabet, chars, used, LookupHash);
            var hash = LookupHash(code);

            var row = new PupilCode
            {
                Id = Guid.NewGuid(),
                SchoolClassId = schoolClass.Id,
                Number = ++maxNumber,
                CodeLookupHash = hash,
                CodeProtected = Protect(code),
                Status = PupilCodeStatus.NotStarted,
                SessionVersion = 0,
                CreatedAtUtc = DateTime.UtcNow
            };
            _db.PupilCodes.Add(row);
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

    public async Task<PupilCode> ReplaceAsync(PupilCode existing, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(existing);

        var alphabet = PupilCodeFormat.Alphabet.ToCharArray();
        var classHashes = await _db.PupilCodes
            .AsNoTracking()
            .Where(c => c.SchoolClassId == existing.SchoolClassId && c.Id != existing.Id)
            .Select(c => c.CodeLookupHash)
            .ToListAsync(cancellationToken);
        var used = new HashSet<string>(classHashes, StringComparer.Ordinal);

        Span<char> chars = stackalloc char[PupilCodeFormat.Length];
        var code = NextUniquePlainCode(
            alphabet,
            chars,
            used,
            LookupHash,
            "Kon geen nieuwe unieke leerlingcode genereren.");
        var hash = LookupHash(code);

        existing.CodeLookupHash = hash;
        existing.CodeProtected = Protect(code);
        existing.SessionVersion++;
        existing.LockedUntilUtc = null;
        await _db.SaveChangesAsync(cancellationToken);
        return existing;
    }

    /// <summary>
    /// Generates a unique plain code by retrying into <paramref name="used"/>.
    /// The char buffer must be allocated by the caller (never inside the loop).
    /// </summary>
    internal static string NextUniquePlainCode(
        char[] alphabet,
        Span<char> chars,
        HashSet<string> used,
        Func<string, string> lookupHash,
        string exhaustedMessage = "Kon geen unieke leerlingcode genereren.",
        int maxAttempts = 100)
    {
        ArgumentNullException.ThrowIfNull(alphabet);
        ArgumentNullException.ThrowIfNull(used);
        ArgumentNullException.ThrowIfNull(lookupHash);
        if (chars.Length < PupilCodeFormat.Length)
        {
            throw new ArgumentException("Buffer is te klein voor een leerlingcode.", nameof(chars));
        }

        var attempts = 0;
        while (true)
        {
            if (++attempts > maxAttempts)
            {
                throw new InvalidOperationException(exhaustedMessage);
            }

            RandomNumberGenerator.GetItems(alphabet, chars[..PupilCodeFormat.Length]);
            var code = new string(chars[..PupilCodeFormat.Length]);
            var hash = lookupHash(code);
            if (used.Add(hash))
            {
                return code;
            }
        }
    }

    private static byte[] ResolveHmacKey(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration["Scholen:CodeHmacKey"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Encoding.UTF8.GetBytes(configured.Trim());
        }

        if (environment.IsProduction())
        {
            throw new InvalidOperationException(
                "Scholen:CodeHmacKey ontbreekt. Stel een geheime HMAC-sleutel in voor productie.");
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
