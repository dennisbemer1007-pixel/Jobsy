namespace Jobsy.Tests;

/// <summary>
/// Step 7–10 SSR mini test saves must queue rapid taps instead of parallel POSTs.
/// </summary>
public sealed class OnboardingSsrMiniSaveQueueGuardTests
{
    [Fact]
    public void Onboarding_client_form_queues_ssr_mini_pick_saves()
    {
        var root = FindRepoRoot();
        var clientForm = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "onboarding-client-form.js"));

        Assert.Contains("bindSsrMiniPickQueue", clientForm, StringComparison.Ordinal);
        Assert.Contains("enqueueSsrMiniSave", clientForm, StringComparison.Ordinal);
        Assert.Contains("ssrMiniSaveChains", clientForm, StringComparison.Ordinal);
        Assert.Contains("jobsyOnboardingSsrSave.putStep", clientForm, StringComparison.Ordinal);
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