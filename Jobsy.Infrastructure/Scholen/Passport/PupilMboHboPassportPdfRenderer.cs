using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Scholen.MboHbo;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Jobsy.Infrastructure.Scholen.Passport;

internal static class PupilMboHboPassportPdfRenderer
{
    private static readonly Color Navy = Color.FromHex("#0f2747");
    private static readonly Color Gold = Color.FromHex("#c9a24a");
    private static readonly Color Muted = Color.FromHex("#5a6578");
    private static readonly Color Line = Color.FromHex("#e2e8f0");

    internal static byte[] Render(
        PupilCode code,
        SchoolClass schoolClass,
        string schoolName,
        string displayCode,
        string? displayNameOnPdf)
    {
        PupilPassportFonts.EnsureRegistered();
        var state = PupilMboHboProgressState.Parse(code.Progress?.MboHboProgressJson);
        var level = schoolClass.Level;
        var date = DateOnly.FromDateTime(
            code.Result?.CompletedAtUtc ?? code.Progress?.CompletedAtUtc ?? DateTime.UtcNow);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Navy));
                page.Content().Column(col =>
                {
                    col.Item().Background(Navy).Padding(16).Column(header =>
                    {
                        header.Item().Text("Lobsy · MBO/HBO profiel").FontColor(Colors.White).FontSize(11);
                        header.Item().PaddingTop(6).Text("Jouw testuitslagen").FontColor(Gold).FontSize(20).Bold();
                        header.Item().PaddingTop(8).Text(text =>
                        {
                            text.Span("Code: ").FontColor(Colors.White);
                            text.Span(displayCode).FontColor(Gold).Bold();
                        });
                        if (!string.IsNullOrWhiteSpace(displayNameOnPdf))
                        {
                            header.Item().Text(displayNameOnPdf).FontColor(Colors.White);
                        }

                        header.Item().PaddingTop(4).Text($"{schoolName} · {schoolClass.Name} · {date:dd-MM-yyyy}")
                            .FontColor(Colors.White).FontSize(9);
                    });

                    foreach (var block in PupilMboHboCatalog.AllBlocks)
                    {
                        var meta = PupilMboHboCopy.BlockMeta(block);
                        var outcome = PupilMboHboScoring.BuildOutcome(block, state, level);
                        col.Item().PaddingTop(14).Column(section =>
                        {
                            section.Item().Text(meta.Title).FontSize(13).Bold().FontColor(Navy);
                            section.Item().PaddingTop(4).Text(outcome.Summary).FontColor(Muted).FontSize(9);
                            if (block == PupilMboHboBlock.Beroepen)
                            {
                                foreach (var (bar, i) in outcome.Bars.Select((b, idx) => (b, idx)))
                                {
                                    section.Item().PaddingTop(4).Row(r =>
                                    {
                                        r.ConstantItem(28).Text($"#{i + 1}").Bold();
                                        r.RelativeItem().Text(bar.Label);
                                    });
                                }
                            }
                            else
                            {
                                foreach (var bar in outcome.Bars.Take(6))
                                {
                                    section.Item().PaddingTop(6).Row(r =>
                                    {
                                        r.RelativeItem(2).Text(bar.Label);
                                        r.RelativeItem(3).Height(8).Background(Line).Row(barRow =>
                                        {
                                            barRow.RelativeItem(Math.Clamp(bar.Percent, 0, 100))
                                                .Background(Navy);
                                            barRow.RelativeItem(Math.Max(0, 100 - bar.Percent));
                                        });
                                        r.ConstantItem(28).AlignRight().Text($"{bar.Percent}%");
                                    });
                                }
                            }

                            if (outcome.Highlights.Count > 0)
                            {
                                section.Item().PaddingTop(4).Text(string.Join(" · ", outcome.Highlights))
                                    .FontSize(9).FontColor(Muted);
                            }
                        });
                    }

                    col.Item().PaddingTop(20).Text(
                            "Dit overzicht is gebaseerd op wat je zelf hebt ingevuld. Het is een hulpmiddel, geen advies of uitslag.")
                        .FontSize(8).FontColor(Muted);
                });
            });
        }).GeneratePdf();
    }
}
