using Jobsy.Web.Bedrijvenkaart;

namespace Jobsy.Web.Hosting;

public static class BedrijvenkaartClientEndpoints
{
    public static IEndpointRouteBuilder MapBedrijvenkaartClientEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(BedrijvenkaartClientPins.Path, BedrijvenkaartClientPins.GetAsync)
            .AllowAnonymous();

        return app;
    }
}
