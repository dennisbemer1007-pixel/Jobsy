using System.Reflection;
using System.Text.RegularExpressions;

namespace Jobsy.Core.Scholen;

/// <summary>Rejects free-text chip words that look like common Dutch first names (D13).</summary>
public static class PupilNameGuard
{
    private static readonly Lazy<HashSet<string>> Names = new(LoadNames);

    public static bool LooksLikeName(string? word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return false;
        }

        var normalized = Normalize(word);
        if (normalized.Length == 0)
        {
            return false;
        }

        // Single token only for name check; two-word phrases check each token.
        foreach (var part in normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Names.Value.Contains(part))
            {
                return true;
            }
        }

        return false;
    }

    public static int NamesLoadedCount() => Names.Value.Count;

    public static string Normalize(string word)
        => Regex.Replace(word.Trim().ToLowerInvariant(), @"\s+", " ");

    private static HashSet<string> LoadNames()
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        var asm = typeof(PupilNameGuard).Assembly;
        var resource = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("dutch-first-names.txt", StringComparison.OrdinalIgnoreCase));
        if (resource is null)
        {
            // Fallback: read from known relative path when not embedded (unit test host).
            var path = Path.Combine(AppContext.BaseDirectory, "dutch-first-names.txt");
            if (!File.Exists(path))
            {
                path = FindRepoFile("Jobsy.Core/Scholen/Resources/dutch-first-names.txt");
            }

            if (File.Exists(path))
            {
                foreach (var line in File.ReadAllLines(path))
                {
                    AddLine(set, line);
                }
            }

            return set;
        }

        using var stream = asm.GetManifestResourceStream(resource)!;
        using var reader = new StreamReader(stream);
        string? lineRead;
        while ((lineRead = reader.ReadLine()) is not null)
        {
            AddLine(set, lineRead);
        }

        return set;
    }

    private static void AddLine(HashSet<string> set, string line)
    {
        var t = line.Trim();
        if (t.Length == 0 || t.StartsWith('#'))
        {
            return;
        }

        set.Add(Normalize(t));
    }

    private static string FindRepoFile(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relative);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return relative;
    }
}
