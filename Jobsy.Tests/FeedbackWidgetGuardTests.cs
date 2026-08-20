namespace Jobsy.Tests;

/// <summary>
/// Source-level guards so the feedback printscreen actually captures the page
/// (before the modal) and can travel through Blazor Server JS interop.
/// </summary>
public class FeedbackWidgetGuardTests
{
    [Fact]
    public void Widget_captures_screenshot_before_opening_the_modal()
    {
        var widget = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Feedback", "FeedbackWidget.razor"));
        Assert.Contains("lobsyFeedback.captureScreenshot", widget);
        var captureAt = widget.IndexOf("lobsyFeedback.captureScreenshot", StringComparison.Ordinal);
        var openAt = widget.IndexOf("_open = true", StringComparison.Ordinal);
        Assert.True(captureAt > 0 && openAt > captureAt, "Screenshot must be taken before the modal opens.");
        Assert.Contains("_screenshot", widget);
        Assert.Contains("feedback-dialog__shot", widget);
        Assert.Contains("Feedback.PrivacyNote", widget);
        Assert.Contains("PageUrlForSubmit", widget);
        Assert.Contains("GetLeftPart(UriPartial.Path)", widget);
    }

    [Fact]
    public void Screenshot_js_uses_viewport_printscreen_and_absolute_html2canvas()
    {
        var js = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "feedback.js"));
        Assert.Contains("/lib/html2canvas/html2canvas.min.js", js);
        Assert.Contains("window.innerWidth", js);
        Assert.Contains("window.innerHeight", js);
        Assert.Contains("window.scrollX", js);
        Assert.Contains("window.scrollY", js);
        Assert.Contains("image/jpeg", js);
        Assert.Contains("ignoreElements", js);
        Assert.Contains("onclone", js);
        Assert.Contains("feedback-widget", js);
        Assert.Contains("lobsy-dialog", js);
    }

    [Fact]
    public void Blazor_server_accepts_screenshot_sized_js_interop()
    {
        var program = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Program.cs"));
        Assert.Contains("MaximumReceiveMessageSize", program);
        Assert.Contains("2 * 1024 * 1024", program);
    }

    [Fact]
    public void Admin_keeps_saved_prompt_and_requires_review_before_launch()
    {
        var admin = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Pages", "Admin", "FeedbackAdmin.razor"));
        Assert.Contains("Admin.FeedbackUserReport", admin);
        Assert.Contains("Admin.FeedbackReviewAck", admin);
        Assert.Contains("GeneratedPrompt", admin);
        Assert.Contains("CanLaunch", admin);
        var openAt = admin.IndexOf("OpenPromptAsync", StringComparison.Ordinal);
        var saveNullAt = admin.IndexOf("SaveFeedbackPromptAsync(item.Id, null)", StringComparison.Ordinal);
        Assert.True(saveNullAt > openAt);
        Assert.Contains("!string.IsNullOrWhiteSpace(_detail?.GeneratedPrompt)", admin);
    }

    [Fact]
    public void App_shell_does_not_load_feedback_until_the_widget_opens()
    {
        var app = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "App.razor"));
        Assert.DoesNotContain("js/feedback.js", app);

        var widget = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Feedback", "FeedbackWidget.razor"));
        Assert.Contains("lobsyFeedbackEnsure", widget);
        Assert.Contains("FeedbackWidget", File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Layout", "MainLayout.razor")));

        var loader = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "extras-loader.js"));
        Assert.Contains("feedback.js", loader);
        Assert.Contains("lobsyFeedbackEnsure", loader);
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

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
