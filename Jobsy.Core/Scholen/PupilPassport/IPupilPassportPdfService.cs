namespace Jobsy.Core.Scholen.PupilPassport;

public interface IPupilPassportPdfService
{
    byte[] Render(PupilPassportPdfModel model);

    /// <summary>First page PNG preview (e.g. 150 dpi for review artifacts).</summary>
    byte[] RenderFirstPagePng(PupilPassportPdfModel model, int rasterDpi = 150);
}
