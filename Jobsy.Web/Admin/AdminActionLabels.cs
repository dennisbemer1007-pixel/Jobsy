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
            "personalDataAccessLogs" or "admin.personal_data_access_log.list" => "AdminAction.Resource.AccessLog",
            "admin.search" => "AdminAction.Resource.AdminSearch",
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

    public static string TokenKind(string? kind, Func<string, string> text) => kind switch
    {
        "Purchase" => text("AdminToken.Kind.Purchase"),
        "Spend" => text("AdminToken.Kind.Spend"),
        "Grant" => text("AdminToken.Kind.Grant"),
        "Allocation" => text("AdminToken.Kind.Allocation"),
        "Goodwill" => text("AdminToken.Kind.Goodwill"),
        _ => kind ?? ""
    };

    public static string TokenReason(string? reason, Func<string, string> text)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            return "—";
        }

        return reason.Trim() switch
        {
            "Publish" => text("TokenSpendCost.Publish"),
            "Highlight" => text("TokenSpendCost.Highlight"),
            "PushBom" => text("AdminToken.Reason.PushBom"),
            "Extend" => text("AdminToken.Reason.Extend"),
            "ContactUnlock" => text("AdminToken.Reason.ContactUnlock"),
            "InsightsUnlock" => text("AdminToken.Reason.InsightsUnlock"),
            "SkippedNoIban" => text("AdminFinance.SkippedNoIban"),
            _ => VatStatus(reason, text)
        };
    }

    public static string Target(string? label, Func<string, string> text)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return "";
        }

        var trimmed = label.Trim();
        var setting = PlatformSettingsCatalog.Entries.FirstOrDefault(e =>
            string.Equals(e.Key, trimmed, StringComparison.OrdinalIgnoreCase));
        if (setting is not null)
        {
            return text(setting.TitleKey);
        }

        return trimmed switch
        {
            "Data retention" => text("AdminAudit.Target.Retention"),
            "audit-csv" => text("AdminAudit.Target.AuditCsv"),
            _ => trimmed
        };
    }

    public static string LogMessage(string? message, Func<string, string> text)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "";
        }

        const string schoolRetention = "school.retention.run";
        var trimmed = message.Trim();
        if (trimmed.Equals(schoolRetention, StringComparison.OrdinalIgnoreCase))
        {
            return text("AdminLogs.Message.SchoolRetention");
        }

        if (trimmed.StartsWith(schoolRetention, StringComparison.OrdinalIgnoreCase))
        {
            return text("AdminLogs.Message.SchoolRetention") + trimmed[schoolRetention.Length..];
        }

        return trimmed;
    }

    /// <summary>
    /// Raw audit keys whose Dutch (or current-locale) label contains <paramref name="query"/>,
    /// so a search for the label still finds the stored value.
    /// </summary>
    public static IReadOnlyList<string> RawKeysForQuery(string? query, Func<string, string> text)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var term = query.Trim();
        var hits = new List<string>();
        foreach (var (raw, cultureKey) in SearchPairs())
        {
            if (raw.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var label = text(cultureKey);
            if (label.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                hits.Add(raw);
            }
        }

        return hits.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static IEnumerable<(string Raw, string CultureKey)> SearchPairs()
    {
        yield return ("Data retention", "AdminAudit.Target.Retention");
        yield return ("audit-csv", "AdminAudit.Target.AuditCsv");
        yield return ("admin.search", "AdminAction.Resource.AdminSearch");
        yield return ("school.retention.run", "AdminLogs.Message.SchoolRetention");
        yield return ("user.mfa.reset", "AdminAudit.Action.MfaReset");
        yield return ("support-access.grant", "AdminAudit.Action.Support");
        yield return ("settings.platform.update", "AdminAudit.Action.Setting");
        yield return ("privacy.retention.run", "AdminAudit.Action.Retention");
        yield return ("admin.users.list", "AdminAction.Resource.Users");
        yield return ("platformLogs", "AdminAction.Resource.PlatformLogs");
        yield return ("adminAudit", "AdminAction.Resource.Audit");
        yield return ("personalDataAccessLogs", "AdminAction.Resource.AccessLog");

        foreach (var entry in PlatformSettingsCatalog.Entries)
        {
            yield return (entry.Key, entry.TitleKey);
        }
    }

    public static string AccessAction(string? resource, string? action, Func<string, string> text)
    {
        var res = (resource ?? "").Trim();
        var act = (action ?? "").Trim();
        if ((res is "admin.personal_data_access_log.list" or "personalDataAccessLogs")
            && (act.Length == 0 || act.Equals("list", StringComparison.OrdinalIgnoreCase)))
        {
            return text("AdminDataAccess.Action.Viewed");
        }

        if (act.Equals("list", StringComparison.OrdinalIgnoreCase))
        {
            return text("AdminDataAccess.Action.List");
        }

        return Label(act, text);
    }

    public static string AccessReason(string? reason, Func<string, string> text)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return "—";
        }

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in reason.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = part.IndexOf('=');
            if (eq <= 0)
            {
                return reason;
            }

            map[part[..eq]] = part[(eq + 1)..];
        }

        if (!map.ContainsKey("page") && !map.ContainsKey("actor") && !map.ContainsKey("subject"))
        {
            return reason;
        }

        var bits = new List<string>();
        if (map.TryGetValue("page", out var page) && page.Length > 0)
        {
            bits.Add(string.Format(text("AdminDataAccess.Reason.Page"), page));
        }

        if (map.TryGetValue("actor", out var actor) && actor.Length > 0)
        {
            bits.Add(string.Format(text("AdminDataAccess.Reason.Actor"), ShortId(actor)));
        }

        if (map.TryGetValue("subject", out var subject) && subject.Length > 0)
        {
            bits.Add(string.Format(text("AdminDataAccess.Reason.Subject"), ShortId(subject)));
        }

        return bits.Count == 0 ? "—" : string.Join(" · ", bits);
    }

    private static string ShortId(string value)
    {
        var hex = value.Replace("-", "", StringComparison.Ordinal);
        return hex.Length <= 8 ? hex : hex[..8];
    }

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
