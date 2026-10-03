using Jobsy.Core.Scholen;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Jobsy.Infrastructure.Scholen;

/// <summary>One-page A4 pupil report. Generated on demand; never written to disk or blob storage.</summary>
public sealed class PupilReportPdfService : IPupilReportPdfService
{
    private static readonly Color Brand = Color.FromHex("#0f2d5c");
    private static readonly Color Muted = Color.FromHex("#5a6a7a");
    private static readonly Color Soft = Color.FromHex("#eef2f6");
    private static readonly Color Gold = Color.FromHex("#c9a227");

    static PupilReportPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Render(PupilReportPdfModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        var dateLabel = model.Date.ToString("dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(Brand));

                page.Header().Background(Brand).Padding(10).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(PupilReportPdfCopy.Header).FontColor(Colors.White).SemiBold().FontSize(14);
                        col.Item().Text($"Lobsy voor scholen · {dateLabel}").FontColor(Colors.White).FontSize(8);
                    });
                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().AlignRight().Text($"Klas {model.ClassName} · {model.SchoolName}")
                            .FontColor(Colors.White).FontSize(9);
                        col.Item().AlignRight().Text(model.DisplayCode)
                            .FontColor(Colors.White).SemiBold().FontSize(12).FontFamily(Fonts.CourierNew);
                    });
                });

                page.Content().PaddingTop(10).Column(main =>
                {
                    main.Item().Row(nameRow =>
                    {
                        nameRow.RelativeItem().Text(text =>
                        {
                            text.Span(PupilReportPdfCopy.NameLine).SemiBold();
                            text.Span("  ...............................................................");
                        });
                    });

                    main.Item().PaddingTop(8).Row(body =>
                    {
                        body.RelativeItem(3).PaddingRight(8).Column(left =>
                        {
                            left.Item().Text(PupilReportPdfCopy.StoryTitle).SemiBold().FontSize(11);
                            left.Item().PaddingTop(4).Text(model.StoryBody).FontSize(9).LineHeight(1.35f);

                            left.Item().PaddingTop(8).Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn();
                                    c.RelativeColumn();
                                });
                                foreach (var tile in model.Tiles)
                                {
                                    table.Cell().Element(TileCell).Column(col =>
                                    {
                                        col.Item().Text(tile.Label).FontSize(7).FontColor(Muted);
                                        col.Item().Text(tile.Value).SemiBold().FontSize(9);
                                    });
                                }
                            });

                            if (model.Likes.Count > 0)
                            {
                                left.Item().PaddingTop(8).Text(PupilReportPdfCopy.LikesTitle).SemiBold();
                                left.Item().PaddingTop(2).Text(string.Join(" · ", model.Likes)).FontSize(8);
                            }

                            if (model.Dislikes.Count > 0)
                            {
                                left.Item().PaddingTop(6).Text(PupilReportPdfCopy.DislikesTitle).SemiBold();
                                left.Item().PaddingTop(2).Text(string.Join(" · ", model.Dislikes)).FontSize(8);
                            }

                            if (model.JobIdeas.Count > 0)
                            {
                                left.Item().PaddingTop(6).Text(PupilReportPdfCopy.JobsTitle).SemiBold();
                                left.Item().PaddingTop(2).Text(string.Join(" · ", model.JobIdeas)).FontSize(8);
                            }
                        });

                        body.RelativeItem(2).Column(right =>
                        {
                            if (!string.IsNullOrWhiteSpace(model.DreamJobTitle))
                            {
                                right.Item().Text($"Mijn droombaan: {model.DreamJobTitle}").SemiBold().FontSize(10);
                                var stepNo = 1;
                                foreach (var step in model.RouteSteps)
                                {
                                    var isGoal = stepNo == model.RouteSteps.Count;
                                    var label = isGoal ? "★" : stepNo.ToString(System.Globalization.CultureInfo.InvariantCulture);
                                    right.Item().PaddingTop(4).Row(r =>
                                    {
                                        r.ConstantItem(16).AlignMiddle().Text(label)
                                            .FontColor(isGoal ? Gold : Brand).SemiBold();
                                        r.RelativeItem().Text(step).FontSize(8);
                                    });
                                    stepNo++;
                                }

                                if (!string.IsNullOrWhiteSpace(model.Encouragement))
                                {
                                    right.Item().PaddingTop(8).Background(Color.FromHex("#fff8e6"))
                                        .Padding(6).Text(model.Encouragement).FontSize(8);
                                }
                            }
                            else
                            {
                                right.Item().Text(PupilReportPdfCopy.JobsTitle).SemiBold();
                                right.Item().PaddingTop(4).Text(string.Join(" · ", model.JobIdeas)).FontSize(8);
                            }
                        });
                    });
                });

                page.Footer().Column(foot =>
                {
                    foot.Item().LineHorizontal(0.5f).LineColor(Soft);
                    foot.Item().PaddingTop(4).Row(r =>
                    {
                        r.RelativeItem().Text(model.Footer ?? PupilReportPdfCopy.Footer).FontSize(7).FontColor(Muted);
                        r.ConstantItem(90).AlignRight().Text(text =>
                        {
                            text.Span("Pagina ").FontSize(7).FontColor(Muted);
                            text.CurrentPageNumber().FontSize(7).FontColor(Muted);
                            text.Span(" van ").FontSize(7).FontColor(Muted);
                            text.TotalPages().FontSize(7).FontColor(Muted);
                        });
                    });
                    foot.Item().AlignRight().Text(model.DisplayCode).FontSize(6).FontColor(Muted)
                        .FontFamily(Fonts.CourierNew);
                });
            });
        }).GeneratePdf();
    }

    private static IContainer TileCell(IContainer c)
        => c.Background(Soft).Padding(6).Border(0);
}
