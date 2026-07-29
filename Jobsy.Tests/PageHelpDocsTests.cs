using Jobsy.Web.Help;

namespace Jobsy.Tests;

public class PageHelpDocsTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/banen")]
    [InlineData("/banen/")]
    public void Banenkaart_paths_are_excluded(string path)
    {
        Assert.True(PageHelpDocs.IsExcludedPath(path));
        Assert.Null(PageHelpDocs.TryGet(path));
    }

    [Theory]
    [InlineData("/login", "Inloggen")]
    [InlineData("/home", "Home / dashboard")]
    [InlineData("/candidate/applications", "Mijn sollicitaties")]
    [InlineData("/employer/tokens", "Tokens")]
    [InlineData("/employer/company", "Bedrijfsgegevens")]
    [InlineData("/admin/integrations", "Beheer · Integraties")]
    [InlineData("/branch/vacancies/new", "Vacature plaatsen")]
    public void Known_paths_return_titled_docs(string path, string title)
    {
        var doc = PageHelpDocs.TryGet(path);
        Assert.NotNull(doc);
        Assert.Equal(title, doc.Title);
        Assert.False(string.IsNullOrWhiteSpace(doc.Purpose));
        Assert.False(string.IsNullOrWhiteSpace(doc.HowItWorks));
        Assert.False(string.IsNullOrWhiteSpace(doc.UsedFor));
    }

    [Fact]
    public void Vacancy_detail_uses_prefix_docs()
    {
        var doc = PageHelpDocs.TryGet($"/vacancies/{Guid.NewGuid()}");
        Assert.NotNull(doc);
        Assert.Equal("Vacaturedetail", doc.Title);
    }

    [Fact]
    public void Unknown_path_gets_fallback_docs()
    {
        var doc = PageHelpDocs.TryGet("/this-route-does-not-exist");
        Assert.NotNull(doc);
        Assert.Equal("Deze pagina", doc.Title);
    }
}
