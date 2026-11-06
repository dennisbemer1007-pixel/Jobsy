using System.Text.Json;
using Jobsy.Web.Models;

namespace Jobsy.Web.Services;

/// <summary>
/// Reads a deep-analysis state without keeping the question bank on the circuit.
/// Older APIs still send 150–200 prompts after unlock. The player uses
/// <c>DeepAnalysisScreenOrder</c> and must not wait on that array.
/// </summary>
public static class DeepAnalysisWireReader
{
    public static async Task<DeepAnalysisState?> ReadAsync(Stream utf8, CancellationToken ct)
    {
        using var document = await JsonDocument.ParseAsync(utf8, cancellationToken: ct);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.NameEquals("questions") || property.NameEquals("Questions"))
                {
                    continue;
                }

                property.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        buffer.Position = 0;
        return await JsonSerializer.DeserializeAsync<DeepAnalysisState>(buffer, JobsyApiClient.ApiJson, ct);
    }
}
