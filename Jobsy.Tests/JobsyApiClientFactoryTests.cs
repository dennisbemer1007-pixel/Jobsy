using Jobsy.Web.Services;

namespace Jobsy.Tests;

public class JobsyApiClientFactoryTests
{
    [Fact]
    public void Factory_reuses_shared_sockets_handler_without_cookie_jar()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Services", "JobsyApiClientFactory.cs"));
        Assert.Contains("SocketsHttpHandler", source, StringComparison.Ordinal);
        Assert.Contains("PooledConnectionLifetime", source, StringComparison.Ordinal);
        Assert.Contains("UseCookies = false", source, StringComparison.Ordinal);
        Assert.Contains("NonDisposingHandler", source, StringComparison.Ordinal);
        Assert.Contains("AutomaticDecompression", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new HttpClientHandler", source, StringComparison.Ordinal);

        Assert.Same(
            JobsyApiClientFactory.SharedSocketsHandler,
            JobsyApiClientFactory.SharedSocketsHandler);
        Assert.False(JobsyApiClientFactory.SharedSocketsHandler.UseCookies);
        Assert.Equal(TimeSpan.FromMinutes(2), JobsyApiClientFactory.SharedSocketsHandler.PooledConnectionLifetime);
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
