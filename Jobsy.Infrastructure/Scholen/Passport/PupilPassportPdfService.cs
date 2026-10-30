using Jobsy.Core.Scholen.PupilPassport;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Jobsy.Infrastructure.Scholen.Passport;

public sealed class PupilPassportPdfService : IPupilPassportPdfService
{
    static PupilPassportPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        PupilPassportFonts.EnsureRegistered();
    }

    public byte[] Render(PupilPassportPdfModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return model.Variant == PupilPassportVariant.Groep78
            ? PupilPassportGroep78PdfRenderer.Render(model)
            : PupilPassportVoPdfRenderer.Render(model);
    }

    public byte[] RenderFirstPagePng(PupilPassportPdfModel model, int rasterDpi = 150)
    {
        ArgumentNullException.ThrowIfNull(model);
        return model.Variant == PupilPassportVariant.Groep78
            ? PupilPassportGroep78PdfRenderer.RenderFirstPagePng(model, rasterDpi)
            : PupilPassportVoPdfRenderer.RenderFirstPagePng(model, rasterDpi);
    }
}
