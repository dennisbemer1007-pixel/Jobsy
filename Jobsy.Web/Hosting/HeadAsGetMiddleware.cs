namespace Jobsy.Web.Hosting;

/// <summary>
/// Razor Components reject HEAD (405). Crawlers and some lab tools probe with HEAD.
/// Rewrite to GET before the rest of the pipeline and suppress the body.
/// If routing already bound a GET-only endpoint, coerce a leftover 405 to 200.
/// </summary>
public sealed class HeadAsGetMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        if (!HttpMethods.IsHead(context.Request.Method))
        {
            await next(context);
            return;
        }

        context.Request.Method = HttpMethods.Get;
        var originalBody = context.Response.Body;
        context.Response.Body = Stream.Null;
        try
        {
            await next(context);
            if (context.Response.StatusCode == StatusCodes.Status405MethodNotAllowed)
            {
                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.Headers.Allow = "GET, HEAD";
            }
        }
        finally
        {
            context.Response.Body = originalBody;
            context.Request.Method = HttpMethods.Head;
        }
    }
}
