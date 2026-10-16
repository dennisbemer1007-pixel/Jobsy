using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.PupilPassport;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Jobsy.Infrastructure.Scholen.Passport;

internal static class PupilPassportGroep78PdfRenderer
{
    private static readonly Color Purple = Color.FromHex("#5b2a9a");
    private static readonly Color Ink = Color.FromHex("#3a2d52");
    private static readonly Color Muted = Color.FromHex("#8d83a0");
    private static readonly Color Mint = Color.FromHex("#e6f8ef");
    private static readonly Color Peach = Color.FromHex("#fff0ea");

    internal static byte[] Render(PupilPassportPdfModel m) => BuildDocument(m).GeneratePdf();

    internal static byte[] RenderFirstPagePng(PupilPassportPdfModel m, int dpi)
        => BuildDocument(m).GenerateImages(new ImageGenerationSettings
        {
            RasterDpi = dpi,
            ImageFormat = ImageFormat.Png
        }).First();

    private static Document BuildDocument(PupilPassportPdfModel m)
        => Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(26);
                page.MarginVertical(22);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontColor(Ink).FontFamily(PupilPassportFonts.Nunito));

                page.Content().Column(col =>
                {
                    col.Item().Element(c => Header(c, m));
                    col.Item().PaddingTop(6).Element(c => NameLine(c, m));
                    col.Item().PaddingTop(8).Element(c => DitBenIk(c, m));
                    col.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem(3).PaddingRight(6).Element(c => DreamBlock(c, m));
                        row.RelativeItem(2).Element(c => LikesBlock(c, m));
                    });
                    col.Item().PaddingTop(8).Element(x => Strengths(x, m));
                    col.Item().PaddingTop(6).Element(c => Cards(c, m));
                    col.Item().PaddingTop(6).Row(row =>
                    {
                        row.RelativeItem().PaddingRight(4).Element(c => Discover(c, m));
                        row.RelativeItem().Element(c => Tip(c, m));
                    });
                    col.Item().PaddingTop(8).Element(c => Footer(c, m));
                });
            });
        });

    private static void Header(IContainer c, PupilPassportPdfModel m)
    {
        c.Background(Purple).Padding(10).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("LOBSY").FontFamily(PupilPassportFonts.Fredoka).FontSize(9).FontColor(Colors.White).LetterSpacing(0.12f);
                col.Item().Text(PupilPassportCopy.Get("LeerlingPassport.Header.G78"))
                    .FontFamily(PupilPassportFonts.Fredoka).FontSize(16).FontColor(Colors.White);
            });
            row.ConstantItem(70).AlignRight().Column(col =>
            {
                col.Item().AlignRight().Text(m.ClassLabel).FontSize(8).FontColor(Colors.White);
                col.Item().AlignRight().Text(m.SchoolName).FontSize(8).FontColor(Colors.White);
                col.Item().AlignRight().Text(m.Date.ToString("d MMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("nl-NL")))
                    .FontSize(7).FontColor(Colors.White);
                col.Item().AlignRight().Text(m.DisplayCode).FontFamily(Fonts.CourierNew).FontSize(8).FontColor(Colors.White);
            });
            row.ConstantItem(58).AlignMiddle().Border(1.5f).BorderColor(Color.FromHex("#ffc9b8"))
                .Padding(4).AlignCenter().Column(col =>
                {
                    col.Item().AlignCenter().Text(text =>
                    {
                        text.Span(string.Format(
                            System.Globalization.CultureInfo.InvariantCulture,
                            PupilPassportCopy.Get("LeerlingPassport.Stamp"),
                            m.WorldsCompleted,
                            m.WorldsTotal)).FontSize(7).FontColor(Color.FromHex("#ffe3da"));
                    });
                });
        });
    }

    private static void NameLine(IContainer c, PupilPassportPdfModel m)
    {
        c.Row(row =>
        {
            row.AutoItem().AlignBottom().Text(PupilPassportCopy.Get("LeerlingPassport.NameIntro") + " ")
                .FontFamily(PupilPassportFonts.Fredoka).FontSize(11).FontColor(Purple);
            if (!string.IsNullOrWhiteSpace(m.DisplayNameOnPdf))
            {
                row.ConstantItem(180).AlignBottom().BorderBottom(1).BorderColor(Color.FromHex("#b99be0"))
                    .Text(m.DisplayNameOnPdf!).FontFamily(PupilPassportFonts.Caveat).FontSize(22).FontColor(Purple);
            }
            else
            {
                row.ConstantItem(180).AlignBottom().BorderBottom(1).BorderColor(Color.FromHex("#b99be0"))
                    .Height(20);
            }

            row.RelativeItem().AlignBottom().PaddingLeft(6).Text(PupilPassportCopy.Get("LeerlingPassport.NamePrivacy"))
                .FontSize(7).FontColor(Muted);
        });
    }

    private static void DitBenIk(IContainer c, PupilPassportPdfModel m)
    {
        c.Column(col =>
        {
            col.Item().Text(PupilPassportCopy.Get("LeerlingPassport.Section.DitBenIk"))
                .FontFamily(PupilPassportFonts.Fredoka).FontSize(13).FontColor(Purple);
            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem(3).Background(Color.FromHex("#fffdf5")).Border(1).BorderColor(Color.FromHex("#f3e6bf"))
                    .Padding(8).Text(m.StoryBody).LineHeight(1.35f).FontSize(8.5f);
                row.RelativeItem(4).PaddingLeft(6).Row(wRow =>
                {
                    foreach (var w in m.Worlds)
                    {
                        wRow.RelativeItem().PaddingHorizontal(2).Element(x => WorldCard(x, w));
                    }
                });
            });
        });
    }

    private static void WorldCard(IContainer c, PupilPassportWorldStamp w)
    {
        var bg = w.ToneKey switch
        {
            "w1" => Color.FromHex("#fff3ee"),
            "w2" => Color.FromHex("#f5effd"),
            "w3" => Color.FromHex("#fdeff8"),
            _ => Color.FromHex("#ebfaf3")
        };
        c.Background(bg).Padding(4).Column(col =>
        {
            col.Item().AlignCenter().Text(w.Title).FontFamily(PupilPassportFonts.Fredoka).FontSize(8).FontColor(Purple);
            col.Item().AlignCenter().Text(w.Subtitle).FontSize(6.5f).FontColor(Muted);
            col.Item().PaddingTop(2).AlignCenter().Text(w.Value).FontSize(7.5f).SemiBold();
        });
    }

    private static void DreamBlock(IContainer c, PupilPassportPdfModel m)
    {
        c.Border(2).BorderColor(Purple).Padding(8).Column(col =>
        {
            col.Item().Text(PupilPassportCopy.Get("LeerlingPassport.Section.Droom"))
                .FontFamily(PupilPassportFonts.Caveat).FontSize(12).FontColor(Purple);
            if (!string.IsNullOrWhiteSpace(m.DreamJobTitle))
            {
                col.Item().Text(m.DreamJobTitle!).FontFamily(PupilPassportFonts.Fredoka).FontSize(14).FontColor(Purple);
                col.Item().PaddingTop(2).Text(string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    PupilVerhaalCopy.Get("LeerlingDroom.Progress"),
                    m.DreamHaveCount)).FontSize(7).FontColor(Muted);
                col.Item().PaddingTop(4).Row(r =>
                {
                    r.RelativeItem().Column(left =>
                    {
                        left.Item().Text("✓ " + PupilVerhaalCopy.Get("LeerlingDroom.HaveTitle")).SemiBold().FontSize(8);
                        foreach (var h in m.DreamHaveItems.Take(3))
                        {
                            left.Item().Text("• " + h).FontSize(7.5f);
                        }
                    });
                    r.RelativeItem().Column(right =>
                    {
                        right.Item().Text("→ " + PupilVerhaalCopy.Get("LeerlingDroom.NeedTitle")).SemiBold().FontSize(8);
                        foreach (var n in m.DreamLearnItems.Take(2))
                        {
                            right.Item().Text("• " + n).FontSize(7.5f);
                        }
                    });
                });
                col.Item().PaddingTop(4).Element(x => Route(x, m));
                if (!string.IsNullOrWhiteSpace(m.AltRouteNote))
                {
                    col.Item().PaddingTop(2).Text(m.AltRouteNote!).FontSize(7).FontColor(Muted);
                }
            }
        });
    }

    private static void Route(IContainer c, PupilPassportPdfModel m)
    {
        c.Row(row =>
        {
            var steps = m.RouteSteps.Take(4).ToList();
            for (var i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                row.RelativeItem().Column(col =>
                {
                    col.Item().AlignCenter().Width(16).Height(16).Background(step.IsGoal ? Color.FromHex("#f5503a") : Colors.White)
                        .Border(1.5f).BorderColor(Purple).AlignCenter().Text(step.IsGoal ? "★" : (i + 1).ToString())
                        .FontSize(8).FontColor(step.IsGoal ? Colors.White : Purple);
                    col.Item().PaddingTop(2).AlignCenter().Text(step.Title).FontSize(7).SemiBold();
                    if (!string.IsNullOrWhiteSpace(step.Detail))
                    {
                        col.Item().AlignCenter().Text(step.Detail).FontSize(6.5f).FontColor(Muted);
                    }
                });
            }
        });
    }

    private static void LikesBlock(IContainer c, PupilPassportPdfModel m)
    {
        c.Column(col =>
        {
            col.Item().Background(Mint).Padding(8).Column(y =>
            {
                y.Item().Text(PupilPassportCopy.Get("LeerlingPassport.Section.Likes"))
                    .FontFamily(PupilPassportFonts.Fredoka).FontSize(11).FontColor(Color.FromHex("#11774f"));
                y.Item().PaddingTop(4).Text(string.Join("  ·  ", m.Likes.Take(8))).FontSize(8);
                if (!string.IsNullOrWhiteSpace(m.LikeOwnWord))
                {
                    y.Item().PaddingTop(4).Text(PupilPassportCopy.Get("LeerlingPassport.OwnWord") + " " + m.LikeOwnWord)
                        .FontFamily(PupilPassportFonts.Caveat).FontSize(12);
                }
            });
            col.Item().PaddingTop(6).Background(Peach).Padding(8).Column(n =>
            {
                n.Item().Text(PupilPassportCopy.Get("LeerlingPassport.Section.Dislikes"))
                    .FontFamily(PupilPassportFonts.Fredoka).FontSize(11).FontColor(Color.FromHex("#c63d28"));
                n.Item().PaddingTop(4).Text(string.Join("  ·  ", m.Dislikes.Take(6))).FontSize(8);
                if (!string.IsNullOrWhiteSpace(m.DislikeOwnWord))
                {
                    n.Item().PaddingTop(4).Text(PupilPassportCopy.Get("LeerlingPassport.OwnWord") + " " + m.DislikeOwnWord)
                        .FontFamily(PupilPassportFonts.Caveat).FontSize(12);
                }
            });
        });
    }

    private static void Strengths(IContainer c, PupilPassportPdfModel m)
    {
        c.Column(col =>
        {
            col.Item().Text(PupilPassportCopy.Get("LeerlingPassport.Section.Strengths"))
                .FontFamily(PupilPassportFonts.Fredoka).FontSize(12).FontColor(Purple);
            col.Item().PaddingTop(4).Row(row =>
            {
                foreach (var s in m.Strengths)
                {
                    row.RelativeItem().PaddingHorizontal(2).Background(Color.FromHex("#f7f4ff")).Padding(6).Column(x =>
                    {
                        x.Item().Text(s.Emoji + " " + s.Title).SemiBold().FontSize(8.5f);
                        x.Item().Text(s.Subtitle).FontSize(7.5f).FontColor(Muted);
                    });
                }
            });
        });
    }

    private static void Cards(IContainer c, PupilPassportPdfModel m)
    {
        c.Column(col =>
        {
            col.Item().Text(PupilPassportCopy.Get("LeerlingPassport.Section.Cards"))
                .FontFamily(PupilPassportFonts.Fredoka).FontSize(12).FontColor(Purple);
            col.Item().Text(PupilPassportCopy.Get("LeerlingPassport.Cards.Sub")).FontSize(7).FontColor(Muted);
            col.Item().PaddingTop(4).Row(row =>
            {
                foreach (var card in m.OccupationCards)
                {
                    row.RelativeItem().PaddingHorizontal(2).Border(1).BorderColor(Color.FromHex("#dcc8f4")).Padding(6)
                        .Column(x =>
                        {
                            x.Item().Text(card.Emoji + " " + card.DomainLabel.ToUpperInvariant()).FontSize(6.5f).FontColor(Muted);
                            x.Item().Text(card.Title).SemiBold().FontSize(9);
                            x.Item().PaddingTop(2).Text(card.Blurb).FontSize(7).LineHeight(1.25f);
                            x.Item().PaddingTop(2).Text(PupilPassportCopy.Get("LeerlingPassport.Cards.Fit") + ": " +
                                                      string.Join(", ", card.FitTags)).FontSize(7).FontColor(Purple);
                        });
                }
            });
        });
    }

    private static void Discover(IContainer c, PupilPassportPdfModel m)
    {
        c.Background(Color.FromHex("#f5effd")).Padding(8).Column(col =>
        {
            col.Item().Text(PupilPassportCopy.Get("LeerlingPassport.Section.Discover"))
                .FontFamily(PupilPassportFonts.Fredoka).FontSize(10).FontColor(Purple);
            foreach (var q in m.DiscoveryQuestions)
            {
                col.Item().PaddingTop(2).Text("☐ " + q).FontSize(7.5f);
            }
        });
    }

    private static void Tip(IContainer c, PupilPassportPdfModel m)
    {
        c.Background(Color.FromHex("#fff8e6")).Padding(8).Column(col =>
        {
            col.Item().Text(PupilPassportCopy.Get("LeerlingPassport.Section.Tip"))
                .FontFamily(PupilPassportFonts.Fredoka).FontSize(10).FontColor(Purple);
            foreach (var t in m.HomeClassTips)
            {
                col.Item().PaddingTop(2).Text(t).FontSize(7.5f).LineHeight(1.3f);
            }
        });
    }

    private static void Footer(IContainer c, PupilPassportPdfModel m)
    {
        c.Row(row =>
        {
            row.RelativeItem().Text(m.FooterPrivacy).FontSize(6.5f).FontColor(Muted).LineHeight(1.3f);
            row.ConstantItem(60).AlignRight().Column(col =>
            {
                col.Item().AlignRight().Text("lobsy.nl").FontColor(Purple).SemiBold();
                col.Item().AlignRight().Text("pagina 1/1").FontSize(6.5f).FontColor(Muted);
            });
        });
    }
}
