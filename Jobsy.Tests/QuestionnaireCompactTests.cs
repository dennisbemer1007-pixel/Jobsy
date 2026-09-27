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
    public void Shared_components_expose_a11y_buttons_and_shell_chrome()
    {
        var root = FindRepoRoot();
        var likert = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Shared/Questionnaire/LikertScaleQuestion.razor"));
        Assert.Contains("<fieldset", likert);
        Assert.Contains("q-likert__legend", likert);
        Assert.Contains("aria-pressed", likert);
        Assert.Contains("q-likert__opt", likert);
        Assert.Contains("Questionnaire.Likert.Aria", likert);
        Assert.Contains("Questionnaire.Likert.Low", likert);
        Assert.Contains("Questionnaire.Likert.High", likert);
        Assert.Contains("q-likert--collapsed", likert);

        var shell = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Shared/Questionnaire/QuestionnaireShell.razor"));
        Assert.Contains("Questionnaire.BackAria", shell);
        Assert.Contains("questionnaire__meter", shell);
        Assert.Contains("Questionnaire.FinishCtaShort", shell);
        Assert.Contains("Questionnaire.NextCta", shell);
        Assert.Contains("Questionnaire.PrivacyShort", shell);
        Assert.Contains("Questionnaire.PrivacyMore", shell);
        Assert.Contains("@(\" \")", shell);
        Assert.Contains("jobsyQuestionnaire.scrollToQuestion", shell);
        Assert.DoesNotContain("Competency.SaveDraft", shell);
        Assert.DoesNotContain("<text> </text>", shell);
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
            Assert.Contains("Collapsed=", text);
            Assert.Contains("OnNext=", text);
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
    public void Questionnaire_css_is_scoped_feature_file_with_overflow_fixes()
    {
        var root = FindRepoRoot();
        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/questionnaire.css"));
        Assert.Contains("fieldset.q-likert", css);
        Assert.Contains("min-inline-size: 0", css);
        Assert.Contains("white-space: normal", css);
        Assert.Contains("overflow-wrap: anywhere", css);
        Assert.Contains("float: inline-start", css);
        Assert.Contains("grid-template-columns: repeat(5, minmax(0, 1fr))", css);
        Assert.Contains("min-block-size: 48px", css);
        Assert.Contains(".q-likert--collapsed .q-likert__text", css);

        var appCss = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/app.css"));
        Assert.DoesNotContain(".questionnaire {\n", appCss);
        Assert.DoesNotContain(".q-likert {\n", appCss);

        var appMin = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/app.min.css"));
        Assert.DoesNotContain(".questionnaire{", appMin);
        Assert.DoesNotContain(".q-likert{", appMin);

        var wizard = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/onboarding-wizard.css"));
        var idx = 0;
        while ((idx = wizard.IndexOf(".q-likert", idx, StringComparison.Ordinal)) >= 0)
        {
            var window = wizard[Math.Max(0, idx - 80)..idx];
            Assert.Contains(".ob-wizard", window);
            idx += ".q-likert".Length;
        }

        Assert.Contains(
            ".ob-wizard .q-likert--collapsed .q-likert__text",
            wizard);
        Assert.DoesNotContain(
            ".ob-wizard .q-likert__text{overflow:hidden;text-overflow:ellipsis;white-space:nowrap}",
            wizard);

        var app = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/App.razor"));
        Assert.Contains("css/features/questionnaire.css?v=20260927-dna-c", app);
        Assert.Contains("css/features/onboarding-wizard.css?v=20260927-dna-c", app);
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
