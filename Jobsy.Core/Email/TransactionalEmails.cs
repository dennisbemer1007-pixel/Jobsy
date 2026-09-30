using Jobsy.Core.Email.Localization;
using Jobsy.Core.Email.Model;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Email;

public sealed record EmailTemplateInfo(
    string Key,
    string Title,
    string Audience,
    string Description,
    string Category,
    bool RequiresEmployers = false);

/// <summary>
/// Canonical composers for every transactional Lobsy mail.
/// </summary>
public static partial class TransactionalEmails
{
    public const string SampleOtp = "123456";
    public const string SampleSetPasswordPath = "/account/wachtwoord-instellen?t=voorbeeld";
    public const string SampleRevealPath = "/koppeling/sleutel?t=voorbeeld";

    public static IReadOnlyList<EmailTemplateInfo> Templates { get; } =
        EmailTemplateRegistry.All
            .Select(d => new EmailTemplateInfo(
                d.Key, d.Title, d.Audience, d.Description, d.Category, d.RequiresEmployers))
            .ToList();

    public static bool TryGet(string? key, out EmailTemplateInfo info)
    {
        info = Templates.FirstOrDefault(t =>
            string.Equals(t.Key, key, StringComparison.OrdinalIgnoreCase))!;
        return info is not null;
    }

    private static EmailLinks Links(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentException("Public web base URL is required for email links.", nameof(baseUrl));
        }

        return EmailLinks.For(baseUrl);
    }

    private static EmailBrand Brand(string? baseUrl)
        => EmailBrand.ForBaseUrl(JobsyPublicUrl.NormalizeOrigin(
            string.IsNullOrWhiteSpace(baseUrl)
                ? throw new ArgumentException("Public web base URL is required.", nameof(baseUrl))
                : baseUrl));

    private static ComposedEmail Finish(EmailDocument doc, string? baseUrl)
        => ComposedEmail.Render(doc, Brand(baseUrl));

    private static EmailDocument Doc(
        string key,
        string subject,
        string preheader,
        string heading,
        IReadOnlyList<EmailBlock> blocks,
        EmailCta? cta = null,
        string? greeting = null,
        EmailEyebrow? eyebrow = null,
        bool showMascot = false,
        string? signOff = null,
        EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var def = EmailTemplateRegistry.GetRequired(key);
        return new EmailDocument(
            TemplateKey: key,
            Kind: def.Kind,
            Culture: c,
            Subject: subject,
            Preheader: preheader,
            Heading: heading,
            Blocks: blocks,
            ReasonText: EmailStrings.Reason(c, def.ReasonKey),
            SignOff: signOff ?? EmailStrings.Get(c, "Email.Common.SignOff"),
            Eyebrow: eyebrow,
            Greeting: greeting,
            Cta: def.Kind == EmailKind.Security ? null : cta,
            ShowMascot: showMascot && def.GoodNews);
    }

    private static string S(EmailCulture c, string key) => EmailStrings.Get(c, key);
    private static string Sf(EmailCulture c, string key, params object[] args) => EmailStrings.FormatRaw(c, key, args);
    private static EmailText T(EmailCulture c, string key, params EmailArg[] args) => EmailStrings.Format(c, key, args);
    private static string GreetCandidate(EmailCulture c, string? name)
        => string.IsNullOrWhiteSpace(name)
            ? S(c, "Email.Common.GreetingCandidateFallback")
            : Sf(c, "Email.Common.GreetingCandidate", EmailBidi.Isolate(c, name.Trim()));
    private static string GreetOther(EmailCulture c, string? name)
        => string.IsNullOrWhiteSpace(name)
            ? S(c, "Email.Common.GreetingOtherFallback")
            : Sf(c, "Email.Common.GreetingOther", EmailBidi.Isolate(c, name.Trim()));

    private static EmailText Plain(string text) => EmailText.Plain(text);
    private static EmailText Bold(string text) => EmailText.Bold(text);
    private static ParagraphBlock P(string text) => new(Plain(text));
    private static ParagraphBlock P(EmailText text) => new(text);
    private static NoteBlock N(string text, EmailLink? link = null) => new(Plain(text), link);
    private static NoteBlock N(EmailText text, EmailLink? link = null) => new(text, link);
    private static FactsBlock F(IEnumerable<(string Label, string Value)> rows, EmailTone tone = EmailTone.Sky)
        => new(rows.Select(r => (r.Label, Plain(r.Value))).ToList(), tone);
    private static FactsBlock F(IEnumerable<(string Label, EmailText Value)> rows, EmailTone tone = EmailTone.Sky)
        => new(rows.ToList(), tone);
    private static CodeBlock C(string digits, string validity) => new(digits, validity);
    private static EmailCta Button(string label, string url) => new(label, url);
    private static EmailText Fmt(string format, params EmailArg[] args) => EmailText.Format(format, args);

    public static ComposedEmail Compose(string key, EmailSampleContext ctx, EmailCulture? culture = null)
    {
        if (!TryGet(key, out _))
            throw new ArgumentException($"Onbekend mailtype: {key}");

        var c = culture ?? EmailCulture.Nl;
        var links = Links(ctx.PublicWebBaseUrl);
        return key.ToLowerInvariant() switch
        {
            "mailtest" => MailTest(ctx.PublicWebBaseUrl, c),
            "applicationconfirmation" => ApplicationConfirmation(ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.VacancyTitle, ctx.CompanyName, false, c),
            "applicationverificationcode" => ApplicationVerificationCode(ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.VacancyTitle, ctx.VacancyId, ctx.OtpCode, c),
            "employerreactionaccepted" => EmployerReactionAccepted(ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.VacancyTitle, ctx.CompanyName, c),
            "employerreactionrejected" => EmployerReactionRejected(ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.VacancyTitle, ctx.CompanyName, c),
            "employercontacting" => EmployerContacting(ctx.PublicWebBaseUrl, ctx.VacancyTitle, c),
            "applicationhired" => ApplicationHired(ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.VacancyTitle, ctx.CompanyName, ctx.ApplicationId, links.WithdrawOthers(ctx.ApplicationId), c),
            "applicationfilledelsewhere" => ApplicationFilledElsewhere(ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.VacancyTitle, ctx.CompanyName, c),
            "employernewapplication" => EmployerNewApplication(ctx.PublicWebBaseUrl, ctx.VacancyTitle, c),
            "candidatewithdrawn" => CandidateWithdrawn(ctx.PublicWebBaseUrl, ctx.VacancyTitle, c),
            "candidatewithdrawnotherjob" => CandidateWithdrawnOtherJob(ctx.PublicWebBaseUrl, ctx.VacancyTitle, c),
            "pushbom" => PushBom(ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.VacancyTitle, ctx.CompanyName, ctx.VacancyId, ctx.LocationLabel, ctx.DistanceKm, ctx.TravelMinutes, ctx.HourlyWage, "Uurloon", links.SetUnavailable, c),
            "pendingapproval" => PendingApproval(ctx.PublicWebBaseUrl, ctx.VacancyTitle, ctx.CompanyName, c),
            "vacancyengagementreminder" => VacancyEngagementReminder(ctx.PublicWebBaseUrl, ctx.VacancyTitle, ctx.VacancyId, 42, 18, 3, 5, 2, VacancyEngagementReminderRules.BuildHeuristicTip(42, 18, 3, 5, 2), ctx.CompanyName, c),
            "draftvacancycleanupwarning" => DraftVacancyCleanupWarning(ctx.PublicWebBaseUrl, ctx.VacancyTitle, ctx.CompanyName, ctx.VacancyId, new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc), c),
            "companyreengagement" => CompanyReEngagement(ctx.PublicWebBaseUrl, ctx.CompanyName, c),
            "registrationactivation" => RegistrationActivation(ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.EstablishmentName, ctx.RoleLabel, "5610", ctx.OtpCode, c),
            "registrationcredentials" => RegistrationCredentials(ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.EstablishmentName, ctx.ContactEmail, ctx.SetPasswordUrl, c),
            "companyverificationreminder" => CompanyVerificationReminder(ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.CompanyName, 7, null, c),
            "companyverified" => CompanyVerified(ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.CompanyName, true, [ctx.VacancyTitle], c),
            "companybusinessemailverification" => CompanyBusinessEmailVerification(ctx.CompanyName, ctx.OtpCode, ctx.PublicWebBaseUrl, c),
            "companyverificationrejected" => CompanyVerificationRejected(ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.CompanyName, ctx.RoleLabel, c),
            "engagementclaimremoved" => EngagementClaimRemoved(ctx.PublicWebBaseUrl, ctx.CompanyName, "duurzaamheid", ctx.RoleLabel, c),
            "companyunverifieddeleted" => CompanyUnverifiedDeleted(ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.CompanyName, c),
            "takeoveremailverification" => TakeoverEmailVerification(ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.CompanyName, ctx.OtpCode, c),
            "takeoverrequest" => TakeoverRequest(ctx.PublicWebBaseUrl, ctx.CompanyName, ctx.KvkEstablishmentId, ctx.RecipientName, ctx.ContactEmail, c),
            "takeoversubmitted" => TakeoverSubmitted(ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.CompanyName, c),
            "takeoverapproved" => TakeoverApproved(ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.CompanyName, ctx.ContactEmail, ctx.SetPasswordUrl, true, c),
            "takeoverrejected" => TakeoverRejected(ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.CompanyName, c),
            "userinvite" => UserInvite(ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.RoleLabel, ctx.ContactEmail, ctx.SetPasswordUrl, false, c),
            "salesmanagerinvite" => SalesManagerInvite(ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.ContactEmail, null, c),
            "ambassadeurinvite" => AmbassadeurInvite(ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.ContactEmail, null, c),
            "companyapikeycredentials" => CompanyApiKeyCredentials(ctx.PublicWebBaseUrl, ctx.CompanyName, ctx.ApiBaseUrl, ctx.SampleRevealUrl, new DateTime(2026, 10, 3, 14, 0, 0, DateTimeKind.Utc), c),
            "accountunsubscribeverification" => AccountUnsubscribeVerification(ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.OtpCode, 10, c),
            "mfaresetbyadmin" => MfaResetByAdmin(ctx.PublicWebBaseUrl, ctx.RecipientName, c),
            "emailsignupcode" => EmailSignUpCode(ctx.PublicWebBaseUrl, ctx.OtpCode, c.Language),
            "emailsignincode" => EmailSignInCode(ctx.PublicWebBaseUrl, ctx.OtpCode, c.Language),
            "emailcodeusepassword" => EmailCodeUsePassword(ctx.PublicWebBaseUrl, c.Language),
            "parentalconsent" => ParentalConsent(ctx.PublicWebBaseUrl, "Sanne", links.ParentalConsent("voorbeeld"), new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc), c),
            "supportaccessrequested" => SupportAccessRequested(ctx.PublicWebBaseUrl, "Admin Demo", "Voorbeeldreden", new DateTime(2026, 9, 30, 18, 0, 0, DateTimeKind.Utc), "Persoonsgegevens", c),
            "accountlockout" => AccountLockout(ctx.PublicWebBaseUrl, c),
            "accessrequestemailverification" => AccessRequestEmailVerification(ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.CompanyName, ctx.OtpCode, c),
            "accessrequestsubmitted" => AccessRequestSubmitted(ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.CompanyName, c),
            "accessrequesttomanager" => AccessRequestToManager(ctx.PublicWebBaseUrl, ctx.CompanyName, ctx.RecipientName, ctx.RoleLabel, ctx.ContactEmail, ctx.RoleLabel, c),
            "accessrequestreminder" => AccessRequestReminder(ctx.PublicWebBaseUrl, ctx.CompanyName, ctx.RecipientName, c),
            "accessrequestrejected" => AccessRequestRejected(ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.CompanyName, "Niet goedgekeurd", c),
            "accessrequestexpired" => AccessRequestExpired(ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.CompanyName, c),
            "ownershiptransfermanagersnotify" => OwnershipTransferManagersNotify(ctx.PublicWebBaseUrl, ctx.CompanyName, c),
            "intermediaryclientselfmanaged" => IntermediaryClientSelfManaged(ctx.PublicWebBaseUrl, ctx.CompanyName, c),
            _ => throw new ArgumentException($"Onbekend mailtype: {key}")
        };
    }

    /// <summary>Build a one-off notice mail (sales/scholen) through the shared renderer.</summary>
    public static ComposedEmail AdHoc(
        string key,
        string category,
        EmailKind kind,
        string? baseUrl,
        string subject,
        string preheader,
        string heading,
        IReadOnlyList<EmailBlock> blocks,
        EmailCta? cta = null,
        string? greeting = null,
        string? reasonText = null,
        string? signOff = null,
        EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var doc = new EmailDocument(
            TemplateKey: key,
            Kind: kind,
            Culture: c,
            Subject: subject,
            Preheader: preheader,
            Heading: heading,
            Blocks: blocks,
            ReasonText: reasonText ?? EmailStrings.Get(c, "Email.Reason.Fallback"),
            SignOff: signOff ?? EmailStrings.Get(c, "Email.Common.SignOff"),
            Greeting: greeting,
            Cta: kind == EmailKind.Security ? null : cta,
            ShowMascot: false);
        return ComposedEmail.Render(doc, Brand(baseUrl), category);
    }

}
