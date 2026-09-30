using Jobsy.Core.Scholen;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Jobsy.Infrastructure.Scholen;

public sealed class SchoolCodeListPdfService : ISchoolCodeListPdfService
{
    private static readonly Color Brand = Color.FromHex("#0f2d5c");
    private static readonly Color Muted = Color.FromHex("#5a6a7a");
    private static readonly Color Line = Color.FromHex("#c5cdd6");

    static SchoolCodeListPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Render(
        string schoolName,
        string className,
        string schoolYearLabel,
        IReadOnlyList<SchoolCodeListRow> rows,
        bool includeCutoutCards = true)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var ordered = rows.OrderBy(r => r.Number).ToList();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Brand));

                page.Header().Column(col =>
                {
                    col.Item().Text(schoolName).SemiBold().FontSize(14);
                    col.Item().Text($"Klas {className} · {schoolYearLabel}").FontSize(11).FontColor(Muted);
                    col.Item().PaddingTop(6).Text(SchoolCodeListPdfCopy.Title).SemiBold().FontSize(12);
                });

                page.Content().PaddingTop(12).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(36);
                        c.RelativeColumn(2);
                        c.RelativeColumn(4);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(HeaderCell).Text("Nr");
                        h.Cell().Element(HeaderCell).Text("Code");
                        h.Cell().Element(HeaderCell).Text(SchoolCodeListPdfCopy.NameColumnHeader);
                    });

                    foreach (var row in ordered)
                    {
                        table.Cell().Element(BodyCell).Text(row.Number.ToString());
                        table.Cell().Element(BodyCell).Text(row.DisplayCode).FontFamily(Fonts.CourierNew);
                        // Empty name column — lined for handwriting
                        table.Cell().Element(NameCell).Text(" ");
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span(SchoolCodeListPdfCopy.NoNamesFooter)
                        .FontSize(8).FontColor(Muted);
                });
            });

            if (includeCutoutCards && ordered.Count > 0)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(28);
                    page.DefaultTextStyle(x => x.FontSize(9).FontColor(Brand));
                    page.Header().Text(SchoolCodeListPdfCopy.CutoutTitle).SemiBold().FontSize(12);
                    page.Content().PaddingTop(10).Column(col =>
                    {
                        const int perRow = 2;
                        for (var i = 0; i < ordered.Count; i += perRow)
                        {
                            var slice = ordered.Skip(i).Take(perRow).ToList();
                            col.Item().PaddingBottom(8).Row(row =>
                            {
                                foreach (var code in slice)
                                {
                                    row.RelativeItem().Border(1).BorderColor(Line).Padding(8).Column(card =>
                                    {
                                        card.Item().Text(schoolName).FontSize(8).FontColor(Muted);
                                        card.Item().Text($"Klas {className}").SemiBold();
                                        card.Item().PaddingTop(4).Text(code.DisplayCode)
                                            .FontFamily(Fonts.CourierNew).FontSize(14).SemiBold();
                                        card.Item().PaddingTop(4).Text("lobsy.nl/leerling").FontSize(8).FontColor(Muted);
                                        // Explicitly no name line (D3)
                                    });
                                    row.ConstantItem(8);
                                }

                                if (slice.Count < perRow)
                                {
                                    row.RelativeItem();
                                }
                            });
                        }
                    });
                });
            }
        }).GeneratePdf();
    }

    private static IContainer HeaderCell(IContainer c) =>
        c.DefaultTextStyle(x => x.SemiBold().FontSize(9))
            .BorderBottom(1).BorderColor(Line)
            .PaddingVertical(4).PaddingHorizontal(2);

    private static IContainer BodyCell(IContainer c) =>
        c.BorderBottom(0.5f).BorderColor(Line)
            .PaddingVertical(5).PaddingHorizontal(2);

    private static IContainer NameCell(IContainer c) =>
        c.BorderBottom(0.5f).BorderColor(Line)
            .PaddingVertical(5).PaddingHorizontal(2)
            .MinHeight(18);
}
