using Jobsy.Web.Components.Shared.Questionnaire;

namespace Jobsy.Tests;

public class QuestionnaireAutosaveTests
{
    [Fact]
    public async Task SetAnswer_debounces_and_persists_batched_answers()
    {
        var calls = new List<IReadOnlyDictionary<int, int>>();
        await using var autosave = new QuestionnaireAutosave(
            persist: (answers, _) =>
            {
                calls.Add(new Dictionary<int, int>(answers));
                return Task.CompletedTask;
            },
            debounceMs: 50);

        var t1 = autosave.SetAnswerAsync(1, 4);
        var t2 = autosave.SetAnswerAsync(2, 5);
        await Task.WhenAll(t1, t2);
        await autosave.FlushAsync();

        Assert.True(calls.Count >= 1);
        var last = calls[^1];
        Assert.Equal(4, last[1]);
        Assert.Equal(5, last[2]);
        Assert.Equal(QuestionnaireSaveStatus.Saved, autosave.Status);
        Assert.Equal(2, autosave.AnsweredCount);
    }

    [Fact]
    public async Task ReplaceAll_restores_answers_as_idle()
    {
        await using var autosave = new QuestionnaireAutosave(
            persist: (_, _) => Task.CompletedTask,
            debounceMs: 0);

        autosave.ReplaceAll(new Dictionary<int, int> { [3] = 2, [7] = 5, [9] = 99 });
        Assert.True(autosave.TryGetAnswer(3, out var v3));
        Assert.Equal(2, v3);
        Assert.True(autosave.TryGetAnswer(7, out var v7));
        Assert.Equal(5, v7);
        Assert.False(autosave.TryGetAnswer(9, out _));
        Assert.Equal(2, autosave.AnsweredCount);
        Assert.Equal(QuestionnaireSaveStatus.Idle, autosave.Status);
    }

    [Fact]
    public async Task Persist_failure_marks_failed_without_throw_and_retry_succeeds()
    {
        var fail = true;
        await using var autosave = new QuestionnaireAutosave(
            persist: (_, _) =>
            {
                if (fail)
                {
                    throw new InvalidOperationException("network");
                }

                return Task.CompletedTask;
            },
            debounceMs: 0);

        await autosave.SetAnswerAsync(1, 3);
        Assert.Equal(QuestionnaireSaveStatus.Failed, autosave.Status);

        fail = false;
        await autosave.RetryAsync();
        Assert.Equal(QuestionnaireSaveStatus.Saved, autosave.Status);
    }

    [Fact]
    public async Task Dispose_flushes_pending_answer_within_debounce()
    {
        IReadOnlyDictionary<int, int>? persisted = null;
        await using (var autosave = new QuestionnaireAutosave(
                         (answers, _) =>
                         {
                             persisted = answers.ToDictionary(kv => kv.Key, kv => kv.Value);
                             return Task.CompletedTask;
                         },
                         debounceMs: 800))
        {
            await autosave.SetAnswerAsync(1, 4);
        }

        Assert.NotNull(persisted);
        Assert.Equal(4, persisted![1]);
    }

    [Fact]
    public async Task Initial_status_is_idle()
    {
        await using var autosave = new QuestionnaireAutosave((_, _) => Task.CompletedTask);
        Assert.Equal(QuestionnaireSaveStatus.Idle, autosave.Status);
    }
}
