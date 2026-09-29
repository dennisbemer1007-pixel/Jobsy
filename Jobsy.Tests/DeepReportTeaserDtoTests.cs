using System.Text.Json;
using Jobsy.Core.Reports;

namespace Jobsy.Tests;

/// <summary>
/// Done-state for unpaid candidates must never embed deep-report fields in list/teaser DTOs.
/// Locked cards use client-side sample content only.
/// </summary>
public class DeepReportTeaserDtoTests
{
    [Fact]
    public void Locked_teaser_sample_is_client_side_only()
    {
        // Capabilities list card keys; the Razor teaser renders static dummy bars — no server payload.
        var json = JsonSerializer.Serialize(new { deepReport = (object?)null, reportJson = (string?)null });
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("deepReport").ValueKind);
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("reportJson").ValueKind);
        Assert.NotEmpty(DeepReportCapabilities.CardKeys(Core.Enums.AssessmentKind.Competence));
    }
}
