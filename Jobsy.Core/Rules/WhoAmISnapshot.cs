using System.Text.Json;
using Jobsy.Core.Contracts;

namespace Jobsy.Core.Rules;

public static class WhoAmISnapshot
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static string? Serialize(LobsyCvWhoAmI? who)
    {
        if (who is null || string.IsNullOrWhiteSpace(who.Story))
        {
            return null;
        }

        return JsonSerializer.Serialize(who, Json);
    }

    public static LobsyCvWhoAmI? TryParse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<LobsyCvWhoAmI>(json, Json);
            if (parsed is null || string.IsNullOrWhiteSpace(parsed.Story))
            {
                return null;
            }

            var story = WhoAmIStoryBuilder.Sanitize(parsed.Story);
            if (story is null)
            {
                return null;
            }

            var keywords = (parsed.Keywords ?? [])
                .Where(k => !string.IsNullOrWhiteSpace(k) && !CareerCompassBuilder.ContainsForbiddenJargon(k))
                .Select(k => k.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(WhoAmIKeywords.MaxCount)
                .ToList();

            return parsed with { Story = story, Keywords = keywords };
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
