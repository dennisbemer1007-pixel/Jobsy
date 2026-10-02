using System.Text.Json;
using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services.Letters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Services.Verification;

public sealed class CompanyVerificationFlowService : ICompanyVerificationFlowService
{
    private readonly JobsyDbContext _db;
    private readonly ICompanyVerificationService _verification;
    private readonly IKvkService _kvk;
    private readonly ILetterService _letters;
    private readonly ITransactionalMailer _mailer;
    private readonly IOptions<CompanyVerificationSettings> _options;
    private readonly IPlatformCompanySettingsService _brand;
    private readonly ILogger<CompanyVerificationFlowService> _logger;

    public CompanyVerificationFlowService(
        JobsyDbContext db,
        ICompanyVerificationService verification,
        IKvkService kvk,
        ILetterService letters,
        ITransactionalMailer mailer,
        IOptions<CompanyVerificationSettings> options,
        IPlatformCompanySettingsService brand,
        ILogger<CompanyVerificationFlowService> logger)
    {
        _db = db;
        _verification = verification;
        _kvk = kvk;
        _letters = letters;
        _mailer = mailer;
        _options = options;
        _brand = brand;
        _logger = logger;
    }

    public async Task<CompanyVerificationOptionsView> GetOptionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var (user, root) = await LoadEmployerRootAsync(userId, cancellationToken);
        var settings = _options.Value;
        var profile = await TryProfileAsync(root.KvkNumber, cancellationToken);
        var websites = profile?.Websites ?? [];
        var domains = websites
            .Select(DomainMatch.RegistrableDomain)
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();

        var emailAvailable = domains.Count > 0;
        var suggested = user.Email;
        if (emailAvailable && !DomainMatch.EmailMatchesAnyWebsite(suggested, websites))
        {
            suggested = domains.Count > 0 ? $"info@{domains[0]}" : suggested;
        }

        var monthCount = await CountLettersThisMonthAsync(cancellationToken);
        var capReached = monthCount >= Math.Max(1, settings.MonthlyLetterCap);
        var address = ResolveLetterAddress(root, profile);
        var addressMasked = address is null ? null : MaskAddress(address);

        var activeLetter = await _db.CompanyVerificationLetters
            .Where(l => l.CompanyId == root.Id
                        && l.Status != CompanyVerificationLetterStatus.Used
                        && l.Status != CompanyVerificationLetterStatus.Expired
                        && l.Status != CompanyVerificationLetterStatus.Blocked
                        && l.ExpiresAtUtc > DateTime.UtcNow)
            .OrderByDescending(l => l.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var manualPending = root.ManualVerificationOpenedAtUtc is not null
            && root.ManualVerificationClosedAtUtc is null;

        string? letterUnavailable = null;
        var letterAvailable = true;
        if (address is null)
        {
            letterAvailable = false;
            letterUnavailable = "no_address";
        }
        else if (capReached)
        {
            letterAvailable = false;
            letterUnavailable = "monthly_cap";
        }

        var resendsRemaining = activeLetter is null
            ? settings.LetterMaxResends
            : Math.Max(0, settings.LetterMaxResends - activeLetter.ResendCount);
        DateTime? resendAt = activeLetter?.SentAtUtc?.AddDays(settings.LetterResendAfterDays);

        return new CompanyVerificationOptionsView(
            root.Id,
            root.Name,
            root.KvkNumber,
            root.VerificationStatus,
            root.VerificationMethod,
            emailAvailable,
            domains,
            suggested,
            letterAvailable,
            addressMasked,
            letterUnavailable,
            manualPending,
            activeLetter?.Id,
            activeLetter?.SentAtUtc,
            activeLetter?.ExpiresAtUtc,
            resendsRemaining,
            resendAt);
    }

    public async Task<EmailVerificationStartResult> StartEmailAsync(
        Guid userId,
        string email,
        CancellationToken cancellationToken = default)
    {
        var (_, root) = await LoadEmployerRootAsync(userId, cancellationToken);
        EnsureNotVerified(root);

        var normalized = (email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || !normalized.Contains('@'))
        {
            return new EmailVerificationStartResult(false, "invalid_email", "Ongeldig e-mailadres.", null, null);
        }

        if (FreeMailDomains.IsFreeMail(normalized))
        {
            return new EmailVerificationStartResult(
                false, "domain_mismatch", "Gratis mailadressen tellen niet mee.", null, null);
        }

        var profile = await TryProfileAsync(root.KvkNumber, cancellationToken);
        var websites = profile?.Websites ?? [];
        if (websites.Count == 0)
        {
            return new EmailVerificationStartResult(
                false, "domain_mismatch", "Bij de KVK staat geen website.", null, null);
        }

        if (!DomainMatch.EmailMatchesAnyWebsite(normalized, websites))
        {
            var expected = string.Join(", ", websites
                .Select(DomainMatch.RegistrableDomain)
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Distinct(StringComparer.OrdinalIgnoreCase));
            return new EmailVerificationStartResult(
                false,
                "domain_mismatch",
                $"Het e-maildomein past niet bij de website(s) bij KVK ({expected}).",
                null,
                null);
        }

        var settings = _options.Value;
        var hourAgo = DateTime.UtcNow.AddHours(-1);
        var sendsLastHour = await _db.CompanyVerificationEmailChallenges
            .CountAsync(c => c.CompanyId == root.Id && c.CreatedAtUtc >= hourAgo, cancellationToken);
        if (sendsLastHour >= settings.EmailMaxSendsPerHour)
        {
            return new EmailVerificationStartResult(
                false, "rate_limited", "Te veel codes aangevraagd. Probeer later opnieuw.", null, null);
        }

        var latest = await _db.CompanyVerificationEmailChallenges
            .Where(c => c.CompanyId == root.Id)
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (latest?.LockedUntilUtc is DateTime locked && locked > DateTime.UtcNow)
        {
            return new EmailVerificationStartResult(
                false,
                "locked",
                $"Nieuwe code mogelijk vanaf {locked:HH:mm} UTC.",
                null,
                null);
        }

        var code = VerificationCodes.CreateNumericCode(6);
        var now = DateTime.UtcNow;
        var challenge = new CompanyVerificationEmailChallenge
        {
            Id = Guid.NewGuid(),
            CompanyId = root.Id,
            RequestedByUserId = userId,
            Email = normalized,
            CodeHash = VerificationCodes.Hash(code),
            ExpiresAtUtc = now.AddMinutes(settings.EmailCodeValidityMinutes),
            FailedAttempts = 0,
            CreatedAtUtc = now
        };
        _db.CompanyVerificationEmailChallenges.Add(challenge);
        await _db.SaveChangesAsync(cancellationToken);

        var composed = TransactionalEmails.CompanyBusinessEmailVerification(
            root.Name,
            code,
            PublicBase());
        await _mailer.SendAsync(composed, normalized, cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Business-email verification started company={CompanyId} email={Email} (code not logged)",
            root.Id,
            MaskEmail(normalized));

        return new EmailVerificationStartResult(
            true,
            null,
            null,
            challenge.ExpiresAtUtc,
            MaskEmail(normalized));
    }

    public async Task<VerificationConfirmResult> ConfirmEmailAsync(
        Guid userId,
        string code,
        CancellationToken cancellationToken = default)
    {
        var (_, root) = await LoadEmployerRootAsync(userId, cancellationToken);
        if (root.VerificationStatus == CompanyVerificationStatus.Verified)
        {
            return new VerificationConfirmResult(true, null, null, root.VerificationStatus);
        }

        var challenge = await _db.CompanyVerificationEmailChallenges
            .Where(c => c.CompanyId == root.Id && c.ConsumedAtUtc == null)
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (challenge is null)
        {
            return new VerificationConfirmResult(false, "no_challenge", "Vraag eerst een code aan.", null);
        }

        if (challenge.LockedUntilUtc is DateTime locked && locked > DateTime.UtcNow)
        {
            return new VerificationConfirmResult(false, "locked", "Code geblokkeerd. Vraag later een nieuwe aan.", null);
        }

        if (challenge.ExpiresAtUtc < DateTime.UtcNow || string.IsNullOrWhiteSpace(challenge.CodeHash))
        {
            return new VerificationConfirmResult(false, "expired", "Code verlopen. Vraag een nieuwe aan.", null);
        }

        if (!VerificationCodes.MatchesHash(challenge.CodeHash, code))
        {
            var attempts = challenge.FailedAttempts;
            var dead = VerificationCodes.RegisterFailedAttempt(ref attempts);
            challenge.FailedAttempts = attempts;
            if (dead)
            {
                challenge.CodeHash = string.Empty;
                challenge.LockedUntilUtc = DateTime.UtcNow.AddMinutes(_options.Value.EmailCooldownAfterLockoutMinutes);
            }

            await _db.SaveChangesAsync(cancellationToken);
            return new VerificationConfirmResult(
                false,
                dead ? "dead" : "invalid_code",
                dead ? "Te veel foute pogingen. Vraag over 15 minuten een nieuwe code aan." : "Onjuiste code.",
                null);
        }

        challenge.ConsumedAtUtc = DateTime.UtcNow;
        challenge.CodeHash = string.Empty;
        await _db.SaveChangesAsync(cancellationToken);

        await _verification.MarkVerifiedAsync(
            root.Id, CompanyVerificationMethod.BusinessEmail, userId, note: challenge.Email, cancellationToken);

        return new VerificationConfirmResult(true, null, null, CompanyVerificationStatus.Verified);
    }

    public async Task<LetterVerificationStartResult> RequestLetterAsync(
        Guid userId,
        bool isResend,
        CancellationToken cancellationToken = default)
        => await SendLetterInternalAsync(userId, companyOverride: null, adminBypassLimit: false, isResend, cancellationToken);

    public async Task<LetterVerificationStartResult> AdminSendLetterAsync(
        Guid companyId,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
        => await SendLetterInternalAsync(adminUserId, companyOverride: companyId, adminBypassLimit: true, isResend: false, cancellationToken);

    public async Task<VerificationConfirmResult> ConfirmLetterAsync(
        Guid userId,
        string code,
        CancellationToken cancellationToken = default)
    {
        var (_, root) = await LoadEmployerRootAsync(userId, cancellationToken);
        if (root.VerificationStatus == CompanyVerificationStatus.Verified)
        {
            return new VerificationConfirmResult(true, null, null, root.VerificationStatus);
        }

        var letter = await _db.CompanyVerificationLetters
            .Where(l => l.CompanyId == root.Id
                        && l.Status != CompanyVerificationLetterStatus.Used
                        && l.Status != CompanyVerificationLetterStatus.Expired)
            .OrderByDescending(l => l.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (letter is null)
        {
            return new VerificationConfirmResult(false, "no_letter", "Geen brief aangevraagd.", null);
        }

        if (letter.Status == CompanyVerificationLetterStatus.Blocked)
        {
            return new VerificationConfirmResult(false, "blocked", "Brief geblokkeerd na 5 pogingen.", null);
        }

        if (letter.ExpiresAtUtc < DateTime.UtcNow)
        {
            letter.Status = CompanyVerificationLetterStatus.Expired;
            await _db.SaveChangesAsync(cancellationToken);
            return new VerificationConfirmResult(false, "expired", "Code verlopen.", null);
        }

        var normalized = LetterVerificationCodes.Normalize(code);
        if (!LetterVerificationCodes.IsWellFormed(normalized)
            || !VerificationCodes.MatchesHash(letter.CodeHash, normalized))
        {
            var attempts = letter.FailedAttempts;
            var dead = VerificationCodes.RegisterFailedAttempt(ref attempts);
            letter.FailedAttempts = attempts;
            if (dead)
            {
                letter.Status = CompanyVerificationLetterStatus.Blocked;
                letter.BlockedAtUtc = DateTime.UtcNow;
                letter.CodeHash = string.Empty;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return new VerificationConfirmResult(
                false,
                dead ? "blocked" : "invalid_code",
                dead ? "Brief geblokkeerd na 5 pogingen." : "Onjuiste code.",
                null);
        }

        letter.Status = CompanyVerificationLetterStatus.Used;
        letter.UsedAtUtc = DateTime.UtcNow;
        letter.CodeHash = string.Empty;
        await _db.SaveChangesAsync(cancellationToken);

        await _verification.MarkVerifiedAsync(
            root.Id, CompanyVerificationMethod.Letter, userId, note: null, cancellationToken);

        return new VerificationConfirmResult(true, null, null, CompanyVerificationStatus.Verified);
    }

    public async Task<ManualVerificationStartResult> RequestManualAsync(
        Guid userId,
        string reason,
        string? message,
        IReadOnlyList<string>? attachmentIds,
        CancellationToken cancellationToken = default)
    {
        var (_, root) = await LoadEmployerRootAsync(userId, cancellationToken);
        EnsureNotVerified(root);

        if (string.IsNullOrWhiteSpace(reason))
        {
            return new ManualVerificationStartResult(false, "reason_required", "Geef een reden op.");
        }

        var open = await _db.CompanyManualVerificationRequests
            .AnyAsync(r => r.CompanyId == root.Id && r.DecidedAtUtc == null, cancellationToken);
        if (open)
        {
            return new ManualVerificationStartResult(false, "already_open", "Er loopt al een handmatige controle.");
        }

        var req = new CompanyManualVerificationRequest
        {
            Id = Guid.NewGuid(),
            CompanyId = root.Id,
            RequestedByUserId = userId,
            Reason = reason.Trim(),
            Message = string.IsNullOrWhiteSpace(message) ? null : message.Trim(),
            AttachmentIdsJson = attachmentIds is { Count: > 0 }
                ? JsonSerializer.Serialize(attachmentIds)
                : null,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.CompanyManualVerificationRequests.Add(req);
        await _db.SaveChangesAsync(cancellationToken);

        await _verification.MarkPendingAsync(
            root.Id, CompanyVerificationMethod.Manual, userId, reason, cancellationToken);

        return new ManualVerificationStartResult(true, null, null);
    }

    private async Task<LetterVerificationStartResult> SendLetterInternalAsync(
        Guid actorUserId,
        Guid? companyOverride,
        bool adminBypassLimit,
        bool isResend,
        CancellationToken cancellationToken)
    {
        Company root;
        User user;
        if (companyOverride is Guid companyId)
        {
            root = await ResolveRootCompanyAsync(companyId, cancellationToken);
            user = await _db.Users.FirstAsync(u => u.Id == actorUserId, cancellationToken);
        }
        else
        {
            (user, root) = await LoadEmployerRootAsync(actorUserId, cancellationToken);
            EnsureNotVerified(root);
        }

        var settings = _options.Value;
        var profile = await TryProfileAsync(root.KvkNumber, cancellationToken);
        var address = ResolveLetterAddress(root, profile);
        if (address is null)
        {
            return FailLetter("no_address", "Geen postadres of bezoekadres bij KVK.");
        }

        if (!adminBypassLimit)
        {
            var monthCount = await CountLettersThisMonthAsync(cancellationToken);
            if (monthCount >= Math.Max(1, settings.MonthlyLetterCap))
            {
                return FailLetter("monthly_cap", "Tijdelijk niet beschikbaar, vraag een handmatige controle aan.");
            }
        }

        var existing = await _db.CompanyVerificationLetters
            .Where(l => l.KvkNumber == root.KvkNumber)
            .OrderByDescending(l => l.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var windowStart = DateTime.UtcNow.AddDays(-30);
        var primaryInWindow = existing.FirstOrDefault(l =>
            l.CreatedAtUtc >= windowStart
            && l.ResendOfLetterId is null
            && l.Status != CompanyVerificationLetterStatus.Expired);

        CompanyVerificationLetter? resendOf = null;
        var resendCount = 0;
        if (isResend)
        {
            var latest = existing.FirstOrDefault(l =>
                l.CompanyId == root.Id
                && l.Status is CompanyVerificationLetterStatus.Requested
                    or CompanyVerificationLetterStatus.Sent
                    or CompanyVerificationLetterStatus.Delivered);
            if (latest is null)
            {
                return FailLetter("no_letter", "Geen brief om opnieuw te versturen.");
            }

            if (latest.SentAtUtc is null
                || latest.SentAtUtc.Value.AddDays(settings.LetterResendAfterDays) > DateTime.UtcNow)
            {
                return FailLetter("resend_too_early", "Opnieuw versturen kan pas na 7 dagen.");
            }

            if (latest.ResendCount >= settings.LetterMaxResends)
            {
                return FailLetter("resend_exhausted", "Maximaal aantal herzendingen bereikt.");
            }

            resendOf = latest;
            resendCount = latest.ResendCount + 1;
            // Invalidate earlier code.
            latest.Status = CompanyVerificationLetterStatus.Expired;
            latest.CodeHash = string.Empty;
        }
        else if (!adminBypassLimit && primaryInWindow is not null)
        {
            return FailLetter("kvk_limit", "Maximaal 1 brief per KvK-nummer per 30 dagen.");
        }

        var plainCode = LetterVerificationCodes.Create();
        var formatted = LetterVerificationCodes.Format(plainCode);
        var now = DateTime.UtcNow;
        var expires = now.AddDays(settings.LetterValidityDays);
        var logo = _brand.GetBrandLogoPng();
        var pdf = VerificationLetterPdfBuilder.Build(
            root.Name,
            user.FullName,
            FunctionLabel(user),
            address.AddressLines,
            address.PostalCode,
            address.City,
            formatted,
            expires.ToLocalTime().Date,
            settings.SupportEmail,
            logo);

        var provider = ResolveProviderKind(settings);
        var send = await _letters.SendAsync(
            new LetterRequest(
                root.Name,
                address.AddressLines,
                address.PostalCode,
                address.City,
                address.Country,
                pdf,
                $"wa-{root.KvkNumber}-{now:yyyyMMddHHmmss}"),
            cancellationToken);

        if (!send.Ok)
        {
            return FailLetter(send.ErrorCode ?? "send_failed", send.ErrorMessage ?? "Versturen mislukt.");
        }

        var letter = new CompanyVerificationLetter
        {
            Id = Guid.NewGuid(),
            CompanyId = root.Id,
            KvkNumber = root.KvkNumber,
            RequestedByUserId = actorUserId,
            CodeHash = VerificationCodes.Hash(plainCode),
            ExpiresAtUtc = expires,
            FailedAttempts = 0,
            ResendCount = resendCount,
            ResendOfLetterId = resendOf?.Id,
            AddressLine1 = address.AddressLines.ElementAtOrDefault(0) ?? root.Address,
            AddressLine2 = address.AddressLines.ElementAtOrDefault(1),
            PostalCode = address.PostalCode,
            City = address.City,
            Country = address.Country,
            Provider = provider,
            ProviderLetterId = send.ProviderLetterId,
            Status = CompanyVerificationLetterStatus.Sent,
            CreatedAtUtc = now,
            SentAtUtc = now
        };
        _db.CompanyVerificationLetters.Add(letter);
        await _db.SaveChangesAsync(cancellationToken);

        if (root.VerificationStatus != CompanyVerificationStatus.Verified)
        {
            await _verification.MarkPendingAsync(
                root.Id, CompanyVerificationMethod.Letter, actorUserId, "letter_sent", cancellationToken);
        }

        // Alert at 80 % of monthly cap.
        var monthCountAfter = await CountLettersThisMonthAsync(cancellationToken);
        var alertAt = (int)Math.Floor(settings.MonthlyLetterCap * settings.MonthlyLetterAlertRatio);
        if (monthCountAfter >= alertAt)
        {
            _db.PlatformLogs.Add(new PlatformLog
            {
                Id = Guid.NewGuid(),
                Level = PlatformLogLevel.Warning,
                Category = "company.verification.letter_cap_alert",
                Message = $"month_count={monthCountAfter};cap={settings.MonthlyLetterCap}",
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
        }

        var resendsRemaining = Math.Max(0, settings.LetterMaxResends - letter.ResendCount);
        return new LetterVerificationStartResult(
            true,
            null,
            null,
            letter.Id,
            MaskAddress(address),
            letter.ExpiresAtUtc,
            letter.SentAtUtc,
            letter.SentAtUtc?.AddDays(settings.LetterResendAfterDays),
            resendsRemaining);
    }

    private static LetterVerificationStartResult FailLetter(string code, string message)
        => new(false, code, message, null, null, null, null, null, 0);

    private static LetterProviderKind ResolveProviderKind(CompanyVerificationSettings settings)
    {
        if (settings.LetterProvider == LetterProviderKind.Pingen
            && !string.IsNullOrWhiteSpace(settings.PingenClientId)
            && !string.IsNullOrWhiteSpace(settings.PingenClientSecret)
            && !string.IsNullOrWhiteSpace(settings.PingenOrganisationId))
        {
            return LetterProviderKind.Pingen;
        }

        return LetterProviderKind.Stub;
    }

    private async Task<(User User, Company Root)> LoadEmployerRootAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("User not found.");

        if (user.CompanyId is null || user.Company is null)
        {
            throw new InvalidOperationException("User has no company.");
        }

        if (user.Role is not (UserRole.EnterpriseManager or UserRole.BranchManager or UserRole.Intermediary
            or UserRole.RegionalManager or UserRole.Admin))
        {
            throw new UnauthorizedAccessException("Not an employer manager.");
        }

        var root = await ResolveRootCompanyAsync(user.CompanyId.Value, cancellationToken);
        return (user, root);
    }

    private async Task<Company> ResolveRootCompanyAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            ?? throw new KeyNotFoundException("Company not found.");
        var guard = 0;
        while (company.ParentCompanyId is Guid parentId && guard++ < 32)
        {
            company = await _db.Companies.FirstAsync(c => c.Id == parentId, cancellationToken);
        }

        return company;
    }

    private static void EnsureNotVerified(Company root)
    {
        if (root.VerificationStatus == CompanyVerificationStatus.Verified)
        {
            throw new InvalidOperationException("Company already verified.");
        }
    }

    private async Task<KvkCompanyProfile?> TryProfileAsync(string kvkNumber, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(kvkNumber))
        {
            return null;
        }

        try
        {
            var profile = await _kvk.GetProfileAsync(kvkNumber, cancellationToken);
            return profile.Status == KvkLookupStatus.Ok ? profile : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<int> CountLettersThisMonthAsync(CancellationToken cancellationToken)
    {
        var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return await _db.CompanyVerificationLetters
            .CountAsync(l => l.CreatedAtUtc >= start && l.ResendOfLetterId == null, cancellationToken);
    }

    internal static LetterAddress? ResolveLetterAddress(Company root, KvkCompanyProfile? profile)
    {
        if (profile is { Establishments.Count: > 0 })
        {
            KvkEstablishmentProfile? est = null;
            if (!string.IsNullOrWhiteSpace(root.KvkEstablishmentId))
            {
                est = profile.Establishments.FirstOrDefault(e =>
                    string.Equals(e.KvkEstablishmentId, root.KvkEstablishmentId, StringComparison.OrdinalIgnoreCase));
            }

            est ??= profile.Establishments.FirstOrDefault(e =>
                        e.EstablishmentNumber is "000000000000" or "0001" or "1")
                    ?? profile.Establishments[0];

            var chosen = est.PostalAddress ?? est.VisitingAddress;
            if (chosen is not null)
            {
                var lines = new List<string> { root.Name };
                var street = chosen.FormattedLine.Split(',')[0].Trim();
                if (!string.IsNullOrWhiteSpace(street))
                {
                    lines.Add(street);
                }

                return new LetterAddress(root.Name, lines, chosen.Postcode, chosen.Place, "NL");
            }

            if (!string.IsNullOrWhiteSpace(est.Address))
            {
                return ParseLooseAddress(root.Name, est.Address);
            }
        }

        if (!string.IsNullOrWhiteSpace(root.Address))
        {
            return ParseLooseAddress(root.Name, root.Address);
        }

        return null;
    }

    private static LetterAddress ParseLooseAddress(string name, string address)
    {
        var parts = address.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var street = parts.ElementAtOrDefault(0) ?? address;
        var rest = parts.ElementAtOrDefault(1) ?? "";
        var tokens = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var postcode = tokens.Length >= 2 ? $"{tokens[0]} {tokens[1]}" : tokens.ElementAtOrDefault(0) ?? "";
        var city = tokens.Length >= 3 ? string.Join(' ', tokens.Skip(2)) : tokens.ElementAtOrDefault(1) ?? "";
        return new LetterAddress(name, [name, street], postcode, city, "NL");
    }

    private static string MaskAddress(LetterAddress address)
    {
        var street = address.AddressLines.LastOrDefault() ?? "";
        var streetMasked = street.Length <= 3
            ? street
            : street[..Math.Min(3, street.Length)] + "…";
        return $"{streetMasked}, {address.PostalCode} {address.City}".Trim().Trim(',');
    }

    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1)
        {
            return "***";
        }

        return email[0] + "***" + email[at..];
    }

    private static string? FunctionLabel(User user)
        => user.Role switch
        {
            UserRole.EnterpriseManager => "Bedrijfsmanager",
            UserRole.BranchManager => "Vestigingsmanager",
            UserRole.Intermediary => "Intermediair",
            _ => null
        };

    private static string PublicBase()
    {
        // Prefer configured public URL from features when available via brand/options; fall back.
        return "https://lobsy.nl";
    }
}
