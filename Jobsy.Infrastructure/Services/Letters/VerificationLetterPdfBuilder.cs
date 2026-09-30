using Jobsy.Core.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Jobsy.Infrastructure.Services.Letters;

/// <summary>A4 window-envelope letter (nl) with the verification code — QuestPDF.</summary>
public static class VerificationLetterPdfBuilder
{
    private static readonly Color Navy = Color.FromHex("#0f2d5c");
    private static readonly Color Slate = Color.FromHex("#2c3a4a");

    static VerificationLetterPdfBuilder()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(
        string companyName,
        string requesterName,
        string? requesterFunction,
        IReadOnlyList<string> addressLines,
        string postalCode,
        string city,
        string codeFormatted,
        DateTime validUntilLocalDate,
        string supportEmail,
        byte[]? logoPng = null)
    {
        var dateNl = DateTime.UtcNow.ToLocalTime().ToString("d MMMM yyyy", new System.Globalization.CultureInfo("nl-NL"));
        var validNl = validUntilLocalDate.ToString("d MMMM yyyy", new System.Globalization.CultureInfo("nl-NL"));
        var functionBit = string.IsNullOrWhiteSpace(requesterFunction)
            ? requesterName
            : $"{requesterName} ({requesterFunction})";

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginLeft(20, Unit.Millimetre);
                page.MarginRight(20, Unit.Millimetre);
                page.MarginTop(15, Unit.Millimetre);
                page.MarginBottom(20, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontSize(11).FontColor(Slate));

                page.Content().Column(col =>
                {
                    col.Spacing(10);

                    // Window envelope address block (approx. Dutch window position).
                    col.Item().Height(45, Unit.Millimetre).Column(addr =>
                    {
                        addr.Item().PaddingTop(8, Unit.Millimetre);
                        foreach (var line in addressLines.Where(l => !string.IsNullOrWhiteSpace(l)).Take(4))
                        {
                            addr.Item().Text(line.Trim()).FontSize(11);
                        }

                        addr.Item().Text($"{postalCode}  {city}".Trim()).FontSize(11);
                    });

                    col.Item().Row(row =>
                    {
                        if (logoPng is { Length: > 0 })
                        {
                            row.ConstantItem(70).Height(28).Image(logoPng).FitArea();
                            row.ConstantItem(12);
                        }

                        row.RelativeItem().AlignRight().Text("Lobsy").Bold().FontSize(18).FontColor(Navy);
                    });

                    col.Item().Text(dateNl).FontSize(10).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(6).Text($"Beste {companyName},").FontSize(12);

                    col.Item().Text(
                        $"{functionBit} heeft gevraagd om jullie bedrijf op Lobsy te verifiëren. " +
                        "Met de code hieronder bewijzen jullie dat dit verzoek bij jullie hoort.");

                    col.Item().Text(
                        "Lobsy is het platform waarop werkgevers vacatures plaatsen en kandidaten vinden. " +
                        "Zonder verificatie blijft jullie bedrijf onzichtbaar voor kandidaten.");

                    col.Item().Text(
                        "Voer de code in op lobsy.nl/register/verifieren/brief of in je dashboard. " +
                        "This letter is in Dutch (KvK address).");

                    col.Item().PaddingVertical(12).AlignCenter().Column(code =>
                    {
                        code.Item().AlignCenter().Text("Uw verificatiecode").FontSize(10).FontColor(Colors.Grey.Darken1);
                        code.Item().AlignCenter().Text(codeFormatted).FontSize(28).Bold().FontColor(Navy).FontFamily(Fonts.CourierNew);
                    });

                    col.Item().Text($"Geldig tot {validNl}.").FontSize(10);

                    col.Item().PaddingTop(8).Text(
                        $"Heb je dit niet aangevraagd? Mail naar {supportEmail}; zonder code gebeurt er niets.")
                        .FontSize(9).FontColor(Colors.Grey.Darken2);
                });
            });
        }).GeneratePdf();
    }
}
