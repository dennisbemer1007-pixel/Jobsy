using Jobsy.Core.WerkgeverApk;

namespace Jobsy.Tests.WerkgeverApk;

public sealed class WerkgeverApkEmployeeCatalogTests
{
    [Fact]
    public void Basis_bank_is_work_focused_concept_copy()
    {
        var item = WerkgeverApkEmployeeAbCatalog.ForPackage(WerkgeverApkPackageKind.Basis).Items[0];
        Assert.Contains("Wat past", item.Heading, StringComparison.Ordinal);
        Assert.DoesNotContain("school", item.Situation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Ab_banks_use_concept_scoring_version_and_sizes()
    {
        Assert.Contains("concept", WerkgeverApkEmployeeAbCatalog.ScoringVersion, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(40, WerkgeverApkEmployeeAbCatalog.ForPackage(WerkgeverApkPackageKind.Basis).QuestionCount);
        Assert.Equal(200, WerkgeverApkEmployeeAbCatalog.ForPackage(WerkgeverApkPackageKind.Uitgebreid).QuestionCount);
    }
}