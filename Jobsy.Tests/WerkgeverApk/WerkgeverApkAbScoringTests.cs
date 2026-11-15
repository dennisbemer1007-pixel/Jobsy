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
    public void Ab_choice_scores_interesse_dimension()
    {
        var catalog = WerkgeverApkEmployeeAbCatalog.ForPackage(WerkgeverApkPackageKind.Basis);
        var item = catalog.Items.First(i => i.Kind == WerkgeverApkAbItemKind.Ab);
        var state = new PupilMboHboProgressState();
        state.Answers[item.Id] = WerkgeverApkAnswerValidation.AbChoiceA;
        var counts = WerkgeverApkAbScoring.CountDimensions(state, catalog);
        Assert.Contains(item.DimensionA, counts.Counts.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.True(counts.Counts[item.DimensionA] >= 1);
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