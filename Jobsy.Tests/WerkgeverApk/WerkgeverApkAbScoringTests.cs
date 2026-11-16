using Jobsy.Core.Scholen.MboHbo;
using Jobsy.Core.WerkgeverApk;

namespace Jobsy.Tests.WerkgeverApk;

public sealed class WerkgeverApkAbScoringTests
{
    [Theory]
    [InlineData(WerkgeverApkPackageKind.Basis, 4, 3)]
    [InlineData(WerkgeverApkPackageKind.Uitgebreid, 12, 10)]
    public void Coverage_meets_minimum_per_dimension(WerkgeverApkPackageKind package, int minItems, int minValueItems)
    {
        var catalog = WerkgeverApkEmployeeAbCatalog.ForPackage(package);
        var issues = WerkgeverApkAbScoring.ValidateMinimumCoverage(catalog, minItems)
            .Where(i => !i.StartsWith("Werkwaarde", StringComparison.Ordinal))
            .Concat(WerkgeverApkAbScoring.ValidateMinimumCoverage(catalog, minValueItems)
                .Where(i => i.StartsWith("Werkwaarde", StringComparison.Ordinal)))
            .ToList();
        Assert.True(
            issues.Count == 0,
            $"Dekking onvolledig voor {package}: {string.Join("; ", issues.Take(8))}");
    }

    [Fact]
    public void Ab_zeker_a_scores_opposite_pole_a()
    {
        var catalog = WerkgeverApkEmployeeAbCatalog.ForPackage(WerkgeverApkPackageKind.Basis);
        var item = catalog.Items.First(i =>
            i.Kind == WerkgeverApkAbItemKind.Ab
            && i.Block == PupilMboHboBlock.Interesse);
        var state = new PupilMboHboProgressState();
        state.Answers[item.Id] = WerkgeverApkAnswerValidation.AbChoiceA;
        var counts = WerkgeverApkAbScoring.CountDimensions(state, catalog);
        Assert.Equal(WerkgeverApkAbContrastScale.FullWeight, counts.Counts[item.DimensionA]);
        if (item.DimensionB is not null)
        {
            Assert.False(counts.Counts.ContainsKey(item.DimensionB));
        }
    }

    [Theory]
    [InlineData(1, 2, 0)]
    [InlineData(2, 1, 0)]
    [InlineData(3, 0, 0)]
    [InlineData(4, 0, 1)]
    [InlineData(5, 0, 2)]
    public void Ab_bipolar_weights_zeker_half_neutral(int answer, int weightA, int weightB)
    {
        var item = new WerkgeverApkAbItem(
            "t1",
            1,
            PupilMboHboBlock.Interesse,
            1,
            WerkgeverApkAbItemKind.Ab,
            "scene",
            "kop",
            "A text",
            "R (Praktisch)",
            "B text",
            "S (Sociaal)");
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        WerkgeverApkAbScoring.ApplyAbContrastAnswer(counts, item, answer);
        Assert.Equal(weightA, counts.GetValueOrDefault("R (Praktisch)"));
        Assert.Equal(weightB, counts.GetValueOrDefault("S (Sociaal)"));
    }

    [Theory]
    [InlineData(1, 5)]
    [InlineData(2, 4)]
    [InlineData(3, 3)]
    [InlineData(5, 1)]
    public void Bipolar_competence_items_score_one_dimension_from_pole(int answer, int expected)
    {
        var catalog = WerkgeverApkEmployeeAbCatalog.ForPackage(WerkgeverApkPackageKind.Basis);
        var item = catalog.Items.First(i => i.Block == PupilMboHboBlock.Competentie && i.Kind == WerkgeverApkAbItemKind.Ab);
        Assert.Equal(item.DimensionA, item.DimensionB);
        var state = new PupilMboHboProgressState();
        state.Answers[item.Id] = answer;
        Assert.Equal(expected, WerkgeverApkAbScoring.BipolarScore(state, catalog, item.DimensionA));
    }

    [Fact]
    public void Ab_rejects_binary_only_values_for_validation()
    {
        var catalog = WerkgeverApkEmployeeAbCatalog.ForPackage(WerkgeverApkPackageKind.Basis);
        var item = catalog.Items.First(i => i.Kind == WerkgeverApkAbItemKind.Ab);
        Assert.True(WerkgeverApkAnswerValidation.IsValidForItem(item, 3));
        Assert.False(WerkgeverApkAnswerValidation.IsValidForItem(item, 0));
        Assert.False(WerkgeverApkAnswerValidation.IsValidForItem(item, 6));
    }

    [Fact]
    public void Package_pricing_fallback_is_op_aanvraag_when_empty()
    {
        Assert.Equal(WerkgeverApkPackagePricing.OnRequestLabel, WerkgeverApkPackagePricing.FormatPrice(null, "per medewerker"));
        Assert.Equal("€ 12 per medewerker", WerkgeverApkPackagePricing.FormatPrice("€ 12", "per medewerker"));
    }

    [Theory]
    [InlineData(WerkgeverApkPackageKind.Basis, 40)]
    [InlineData(WerkgeverApkPackageKind.Uitgebreid, 200)]
    public void Question_bank_sizes(WerkgeverApkPackageKind package, int expected)
    {
        var catalog = WerkgeverApkEmployeeAbCatalog.ForPackage(package);
        Assert.Equal(expected, catalog.QuestionCount);
    }
}