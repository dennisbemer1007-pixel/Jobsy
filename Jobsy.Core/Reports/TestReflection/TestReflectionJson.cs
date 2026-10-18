using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jobsy.Core.Reports.TestReflection;

public static class TestReflectionJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public static string Serialize(TestReflectionFull mirror)
        => JsonSerializer.Serialize(mirror, Options);

    public static TestReflectionFull? Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<TestReflectionFull>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static string FlattenForGuard(TestReflectionFull mirror)
    {
        var parts = new List<string>
        {
            mirror.HeroTitle,
            mirror.HeroSubtitle,
            mirror.EnvironmentTitle,
            mirror.EnvironmentBody,
            mirror.DreamCallout ?? ""
        };
        parts.AddRange(mirror.Themes.SelectMany(t => new[] { t.Title, t.Summary }));
        parts.AddRange(mirror.PracticeExamples);
        parts.AddRange(mirror.WatchPoints);
        parts.AddRange(mirror.EnergyGives);
        parts.AddRange(mirror.EnergyDrains);
        return string.Join("\n", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }
}
