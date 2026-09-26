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
    public async Task ReplaceAll_restores_saved_answers()
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
        Assert.Equal(QuestionnaireSaveStatus.Saved, autosave.Status);
    }

    [Fact]
    public async Task Persist_failure_marks_failed_and_retry_succeeds()
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

        await Assert.ThrowsAsync<InvalidOperationException>(() => autosave.SetAnswerAsync(1, 3));
        Assert.Equal(QuestionnaireSaveStatus.Failed, autosave.Status);

        fail = false;
        await autosave.RetryAsync();
        Assert.Equal(QuestionnaireSaveStatus.Saved, autosave.Status);
    }
}

public class QuestionnaireFlowTests
{
    private static List<QuestionnaireQuestion> Sample() =>
    [
        new() { Id = 1, Text = "a", CategoryKey = "A", CategoryLabel = "Alpha" },
        new() { Id = 2, Text = "b", CategoryKey = "A", CategoryLabel = "Alpha" },
        new() { Id = 3, Text = "c", CategoryKey = "B", CategoryLabel = "Beta" },
        new() { Id = 4, Text = "d", CategoryKey = "B", CategoryLabel = "Beta" }
    ];

    [Fact]
    public void FirstUnanswered_and_next_after_work()
    {
        var questions = Sample();
        var answers = new Dictionary<int, int> { [1] = 4, [2] = 3 };
        Assert.Equal(3, QuestionnaireFlow.FirstUnansweredId(questions, answers));
        Assert.Equal(3, QuestionnaireFlow.NextUnansweredAfter(questions, answers, 2));
        Assert.Equal(4, QuestionnaireFlow.NextUnansweredAfter(questions, answers, 3));
    }

    [Fact]
    public void Finish_is_only_when_all_answered()
    {
        var questions = Sample();
        var partial = new Dictionary<int, int> { [1] = 1, [2] = 2, [3] = 3 };
        Assert.NotNull(QuestionnaireFlow.FirstUnansweredId(questions, partial));
        Assert.False(partial.Count >= questions.Count);

        var full = new Dictionary<int, int> { [1] = 1, [2] = 2, [3] = 3, [4] = 4 };
        Assert.Null(QuestionnaireFlow.FirstUnansweredId(questions, full));
        Assert.True(full.Count >= questions.Count);
    }

    [Fact]
    public void Category_progress_for_current_question()
    {
        var questions = Sample();
        var answers = new Dictionary<int, int> { [1] = 5 };
        var (done, total) = QuestionnaireFlow.CategoryProgress(questions, answers, 2);
        Assert.Equal(1, done);
        Assert.Equal(2, total);
        Assert.Equal("Alpha", QuestionnaireFlow.CategoryLabelFor(questions, 2));
    }
}

public class CompactQuestionnaireContractTests
{
    [Fact]
    public void Shared_components_expose_a11y_radios_and_shell_chrome()
    {
        var root = FindRepoRoot();
        var likert = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Shared/Questionnaire/LikertScaleQuestion.razor"));
        Assert.Contains("<fieldset", likert);
        Assert.Contains("q-likert__legend", likert);
        Assert.Contains("type=\"radio\"", likert);
        Assert.Contains("aria-label=\"@Culture[$\"Competency.Likert.{current}\"]\"", likert);
        Assert.Contains("q-likert__opt--lg", likert);
        Assert.Contains("q-likert__opt--sm", likert);
        Assert.Contains("Questionnaire.Disagree", likert);
        Assert.Contains("Questionnaire.Agree", likert);

        var shell = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Shared/Questionnaire/QuestionnaireShell.razor"));
        Assert.Contains("Questionnaire.BackAria", shell);
        Assert.Contains("questionnaire__meter", shell);
        Assert.Contains("Questionnaire.FinishCta", shell);
        Assert.Contains("Questionnaire.FinishAfter", shell);
        Assert.Contains("jobsyQuestionnaire.scrollToQuestion", shell);
        Assert.DoesNotContain("Competency.SaveDraft", shell);
    }

    [Fact]
    public void All_five_candidate_tests_use_shared_shell_and_autosave()
    {
        var root = FindRepoRoot();
        string[] pages =
        [
            "CompetencyTest.razor",
            "CareerTest.razor",
            "CultureScan.razor",
            "ValuesScan.razor",
            "DeepAnalysis.razor"
        ];

        foreach (var page in pages)
        {
            var text = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate", page));
            Assert.Contains("<QuestionnaireShell", text);
            Assert.Contains("QuestionnaireAutosave", text);
            Assert.Contains("<LikertScaleQuestion", text);
            Assert.DoesNotContain("Competency.SaveDraft", text);
            Assert.DoesNotContain("profile-save-bar", text);
            Assert.DoesNotContain("competency-likert", text);
        }
    }

    [Fact]
    public void MainLayout_hides_chrome_on_questionnaire_routes()
    {
        var layout = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Layout/MainLayout.razor"));
        Assert.Contains("app-shell--questionnaire", layout);
        Assert.Contains("IsQuestionnairePath", layout);
        Assert.Contains("candidate/competencies", layout);
        Assert.Contains("candidate/deep-analysis", layout);
        Assert.True(Jobsy.Web.Components.Layout.MainLayout.IsQuestionnairePath("candidate/competencies"));
        Assert.True(Jobsy.Web.Components.Layout.MainLayout.IsQuestionnairePath("candidate/deep-analysis/career"));
        Assert.False(Jobsy.Web.Components.Layout.MainLayout.IsQuestionnairePath("candidate/profile"));
    }

    [Fact]
    public void Css_defines_compact_likert_circle_sizes_and_footer()
    {
        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains(".q-likert__opt--lg .q-likert__circle {\n    width: 34px;\n    height: 34px;", css);
        Assert.Contains(".q-likert__opt--sm .q-likert__circle {\n    width: 22px;\n    height: 22px;", css);
        Assert.Contains(".questionnaire__footer", css);
        Assert.Contains("max-width: 40rem", css);
        Assert.Contains("jobsyQuestionnaire", File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/js/app-core.js")));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repo root not found.");
    }
}
