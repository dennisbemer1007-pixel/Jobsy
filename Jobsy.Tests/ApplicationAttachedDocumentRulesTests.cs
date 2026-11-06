using Jobsy.Core.Passport;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class ApplicationAttachedDocumentRulesTests
{
    [Theory]
    [InlineData(ApplicationAttachedDocumentRules.Passport, true, true)]
    [InlineData(ApplicationAttachedDocumentRules.LobsyCv, true, false)]
    [InlineData(ApplicationAttachedDocumentRules.LobsyCv, false, false)]
    [InlineData(null, true, true)]
    [InlineData(null, false, false)]
    public void UsePassportPdf_respects_snapshot_and_flag(string? kind, bool v2, bool expected)
        => Assert.Equal(expected, ApplicationAttachedDocumentRules.UsePassportPdf(kind, v2));

    [Fact]
    public void KindForNewApplication_requires_all_tests_and_v2()
    {
        Assert.Equal(
            ApplicationAttachedDocumentRules.Passport,
            ApplicationAttachedDocumentRules.KindForNewApplication(allTestsComplete: true, passportPdfV2Enabled: true));
        Assert.Equal(
            ApplicationAttachedDocumentRules.LobsyCv,
            ApplicationAttachedDocumentRules.KindForNewApplication(allTestsComplete: false, passportPdfV2Enabled: true));
        Assert.Equal(
            ApplicationAttachedDocumentRules.LobsyCv,
            ApplicationAttachedDocumentRules.KindForNewApplication(allTestsComplete: true, passportPdfV2Enabled: false));
    }

    [Fact]
    public void AllLayersCompleted_requires_four_done_layers()
    {
        var incomplete = PassportDnaLayer.None();
        Assert.False(PassportDnaCompletion.AllLayersCompleted(incomplete));

        var done = PassportDnaLayer.Keys
            .Select(key => new PassportDnaLayerFact(key, true, DateTime.UtcNow, ["x"]))
            .ToArray();
        Assert.True(PassportDnaCompletion.AllLayersCompleted(done));
    }
}
