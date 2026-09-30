using Jobsy.Core.Email;
using Jobsy.Core.Email.Model;
using Jobsy.Core.Options;

namespace Jobsy.Tests;

public class EmailAssetTests
{
    [Fact]
    public void Email_png_assets_exist_are_valid_and_small()
    {
        var root = FindRepoRoot();
        foreach (var name in new[] { "lobsy-mark-72.png", "mascot-celebrating-128.png" })
        {
            var path = Path.Combine(root, "Jobsy.Web", "wwwroot", "images", "email", name);
            Assert.True(File.Exists(path), path);
            var bytes = File.ReadAllBytes(path);
            Assert.True(bytes.Length is > 100 and <= 20_000, $"{name} size {bytes.Length}");
            Assert.Equal(0x89, bytes[0]);
            Assert.Equal((byte)'P', bytes[1]);
            Assert.Equal((byte)'N', bytes[2]);
            Assert.Equal((byte)'G', bytes[3]);
        }
    }

    [Fact]
    public void Renderer_imgs_are_absolute_https_with_size_and_alt()
    {
        var brand = EmailBrand.From(new MailOptions { AssetVersion = "testv" }, "https://lobsy.nl");
        var doc = new EmailDocument(
            "EmployerReactionAccepted",
            EmailKind.Essential,
            EmailCulture.Nl,
            "Subject",
            "Preheader other",
            "Heading",
            [new ParagraphBlock(EmailText.Plain("Body"))],
            "Reason",
            "Team Lobsy",
            ShowMascot: true);
        var html = EmailRenderer.Render(doc, brand).Html;
        Assert.Contains("https://lobsy.nl/images/email/lobsy-mark-72.png?v=testv", html);
        Assert.Contains("width=\"36\"", html);
        Assert.Contains("height=\"36\"", html);
        Assert.Contains("alt=\"Lobsy\"", html);
        Assert.Contains("mascot-celebrating-128.png?v=testv", html);
        Assert.Contains("width=\"64\"", html);
        Assert.Contains("alt=\"\"", html);
        Assert.DoesNotContain("cid:", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Smtp_resend_payload_has_no_cid()
    {
        var composed = TransactionalEmails.MailTest("https://lobsy.nl");
        var request = Jobsy.Infrastructure.Services.SmtpEmailService.CreateResendRequest(
            new Jobsy.Core.Interfaces.EmailMessage("dev@example.com", composed.Subject, composed.Html, composed.Category),
            "Lobsy <hallo@mail.lobsy.nl>");
        Assert.DoesNotContain("cid:", request.Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("lobsy-mark-72.png", request.Html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Chatbots_keep_the_illustrated_mascot()
    {
        var root = FindRepoRoot();
        var assistant = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "LobsyAssistantChat.razor"));
        Assert.Contains("UseMascot=\"true\"", assistant);
        var coach = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "LobsyCoachAvatar.razor"));
        Assert.Contains("BrandImages.MascotWebp128", coach);
        Assert.True(File.Exists(Path.Combine(root, "Jobsy.Web", "wwwroot", "images", "brand", "mascot-128.png")));
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
