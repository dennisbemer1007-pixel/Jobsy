namespace Jobsy.Tests;

public class VacancySourceDisplayTests
{
    [Theory]
    [InlineData("Ats", true)]
    [InlineData("ats", true)]
    [InlineData("ATS", true)]
    [InlineData("Manual", false)]
    [InlineData("Api", false)]
    [InlineData("Csv", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IsAts_detects_scrape_channel(string? createdVia, bool expected)
        => Assert.Equal(expected, Jobsy.Web.VacancySourceDisplay.IsAts(createdVia));

    [Theory]
    [InlineData("Ats", "ATS", "ats")]
    [InlineData("Manual", "Regulier", "regular")]
    [InlineData("Api", "Regulier", "regular")]
    [InlineData("Csv", "Regulier", "regular")]
    public void ChannelLabel_is_binary_ats_or_regular(string createdVia, string label, string css)
    {
        Assert.Equal(label, Jobsy.Web.VacancySourceDisplay.ChannelLabel(createdVia));
        Assert.Equal(css, Jobsy.Web.VacancySourceDisplay.ChannelCssModifier(createdVia));
    }

    [Fact]
    public void Detailed_label_still_distinguishes_manual_api_csv()
    {
        Assert.Equal("Handmatig", Jobsy.Web.VacancySourceDisplay.Label("Manual"));
        Assert.Equal("API", Jobsy.Web.VacancySourceDisplay.Label("Api"));
        Assert.Equal("CSV", Jobsy.Web.VacancySourceDisplay.Label("Csv"));
        Assert.Equal("ATS", Jobsy.Web.VacancySourceDisplay.Label("Ats"));
    }
}
