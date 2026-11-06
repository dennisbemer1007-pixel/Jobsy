using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Scholen.MboHbo;

/// <summary>Clears MBO/HBO session data when a pupil code is reused (replace / fresh start).</summary>
public static class PupilMboHboProgressReset
{
    /// <summary>Class KPI average: ignore implausible single-pupil totals (open/stale sessions).</summary>
    public const int MaxActiveMinutesForAverage = 120;

    public static void ApplyFreshStart(PupilCode code, PupilQuestionSet questionSet)
    {
        if (questionSet != PupilQuestionSet.MboHbo)
        {
            return;
        }

        code.Status = PupilCodeStatus.NotStarted;
        code.Result = null;
        if (code.Progress is null)
        {
            code.Progress = new PupilProgress { PupilCodeId = code.Id };
        }

        var progress = code.Progress;
        var now = DateTime.UtcNow;
        progress.MboHboProgressJson = new PupilMboHboProgressState().ToJson();
        progress.AnswersJson = "{}";
        progress.CurrentIndex = 0;
        progress.StartedAtUtc = now;
        progress.UpdatedAtUtc = now;
        progress.CompletedAtUtc = null;
        progress.ChipsSavedAtUtc = null;
        progress.LikesJson = "[]";
        progress.DislikesJson = "[]";
        progress.LikeOtherWord = null;
        progress.DislikeOtherWord = null;
        progress.PuzzlesJson = "{}";
        progress.VoFavoriteSubjectsJson = null;
        progress.VoProfileKey = null;
        progress.VoSideJobAvailabilityJson = null;
        progress.VoTravelModeKey = null;
        progress.VoOwnWords = null;
        progress.VoLearningGoalsJson = null;
    }

    /// <summary>Active minutes from first to last completed block (excludes long idle gaps between sessions).</summary>
    public static int? ActiveMinutes(PupilProgress? progress)
    {
        var state = PupilMboHboProgressState.Parse(progress?.MboHboProgressJson);
        if (!state.AllBlocksComplete() || state.BlocksCompleted.Count == 0)
        {
            return null;
        }

        var stamps = state.BlocksCompleted.Values
            .Select(raw => DateTime.TryParse(raw, null, System.Globalization.DateTimeStyles.RoundtripKind, out var t)
                ? t.ToUniversalTime()
                : (DateTime?)null)
            .Where(t => t.HasValue)
            .Select(t => t!.Value)
            .OrderBy(t => t)
            .ToList();
        if (stamps.Count == 0)
        {
            return null;
        }

        var total = 0d;
        for (var i = 0; i < stamps.Count; i++)
        {
            if (i == 0)
            {
                total += 1;
                continue;
            }

            var gap = (stamps[i] - stamps[i - 1]).TotalMinutes;
            if (gap <= 0)
            {
                continue;
            }

            // Long idle between blocks = new session; don't count days/weeks as active time.
            total += gap > 45 ? 1 : gap;
        }

        return (int)Math.Clamp(Math.Round(total), 1, 180);
    }

    public static int? MedianMinutes(IReadOnlyList<int> durations)
    {
        if (durations.Count == 0)
        {
            return null;
        }

        var sorted = durations.OrderBy(m => m).ToList();
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 1
            ? sorted[mid]
            : (int)Math.Round((sorted[mid - 1] + sorted[mid]) / 2.0);
    }
}
