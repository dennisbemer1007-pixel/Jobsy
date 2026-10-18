namespace Jobsy.Tests;

/// <summary>
/// Playwright captures for contextual tips use <see cref="IllustratedPagesApiStub"/> contextual-tips payloads.
/// Run with the illustrated factory and <c>ContextualTipsEnabled: true</c> when adding full matrix captures.
/// </summary>
public sealed class ContextualTipsScreenshotTests
{
    [Fact]
    public void Stub_includes_contextual_tips_for_capture_hosts()
    {
        var json = IllustratedPagesApiStub.BuildJson("http://api.test/api/me/contextual-tips/settings");
        Assert.Contains("momentTipsEnabled", json, StringComparison.OrdinalIgnoreCase);
    }
}
