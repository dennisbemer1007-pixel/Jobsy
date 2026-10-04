namespace Jobsy.Core.Admin;

/// <summary>Stable action keys for <see cref="Entities.AdminAuditEvent"/>.</summary>
public static class AdminAuditKeys
{
    public const string SettingsPlatformUpdate = "settings.platform.update";
    public const string SettingsPricingUpdate = "settings.pricing.update";
    public const string SettingsPricingDelete = "settings.pricing.delete";
    public const string SettingsCompanyUpdate = "settings.company.update";
    public const string SettingsAboutUpdate = "settings.about.update";
    public const string SettingsFlyerUpdate = "settings.flyer.update";
    public const string SettingsIntegrationUpdate = "settings.integration.update";

    /// <summary>Maintenance switch on (errors 05).</summary>
    public const string MaintenanceOn = "maintenance.on";

    /// <summary>Maintenance switch off (errors 05).</summary>
    public const string MaintenanceOff = "maintenance.off";

    public const string UserMfaReset = "user.mfa.reset";
    public const string UserTestUnlockReset = "user.test-unlock.reset";
    public const string UserRoleChange = "user.role.change";
    public const string UserSessionsRevoke = "user.sessions.revoke";
    public const string UserSessionsRevokeAll = "user.sessions.revoke-all";
    public const string UserBlock = "user.block";
    public const string UserUnblock = "user.unblock";

    public const string SupportAccessGrant = "support-access.grant";
    public const string SupportAccessRevoke = "support-access.revoke";

    public const string TokensGoodwillGrant = "tokens.goodwill.grant";
    public const string TakeoverApprove = "takeover.approve";
    public const string TakeoverReject = "takeover.reject";
    public const string InvoiceMarkPaid = "invoice.mark-paid";
    public const string ExportCreate = "export.create";

    public const string VacancyExtend = "vacancy.extend";
    public const string VacancyInactive = "vacancy.inactive";
    public const string ApiKeyDeactivate = "apikey.deactivate";

    /// <summary>DSA notice and action: an admin decided on the reports of one target (06).</summary>
    public const string ReportDecided = "report.decided";

    public const string PrivacyRetentionRun = "privacy.retention.run";
    public const string PrivacyAccountDeleted = "privacy.account.deleted";
    public const string AuthAdminLoginFailed = "auth.admin.login-failed";
    public const string EmailTestSend = "email.test-send";
    public const string ReferenceMisuseHandled = "reference.misuse.handled";

    public const string VacancyCategoryCreate = "vacancy-category.create";
    public const string VacancyCategoryUpdate = "vacancy-category.update";
    public const string VacancyCategoryDelete = "vacancy-category.delete";
    public const string MasterdataCreate = "masterdata.create";
    public const string MasterdataUpdate = "masterdata.update";
    public const string MasterdataDelete = "masterdata.delete";

    public static class Results
    {
        public const string Success = "success";
        public const string Denied = "denied";
        public const string Failed = "failed";
    }

    public static class ActorKinds
    {
        public const string Admin = "admin";
        public const string System = "system";
        public const string Self = "self";
    }

    public static class TargetTypes
    {
        public const string User = "user";
        public const string Company = "company";
        public const string Setting = "setting";
        public const string Vacancy = "vacancy";
        public const string Invoice = "invoice";
        public const string Grant = "grant";
        public const string Export = "export";
        public const string ApiKey = "apikey";
        public const string Takeover = "takeover";
        public const string Retention = "retention";
        public const string Account = "account";
        public const string EmailTemplate = "email-template";
        public const string ContentReport = "content-report";
    }
}
