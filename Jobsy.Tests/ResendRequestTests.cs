using System.Text.Json;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Services;

namespace Jobsy.Tests;

public class ResendRequestTests
{
    [Fact]
    public void Optional_mail_payload_has_text_reply_to_tags_and_list_unsubscribe()
    {
        var message = new EmailMessage("alex@example.com", "Tips", "<p>hi</p>", "PushBom")
        {
            BodyText = "hi",
            ReplyTo = "support@lobsy.nl",
            Headers = new Dictionary<string, string>
            {
                ["X-Entity-Ref-ID"] = Guid.NewGuid().ToString("D"),
                ["Auto-Submitted"] = "auto-generated",
                ["List-Unsubscribe"] = "<https://lobsy.nl/mail/afmelden?t=abc>",
                ["List-Unsubscribe-Post"] = "List-Unsubscribe=One-Click"
            },
            Tags =
            [
                ("category", "PushBom"),
                ("lang", "nl"),
                ("kind", "O")
            ],
            IdempotencyKey = "pushbom:1:20260930"
        };

        var req = SmtpEmailService.CreateResendRequest(message, "Lobsy <hallo@mail.lobsy.nl>");
        var json = JsonSerializer.Serialize(req);
        Assert.Contains("\"text\"", json, StringComparison.Ordinal);
        Assert.Contains("\"reply_to\"", json, StringComparison.Ordinal);
        Assert.Contains("support@lobsy.nl", json, StringComparison.Ordinal);
        Assert.Contains("List-Unsubscribe", json, StringComparison.Ordinal);
        Assert.Contains("List-Unsubscribe-Post", json, StringComparison.Ordinal);
        Assert.Contains("\"tags\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"tracking\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("click_tracking", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("open_tracking", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Essential_mail_payload_has_no_list_unsubscribe()
    {
        var message = new EmailMessage("alex@example.com", "Bevestiging", "<p>ok</p>", "ApplicationConfirmation")
        {
            BodyText = "ok",
            ReplyTo = "support@lobsy.nl",
            Headers = new Dictionary<string, string>
            {
                ["X-Entity-Ref-ID"] = Guid.NewGuid().ToString("D"),
                ["Auto-Submitted"] = "auto-generated"
            },
            Tags = [("category", "ApplicationConfirmation"), ("lang", "nl"), ("kind", "E")]
        };

        var json = JsonSerializer.Serialize(SmtpEmailService.CreateResendRequest(message, "Lobsy <hallo@mail.lobsy.nl>"));
        Assert.DoesNotContain("List-Unsubscribe", json, StringComparison.Ordinal);
        Assert.Contains("\"text\"", json, StringComparison.Ordinal);
        Assert.Contains("\"reply_to\"", json, StringComparison.Ordinal);
    }
}
