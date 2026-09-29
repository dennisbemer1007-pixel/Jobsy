namespace Jobsy.Core.Scholen;

/// <summary>One Likert item in the leerling question bank (05 supplies the real 60).</summary>
public sealed record PupilQuestionItem(
    string Id,
    string WorldKey,
    int IndexInWorld,
    int GlobalIndex,
    string TextKey,
    string ImagineKey);

public interface IPupilQuestionBank
{
    IReadOnlyList<PupilQuestionItem> AllItems { get; }

    PupilQuestionItem? GetById(string itemId);

    PupilQuestionItem? GetByGlobalIndex(int globalIndex);
}

/// <summary>
/// Placeholder bank: 4 test worlds × 3 items = 12. File 05 replaces with the real 60.
/// Registered in production too so the wizard shell works end-to-end before 05.
/// </summary>
public sealed class PlaceholderPupilQuestionBank : IPupilQuestionBank
{
    private readonly IReadOnlyList<PupilQuestionItem> _items;
    private readonly Dictionary<string, PupilQuestionItem> _byId;

    public PlaceholderPupilQuestionBank()
    {
        var list = new List<PupilQuestionItem>();
        var global = 0;
        foreach (var world in PupilWorldCatalog.Worlds.Where(w => w.IsTestWorld))
        {
            for (var i = 0; i < 3; i++)
            {
                var id = $"{world.Key}-{i + 1}";
                list.Add(new PupilQuestionItem(
                    id,
                    world.Key,
                    i,
                    global,
                    $"Leerling.Placeholder.Q.{world.Key}.{i + 1}",
                    $"Leerling.Placeholder.Imagine.{world.Key}.{i + 1}"));
                global++;
            }
        }

        _items = list;
        _byId = list.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<PupilQuestionItem> AllItems => _items;

    public PupilQuestionItem? GetById(string itemId)
        => _byId.TryGetValue(itemId, out var item) ? item : null;

    public PupilQuestionItem? GetByGlobalIndex(int globalIndex)
        => globalIndex >= 0 && globalIndex < _items.Count ? _items[globalIndex] : null;
}

/// <summary>Builds <see cref="Entities.Scholen.PupilResult"/> after all items are answered (stub → 05/06).</summary>
public interface IPupilResultBuilder
{
    Task BuildAsync(Guid pupilCodeId, CancellationToken cancellationToken = default);
}

/// <summary>No-op stub until scoring lands in 05/06.</summary>
public sealed class StubPupilResultBuilder : IPupilResultBuilder
{
    public Task BuildAsync(Guid pupilCodeId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
