using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Services;
using MimeKit;

namespace Jobsy.Tests;

public class SmtpMimeTests
{
    [Fact]
    public void Multipart_alternative_has_text_html_reply_to_and_optional_headers()
    {
        var message = new EmailMessage("alex@example.com", "Tips", "<p>hi</p>", "PushBom")
        {
            BodyText = "hi plain",
            ReplyTo = "support@lobsy.nl",
            Headers = new Dictionary<string, string>
            {
                ["X-Entity-Ref-ID"] = "guid-1",
                ["Auto-Submitted"] = "auto-generated",
                ["List-Unsubscribe"] = "<https://lobsy.nl/mail/afmelden?t=abc>",
                ["List-Unsubscribe-Post"] = "List-Unsubscribe=One-Click"
            }
        };

        var mime = BuildMime(message, "Lobsy <hallo@mail.lobsy.nl>");
        Assert.Contains(mime.ReplyTo, a => a.ToString().Contains("support@lobsy.nl", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("guid-1", mime.Headers["X-Entity-Ref-ID"]);
        Assert.Equal("List-Unsubscribe=One-Click", mime.Headers["List-Unsubscribe-Post"]);
        Assert.NotNull(mime.Body);
        Assert.DoesNotContain("cid:", mime.ToString(), StringComparison.OrdinalIgnoreCase);

        var multipart = Assert.IsAssignableFrom<Multipart>(mime.Body);
        Assert.Equal("alternative", multipart.ContentType.MediaSubtype, ignoreCase: true);
        Assert.Contains(multipart, p => p is TextPart tp && tp.IsPlain && tp.Text.Contains("hi plain", StringComparison.Ordinal));
        Assert.Contains(multipart, p => p is TextPart th && th.IsHtml && th.Text.Contains("<p>hi</p>", StringComparison.Ordinal));
    }

    [Fact]
    public void Essential_mail_omits_list_unsubscribe_headers()
    {
        var message = new EmailMessage("alex@example.com", "Ok", "<p>ok</p>", "ApplicationConfirmation")
        {
            BodyText = "ok",
            ReplyTo = "support@lobsy.nl",
            Headers = new Dictionary<string, string>
            {
                ["X-Entity-Ref-ID"] = "guid-2",
                ["Auto-Submitted"] = "auto-generated"
            }
        };
        var mime = BuildMime(message, "hallo@mail.lobsy.nl");
        Assert.Null(mime.Headers["List-Unsubscribe"]);
        Assert.Null(mime.Headers["List-Unsubscribe-Post"]);
        Assert.Equal("Lobsy", ((MailboxAddress)mime.From[0]).Name);
    }

    private static MimeMessage BuildMime(EmailMessage message, string from)
    {
        var mime = new MimeMessage();
        mime.From.Add(SmtpEmailService.EnsureLobsyDisplayName(MailboxAddress.Parse(from)));
        mime.To.Add(MailboxAddress.Parse(message.To));
        mime.Subject = message.Subject;
        if (!string.IsNullOrWhiteSpace(message.ReplyTo))
        {
            mime.ReplyTo.Add(MailboxAddress.Parse(message.ReplyTo));
        }

        if (message.Headers is not null)
        {
            foreach (var (name, value) in message.Headers)
            {
                mime.Headers.Add(name, value);
            }
        }

        var builder = new BodyBuilder { HtmlBody = message.BodyHtml, TextBody = message.BodyText ?? "" };
        mime.Body = builder.ToMessageBody();
        return mime;
    }
}
