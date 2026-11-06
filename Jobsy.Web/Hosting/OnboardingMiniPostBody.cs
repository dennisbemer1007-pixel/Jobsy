using System.Text.Json;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Onboarding;

namespace Jobsy.Web.Hosting;

internal static class OnboardingMiniPostBody
{
    public static JsonElement BuildMiniBlockBody(
        HttpContext http,
        int step,
        string? consentRequested,
        string? obMiniPick,
        string? obPayload)
    {
        var answers = ReadMiniAnswersFromForm(http.Request.Form);
        if (!string.IsNullOrWhiteSpace(obMiniPick))
        {
            var parts = obMiniPick.Split(':', 2);
            if (parts.Length == 2
                && int.TryParse(parts[0], out var qid)
                && int.TryParse(parts[1], out var score)
                && score is >= 1 and <= 5)
            {
                answers[qid] = score;
            }
        }

        if (answers.Count == 0 && !string.IsNullOrWhiteSpace(obPayload))
        {
            try
            {
                using var doc = JsonDocument.Parse(obPayload);
                foreach (var pair in OnboardingClientStepBridge.ParseMiniAnswers(doc.RootElement.GetRawText()))
                {
                    answers[pair.Key] = pair.Value;
                }

                if (doc.RootElement.TryGetProperty("consentRequested", out var consentProp)
                    && (consentProp.ValueKind == JsonValueKind.True || consentProp.ValueKind == JsonValueKind.False))
                {
                    consentRequested = consentProp.GetBoolean() ? "true" : "false";
                }
            }
            catch (JsonException)
            {
                // fall through
            }
        }

        var consent = step == 7
                      && (string.Equals(consentRequested, "true", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(consentRequested, "on", StringComparison.OrdinalIgnoreCase));

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            if (step == 7)
            {
                writer.WriteBoolean("consentRequested", consent);
            }

            writer.WriteStartObject("answers");
            foreach (var (id, value) in answers.OrderBy(static p => p.Key))
            {
                writer.WriteNumber(id.ToString(), value);
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        stream.Position = 0;
        using var outDoc = JsonDocument.Parse(stream);
        return outDoc.RootElement.Clone();
    }

    public static JsonElement MergeNavFlags(JsonElement body, bool advance, bool goBack, bool skipOptional)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var prop in body.EnumerateObject())
            {
                writer.WritePropertyName(prop.Name);
                prop.Value.WriteTo(writer);
            }

            writer.WriteBoolean("advance", advance);
            writer.WriteBoolean("back", goBack);
            writer.WriteBoolean("skipOptional", skipOptional);
            writer.WriteEndObject();
        }

        stream.Position = 0;
        using var doc = JsonDocument.Parse(stream);
        return doc.RootElement.Clone();
    }

    private static Dictionary<int, int> ReadMiniAnswersFromForm(IFormCollection form)
    {
        var answers = new Dictionary<int, int>();
        foreach (var key in form.Keys)
        {
            if (string.IsNullOrEmpty(key)
                || !key.StartsWith("miniAnswers[", StringComparison.Ordinal)
                || !key.EndsWith(']'))
            {
                continue;
            }

            var inner = key["miniAnswers[".Length..^1];
            if (!int.TryParse(inner, out var id))
            {
                continue;
            }

            if (int.TryParse(form[key], out var score) && score is >= 1 and <= 5)
            {
                answers[id] = score;
            }
        }

        return answers;
    }
}
