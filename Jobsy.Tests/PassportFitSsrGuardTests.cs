namespace Jobsy.Tests;

public sealed class PassportFitSsrGuardTests
{
    [Fact]
    public void Passport_fit_check_sets_query_param_after_post()
    {
        var root = TestRepo.FindRoot();
        var js = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "passport-ssr-fit.js"));
        Assert.Contains("searchParams.set(\"check\"", js, StringComparison.Ordinal);
    }
}
