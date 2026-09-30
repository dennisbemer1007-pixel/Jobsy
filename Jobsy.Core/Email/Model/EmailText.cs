using System.Text;

namespace Jobsy.Core.Email.Model;

public abstract record EmailSegment;

public sealed record PlainSegment(string Text) : EmailSegment;

public sealed record BoldSegment(string Text) : EmailSegment;

public sealed record LinkSegment(string Label, string Url) : EmailSegment;

public abstract record EmailArg
{
    public static EmailArg Plain(string value) => new PlainArg(value);
    public static EmailArg Bold(string value) => new BoldArg(value);
    public static EmailArg Link(string label, string url) => new LinkArg(label, url);
}

public sealed record PlainArg(string Value) : EmailArg;

public sealed record BoldArg(string Value) : EmailArg;

public sealed record LinkArg(string Label, string Url) : EmailArg;

/// <summary>Rich text without HTML. Templates never write markup.</summary>
public sealed class EmailText
{
    public IReadOnlyList<EmailSegment> Segments { get; }

    private EmailText(IReadOnlyList<EmailSegment> segments)
        => Segments = segments;

    public static EmailText Empty { get; } = new([]);

    public static EmailText Plain(string text)
        => new([new PlainSegment(text ?? string.Empty)]);

    public static EmailText Bold(string text)
        => new([new BoldSegment(text ?? string.Empty)]);

    public static EmailText Link(string label, string url)
        => new([new LinkSegment(label ?? string.Empty, url ?? string.Empty)]);

    public static EmailText Join(params EmailText[] parts)
    {
        var list = new List<EmailSegment>();
        foreach (var part in parts)
        {
            if (part is null)
            {
                continue;
            }

            list.AddRange(part.Segments);
        }

        return new EmailText(list);
    }

    /// <summary>
    /// Formats a template with <c>{n}</c> placeholders. Resource values never contain HTML.
    /// </summary>
    public static EmailText Format(string format, params EmailArg[] args)
    {
        format ??= string.Empty;
        args ??= [];
        var segments = new List<EmailSegment>();
        var sb = new StringBuilder();
        for (var i = 0; i < format.Length; i++)
        {
            if (format[i] == '{' && i + 1 < format.Length)
            {
                var end = format.IndexOf('}', i + 1);
                if (end > i + 1 && int.TryParse(format.AsSpan(i + 1, end - i - 1), out var index)
                    && index >= 0 && index < args.Length)
                {
                    if (sb.Length > 0)
                    {
                        segments.Add(new PlainSegment(sb.ToString()));
                        sb.Clear();
                    }

                    segments.AddRange(ArgToSegments(args[index]));
                    i = end;
                    continue;
                }
            }

            sb.Append(format[i]);
        }

        if (sb.Length > 0)
        {
            segments.Add(new PlainSegment(sb.ToString()));
        }

        return new EmailText(segments);
    }

    private static IEnumerable<EmailSegment> ArgToSegments(EmailArg arg) => arg switch
    {
        BoldArg b => [new BoldSegment(b.Value ?? string.Empty)],
        LinkArg l => [new LinkSegment(l.Label ?? string.Empty, l.Url ?? string.Empty)],
        PlainArg p => [new PlainSegment(p.Value ?? string.Empty)],
        _ => [new PlainSegment(string.Empty)]
    };

    public string Flatten()
    {
        var sb = new StringBuilder();
        foreach (var seg in Segments)
        {
            switch (seg)
            {
                case PlainSegment p:
                    sb.Append(p.Text);
                    break;
                case BoldSegment b:
                    sb.Append(b.Text);
                    break;
                case LinkSegment l:
                    sb.Append(l.Label);
                    if (!string.IsNullOrWhiteSpace(l.Url))
                    {
                        sb.Append(" (").Append(l.Url).Append(')');
                    }

                    break;
            }
        }

        return sb.ToString();
    }
}
