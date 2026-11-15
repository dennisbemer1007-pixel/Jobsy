using System.Text.RegularExpressions;
using Jobsy.Core.WerkgeverApk;

namespace Jobsy.Tests.WerkgeverApk;

/// <summary>
/// CONCEPT-banken: unieke situaties, geen generieke templates, dimensies passen bij A/B.
/// </summary>
public sealed class WerkgeverApkAbBankRealismTests
{
    private static readonly Regex WordCount = new(@"\S+", RegexOptions.CultureInvariant);
    private static readonly Regex GenericSituation = new(
        @"^(Denk aan |Plots komt|Even later blijkt)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    [Theory]
    [InlineData(WerkgeverApkPackageKind.Basis)]
    [InlineData(WerkgeverApkPackageKind.Uitgebreid)]
    public void All_situations_are_unique_and_concrete(WerkgeverApkPackageKind package)
    {
        var catalog = WerkgeverApkEmployeeAbCatalog.ForPackage(package);
        var situations = catalog.Items.Select(i => i.Situation.Trim()).ToList();
        Assert.Equal(situations.Count, situations.Distinct(StringComparer.Ordinal).Count());

        foreach (var item in catalog.Items)
        {
            Assert.False(
                GenericSituation.IsMatch(item.Situation.Trim()),
                $"Generieke situatie in {package} nr {item.Number}: {item.Situation}");
            Assert.True(
                item.Situation.TrimEnd().EndsWith('.') || item.Situation.TrimEnd().EndsWith('!') || item.Situation.TrimEnd().EndsWith('?'),
                $"Situatie eindigt niet op leesteken: {item.Situation}");
            Assert.True(WordCount.Matches(item.Situation).Count <= 15, $"Situatie te lang: {item.Situation}");
            if (item.Kind == WerkgeverApkAbItemKind.Ab)
            {
                Assert.NotNull(item.OptionA);
                Assert.NotNull(item.OptionB);
                Assert.True(WordCount.Matches(item.OptionA).Count <= 15, $"optie_a te lang: {item.OptionA}");
                Assert.True(WordCount.Matches(item.OptionB).Count <= 15, $"optie_b te lang: {item.OptionB}");
                Assert.NotEqual(item.DimensionA, item.DimensionB);
            }
        }
    }
}