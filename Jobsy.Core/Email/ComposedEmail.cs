using Jobsy.Core.Email.Model;

namespace Jobsy.Core.Email;

/// <summary>Fully rendered transactional mail ready to send or preview.</summary>
public sealed record ComposedEmail(
    string Key,
    string Category,
    EmailKind Kind,
    string Language,
    string Subject,
    string Preheader,
    string Html,
    string Text)
{
    /// <summary>Legacy 4-arg shape used during incremental ports and tests.</summary>
    public ComposedEmail(string key, string category, string subject, string html)
        : this(key, category, EmailKind.Essential, "nl", subject, "", html, "")
    {
    }

    public static ComposedEmail FromDocument(EmailDocument doc, EmailRenderResult rendered, string category)
        => new(
            doc.TemplateKey,
            category,
            doc.Kind,
            doc.Culture.Language,
            doc.Subject,
            doc.Preheader,
            rendered.Html,
            rendered.Text);

    public static ComposedEmail Render(
        EmailDocument doc,
        EmailBrand brand,
        string? category = null,
        EmailRenderMode mode = EmailRenderMode.Normal)
    {
        var def = EmailTemplateRegistry.TryGet(doc.TemplateKey, out var d) ? d : null;
        var cat = category ?? def?.Category ?? doc.TemplateKey;
        // Mascot only when registry allows it.
        var effective = doc.ShowMascot && !EmailTemplateRegistry.AllowsMascot(doc.TemplateKey)
            ? doc with { ShowMascot = false }
            : doc;
        // Preheader must not equal subject — fall back to first paragraph sentence.
        if (string.Equals(effective.Preheader?.Trim(), effective.Subject?.Trim(), StringComparison.Ordinal))
        {
            var fallback = FirstSentence(effective)
                           ?? "Bericht van Lobsy";
            if (string.Equals(fallback, effective.Subject?.Trim(), StringComparison.Ordinal))
            {
                fallback = "Bericht van Lobsy";
            }

            effective = effective with { Preheader = fallback };
        }

        var rendered = EmailRenderer.Render(effective, brand, mode);
        return FromDocument(effective, rendered, cat);
    }

    private static string? FirstSentence(EmailDocument doc)
    {
        foreach (var block in doc.Blocks)
        {
            if (block is ParagraphBlock p)
            {
                var flat = p.Text.Flatten().Trim();
                if (flat.Length == 0)
                {
                    continue;
                }

                var end = flat.IndexOfAny(['.', '!', '?']);
                return end > 0 ? flat[..(end + 1)].Trim() : flat;
            }
        }

        return null;
    }
}
