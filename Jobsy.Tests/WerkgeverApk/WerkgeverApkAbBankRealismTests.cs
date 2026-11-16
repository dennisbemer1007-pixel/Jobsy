using System.Text.RegularExpressions;
using Jobsy.Core.Scholen.MboHbo;
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

    private static readonly Regex ForbiddenText = new(
        @"\bplots|denk aan|wethouder|burgemeester|rampgebied|presentator|tv-programma|tv-show",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly HashSet<string> RiasecOpposites = new(StringComparer.Ordinal)
    {
        "R|S",
        "I|E",
        "A|C",
    };

    private static readonly HashSet<string> SchwartzOpposites = new(StringComparer.Ordinal)
    {
        "Stability|Autonomy",
        "Achievement|Connection",
        "Impact|Stability",
    };

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
                if (item.Block == PupilMboHboBlock.Competentie)
                {
                    Assert.Equal(item.DimensionA, item.DimensionB);
                }
                else
                {
                    Assert.NotEqual(item.DimensionA, item.DimensionB);
                }
            }
        }
    }

    [Theory]
    [InlineData(WerkgeverApkPackageKind.Basis)]
    [InlineData(WerkgeverApkPackageKind.Uitgebreid)]
    public void All_ab_items_use_valid_opposite_poles(WerkgeverApkPackageKind package)
    {
        var catalog = WerkgeverApkEmployeeAbCatalog.ForPackage(package);
        var abItems = catalog.Items.Where(i => i.Kind == WerkgeverApkAbItemKind.Ab).ToList();
        Assert.NotEmpty(abItems);

        foreach (var item in abItems)
        {
            var where = $"{package} nr {item.Number} ({item.Block})";
            Assert.False(string.IsNullOrWhiteSpace(item.DimensionB), $"Geen tegenpool in {where}");
            Assert.DoesNotMatch(ForbiddenText, item.Situation);
            Assert.DoesNotMatch(ForbiddenText, item.OptionA!);
            Assert.DoesNotMatch(ForbiddenText, item.OptionB!);
            Assert.NotEqual(item.OptionA!.Trim(), item.OptionB!.Trim());

            switch (item.Block)
            {
                case PupilMboHboBlock.Interesse:
                    var pair = $"{item.DimensionA.Trim()[0]}|{item.DimensionB!.Trim()[0]}";
                    var reverse = $"{item.DimensionB.Trim()[0]}|{item.DimensionA.Trim()[0]}";
                    Assert.True(
                        RiasecOpposites.Contains(pair) || RiasecOpposites.Contains(reverse),
                        $"RIASEC niet tegengesteld in {where}: {item.DimensionA} / {item.DimensionB}");
                    break;
                case PupilMboHboBlock.Werkwaarden:
                    var a = SchwartzKey(item.DimensionA);
                    var b = SchwartzKey(item.DimensionB!);
                    Assert.True(
                        SchwartzOpposites.Contains($"{a}|{b}") || SchwartzOpposites.Contains($"{b}|{a}"),
                        $"Werkwaarden niet tegengesteld in {where}: {item.DimensionA} / {item.DimensionB}");
                    break;
                case PupilMboHboBlock.Competentie:
                    Assert.Equal(item.DimensionA, item.DimensionB);
                    break;
                default:
                    Assert.Fail($"Onbekend blok in {where}");
                    break;
            }
        }

        foreach (var check in catalog.Items.Where(i => i.Kind == WerkgeverApkAbItemKind.Check))
        {
            Assert.Equal(PupilMboHboBlock.Competentie, check.Block);
            Assert.True(string.IsNullOrWhiteSpace(check.DimensionB));
        }
    }

    private static string SchwartzKey(string dimension)
    {
        var open = dimension.LastIndexOf('(');
        var close = dimension.LastIndexOf(')');
        Assert.True(open >= 0 && close > open, $"Onbekende werkwaarde: {dimension}");
        return dimension[(open + 1)..close];
    }
}
