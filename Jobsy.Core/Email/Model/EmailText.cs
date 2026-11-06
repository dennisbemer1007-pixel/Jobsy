using System.Text;
using Jobsy.Core.Email;

namespace Jobsy.Core.Email.Model;

public abstract record EmailSegment;

public sealed record PlainSegment(string Text, bool Isolate = false) : EmailSegment;

public sealed record BoldSegment(string Text, bool Isolate = false) : EmailSegment;

public sealed record LinkSegment(string Label, string Url, bool IsolateLabel = false) : EmailSegment;

public abstract record EmailArg
{
    /// <summary>User-data values default to bidi isolation.</summary>
    public static EmailArg Plain(string value, bool isolate = true) => new PlainArg(value, isolate);

    public static EmailArg Bold(string value, bool isolate = true) => new BoldArg(value, isolate);

    public static EmailArg Link(string label, string url, bool isolateLabel = false)
        => new LinkArg(label, url, isolateLabel);
}

public sealed record PlainArg(string Value, bool Isolate = true) : EmailArg;

public sealed record BoldArg(string Value, bool Isolate = true) : EmailArg;

public sealed record LinkArg(string Label, string Url, bool IsolateLabel = false) : EmailArg;

/// <summary>Rich text without HTML. Templates never write markup.</summary>
public sealed class EmailText
{
    public IReadOnlyList<EmailSegment> Segments { get; }

    private EmailText(IReadOnlyList<EmailSegment> segments)
        => Segments = segments;

    public static EmailText Empty { get; } = new([]);

    public static EmailText Plain(string text, bool isolate = false)
        => new([new PlainSegment(text ?? string.Empty, isolate)]);

    public static EmailText Bold(string text, bool isolate = false)
        => new([new BoldSegment(text ?? string.Empty, isolate)]);

    public static EmailText Link(string label, string url, bool isolateLabel = false)
        => new([new LinkSegment(label ?? string.Empty, url ?? string.Empty, isolateLabel)]);

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
        BoldArg b => [new BoldSegment(b.Value ?? string.Empty, b.Isolate)],
        LinkArg l => [new LinkSegment(l.Label ?? string.Empty, l.Url ?? string.Empty, l.IsolateLabel)],
        PlainArg p => [new PlainSegment(p.Value ?? string.Empty, p.Isolate)],
        _ => [new PlainSegment(string.Empty)]
    };

    public string Flatten(bool forRtlText = false)
    {
        var sb = new StringBuilder();
        foreach (var seg in Segments)
        {
            switch (seg)
            {
                case PlainSegment p:
                    sb.Append(forRtlText && p.Isolate ? EmailBidiWrap(p.Text) : p.Text);
                    break;
                case BoldSegment b:
                    sb.Append(forRtlText && b.Isolate ? EmailBidiWrap(b.Text) : b.Text);
                    break;
                case LinkSegment l:
                    var label = forRtlText && l.IsolateLabel ? EmailBidiWrap(l.Label) : l.Label;
                    sb.Append(label);
                    if (!string.IsNullOrWhiteSpace(l.Url))
                    {
                        sb.Append(" (").Append(l.Url).Append(')');
                    }

                    break;
            }
        }

        return sb.ToString();
    }

    public string Flatten() => Flatten(forRtlText: false);

    private static string EmailBidiWrap(string text)
        => EmailBidi.FirstStrongIsolate + text + EmailBidi.PopDirectionalIsolate;
}
