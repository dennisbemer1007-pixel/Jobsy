using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Scholen.MboHbo;

/// <summary>Clears MBO/HBO session data when a pupil code is reused (replace / fresh start).</summary>
public static class PupilMboHboProgressReset
{
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

        if (stamps.Count == 1)
        {
            return 1;
        }

        var minutes = (stamps[^1] - stamps[0]).TotalMinutes;
        return (int)Math.Clamp(Math.Round(minutes), 1, 180);
    }
}
