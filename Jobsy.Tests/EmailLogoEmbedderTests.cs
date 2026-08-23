using System.Text.Json;
using Jobsy.Core.Email;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Services;

namespace Jobsy.Tests;

public class EmailLogoEmbedderTests
{
    [Fact]
    public void RewriteToCid_replaces_remote_brand_mark()
    {
        var html = EmailLayout.Wrap("<p>Hallo</p>", "https://lobsy.nl");
        var embedded = EmailLogoEmbedder.RewriteToCid(html);

        Assert.Contains("cid:" + EmailLayout.LogoContentId, embedded);
        Assert.DoesNotContain("/images/brand/lobsy", embedded, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RewriteToCid_replaces_relative_brand_mark()
    {
        var html = """<img src="/images/brand/lobsy-128.png?v=mail" alt="Lobsy" />""";
        var embedded = EmailLogoEmbedder.RewriteToCid(html);
        Assert.Equal("""<img src="cid:lobsy-logo" alt="Lobsy" />""", embedded);
    }

    [Fact]
    public void Resend_payload_embeds_png_bytes_not_base64_text()
    {
        var composed = TransactionalEmails.MailTest("https://lobsy.nl");
        var request = SmtpEmailService.CreateResendRequest(
            new EmailMessage("dev@example.com", composed.Subject, composed.Html, composed.Category),
            "Lobsy <noreply@lobsy.nl>");

        Assert.Contains("cid:" + EmailLayout.LogoContentId, request.Html);
        Assert.DoesNotContain("/images/brand/lobsy", request.Html, StringComparison.OrdinalIgnoreCase);

        var attachment = Assert.Single(request.Attachments);
        Assert.Equal("lobsy-logo.png", attachment.Filename);
        Assert.Equal("lobsy-logo", attachment.ContentId);
        Assert.Equal("image/png", attachment.ContentType);
        Assert.Equal(0x89, attachment.Content[0]);
        Assert.Equal((int)'P', attachment.Content[1]);
        Assert.Equal((int)'N', attachment.Content[2]);
        Assert.Equal((int)'G', attachment.Content[3]);
        Assert.Equal(EmailLogoEmbedder.PngBytes().Length, attachment.Content.Length);

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Contains("\"content_id\":\"lobsy-logo\"", json);
        Assert.Contains("\"content_type\":\"image/png\"", json);
        Assert.Contains("\"content\":[137,80,78,71,", json);
        Assert.DoesNotContain("\"content\":\"iVBOR", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Embedded_png_is_a_small_valid_png()
    {
        var bytes = EmailLogoEmbedder.PngBytes();
        Assert.True(bytes.Length is > 1000 and < 40_000);
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'N', bytes[2]);
        Assert.Equal((byte)'G', bytes[3]);
    }

    [Fact]
    public void Wwwroot_email_logo_file_exists()
    {
        var path = Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "images", "brand", "lobsy-128.png");
        Assert.True(File.Exists(path), path);
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
