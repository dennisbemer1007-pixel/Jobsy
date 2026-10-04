using Bunit;
using Jobsy.Core.Legal;

namespace Jobsy.Tests;

public class DataLocationSentenceTests : PrivacyRenderTestBase
{
    [Theory]
    [InlineData("nl", "Waar staan je gegevens?", "Amerikaanse bedrijven", "Al je gegevens blijven in de EU")]
    [InlineData("en", "Where is your data?", "American companies", "All your data stays in the EU")]
    [InlineData("pl", "Gdzie są twoje dane?", "amerykańskich firm", "Wszystkie twoje dane zostają w UE")]
    [InlineData("ro", "Unde sunt datele tale?", "companii americane", "Toate datele tale rămân în UE")]
    [InlineData("ar", "أين توجد بياناتك؟", "شركات أمريكية", "كل بياناتك تبقى في الاتحاد الأوروبي")]
    public void Default_mail_names_american_companies_in_every_language(
        string language,
        string title,
        string american,
        string allEu)
    {
        UseLanguage(language);
        var sentence = RenderPrivacy().Find("#buiten-de-eu");

        Assert.Equal(language, sentence.GetAttribute("lang"));
        var text = sentence.TextContent;
        Assert.Contains(title, text, StringComparison.Ordinal);
        Assert.Contains(american, text, StringComparison.Ordinal);
        Assert.Contains("Resend", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Lettermint", text, StringComparison.Ordinal);
        Assert.DoesNotContain(allEu, text, StringComparison.Ordinal);
    }

    [Fact]
    public void Lettermint_replaces_resend_in_the_table_and_in_the_sentence()
    {
        Config["Mail:Provider"] = "Lettermint";
        Config["Lettermint:ApiKey"] = "lm_test_key";

        var page = RenderPrivacy();
        var table = page.Find("#delen .pp-table__grid").TextContent;
        var sentence = page.Find("#buiten-de-eu").TextContent;

        Assert.Contains("Lettermint", table, StringComparison.Ordinal);
        Assert.DoesNotContain("Resend", table, StringComparison.Ordinal);
        Assert.DoesNotContain("Lettermint", sentence, StringComparison.Ordinal);
        Assert.DoesNotContain("Resend", sentence, StringComparison.Ordinal);
        Assert.Contains("Amerikaanse bedrijven", sentence, StringComparison.Ordinal);
        Assert.Contains("Render", sentence, StringComparison.Ordinal);
    }

    [Fact]
    public void Lettermint_without_a_key_keeps_resend_in_the_list()
    {
        Config["Mail:Provider"] = "Lettermint";

        var table = RenderPrivacy().Find("#delen .pp-table__grid").TextContent;

        Assert.Contains("Resend", table, StringComparison.Ordinal);
        Assert.DoesNotContain("Lettermint", table, StringComparison.Ordinal);
    }

    [Fact]
    public void A_list_without_american_companies_says_data_stays_in_the_eu()
    {
        var rows = new[]
        {
            LegalProcessors.ById("mollie"),
            LegalProcessors.ById("kvk"),
            LegalProcessors.ById("lettermint")
        };

        var text = Render<Jobsy.Web.Components.Legal.DataLocationSentence>(parameters =>
            parameters.Add(component => component.Rows, rows)).Find("#buiten-de-eu").TextContent;

        Assert.Contains("Al je gegevens blijven in de EU, bij Europese bedrijven.", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Amerikaanse bedrijven", text, StringComparison.Ordinal);
    }

    [Fact]
    public void A_swiss_company_is_named_even_when_no_company_is_american()
    {
        var rows = new[] { LegalProcessors.ById("mollie"), LegalProcessors.ById("pingen") };

        var text = Render<Jobsy.Web.Components.Legal.DataLocationSentence>(parameters =>
            parameters.Add(component => component.Rows, rows)).Find("#buiten-de-eu").TextContent;

        Assert.Contains("Al je gegevens blijven in de EU, bij Europese bedrijven.", text, StringComparison.Ordinal);
        Assert.Contains("Pingen zit in Zwitserland.", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Mistral_on_the_eu_host_says_processing_stays_in_the_eu()
    {
        var mistral = LegalProcessorSelection.ApplyMistralHost(
            LegalProcessors.ById("mistral"),
            mistralDataStaysInTheEu: true);
        var rows = new[] { LegalProcessors.ById("mollie"), mistral };

        var text = Render<Jobsy.Web.Components.Legal.DataLocationSentence>(parameters =>
            parameters.Add(component => component.Rows, rows)).Find("#buiten-de-eu").TextContent;

        Assert.Contains("Verwerking bij Mistral AI gebeurt in de EU.", text, StringComparison.Ordinal);
        Assert.Contains("Account en facturen van Mistral kunnen buiten de EU staan.", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Al je gegevens blijven in de EU", text, StringComparison.Ordinal);
        Assert.DoesNotContain("belooft geen plek", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Mistral_on_another_host_promises_no_place()
    {
        var rows = new[] { LegalProcessors.ById("mollie"), LegalProcessors.ById("mistral") };

        var text = Render<Jobsy.Web.Components.Legal.DataLocationSentence>(parameters =>
            parameters.Add(component => component.Rows, rows)).Find("#buiten-de-eu").TextContent;

        Assert.Contains("Mistral belooft geen plek voor de verwerking.", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Al je gegevens blijven in de EU", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Amerikaanse bedrijven", text, StringComparison.Ordinal);
    }

    [Fact]
    public void OpenAI_is_named_with_the_american_companies()
    {
        var rows = new[] { LegalProcessors.ById("openai"), LegalProcessors.ById("mollie") };

        var text = Render<Jobsy.Web.Components.Legal.DataLocationSentence>(parameters =>
            parameters.Add(component => component.Rows, rows)).Find("#buiten-de-eu").TextContent;

        Assert.Contains("OpenAI", text, StringComparison.Ordinal);
        Assert.Contains("Amerikaanse bedrijven", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Al je gegevens blijven in de EU", text, StringComparison.Ordinal);
    }

    [Fact]
    public void The_privacy_markup_no_longer_hardcodes_the_data_location_sentence()
    {
        Assert.DoesNotContain("Een paar diensten zijn van Amerikaanse bedrijven", PrivacyMarkupSource, StringComparison.Ordinal);
        Assert.Contains("DataLocationSentence", PrivacyMarkupSource, StringComparison.Ordinal);
    }
}
