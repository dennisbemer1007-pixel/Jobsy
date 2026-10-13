using System.Text.RegularExpressions;

namespace Jobsy.Tests;

/// <summary>
/// Razor passes <c>Error="_windowError"</c> as the literal text when the parameter is a string.
/// The field must be <c>Error="@_windowError"</c>.
/// </summary>
public class RazorLiteralParameterTests
{
    private static readonly Regex StringParameter = new(
        @"\[Parameter(?:\s*,[^\]]*)?\]\s*public\s+string\??\s+(\w+)",
        RegexOptions.Compiled);

    private static readonly Regex LiteralAttribute = new(
        @"\s([A-Za-z][A-Za-z0-9]*)=""(_[A-Za-z][A-Za-z0-9]*)""",
        RegexOptions.Compiled);

    private static readonly Regex OpenTag = new(
        @"^<([A-Z][A-Za-z0-9]*)\b",
        RegexOptions.Compiled);

    [Fact]
    public void String_parameters_do_not_receive_a_literal_underscore_field()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var web = Path.Combine(root, "Jobsy.Web");
        var stringParams = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var file in SourceFiles(web))
        {
            var component = Path.GetFileNameWithoutExtension(file);
            if (component.EndsWith(".razor", StringComparison.Ordinal))
            {
                component = Path.GetFileNameWithoutExtension(component);
            }

            var names = stringParams.GetValueOrDefault(component) ?? new HashSet<string>(StringComparer.Ordinal);
            foreach (Match match in StringParameter.Matches(File.ReadAllText(file)))
            {
                names.Add(match.Groups[1].Value);
            }

            if (names.Count > 0)
            {
                stringParams[component] = names;
            }
        }

        Assert.NotEmpty(stringParams);
        var hits = new List<string>();
        foreach (var file in Directory.EnumerateFiles(web, "*.razor", SearchOption.AllDirectories))
        {
            if (IsGenerated(file))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            foreach (Match match in LiteralAttribute.Matches(text))
            {
                var attr = match.Groups[1].Value;
                var tag = TagOwning(text, match.Index);
                if (tag is null || !stringParams.TryGetValue(tag, out var names) || !names.Contains(attr))
                {
                    continue;
                }

                var line = text[..match.Index].Count(c => c == '\n') + 1;
                hits.Add($"{Path.GetRelativePath(root, file)}:{line} {tag} {attr}=\"{match.Groups[2].Value}\"");
            }
        }

        Assert.True(hits.Count == 0, "Razor string parameters got a literal _field:\n" + string.Join("\n", hits));
    }

    private static string? TagOwning(string text, int attributeIndex)
    {
        var start = text.LastIndexOf('<', attributeIndex);
        if (start < 0)
        {
            return null;
        }

        var between = text[start..attributeIndex];
        if (between.Contains('>', StringComparison.Ordinal) || between.StartsWith("</", StringComparison.Ordinal))
        {
            return null;
        }

        var tag = OpenTag.Match(between);
        return tag.Success ? tag.Groups[1].Value : null;
    }

    private static IEnumerable<string> SourceFiles(string web)
    {
        foreach (var file in Directory.EnumerateFiles(web, "*.*", SearchOption.AllDirectories))
        {
            if (IsGenerated(file))
            {
                continue;
            }

            if (file.EndsWith(".razor", StringComparison.Ordinal)
                || file.EndsWith(".cs", StringComparison.Ordinal))
            {
                yield return file;
            }
        }
    }

    private static bool IsGenerated(string file)
        => file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
           || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
}
