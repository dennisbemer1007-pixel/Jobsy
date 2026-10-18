namespace Jobsy.Core.Rules;

/// <summary>Inspiration chips for the illustrated dream-job step (Sfeer A).</summary>
public sealed record DreamJobInspirationChip(
    string TitleNl,
    string? DreamJobCatalogKey = null,
    string? LabelResourceKey = null);

public static class DreamJobIllustrationChips
{
    public static readonly IReadOnlyList<DreamJobInspirationChip> Default =
    [
        new("Dierenarts", DreamJobCatalogKey: "dierenarts"),
        new("Piloot", DreamJobCatalogKey: "piloot"),
        new("Lasser", LabelResourceKey: "Onboarding.Step6.Chip.Lasser"),
        new("Verpleegkundige", DreamJobCatalogKey: "verpleegkundige"),
        new("ICT-medewerker", DreamJobCatalogKey: "ict-medewerker"),
        new("Kok", DreamJobCatalogKey: "kok"),
        new("Astronaut", LabelResourceKey: "Onboarding.Step6.Chip.AstronautSurprise"),
    ];
}
