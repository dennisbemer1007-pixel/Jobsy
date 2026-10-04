namespace Jobsy.Web.Admin;

/// <summary>
/// One map from stored audit, access-log and retention keys to culture keys.
/// Unknown keys stay as stored so new actions remain visible.
/// </summary>
public static class AdminActionLabels
{
    public static string Label(string? action, Func<string, string> text)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            return "";
        }

        var key = action.Trim();
        var cultureKey = key switch
        {
            "user.mfa.reset" => "AdminAudit.Action.MfaReset",
            "support-access.grant" or "support-access.revoke" => "AdminAudit.Action.Support",
            "settings.platform.update" or "maintenance.on" or "maintenance.off" => "AdminAudit.Action.Setting",
            "settings.flyer.update" => "AdminAudit.Action.Flyer",
            "export.create" or "audit-csv" => "AdminAudit.Action.Export",
            "user.block" or "user.unblock" => "AdminAudit.Action.Block",
            "tokens.goodwill.grant" => "AdminAudit.Action.Tokens",
            "privacy.account.deleted" => "AdminAudit.Action.Deleted",
            "privacy.retention.run" or "Data retention" => "AdminAudit.Action.Retention",
            "auth.admin.login-failed" => "AdminAudit.Action.LoginFailed",
            "vacancy-category.create" => "AdminAudit.Action.CategoryCreated",
            "vacancy-category.update" => "AdminAudit.Action.CategoryUpdated",
            "vacancy-category.delete" => "AdminAudit.Action.CategoryDeleted",
            "masterdata.create" => "AdminAudit.Action.MasterdataCreated",
            "masterdata.update" => "AdminAudit.Action.MasterdataUpdated",
            "masterdata.delete" => "AdminAudit.Action.MasterdataDeleted",
            "user.sessions" or "user.sessions.list" or "user.sessions.revoke" or "user.sessions.revoke-all"
                => "AdminAudit.Action.Sessions",
            _ => null
        };

        if (cultureKey is not null)
        {
            return text(cultureKey);
        }

        if (key.StartsWith("admin.", StringComparison.OrdinalIgnoreCase))
        {
            return Resource(key, text);
        }

        return key;
    }

    public static string Resource(string? resource, Func<string, string> text)
    {
        if (string.IsNullOrWhiteSpace(resource))
        {
            return "";
        }

        var key = resource.Trim();
        var cultureKey = key switch
        {
            "admin.users.list" or "user" or "users" => "AdminAction.Resource.Users",
            "admin.users.reveal" or "user.sessions" => "AdminAction.Resource.UserSessions",
            "platformLogs" or "platform-logs" => "AdminAction.Resource.PlatformLogs",
            "adminAudit" or "adminAuditEvents" => "AdminAction.Resource.Audit",
            "personalDataAccessLogs" => "AdminAction.Resource.AccessLog",
            _ => null
        };

        return cultureKey is null ? key : text(cultureKey);
    }

    public static string Role(string? role, Func<string, string> text)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return "";
        }

        return role.Trim() switch
        {
            "Candidate" => text("AdminUsers.Role.Candidate"),
            "BranchManager" => text("AdminUsers.Role.BranchManager"),
            "RegionalManager" => text("AdminUsers.Role.RegionalManager"),
            "EnterpriseManager" => text("AdminUsers.Role.EnterpriseManager"),
            "Intermediary" => text("AdminUsers.Role.Intermediary"),
            "Admin" => text("AdminUsers.Role.Admin"),
            "SalesManager" => text("AdminUsers.Role.SalesManager"),
            "Ambassadeur" => text("AdminUsers.Role.Ambassadeur"),
            "SchoolAdmin" => text("AdminUsers.Role.SchoolAdmin"),
            "Teacher" => text("AdminUsers.Role.Teacher"),
            "Privileged" => text("AdminMfa.Role.Privileged"),
            _ => role
        };
    }

    public static string CompanyType(string? type, Func<string, string> text) => type switch
    {
        "Employer" => text("AdminOrgs.TypeEmployer"),
        "Intermediary" => text("AdminOrgs.TypeIntermediary"),
        _ => type ?? ""
    };

    public static string VatStatus(string? status, Func<string, string> text) => status switch
    {
        "SkippedNoIban" => text("AdminFinance.SkippedNoIban"),
        _ => status ?? ""
    };

    public static string Scope(string cell) => cell switch
    {
        "branch+" => "vestiging+",
        "branch" => "vestiging",
        "region" => "regio",
        "org" => "organisatie",
        "clients" => "klanten",
        "sales" => "sales",
        "amb" => "ambassadeur",
        "all" => "alles",
        "own" => "eigen",
        _ => cell
    };
}
