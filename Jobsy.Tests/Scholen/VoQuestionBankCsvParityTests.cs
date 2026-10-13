using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Localization;

namespace Jobsy.Tests.Scholen;

public class VoQuestionBankCsvParityTests
{
    [Fact]
    public void Csv_rows_match_bank_structure_and_strings_byte_for_byte()
    {
        var root = FindRepoRoot();
        var path = Path.Combine(root, "docs", "scholen", "vo-vragenset-100.csv");
        Assert.True(File.Exists(path), path);
        var rows = ParseCsv(File.ReadAllText(path));
        Assert.Equal(100, rows.Count);

        var bank = new PupilQuestionBankVo();
        var strings = new Dictionary<string, string>(StringComparer.Ordinal);
        UiStringsLeerlingVragenVo.MergeNl(strings);

        for (var i = 0; i < rows.Count; i++)
        {
            var id = 9100 + i + 1;
            var q = bank.Questions[i];
            Assert.Equal(id, q.Id);
            Assert.Equal(ParseWorld(rows[i][0]), q.World);
            Assert.Equal(rows[i][1], q.Category, StringComparer.Ordinal);
            Assert.Equal(bool.Parse(rows[i][2]), q.Reverse);

            var text = strings[$"LeerlingQ.{id}"];
            var example = strings[$"LeerlingQ.{id}.Voorbeeld"];
            Assert.Equal(rows[i][3].Trim(), text);
            Assert.Equal(rows[i][4].Trim(), example);
        }

        _ = new PupilQuestionSetRegistry();
    }

    private static PupilWorld ParseWorld(string value) => value switch
    {
        "Koraalrif" => PupilWorld.Koraalrif,
        "Schatgrot" => PupilWorld.Schatgrot,
        "Vuurtoren" => PupilWorld.Vuurtoren,
        "Lagune" => PupilWorld.Lagune,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown world")
    };

    private static List<string[]> ParseCsv(string raw)
    {
        var lines = raw.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var rows = new List<string[]>();
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = ParseLine(line);
            if (fields.Count > 0 && string.Equals(fields[0], "world", StringComparison.Ordinal))
            {
                continue;
            }

            rows.Add([.. fields]);
        }

        return rows;
    }

    private static List<string> ParseLine(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == ',')
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields;
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

        throw new InvalidOperationException("Jobsy.sln not found");
    }
}
