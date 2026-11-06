namespace Jobsy.Tests;

/// <summary>
/// NL candidate assistant copy must not use em/en dashes in user-facing scripted replies.
/// </summary>
public sealed class AssistantChatNlCopyGuardTests
{
    [Fact]
    public void AssistantChatService_nl_candidate_bullets_avoid_em_dash_in_source()
    {
        var src = File.ReadAllText(Path.Combine(
            TestRepo.FindRoot(),
            "Jobsy.Infrastructure",
            "Services",
            "AssistantChatService.cs"));

        const string nlVacancyBullet = "FormatVacancyBullet";
        const string nlApplicationBullet = "FormatApplicationBullet";
        Assert.Contains(nlVacancyBullet, src, StringComparison.Ordinal);
        Assert.Contains(nlApplicationBullet, src, StringComparison.Ordinal);
        Assert.Contains(": lang == \"en\"", src, StringComparison.Ordinal);
        Assert.DoesNotContain("Er zijn {count} vacatures in deze branche —", src, StringComparison.Ordinal);
    }
}
