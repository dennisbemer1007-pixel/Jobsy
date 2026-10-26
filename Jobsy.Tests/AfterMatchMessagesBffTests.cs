using System.IO;

namespace Jobsy.Tests;

public sealed class AfterMatchMessagesBffTests
{
    [Fact]
    public void Web_host_maps_after_match_messages_send_to_bff()
    {
        var root = FindRepoRoot();
        var endpoints = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Hosting/AfterMatchMessagesClientEndpoints.cs"));
        Assert.Contains("/api/applications/{applicationId:guid}/after-match/messages", endpoints, StringComparison.Ordinal);

        var program = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Program.cs"));
        Assert.Contains("MapAfterMatchMessagesClientEndpoints", program, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "Jobsy.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? "";
        }

        throw new InvalidOperationException("Could not find repo root.");
    }
}
