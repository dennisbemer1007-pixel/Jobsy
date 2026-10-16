using Jobsy.Core.Passport;
using Jobsy.Infrastructure.Scholen.Passport;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Jobsy.Infrastructure.Services.Passport;

internal static class StudentPassportPdfRenderer
{
    private static readonly Color Navy = Color.FromHex("#0F2D5C");
    private static readonly Color Purple = Color.FromHex("#5b2a9a");
    private static readonly Color Ink = Color.FromHex("#2f2445");
    private static readonly Color Muted = Color.FromHex("#6b6280");
    private static readonly Color Line = Color.FromHex("#e8e2f0");
    private static readonly Color Coral = Color.FromHex("#f5503a");
    private static readonly Color SoftPeach = Color.FromHex("#fff5f0");
    private static readonly Color SoftPurple = Color.FromHex("#f5effd");

    static StudentPassportPdfRenderer()
    {
        PupilPassportFonts.EnsureRegistered();
    }

    internal static byte[] RenderPdf(StudentPassportPdfModel m)
        => Document.Create(doc =>
        {
            doc.Page(p => Page(p, m, page2: false));
            doc.Page(p => Page(p, m, page2: true));
        }).GeneratePdf();

    internal static byte[] RenderPagePng(StudentPassportPdfModel m, int pageIndex, int dpi)
        => Document.Create(doc => doc.Page(p => Page(p, m, page2: pageIndex == 2)))
            .GenerateImages(new ImageGenerationSettings { RasterDpi = dpi, ImageFormat = ImageFormat.Png })
            .First();

    private static void Page(PageDescriptor page, StudentPassportPdfModel m, bool page2)
    {
        page.Size(PageSizes.A4);
        page.MarginHorizontal(22);
        page.MarginVertical(16);
        page.DefaultTextStyle(x => x.FontSize(9f).FontColor(Ink).FontFamily(PupilPassportFonts.Nunito));

        page.Header().Height(36).Background(Navy).PaddingHorizontal(14).AlignMiddle().Row(row =>
        {
            row.RelativeItem().Text(text =>
            {
                text.Span("Lobsy").FontSize(14).Bold().FontColor(Colors.White);
                text.Span("  ·  DNA-paspoort student").FontSize(9).FontColor(Color.FromHex("#D5E2F2"));
            });
            row.ConstantItem(80).AlignRight().Text(page2 ? "pagina 2/2" : "pagina 1/2")
                .FontSize(8).FontColor(Colors.White);
        });

        page.Content().Column(col =>
        {
            col.Spacing(6);
            if (page2)
            {
                Page2(col, m);
            }
            else
            {
                Page1(col, m);
            }

            col.Item().PaddingTop(8).BorderTop(1).BorderColor(Line).Text(m.FooterNote).FontSize(7).FontColor(Muted);
        });
    }

    private static void Page1(ColumnDescriptor col, StudentPassportPdfModel m)
    {
        col.Item().Row(row =>
        {
            if (!string.IsNullOrWhiteSpace(m.FullName))
            {
                row.ConstantItem(40).Height(40).Background(Purple).AlignCenter().AlignMiddle()
                    .Text(m.Initials).FontColor(Colors.White).SemiBold();
            }

            row.RelativeItem().PaddingLeft(8).Column(c =>
            {
                if (!string.IsNullOrWhiteSpace(m.FullName))
                {
                    c.Item().Text(m.FullName).FontSize(16).SemiBold();
                }
                else
                {
                    c.Item().Text("Naam (optioneel op PDF)").FontSize(9).FontColor(Muted);
                    c.Item().PaddingTop(4).BorderBottom(1).BorderColor(Purple).Height(14);
                }

                c.Item().Text($"{m.StudyLine} · {m.YearLine}").FontSize(9).FontColor(Purple);
                if (!string.IsNullOrWhiteSpace(m.Tagline))
                {
                    c.Item().Text(m.Tagline!).FontSize(8.5f).FontColor(Muted);
                }

                if (!string.IsNullOrWhiteSpace(m.Region))
                {
                    c.Item().PaddingTop(2).Text(m.Region!).FontSize(8).FontColor(Muted);
                }
            });
        });

        col.Item().Height(3).Svg(VoGradientRule());

        col.Item().Row(dnaRow =>
        {
            foreach (var card in m.DnaCards)
            {
                dnaRow.RelativeItem().Padding(2).Background(SoftPeach).Border(1).BorderColor(Line).Padding(8).Column(x =>
                {
                    x.Item().Svg(PupilPassportIcons.Icon(card.IconKey, Purple, 16));
                    x.Item().PaddingTop(4).Text(card.Label.ToUpperInvariant()).FontSize(7).FontColor(Muted);
                    x.Item().Text(card.Value).SemiBold().FontSize(9);
                    if (!string.IsNullOrWhiteSpace(card.Sub))
                    {
                        x.Item().Text(card.Sub!).FontSize(7.5f).FontColor(Muted);
                    }
                });
            }
        });

        if (m.Strengths.Count > 0)
        {
            col.Item().Text("Sterke punten").SemiBold().FontSize(10);
            col.Item().Row(r =>
            {
                foreach (var s in m.Strengths.Take(3))
                {
                    r.RelativeItem().Padding(2).Background(SoftPurple).Padding(6).Text(s).FontSize(8.5f);
                }
            });
        }

        if (m.Vacancies.Count > 0)
        {
            col.Item().Text("Passende stages & bijbanen (types)").SemiBold().FontSize(10);
            col.Item().Row(r =>
            {
                foreach (var v in m.Vacancies.Take(3))
                {
                    r.RelativeItem().Padding(2).Border(1).BorderColor(Line).Padding(8).Column(x =>
                    {
                        x.Item().Text(v.Title).SemiBold().FontSize(9);
                        x.Item().Text(v.Org).FontSize(7.5f).FontColor(Muted);
                        foreach (var b in v.Bullets.Take(3))
                        {
                            x.Item().PaddingTop(2).Row(li =>
                            {
                                li.ConstantItem(5).Height(5).Background(Coral);
                                li.RelativeItem().PaddingLeft(4).Text(b).FontSize(7.5f);
                            });
                        }
                    });
                }
            });
        }

        if (m.Experience.Count > 0)
        {
            col.Item().Text("Ervaring").SemiBold().FontSize(10);
            foreach (var e in m.Experience)
            {
                col.Item().Row(r =>
                {
                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text(e.Title).SemiBold().FontSize(9);
                        if (!string.IsNullOrWhiteSpace(e.Detail))
                        {
                            c.Item().Text(e.Detail!).FontSize(7.5f).FontColor(Muted);
                        }
                    });
                    r.ConstantItem(60).AlignRight().Text(e.Period).FontSize(7.5f).FontColor(Muted);
                });
            }
        }
    }

    private static void Page2(ColumnDescriptor col, StudentPassportPdfModel m)
    {
        col.Item().Text("Mijn richting").SemiBold().FontSize(11).FontColor(Purple);
        col.Item().Column(steps =>
        {
            foreach (var step in m.DirectionSteps)
            {
                steps.Item().PaddingBottom(4).Row(r =>
                {
                    var n = r.ConstantItem(20).Height(20).Border(2).BorderColor(step.IsGoal ? Coral : Purple)
                        .Background(step.IsGoal ? Coral : Colors.White).AlignCenter().AlignMiddle();
                    if (step.IsGoal)
                    {
                        n.Svg(PupilPassportIcons.Icon("star", Colors.White, 10));
                    }
                    else
                    {
                        n.Text(step.Number.ToString()).SemiBold().FontSize(8).FontColor(Purple);
                    }

                    r.RelativeItem().PaddingLeft(6).Column(c =>
                    {
                        c.Item().Text(step.Title).SemiBold().FontSize(9);
                        c.Item().Text(step.Detail).FontSize(8).FontColor(Muted);
                    });
                });
            }
        });

        col.Item().PaddingTop(6).Text("Wat ik wil leren").SemiBold().FontSize(10);
        if (m.LearningGoals.Count > 0)
        {
            foreach (var g in m.LearningGoals)
            {
                col.Item().Row(r =>
                {
                    r.ConstantItem(10).Svg(PupilPassportIcons.Icon("flag", Coral, 10));
                    r.RelativeItem().Text(g).FontSize(8.5f);
                });
            }
        }
        else
        {
            col.Item().Text("—").FontColor(Muted).Italic();
        }

        col.Item().PaddingTop(6).Border(1).BorderColor(Line).Padding(10).Row(r =>
        {
            r.ConstantItem(18).Svg(PupilPassportIcons.Icon("quote", Color.FromHex("#d9c7f0"), 16));
            r.RelativeItem().PaddingLeft(6).Column(c =>
            {
                c.Item().Text("In mijn eigen woorden").SemiBold().FontSize(9);
                c.Item().PaddingTop(4).Text(string.IsNullOrWhiteSpace(m.OwnWords) ? "—" : m.OwnWords!)
                    .FontSize(9).LineHeight(1.4f);
            });
        });

        if (m.HowIWork.Count > 0)
        {
            col.Item().PaddingTop(6).Text("Hoe ik werk").SemiBold().FontSize(10);
            foreach (var h in m.HowIWork)
            {
                col.Item().Text("• " + h).FontSize(8.5f);
            }
        }

        if (m.Certificates.Count > 0)
        {
            col.Item().PaddingTop(6).Text("Certificaten").SemiBold().FontSize(10);
            col.Item().Text(string.Join(" · ", m.Certificates)).FontSize(8.5f);
        }
    }

    private static string VoGradientRule()
        => """<svg xmlns="http://www.w3.org/2000/svg" width="100%" height="3"><defs><linearGradient id="g" x1="0" y1="0" x2="1" y2="0"><stop offset="0%" stop-color="#5b2a9a"/><stop offset="50%" stop-color="#c94aa8"/><stop offset="100%" stop-color="#f5503a"/></linearGradient></defs><rect width="100%" height="3" fill="url(#g)" rx="1.5"/></svg>""";
}
