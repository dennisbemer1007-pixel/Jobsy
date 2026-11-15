using Jobsy.Core.Scholen.MboHbo;
using Jobsy.Core.WerkgeverApk;

namespace Jobsy.Tests.WerkgeverApk;

public sealed class WerkgeverApkAbScoringTests
{
    [Theory]
    [InlineData(WerkgeverApkPackageKind.Basis, 4)]
    [InlineData(WerkgeverApkPackageKind.Uitgebreid, 12)]
    public void Coverage_meets_minimum_per_dimension(WerkgeverApkPackageKind package, int minItems)
    {
        var catalog = WerkgeverApkEmployeeAbCatalog.ForPackage(package);
        var issues = WerkgeverApkAbScoring.ValidateMinimumCoverage(catalog, minItems);
        Assert.True(
            issues.Count == 0,
            $"Dekking onvolledig voor {package}: {string.Join("; ", issues.Take(8))}");
    }

    [Fact]
    public void Ab_full_a_scores_dimension_a_with_g78_weight()
    {
        var catalog = WerkgeverApkEmployeeAbCatalog.ForPackage(WerkgeverApkPackageKind.Basis);
        var item = catalog.Items.First(i => i.Kind == WerkgeverApkAbItemKind.Ab);
        var state = new PupilMboHboProgressState();
        state.Answers[item.Id] = WerkgeverApkAnswerValidation.AbChoiceA;
        var counts = WerkgeverApkAbScoring.CountDimensions(state, catalog);
        Assert.Equal(5, counts.Counts[item.DimensionA]);
        if (item.DimensionB is not null)
        {
            Assert.False(counts.Counts.ContainsKey(item.DimensionB));
        }
    }

    [Theory]
    [InlineData(1, 5, 0)]
    [InlineData(2, 4, 0)]
    [InlineData(5, 0, 5)]
    [InlineData(3, 3, 3)]
    public void Ab_contrast_scale_weights_match_g78(int answer, int weightA, int weightB)
    {
        var item = new WerkgeverApkAbItem(
            "t1",
            1,
            Jobsy.Core.Scholen.MboHbo.PupilMboHboBlock.Competentie,
            2,
            WerkgeverApkAbItemKind.Ab,
            "scene",
            "kop",
            "A text",
            "Dim A",
            "B text",
            "Dim B");
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        WerkgeverApkAbScoring.ApplyAbContrastAnswer(counts, item, answer);
        Assert.Equal(weightA, counts.GetValueOrDefault("Dim A"));
        Assert.Equal(weightB, counts.GetValueOrDefault("Dim B"));
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