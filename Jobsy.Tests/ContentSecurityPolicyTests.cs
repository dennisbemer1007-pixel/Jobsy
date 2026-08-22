using Jobsy.Web.Security;
using Microsoft.AspNetCore.Http;

namespace Jobsy.Tests;

public class ContentSecurityPolicyTests
{
    [Fact]
    public void Web_script_src_uses_a_nonce_instead_of_unsafe_inline()
    {
        var nonce = CspNonce.Create();
        var csp = JobsyContentSecurityPolicy.ForWeb(nonce);

        var scriptSrc = JobsyContentSecurityPolicy.ScriptSrc(csp);
        Assert.NotNull(scriptSrc);
        Assert.Contains($"'nonce-{nonce}'", scriptSrc);
        Assert.DoesNotContain("unsafe-inline", scriptSrc);
        Assert.Contains("'unsafe-eval'", scriptSrc);

        var formAction = JobsyContentSecurityPolicy.Directive(csp, "form-action");
        Assert.Equal("form-action 'self'", formAction);

        var attr = JobsyContentSecurityPolicy.Directive(csp, "script-src-attr");
        Assert.Equal("script-src-attr 'none'", attr);

        var styleElem = JobsyContentSecurityPolicy.Directive(csp, "style-src-elem");
        Assert.NotNull(styleElem);
        Assert.Contains($"'nonce-{nonce}'", styleElem);
        Assert.DoesNotContain("unsafe-inline", styleElem);
    }

    [Fact]
    public void Nonce_is_stable_on_the_same_request_and_unique_across_requests()
    {
        var first = new DefaultHttpContext();
        var second = new DefaultHttpContext();
        var a = CspNonce.GetOrCreate(first);
        var again = CspNonce.GetOrCreate(first);
        var b = CspNonce.GetOrCreate(second);

        Assert.Equal(32, a.Length);
        Assert.Equal(a, again);
        Assert.NotEqual(a, b);
        Assert.Equal(a, CspNonce.Get(first));
    }

    [Fact]
    public async Task Middleware_sets_matching_csp_nonce_header()
    {
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        await middleware.Invoke(context);

        var nonce = CspNonce.Get(context);
        Assert.False(string.IsNullOrEmpty(nonce));
        var csp = context.Response.Headers.ContentSecurityPolicy.ToString();
        Assert.Equal(JobsyContentSecurityPolicy.ForWeb(nonce), csp);
        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"].ToString());
        Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"].ToString());
    }

    [Fact]
    public void App_shell_nonces_scripts_and_avoids_inline_handlers()
    {
        var app = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "App.razor"));
        Assert.Contains("<style nonce=\"@Nonce\">", app);
        Assert.Contains("<script nonce=\"@Nonce\">", app);
        Assert.Contains("nonce=\"@Nonce\" defer", app);
        Assert.Contains("data-app-css", app);
        Assert.DoesNotContain("onload=", app);
        Assert.DoesNotContain("onerror=", app);
        Assert.Contains("el.nonce = nonce", app);

        var program = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Program.cs"));
        Assert.Contains("UseMiddleware<SecurityHeadersMiddleware>", program);
        Assert.Contains("ContentSecurityFrameAncestorsPolicy = null", program);
        Assert.DoesNotContain("script-src 'self' 'unsafe-inline'", program);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found from test base directory.");
    }
}
