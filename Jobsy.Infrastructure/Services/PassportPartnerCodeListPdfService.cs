using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Jobsy.Infrastructure.Services;

public static class PassportPartnerCodeListPdfService
{
    static PassportPartnerCodeListPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Render(string partnerName, IReadOnlyList<(string Branch, string Code)> rows)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11).FontColor("#0f2d5c"));
                page.Header().Column(col =>
                {
                    col.Item().Text("Lobsy").SemiBold().FontSize(14);
                    col.Item().Text(partnerName).FontSize(12);
                    col.Item().PaddingTop(4).Text("Partnercodes").FontColor("#5a6a7a");
                });
                page.Content().PaddingTop(16).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn();
                    });
                    table.Header(header =>
                    {
                        header.Cell().Text("Vestiging").SemiBold();
                        header.Cell().Text("Code").SemiBold();
                    });
                    foreach (var row in rows)
                    {
                        table.Cell().PaddingVertical(4).Text(row.Branch);
                        table.Cell().PaddingVertical(4).Text(row.Code);
                    }
                });
            });
        }).GeneratePdf();
    }
}
