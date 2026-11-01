using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Scholen;

namespace Jobsy.Tests.Scholen;

internal static class PupilGroep78TestHelpers
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task EnsureJourneyThemeAsync(
        HttpClient client,
        string themeKey = PupilJourneyTheme.ZeeExpeditie)
    {
        var response = await client.PutAsJsonAsync(
            "api/pupil/progress/journey-theme",
            new PupilJourneyThemeRequest(themeKey));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public static async Task CompleteVakkenIfNeededAsync(HttpClient client)
    {
        var progress = await client.GetFromJsonAsync<PupilProgressStateDto>("api/pupil/progress", Json);
        if (progress is null || progress.VakkenDone)
        {
            return;
        }

        if (progress.AnsweredCount < progress.TotalItems)
        {
            return;
        }

        var inputs = PupilVakScores.AllSubjectKeys
            .Select(k => new PupilVakScoreInput(k, 4, 4))
            .ToList();
        var response = await client.PutAsJsonAsync(
            "api/pupil/progress/vakscores",
            new PupilVakScoresRequest(inputs));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
