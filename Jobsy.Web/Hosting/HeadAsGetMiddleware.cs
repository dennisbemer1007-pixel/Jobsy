namespace Jobsy.Web.Hosting;

/// <summary>
/// Razor Components reject HEAD (405). Crawlers and some lab tools probe with HEAD.
/// Serve the same status/headers as GET without a body.
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
        }
        finally
        {
            context.Response.Body = originalBody;
            context.Request.Method = HttpMethods.Head;
        }
    }
}
