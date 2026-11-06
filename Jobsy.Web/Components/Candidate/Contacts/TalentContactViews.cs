using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Models;

namespace Jobsy.Web.Components.Candidate.Contacts;

/// <summary>What the candidate sees per request; never the raw enum name (T4).</summary>
public enum TalentContactState
{
    /// <summary>Pending inside the reply window.</summary>
    Open,

    /// <summary>RefundEligible: the window closed, answering is still allowed.</summary>
    OpenLate,

    /// <summary>The candidate said yes; name, e-mail and phone were shared.</summary>
    Shared,

    /// <summary>The candidate said no; nothing was shared.</summary>
    Declined,

    /// <summary>The employer withdrew the request.</summary>
    Withdrawn
}

/// <summary>One contact request card (cr-d6).</summary>
public sealed record TalentContactRow(
    Guid Id,
    string? CompanyName,
    string Message,
    TalentContactState State,
    DateTime RespondByUtc,
    DateTime? HistoryUtc,
    bool DeclinedBecauseAlreadyPlaced)
{
    public bool IsOpen => State is TalentContactState.Open or TalentContactState.OpenLate;

    /// <summary>First letter for the avatar; empty when the company is unknown.</summary>
    public string Initial =>
        string.IsNullOrWhiteSpace(CompanyName)
            ? "?"
            : CompanyName.Trim()[..1].ToUpperInvariant();
}

/// <summary>A decline click: the row plus why (D14).</summary>
public sealed record TalentDeclineRequest(TalentContactRow Row, bool AlreadyPlaced);

public static class TalentContactViewBuilder
{
    /// <summary>Open requests first, then newest (04 §2).</summary>
    public static List<TalentContactRow> Build(IEnumerable<TalentContactRequestModel>? rows)
        => (rows ?? [])
            .Select(Map)
            .OrderByDescending(r => r.IsOpen)
            .ThenByDescending(r => r.HistoryUtc ?? r.RespondByUtc)
            .ToList();

    private static TalentContactRow Map(TalentContactRequestModel row)
    {
        var status = Enum.TryParse<TalentContactStatus>(row.Status, ignoreCase: true, out var parsed)
            ? parsed
            : TalentContactStatus.Pending;

        var state = status switch
        {
            TalentContactStatus.Pending => TalentContactState.Open,
            TalentContactStatus.RefundEligible => TalentContactState.OpenLate,
            TalentContactStatus.ContactShared => TalentContactState.Shared,
            TalentContactStatus.CandidateDeclined => TalentContactState.Declined,
            _ => TalentContactState.Withdrawn
        };

        var history = state switch
        {
            TalentContactState.Shared => row.ContactSharedAtUtc ?? row.RespondedAtUtc ?? row.CreatedAtUtc,
            TalentContactState.Declined => row.RespondedAtUtc ?? row.CreatedAtUtc,
            TalentContactState.Withdrawn => row.RespondedAtUtc ?? row.CreatedAtUtc,
            _ => (DateTime?)null
        };

        return new TalentContactRow(
            row.Id,
            row.CompanyName,
            row.Message ?? "",
            state,
            row.RespondByUtc,
            history,
            state == TalentContactState.Declined
            && string.Equals(
                row.CandidateDeclineReason,
                TalentContactDeclineReasons.AlreadyPlaced,
                StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Localization key for the status pill; never the enum name (T4).</summary>
    public static string StatusKey(TalentContactState state) => state switch
    {
        TalentContactState.Open or TalentContactState.OpenLate => "TalentC.Status.Open",
        TalentContactState.Shared => "TalentC.Status.Shared",
        TalentContactState.Declined => "TalentC.Status.Declined",
        _ => "TalentC.Status.Withdrawn"
    };

    /// <summary>Maps API error codes to <c>TalentC.Err.*</c>; unknown codes fall back.</summary>
    public static string ErrorKey(string? code) => code switch
    {
        "not_found" => "TalentC.Err.NotFound",
        "cannot_respond" => "TalentC.Err.CannotRespond",
        "confirm_share_required" => "TalentC.Err.ConfirmShare",
        _ => "Common.Error"
    };
}
