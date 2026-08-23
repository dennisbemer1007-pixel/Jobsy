using System.Text.Json;
using Jobsy.Core.Email;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Services;

namespace Jobsy.Tests;

public class EmailLogoEmbedderTests
{
    [Fact]
    public void Resend_payload_keeps_hosted_https_logo_and_has_no_cid_attachment()
    {
        var composed = TransactionalEmails.MailTest("https://lobsy.nl");
        var request = SmtpEmailService.CreateResendRequest(
            new EmailMessage("dev@example.com", composed.Subject, composed.Html, composed.Category),
            "Lobsy <noreply@lobsy.nl>");

        Assert.Contains("https://lobsy.nl/images/brand/lobsy-128.png", request.Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("v=20260823-hosted", request.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("cid:", request.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/images/brand/lobsy.png?", request.Html, StringComparison.OrdinalIgnoreCase);

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.DoesNotContain("\"attachments\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("content_id", json, StringComparison.Ordinal);
        Assert.Contains("lobsy-128.png", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Wwwroot_email_logo_file_is_a_small_valid_png()
    {
        var path = Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "images", "brand", "lobsy-128.png");
        Assert.True(File.Exists(path), path);

        var bytes = File.ReadAllBytes(path);
        Assert.True(bytes.Length is > 1000 and < 40_000);
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'N', bytes[2]);
        Assert.Equal((byte)'G', bytes[3]);
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
