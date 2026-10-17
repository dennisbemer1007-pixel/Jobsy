namespace Jobsy.Web.Navigation;

/// <summary>
/// Central list of mascot encouragement copy (message + link keys). Edit <see cref="UiStrings"/> keys under Encourage.Tip.* / Encourage.Action.*.
/// </summary>
public static class ProfileEncouragementTipCatalog
{
    public enum Audience
    {
        /// <summary>Culture or values quick scan not complete yet.</summary>
        TestsIncomplete,
        /// <summary>Tests done (or not needed) but apply-basics still open.</summary>
        ProfileIncomplete,
        /// <summary>Any candidate on employer surfaces.</summary>
        Any
    }

    public sealed record Tip(
        string Id,
        string MessageKey,
        string LinkKey,
        Audience Audience,
        string? Href,
        IReadOnlyList<string> Screens);

    public static IReadOnlyList<Tip> All { get; } =
    [
        new(
            "action-tests",
            "Encourage.Action.Tests.Message",
            "Encourage.Action.Tests.Link",
            Audience.TestsIncomplete,
            "/candidate/paspoort?tab=tests",
            ["vacancy-pre-apply", "apply-sent", "paspoort-home"]),
        new(
            "action-profile",
            "Encourage.Action.Profile.Message",
            "Encourage.Action.Profile.Link",
            Audience.ProfileIncomplete,
            "/candidate/paspoort?tab=data",
            ["vacancy-pre-apply", "apply-sent", "paspoort-home"]),
        new(
            "wist-tests-match",
            "Encourage.Tip.TestsMatch",
            "Encourage.Tip.TestsMatch.Link",
            Audience.TestsIncomplete,
            "/candidate/paspoort?tab=tests",
            ["vacancy-pre-apply", "paspoort-home"]),
        new(
            "wist-cv-vs-passport",
            "Encourage.Tip.PassportApply",
            "Encourage.Tip.PassportApply.Link",
            Audience.TestsIncomplete,
            "/candidate/paspoort?tab=tests",
            ["vacancy-pre-apply", "apply-sent", "paspoort-home"]),
        new(
            "wist-day-in-life",
            "Encourage.Tip.DayInLife",
            "Encourage.Tip.DayInLife.Link",
            Audience.Any,
            null,
            ["vacancy-pre-apply"]),
        new(
            "wist-tests-pause",
            "Encourage.Tip.TestsPause",
            "Encourage.Tip.TestsPause.Link",
            Audience.TestsIncomplete,
            "/candidate/paspoort?tab=tests",
            ["paspoort-home", "apply-sent"])
    ];

    public static Tip? TryGet(string id)
        => All.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.Ordinal));

    public static Tip? PickForScreen(ProfileEncouragementContext ctx, string screenKey, string? previewTipId = null)
    {
        if (!string.IsNullOrWhiteSpace(previewTipId))
        {
            var forced = TryGet(previewTipId);
            if (forced is not null && forced.Screens.Contains(screenKey, StringComparer.Ordinal))
            {
                return forced;
            }
        }

        var pool = All
            .Where(t => t.Screens.Contains(screenKey, StringComparer.Ordinal))
            .Where(t => IsEligible(t, ctx))
            .ToList();
        if (pool.Count == 0)
        {
            return null;
        }

        if (screenKey is "vacancy-pre-apply" or "apply-sent")
        {
            var gentle = pool.FirstOrDefault(t => t.Id == "wist-cv-vs-passport")
                         ?? pool.FirstOrDefault(t => t.Id == "action-tests");
            if (gentle is not null)
            {
                return gentle;
            }
        }

        var seed = HashCode.Combine(ctx.UserId ?? "", screenKey, ctx.RotationBucket);
        var index = Math.Abs(seed) % pool.Count;
        return pool[index];
    }

    public static bool IsEligible(Tip tip, ProfileEncouragementContext ctx)
        => tip.Audience switch
        {
            Audience.TestsIncomplete => !ctx.PassportAllLayersComplete,
            Audience.ProfileIncomplete => ctx.PassportAllLayersComplete && !ctx.ApplyBasicsReady,
            Audience.Any => true,
            _ => false
        };
}

public sealed class ProfileEncouragementContext
{
    public string? UserId { get; init; }
    public bool CultureOrValuesComplete { get; init; }
    /// <summary>All four passport DNA layers complete (quick scan or deep).</summary>
    public bool PassportAllLayersComplete { get; init; }
    public bool ApplyBasicsReady { get; init; }
    /// <summary>Changes daily so eligible tips rotate without showing more than one per screen.</summary>
    public int RotationBucket { get; init; } = DateOnly.FromDateTime(DateTime.UtcNow).DayOfYear;
}
