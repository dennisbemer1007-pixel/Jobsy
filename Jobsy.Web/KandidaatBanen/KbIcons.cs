using Microsoft.AspNetCore.Components;

namespace Jobsy.Web.KandidaatBanen;

/// <summary>Shared inline SVG glyphs for candidate job card parts (avoid Razor markup parsing of path data).</summary>
public static class KbIcons
{
    public static readonly MarkupString Sparkle = new(
        "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 16 16\" fill=\"currentColor\" focusable=\"false\">" +
        "<path d=\"M8 1.2l1.1 3.4L12.5 6 9.1 7.4 8 10.8 6.9 7.4 3.5 6l3.4-1.4L8 1.2zm4.8 7.2l.6 1.8 1.8.6-1.8.6-.6 1.8-.6-1.8-1.8-.6 1.8-.6.6-1.8z\"/>" +
        "</svg>");

    public static MarkupString Transport(string canonicalMode)
    {
        var path = canonicalMode switch
        {
            "Auto" =>
                "<path d=\"M2.5 9.5h11l-.8-2.4A1.5 1.5 0 0 0 11.3 6H4.7a1.5 1.5 0 0 0-1.4 1.1L2.5 9.5zm1.2 1.2a1.1 1.1 0 1 0 0 2.2 1.1 1.1 0 0 0 0-2.2zm8.6 0a1.1 1.1 0 1 0 0 2.2 1.1 1.1 0 0 0 0-2.2z\"/>",
            "Lopend" =>
                "<path d=\"M8.2 2.2a1.1 1.1 0 1 1 0 2.2 1.1 1.1 0 0 1 0-2.2zM6.2 5.2h3.2l1.4 3.2-1.1.5-.9-2.1-.4 1.3 2.1 2.4-.9.8-1.7-2-1.1 3.4H5.2l1.5-4.6-1.2-1.2.7-.7z\"/>",
            "OV" =>
                "<path d=\"M4 3.5h8a1.5 1.5 0 0 1 1.5 1.5v6A1.5 1.5 0 0 1 12 12.5H4A1.5 1.5 0 0 1 2.5 11V5A1.5 1.5 0 0 1 4 3.5zm1.2 7.2a1 1 0 1 0 0 2 1 1 0 0 0 0-2zm5.6 0a1 1 0 1 0 0 2 1 1 0 0 0 0-2zM4 5.5h8v3.2H4V5.5z\"/>",
            _ =>
                "<path d=\"M4.2 10.2a2.2 2.2 0 1 1 0 4.4 2.2 2.2 0 0 1 0-4.4zm7.6 0a2.2 2.2 0 1 1 0 4.4 2.2 2.2 0 0 1 0-4.4zM5.6 11.2h2.1l1.2-3.1h2.2l.9 2.2h-1.5l-.4-1h-1.3L7.4 11.2H9l.6 1.5H5.6v-1.5zM9.8 4.2l1.1 2.6H8.5L7.6 4.2h2.2z\"/>"
        };
        return new MarkupString(
            "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 16 16\" fill=\"currentColor\" focusable=\"false\">" +
            path +
            "</svg>");
    }
}
