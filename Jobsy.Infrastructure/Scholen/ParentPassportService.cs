using System.Globalization;
using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.PupilPassport;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Scholen;

public interface IParentPassportService
{
    Task<(ParentPassportSessionDto? Ok, ParentPassportErrorDto? Error, int StatusCode)> OpenSessionAsync(
        string rawCode,
        CancellationToken cancellationToken = default);

    Task<(PupilG78PassportWebDto? Ok, ParentPassportErrorDto? Error, int StatusCode)> GetG78PassportWebAsync(
        ClaimsPrincipal parent,
        string? language,
        CancellationToken cancellationToken = default);

    Task<(ParentPassportAnswersWebDto? Ok, ParentPassportErrorDto? Error, int StatusCode)> GetTestAnswersWebAsync(
        ClaimsPrincipal parent,
        CancellationToken cancellationToken = default);

    Task<(byte[]? Bytes, string? FileName, ParentPassportErrorDto? Error, int StatusCode)> BuildPdfAsync(
        ClaimsPrincipal parent,
        string? language,
        CancellationToken cancellationToken = default);

    ClaimsPrincipal CreatePrincipal(ParentPassportSessionDto session);
}

public sealed class ParentPassportService : IParentPassportService
{
    public const string InvalidCode = "invalid_code";

    private readonly JobsyDbContext _db;
    private readonly IPupilCodeService _codes;
    private readonly PupilPassportPdfModelBuilder _passportModelBuilder;
    private readonly IPupilPassportPdfService _passportPdf;
    private readonly IPupilQuestionSetRegistry _registry;
    private readonly IPlatformFeatureService _features;
    private readonly TimeProvider _clock;

    public ParentPassportService(
        JobsyDbContext db,
        IPupilCodeService codes,
        PupilPassportPdfModelBuilder passportModelBuilder,
        IPupilPassportPdfService passportPdf,
        IPupilQuestionSetRegistry registry,
        IPlatformFeatureService features,
        TimeProvider? clock = null)
    {
        _db = db;
        _codes = codes;
        _passportModelBuilder = passportModelBuilder;
        _passportPdf = passportPdf;
        _registry = registry;
        _features = features;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<(ParentPassportSessionDto? Ok, ParentPassportErrorDto? Error, int StatusCode)> OpenSessionAsync(
        string rawCode,
        CancellationToken cancellationToken = default)
    {
        if (!PupilCodeFormat.TryNormalize(rawCode, out var normalized))
        {
            return Invalid();
        }

        var hash = _codes.LookupHash(normalized);
        var code = await _db.PupilCodes.AsNoTracking()
            .Include(c => c.SchoolClass)!.ThenInclude(c => c!.School)
            .Include(c => c.Result)
            .FirstOrDefaultAsync(c => c.CodeLookupHash == hash, cancellationToken);

        if (!IsEligible(code))
        {
            return Invalid();
        }

        if (_clock.GetUtcNow().UtcDateTime > AccessEndsUtc(code!))
        {
            return Invalid();
        }

        var accessEnds = AccessEndsUtc(code!);
        var expiresUnix = new DateTimeOffset(accessEnds, TimeSpan.Zero).ToUnixTimeSeconds();
        return (new ParentPassportSessionDto(code!.Id, expiresUnix), null, 200);
    }

    public async Task<(PupilG78PassportWebDto? Ok, ParentPassportErrorDto? Error, int StatusCode)> GetG78PassportWebAsync(
        ClaimsPrincipal parent,
        string? language,
        CancellationToken cancellationToken = default)
    {
        var (code, error, status) = await ResolveCodeAsync(parent, cancellationToken);
        if (code is null)
        {
            return (null, error, status);
        }

        var features = await _features.GetAsync(cancellationToken);
        if (!features.PupilPassportV2Enabled
            || code.SchoolClass!.QuestionSet != PupilQuestionSet.Groep78)
        {
            return InvalidPassport();
        }

        var display = _codes.Unprotect(code.CodeProtected) is { } raw
            ? PupilCodeFormat.Display(raw)
            : "******";
        var schoolName = code.SchoolClass.School?.Name ?? "";
        var passportModel = _passportModelBuilder.Build(
            code,
            code.SchoolClass,
            schoolName,
            display,
            displayNameOnPdf: null,
            language ?? "nl");
        return (PupilPassportWebMapper.ToGroep78Web(passportModel, enabled: true), null, 200);
    }

    public async Task<(ParentPassportAnswersWebDto? Ok, ParentPassportErrorDto? Error, int StatusCode)> GetTestAnswersWebAsync(
        ClaimsPrincipal parent,
        CancellationToken cancellationToken = default)
    {
        var (code, error, status) = await ResolveCodeAsync(parent, cancellationToken);
        if (code is null)
        {
            return (null, error, status);
        }

        var features = await _features.GetAsync(cancellationToken);
        if (!features.PupilPassportV2Enabled
            || code.SchoolClass!.QuestionSet != PupilQuestionSet.Groep78)
        {
            return (null, InvalidPassport().Error, InvalidPassport().StatusCode);
        }

        var def = _registry.ForClass(code.SchoolClass);
        var dto = ParentPassportAnswersBuilder.Build(def, code.Progress?.AnswersJson);
        return (dto, null, 200);
    }

    public async Task<(byte[]? Bytes, string? FileName, ParentPassportErrorDto? Error, int StatusCode)> BuildPdfAsync(
        ClaimsPrincipal parent,
        string? language,
        CancellationToken cancellationToken = default)
    {
        var (code, error, status) = await ResolveCodeAsync(parent, cancellationToken);
        if (code is null)
        {
            return (null, null, error, status);
        }

        var features = await _features.GetAsync(cancellationToken);
        if (!features.PupilPassportV2Enabled
            || code.SchoolClass!.QuestionSet != PupilQuestionSet.Groep78)
        {
            return (null, null, InvalidPassport().Error, InvalidPassport().StatusCode);
        }

        var display = _codes.Unprotect(code.CodeProtected) is { } raw
            ? PupilCodeFormat.Display(raw)
            : "******";
        var schoolName = code.SchoolClass.School?.Name ?? "";
        var passportModel = _passportModelBuilder.Build(
            code,
            code.SchoolClass,
            schoolName,
            display,
            displayNameOnPdf: null,
            language ?? "nl");
        var bytes = _passportPdf.Render(passportModel);
        var fileName = $"lobsy-dna-paspoort-{SanitizeFilePart(code.SchoolClass.Name)}.pdf";
        return (bytes, fileName, null, 200);
    }

    public ClaimsPrincipal CreatePrincipal(ParentPassportSessionDto session)
    {
        var claims = new List<Claim>
        {
            new(ParentPassportClaimTypes.PupilCodeId, session.PupilCodeId.ToString("D")),
            new(ParentPassportClaimTypes.AccessExpiresUtc,
                session.AccessExpiresUnix.ToString(CultureInfo.InvariantCulture)),
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, ParentPassportAuthDefaults.Scheme));
    }

    private async Task<(PupilCode? Code, ParentPassportErrorDto? Error, int StatusCode)> ResolveCodeAsync(
        ClaimsPrincipal parent,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(parent.FindFirst(ParentPassportClaimTypes.PupilCodeId)?.Value, out var codeId)
            || !long.TryParse(parent.FindFirst(ParentPassportClaimTypes.AccessExpiresUtc)?.Value, out var expiresUnix))
        {
            return (null, Invalid().Error, Invalid().StatusCode);
        }

        if (_clock.GetUtcNow() >= DateTimeOffset.FromUnixTimeSeconds(expiresUnix))
        {
            return (null, Invalid().Error, Invalid().StatusCode);
        }

        var code = await _db.PupilCodes.AsNoTracking()
            .Include(c => c.Result)
            .Include(c => c.Progress)
            .Include(c => c.SchoolClass)!.ThenInclude(c => c!.School)
            .FirstOrDefaultAsync(c => c.Id == codeId, cancellationToken);

        if (!IsEligible(code) || _clock.GetUtcNow().UtcDateTime > AccessEndsUtc(code!))
        {
            return (null, Invalid().Error, Invalid().StatusCode);
        }

        return (code, null, 200);
    }

    private static bool IsEligible(PupilCode? code)
    {
        if (code?.SchoolClass?.School is null
            || !code.SchoolClass.School.IsActive
            || code.SchoolClass.School.ProcessorAgreementSignedOn is null
            || code.Status != PupilCodeStatus.Completed
            || code.Result is null)
        {
            return false;
        }

        return true;
    }

    private static DateTime AccessEndsUtc(PupilCode code)
        => code.CreatedAtUtc.Add(ParentPassportAuthDefaults.AccessWindow);

    private static (ParentPassportSessionDto? Ok, ParentPassportErrorDto? Error, int StatusCode) Invalid()
        => (null, new ParentPassportErrorDto(InvalidCode, "invalid"), 404);

    private static (PupilG78PassportWebDto? Ok, ParentPassportErrorDto? Error, int StatusCode) InvalidPassport()
        => (null, new ParentPassportErrorDto(InvalidCode, "invalid"), 404);

    private static string SanitizeFilePart(string name)
    {
        var cleaned = new string(name.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "klas" : cleaned.ToLowerInvariant();
    }
}
