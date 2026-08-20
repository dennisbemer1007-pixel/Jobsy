using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class VideoEmbedTests
{
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", "https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ", "https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ")]
    [InlineData("https://vimeo.com/123456789", "https://player.vimeo.com/video/123456789")]
    public void TryGetEmbedUrl_maps_known_hosts(string input, string expected)
        => Assert.Equal(expected, VideoEmbed.TryGetEmbedUrl(input));

    [Fact]
    public void TryGetEmbedUrl_rejects_unknown_or_unsafe()
    {
        Assert.Null(VideoEmbed.TryGetEmbedUrl("https://example.com/video.mp4"));
        Assert.Null(VideoEmbed.TryGetEmbedUrl("javascript:alert(1)"));
        Assert.Null(VideoEmbed.TryGetEmbedUrl(null));
    }

    [Fact]
    public void TryGetSafeWatchUrl_keeps_https_link()
        => Assert.Equal(
            "https://example.com/video.mp4",
            VideoEmbed.TryGetSafeWatchUrl("https://example.com/video.mp4"));

    [Fact]
    public void Vacancy_detail_video_matches_photo_height()
    {
        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "css", "app.css"));
        var videoIdx = css.IndexOf(".detail-card__video {", StringComparison.Ordinal);
        Assert.True(videoIdx > 0);
        var videoCss = css.Substring(videoIdx, 420);
        Assert.Contains("calc(240px * 16 / 9)", videoCss);
        Assert.Contains("max-height: 240px", videoCss);
        Assert.Contains("aspect-ratio: 16 / 9", videoCss);
        Assert.DoesNotContain("width: 100%", videoCss.Split(".detail-card__video iframe")[0]);
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

        throw new InvalidOperationException("Jobsy.sln not found from test base directory.");
    }
}
