using Jobsy.Web.Models;

namespace Jobsy.Web.Services;

/// <summary>
/// Shared match swipe deck state for <c>/candidate/match</c> and the desktop banenkaart dialog.
/// </summary>
public sealed class MatchDeck
{
    private readonly List<SwipeViewModel> _items = [];

    public MatchProfileGateViewModel Gate { get; private set; } = new();
    public IReadOnlyList<SwipeViewModel> Items => _items;
    public int Index { get; private set; }
    public bool IsLoaded { get; private set; }
    public bool LoadFailed { get; private set; }

    public int Count => _items.Count;
    public int Position => Count == 0 ? 0 : Math.Min(Index + 1, Count);
    public bool IsFinished => IsLoaded && !LoadFailed && Gate.IsProfileComplete && Index >= Count;

    public SwipeViewModel? Current =>
        Index >= 0 && Index < _items.Count ? _items[Index] : null;

    public SwipeViewModel? PeekNext =>
        Index + 1 < _items.Count ? _items[Index + 1] : null;

    public async Task LoadAsync(
        CandidateMatchProfileService profiles,
        MatchVacancyService vacancies,
        int take = 24,
        bool orderByMatch = false,
        CancellationToken cancellationToken = default)
    {
        _items.Clear();
        Index = 0;
        IsLoaded = false;
        LoadFailed = false;
        try
        {
            Gate = await profiles.RefreshAsync(cancellationToken);
            if (Gate.IsProfileComplete)
            {
                var raw = await vacancies.GetRelevantSwipeVacanciesAsync(Gate, take, cancellationToken);
                IEnumerable<VacancyListItem> ordered = raw;
                if (orderByMatch)
                {
                    ordered = raw
                        .OrderByDescending(v => v.MatchPercent ?? int.MinValue)
                        .ThenBy(v => v.Id);
                }

                foreach (var item in ordered)
                {
                    _items.Add(SwipeViewModel.FromVacancy(item, showMatchPercentage: true));
                }
            }
        }
        catch
        {
            LoadFailed = true;
            _items.Clear();
            Index = 0;
        }
        finally
        {
            IsLoaded = true;
        }
    }

    /// <summary>Restore index after a slim persist reload that repopulated items.</summary>
    public void RestoreIndex(IReadOnlyList<Guid> vacancyIds, int savedIndex)
    {
        if (vacancyIds.Count == 0 || _items.Count == 0)
        {
            Index = 0;
            return;
        }

        var resumeId = savedIndex < vacancyIds.Count ? vacancyIds[savedIndex] : vacancyIds[^1];
        var found = _items.FindIndex(v => v.VacancyId == resumeId);
        Index = found >= 0 ? found : 0;
    }

    public void SetGate(MatchProfileGateViewModel gate) => Gate = gate ?? new();

    public void ReplaceItems(IEnumerable<SwipeViewModel> items, int index = 0)
    {
        _items.Clear();
        _items.AddRange(items);
        Index = Math.Clamp(index, 0, Math.Max(0, _items.Count));
        IsLoaded = true;
        LoadFailed = false;
    }

    public IReadOnlyList<SwipeViewModel> UpNext(int count)
    {
        if (count <= 0 || Index + 1 >= _items.Count)
        {
            return [];
        }

        var start = Index + 1;
        var take = Math.Min(count, _items.Count - start);
        return _items.GetRange(start, take);
    }

    public void Advance()
    {
        if (Index < _items.Count)
        {
            Index++;
        }
    }

    public void ResumeAt(Guid vacancyId)
    {
        var found = _items.FindIndex(v => v.VacancyId == vacancyId);
        if (found >= 0)
        {
            Index = found;
        }
    }

    public async Task LikeAsync(JobsyApiClient api, SwipeViewModel model)
    {
        if (model.VacancyId is not Guid id)
        {
            return;
        }

        try
        {
            await api.SetLikedAsync(id, liked: true);
        }
        catch
        {
            // Guest / network — swipe UX must stay instant.
        }
    }
}
