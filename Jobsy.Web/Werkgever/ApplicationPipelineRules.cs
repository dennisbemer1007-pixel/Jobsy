using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Models;
using Jobsy.Web.Navigation;

namespace Jobsy.Web.Werkgever;

/// <summary>Pipeline columns, privacy display and action helpers for Sollicitaties (file 04).</summary>
public static class ApplicationPipelineRules
{
    public const int OverdueHours = WerkgeverDashboardRules.OverduePendingHours;
    public const int PipelineCardCap = 50;
    public const int ListPageSize = 25;
    public const string ViewPipeline = "pijplijn";
    public const string ViewList = "lijst";
    public const string FilterOverdue = "overdue";

    public enum PipelineColumn
    {
        Nieuw,
        Geaccepteerd,
        Uitgenodigd,
        Aangenomen,
        Afgewezen
    }

    public enum StepperState
    {
        Done,
        Current,
        Upcoming
    }

    public sealed record ColumnDef(
        PipelineColumn Column,
        string LabelKey,
        string? HintKey,
        Func<EmployerApplicationItem, bool> Matches);

    public static readonly IReadOnlyList<ColumnDef> Columns =
    [
        new(PipelineColumn.Nieuw, "WgApp.Col.New", "WgApp.Hint.Anonymous",
            a => StatusOf(a) == ApplicationStatus.Pending),
        new(PipelineColumn.Geaccepteerd, "WgApp.Col.Accepted", "WgApp.Hint.NameCv",
            a => StatusOf(a) == ApplicationStatus.Accepted),
        new(PipelineColumn.Uitgenodigd, "WgApp.Col.Invited", null,
            a => StatusOf(a) == ApplicationStatus.EmployerContacting),
        new(PipelineColumn.Aangenomen, "WgApp.Col.Hired", "WgApp.Hint.Contact",
            a => StatusOf(a) == ApplicationStatus.Hired),
        new(PipelineColumn.Afgewezen, "WgApp.Col.Rejected", null, IsRejectedBucket)
    ];

    public static ApplicationStatus? StatusOf(EmployerApplicationItem item)
        => Enum.TryParse<ApplicationStatus>(item.Status, ignoreCase: true, out var s) ? s : null;

    public static bool IsRejectedBucket(EmployerApplicationItem item)
        => StatusOf(item) is ApplicationStatus.Rejected
            or ApplicationStatus.FilledElsewhere
            or ApplicationStatus.Withdrawn;

    public static bool IsPiiStage(EmployerApplicationItem item)
        => StatusOf(item) is { } s && LobsyCvAccessRules.IsPiiRevealed(s);

    public static bool IsContactStage(EmployerApplicationItem item)
        => StatusOf(item) is { } s && LobsyCvAccessRules.IsDirectContactRevealed(s);

    public static bool CanDownloadCv(EmployerApplicationItem item)
        => item.CvPdfAvailable || (IsPiiStage(item) && item.PiiRevealed);

    /// <summary>
    /// Display label follows privacy stage — never trust a leaked name on Pending.
    /// </summary>
    public static string DisplayName(EmployerApplicationItem item, string anonymousTemplate)
    {
        if (IsPiiStage(item) && !string.IsNullOrWhiteSpace(item.CandidateName))
        {
            return item.CandidateName!;
        }

        return string.Format(anonymousTemplate, ShortId(item.Id));
    }

    public static string ShortId(Guid id)
    {
        var n = BitConverter.ToUInt32(id.ToByteArray(), 0) % 10000;
        return n.ToString("D4");
    }

    public static string? Initials(EmployerApplicationItem item)
    {
        if (!IsPiiStage(item) || string.IsNullOrWhiteSpace(item.CandidateName))
        {
            return null;
        }

        var parts = item.CandidateName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return null;
        }

        if (parts.Length == 1)
        {
            return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();
        }

        return string.Concat(
            parts[0][0].ToString().ToUpperInvariant(),
            parts[^1][0].ToString().ToUpperInvariant());
    }

    public static bool IsOverdue(EmployerApplicationItem item, DateTime utcNow)
        => StatusOf(item) == ApplicationStatus.Pending
           && utcNow - item.CreatedAt > TimeSpan.FromHours(OverdueHours);

    public static int AgeDays(EmployerApplicationItem item, DateTime utcNow)
        => Math.Max(0, (int)Math.Floor((utcNow - item.CreatedAt).TotalDays));

    public static IReadOnlyList<StepperState> Stepper(EmployerApplicationItem item)
    {
        var status = StatusOf(item);
        var current = status switch
        {
            ApplicationStatus.Pending => 0,
            ApplicationStatus.Accepted => 1,
            ApplicationStatus.EmployerContacting => 2,
            ApplicationStatus.Hired => 3,
            _ => -1
        };

        if (current < 0)
        {
            // Rejected / withdrawn / filled — mark all upcoming (no active pipeline step).
            return [StepperState.Upcoming, StepperState.Upcoming, StepperState.Upcoming, StepperState.Upcoming];
        }

        return Enumerable.Range(0, 4)
            .Select(i => i < current ? StepperState.Done : i == current ? StepperState.Current : StepperState.Upcoming)
            .ToArray();
    }

    public static bool CanAct(EmployerRole? role)
        => role is EmployerRole.Bedrijfsmanager
            or EmployerRole.Vestigingsmanager
            or EmployerRole.Intermediair;

    public static string? PrimaryActionKey(EmployerApplicationItem item)
        => StatusOf(item) switch
        {
            ApplicationStatus.Pending => "WgApp.Action.Accept",
            ApplicationStatus.Accepted => "WgApp.Action.Invite",
            ApplicationStatus.EmployerContacting => "WgApp.Action.Hire",
            _ => null
        };

    public static bool ShowReject(EmployerApplicationItem item)
        => StatusOf(item) is ApplicationStatus.Pending
            or ApplicationStatus.Accepted
            or ApplicationStatus.EmployerContacting;

    public static bool ShowCvDownload(EmployerApplicationItem item)
        => StatusOf(item) is ApplicationStatus.Accepted
            or ApplicationStatus.EmployerContacting
            or ApplicationStatus.Hired
           && CanDownloadCv(item);

    public static bool ShowContactBlock(EmployerApplicationItem item)
        => IsContactStage(item);

    public static (int Rejected, int Elsewhere, int Withdrawn) RejectedSummary(
        IEnumerable<EmployerApplicationItem> items)
    {
        var list = items.Where(IsRejectedBucket).ToList();
        return (
            list.Count(a => StatusOf(a) == ApplicationStatus.Rejected),
            list.Count(a => StatusOf(a) == ApplicationStatus.FilledElsewhere),
            list.Count(a => StatusOf(a) == ApplicationStatus.Withdrawn));
    }

    /// <summary>
    /// Preselect from <c>?vacancyId=</c> or the older <c>?vacature=</c>. Empty means all vacancies.
    /// </summary>
    public static string ResolveVacancyQuery(string? vacancyId, string? vacature)
    {
        if (Guid.TryParse(vacancyId, out var fromVacancyId))
        {
            return fromVacancyId.ToString("D");
        }

        if (Guid.TryParse(vacature, out var fromVacature))
        {
            return fromVacature.ToString("D");
        }

        return "";
    }

    /// <summary>All applications the user can see, including inactive vacancies, unless one vacancy is selected.</summary>
    public static IEnumerable<EmployerApplicationItem> ItemsForVacancy(
        IEnumerable<EmployerApplicationItem> items,
        string? vacancyId)
    {
        if (!Guid.TryParse(vacancyId, out var vid))
        {
            return items;
        }

        return items.Where(a => a.VacancyId == vid);
    }

    public static Guid? DefaultVacancyId(IEnumerable<EmployerApplicationItem> items, Guid? queryVacancy)
    {
        if (queryVacancy is Guid q && items.Any(a => a.VacancyId == q))
        {
            return q;
        }

        var ranked = items
            .GroupBy(a => a.VacancyId)
            .Select(g => new
            {
                Id = g.Key,
                Pending = g.Count(a => StatusOf(a) == ApplicationStatus.Pending),
                Total = g.Count()
            })
            .OrderByDescending(x => x.Pending)
            .ThenByDescending(x => x.Total)
            .FirstOrDefault();

        return ranked?.Id;
    }

    public static string StatusLabelKey(string status)
        => WerkgeverStatusLabels.ApplicationKey(status);

    public static string StatusCss(string status) => status.ToLowerInvariant() switch
    {
        "pending" => "nieuw",
        "accepted" => "geaccepteerd",
        "employercontacting" => "uitgenodigd",
        "hired" => "aangenomen",
        "rejected" or "filledelsewhere" or "withdrawn" => "afgewezen",
        _ => "neutral"
    };

    public static string PrivacyNoteKey(EmployerApplicationItem item)
        => StatusOf(item) switch
        {
            ApplicationStatus.Pending => "WgApp.Privacy.Pending",
            ApplicationStatus.Accepted or ApplicationStatus.EmployerContacting => "WgApp.Privacy.Accepted",
            ApplicationStatus.Hired => "WgApp.Privacy.Hired",
            _ => "WgApp.Privacy.Pending"
        };

    /// <summary>Wat-je-ziet rows: (labelKey, unlocked).</summary>
    public static IReadOnlyList<(string LabelKey, bool Unlocked)> VisibilityRows(EmployerApplicationItem item)
    {
        var pii = IsPiiStage(item);
        var contact = IsContactStage(item);
        return
        [
            ("WgApp.See.Anonymous", true),
            ("WgApp.See.NameCv", pii),
            ("WgApp.See.Contact", contact)
        ];
    }

    public static bool HasEmployerNoteField()
        => typeof(Jobsy.Core.Entities.Application).GetProperty("EmployerNote") is not null
           || typeof(Jobsy.Core.Entities.Application).GetProperty("InternalNote") is not null;
}
