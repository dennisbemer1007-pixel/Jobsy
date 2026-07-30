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
}
