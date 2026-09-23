using Jobsy.Core.Enums;
using Jobsy.Web.Admin;

namespace Jobsy.Tests;

public class IntegrationHelpDocsTests
{
    [Theory]
    [InlineData(nameof(IntegrationKey.Mollie))]
    [InlineData(nameof(IntegrationKey.Kvk))]
    [InlineData(nameof(IntegrationKey.MicrosoftEntra))]
    [InlineData(nameof(IntegrationKey.GoogleEntra))]
    [InlineData(nameof(IntegrationKey.Mail))]
    [InlineData(nameof(IntegrationKey.OpenAI))]
    public void Every_integration_key_has_help_docs(string key)
    {
        var doc = IntegrationHelpDocs.TryGet(key);
        Assert.NotNull(doc);
        Assert.False(string.IsNullOrWhiteSpace(doc.Summary));
        Assert.False(string.IsNullOrWhiteSpace(doc.UsedFor));
        Assert.False(string.IsNullOrWhiteSpace(doc.WhereToGetKey));
    }

    [Fact]
    public void Kvk_help_docs_explain_base_url()
    {
        var doc = IntegrationHelpDocs.TryGet("kvk");
        Assert.NotNull(doc);
        Assert.Contains("https://api.kvk.nl/api/", doc!.Tip, StringComparison.Ordinal);
        Assert.Contains("https://api.kvk.nl/test/api/", doc.Tip, StringComparison.Ordinal);
    }

    [Fact]
    public void Unknown_key_returns_null()
    {
        Assert.Null(IntegrationHelpDocs.TryGet("PostcodeCheck"));
    }
}
