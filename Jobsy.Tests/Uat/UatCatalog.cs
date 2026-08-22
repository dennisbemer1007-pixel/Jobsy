using System.Globalization;
using System.Text;

namespace Jobsy.Tests.Uat;

/// <summary>
/// Loads the UAT grid from <c>docs/testscenarios-per-rol.csv</c> and assigns stable ids
/// <c>UAT-0001</c> … in file order (one executable script per row).
/// </summary>
public static class UatCatalog
{
    public const string CsvRelativePath = "docs/testscenarios-per-rol.csv";

    public static IReadOnlyList<UatScenario> All { get; } = Load();

    public static UatScenario Get(string id)
        => All.First(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));

    public static IEnumerable<object[]> MemberData()
        => All.Select(s => new object[] { s.Id, s.Role, s.Scenario });

    private static IReadOnlyList<UatScenario> Load()
    {
        var path = ResolveCsvPath();
        var text = File.ReadAllText(path, Encoding.UTF8);
        var rows = Csv.Parse(text);
        if (rows.Count < 2)
        {
            throw new InvalidOperationException($"UAT CSV is empty: {path}");
        }

        var header = rows[0].Select(h => h.Trim()).ToArray();
        var roleIdx = IndexOf(header, "Rol");
        var scenarioIdx = IndexOf(header, "Testscenario");
        var expectedIdx = IndexOf(header, "Verwacht resultaat");
        if (roleIdx < 0 || scenarioIdx < 0 || expectedIdx < 0)
        {
            throw new InvalidOperationException(
                "UAT CSV must have columns Rol, Testscenario, Verwacht resultaat.");
        }

        var list = new List<UatScenario>(rows.Count - 1);
        for (var i = 1; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row.Count == 0 || row.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            list.Add(new UatScenario(
                $"UAT-{list.Count + 1:0000}",
                row[roleIdx].Trim(),
                row[scenarioIdx].Trim(),
                row[expectedIdx].Trim()));
        }

        return list;
    }

    private static int IndexOf(IReadOnlyList<string> header, string name)
    {
        for (var i = 0; i < header.Count; i++)
        {
            if (string.Equals(header[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    public static string ResolveCsvPath()
    {
        var root = RepoRoot.Find();
        var path = Path.Combine(root, CsvRelativePath);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("UAT CSV not found.", path);
        }

        return path;
    }

    private static class Csv
    {
        public static List<List<string>> Parse(string text)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var field = new StringBuilder();
            var inQuotes = false;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        field.Append(c);
                    }

                    continue;
                }

                switch (c)
                {
                    case '"':
                        inQuotes = true;
                        break;
                    case ',':
                        row.Add(field.ToString());
                        field.Clear();
                        break;
                    case '\r':
                        break;
                    case '\n':
                        row.Add(field.ToString());
                        field.Clear();
                        rows.Add(row);
                        row = [];
                        break;
                    default:
                        field.Append(c);
                        break;
                }
            }

            if (inQuotes)
            {
                throw new InvalidOperationException("Unclosed quote in UAT CSV.");
            }

            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString());
                rows.Add(row);
            }

            return rows;
        }
    }
}

internal static class RepoRoot
{
    public static string Find()
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

        var cwd = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (cwd is not null)
        {
            if (File.Exists(Path.Combine(cwd.FullName, "Jobsy.sln")))
            {
                return cwd.FullName;
            }

            cwd = cwd.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate Jobsy.sln for UAT scripts.");
    }
}
