namespace Jobsy.Core.Scholen;

/// <summary>On-demand pupil discovery PDF (QuestPDF). Never stored.</summary>
public interface IPupilReportPdfService
{
    byte[] Render(PupilReportPdfModel model);
}

public sealed record PupilReportPdfModel(
    string SchoolName,
    string ClassName,
    string DisplayCode,
    DateOnly Date,
    string StoryBody,
    IReadOnlyList<(string Label, string Value)> Tiles,
    IReadOnlyList<string> Likes,
    IReadOnlyList<string> Dislikes,
    IReadOnlyList<string> JobIdeas,
    string? DreamJobTitle,
    IReadOnlyList<string> RouteSteps,
    string? Encouragement,
    string? Footer = null);

public static class PupilReportPdfCopy
{
    public const string Header = "Mijn ontdekkingsreis";
    public const string NameLine = "Naam (vul zelf in)";
    public const string Footer = "Lobsy bewaart geen namen. Deze PDF is voor jou en je leraar.";
    public const string LikesTitle = "Je houdt van";
    public const string DislikesTitle = "Niet zo leuk vind je";
    public const string JobsTitle = "Beroepen om eens te bekijken";
    public const string StoryTitle = "Dit ben jij";
}
