namespace Jobsy.Web.Banenkaart;

internal static class BanenkaartSsrTransportIcons
{
    internal static string MetaTravelSvg(string? transport)
    {
        var mode = (transport ?? "Fiets").Trim();
        if (mode.Equals("Auto", StringComparison.OrdinalIgnoreCase))
        {
            return "<svg class=\"ic s\" viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" aria-hidden=\"true\">"
                   + "<path d=\"M5 17h14M7 17l1.2-3.5h7.6L17 17M6.5 13.5 8 9h8l1.5 4.5\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>"
                   + "<circle cx=\"7.5\" cy=\"17.5\" r=\"1.5\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\"/>"
                   + "<circle cx=\"16.5\" cy=\"17.5\" r=\"1.5\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\"/>"
                   + "</svg>";
        }

        if (mode.Equals("OV", StringComparison.OrdinalIgnoreCase))
        {
            return "<svg class=\"ic s\" viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" aria-hidden=\"true\">"
                   + "<path d=\"M6 16V8a2 2 0 0 1 2-2h8a2 2 0 0 1 2 2v8l-2 2H8l-2-2z\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\" stroke-linejoin=\"round\"/>"
                   + "<path d=\"M8 6V4h8v2M9 20h6\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\" stroke-linecap=\"round\"/>"
                   + "</svg>";
        }

        if (mode.Equals("Lopend", StringComparison.OrdinalIgnoreCase))
        {
            return "<svg class=\"ic s\" viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" aria-hidden=\"true\">"
                   + "<circle cx=\"12\" cy=\"5.5\" r=\"2\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\"/>"
                   + "<path d=\"M10 22v-6l-2-3 2-2h4l2 2-2 3v6\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>"
                   + "</svg>";
        }

        return "<svg class=\"ic s\" viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" aria-hidden=\"true\">"
               + "<circle cx=\"6\" cy=\"16\" r=\"3.5\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\"/>"
               + "<circle cx=\"18\" cy=\"16\" r=\"3.5\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\"/>"
               + "<path d=\"M6 16l4-7h5l3 7M10 9 8.5 6H7M12.5 16 15 9\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>"
               + "</svg>";
    }
}
