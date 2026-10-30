using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.PupilPassport;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Jobsy.Infrastructure.Scholen.Passport;

internal static class PupilPassportGroep78PdfRenderer
{
    private static string T(PupilPassportPdfModel m, string key)
        => PupilPassportLocalizedCopy.Get(key, m.Language);

    private static readonly Color Purple = Color.FromHex("#5b2a9a");
    private static readonly Color Ink = Color.FromHex("#3a2d52");
    private static readonly Color Muted = Color.FromHex("#8d83a0");
    private static readonly Color Mint = Color.FromHex("#e6f8ef");
    private static readonly Color Peach = Color.FromHex("#fff0ea");
    private static readonly Color Coral = Color.FromHex("#f5503a");

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
                page.PageColor(Color.FromHex("#fffefb"));
                page.MarginHorizontal(24);
                page.MarginVertical(10);
                page.DefaultTextStyle(x => x.FontSize(9f).FontColor(Ink).FontFamily(PupilPassportFonts.Nunito));

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
                    col.Item().PaddingTop(8).Element(c => Cards(c, m));
                    col.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().PaddingRight(5).MinHeight(102).Element(c => Discover(c, m));
                        row.RelativeItem().MinHeight(102).Element(c => Tip(c, m));
                    });
                    col.Item().PaddingTop(10).Element(c => Footer(c, m));
                });
            });
        });

    private static void Header(IContainer c, PupilPassportPdfModel m)
    {
        var dateCulture = PupilVerhaalLocalized.IsEnglish(m.Language)
            ? System.Globalization.CultureInfo.GetCultureInfo("en-GB")
            : System.Globalization.CultureInfo.GetCultureInfo("nl-NL");
        c.Background(Purple).Padding(10).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("LOBSY").FontFamily(PupilPassportFonts.Fredoka).FontSize(8).FontColor(Colors.White).LetterSpacing(0.12f);
                col.Item().Text(T(m, "LeerlingPassport.Header.G78"))
                    .FontFamily(PupilPassportFonts.Fredoka).FontSize(16).FontColor(Colors.White);
            });
            row.ConstantItem(100).AlignMiddle().Column(col =>
            {
                col.Item().AlignRight().Text(m.ClassLabel).FontSize(8f).FontColor(Colors.White);
                col.Item().AlignRight().Text(m.SchoolName).FontSize(7.5f).FontColor(Colors.White).LineHeight(1.15f);
                col.Item().AlignRight().Text(m.Date.ToString("d MMM yyyy", dateCulture)).FontSize(7f).FontColor(Colors.White);
                col.Item().AlignRight().Text(m.DisplayCode).FontFamily(Fonts.CourierNew).FontSize(7.5f).FontColor(Colors.White);
            });
        });
    }

    private static void NameLine(IContainer c, PupilPassportPdfModel m)
    {
        c.Row(row =>
        {
            row.AutoItem().AlignBottom().Text(T(m, "LeerlingPassport.NameIntro") + " ")
                .FontFamily(PupilPassportFonts.Fredoka).FontSize(10).FontColor(Purple);
            if (!string.IsNullOrWhiteSpace(m.DisplayNameOnPdf))
            {
                row.ConstantItem(160).AlignBottom().BorderBottom(1).BorderColor(Color.FromHex("#b99be0"))
                    .Text(m.DisplayNameOnPdf!).FontFamily(PupilPassportFonts.Caveat).FontSize(20).FontColor(Purple);
            }
            else
            {
                row.ConstantItem(160).AlignBottom().BorderBottom(1).BorderColor(Color.FromHex("#b99be0")).Height(18);
            }

            row.RelativeItem().AlignBottom().PaddingLeft(4).Text(T(m, "LeerlingPassport.NamePrivacy"))
                .FontSize(6.5f).FontColor(Muted);
        });
    }

    private static void DitBenIk(IContainer c, PupilPassportPdfModel m)
    {
        c.Column(col =>
        {
            col.Item().Text(T(m, "LeerlingPassport.Section.DitBenIk"))
                .FontFamily(PupilPassportFonts.Fredoka).FontSize(14).FontColor(Purple);
            col.Item().PaddingTop(3).Row(row =>
            {
                row.RelativeItem(3).MinHeight(88).Background(Color.FromHex("#fffdf5")).Border(1).BorderColor(Color.FromHex("#f3e6bf"))
                    .Padding(8).Text(NormalizePdfLine(m.StoryBody)).LineHeight(1.35f).FontSize(9f);
                row.RelativeItem(4).PaddingLeft(4).Row(wRow =>
                {
                    foreach (var w in m.Worlds)
                    {
                        wRow.RelativeItem().PaddingHorizontal(1).Element(x => WorldCard(x, w));
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
        c.Background(bg).Padding(3).Column(col =>
        {
            col.Item().AlignCenter().Width(22).Height(22).Svg(PupilPassportIcons.Icon(PupilPassportIcons.WorldIconKey(w.WorldNumber), Purple, 12));
            col.Item().AlignCenter().Text(w.Title).FontFamily(PupilPassportFonts.Fredoka).FontSize(7f).FontColor(Purple);
            col.Item().AlignCenter().Text(w.Subtitle).FontSize(6f).FontColor(Muted);
            col.Item().AlignCenter().Text(w.Value).FontSize(6.5f).Bold().LineHeight(1.15f);
        });
    }

    private static void DreamBlock(IContainer c, PupilPassportPdfModel m)
    {
        c.Border(2).BorderColor(Purple).Padding(8).Column(col =>
        {
            col.Item().Text(T(m, "LeerlingPassport.Section.Droom"))
                .FontFamily(PupilPassportFonts.Caveat).FontSize(13).FontColor(Purple);
            if (!string.IsNullOrWhiteSpace(m.DreamJobTitle))
            {
                col.Item().Text(m.DreamJobTitle!).FontFamily(PupilPassportFonts.Fredoka).FontSize(15).FontColor(Purple);
                col.Item().PaddingTop(3).Row(r =>
                {
                    r.RelativeItem().Column(left =>
                    {
                        left.Item().Row(h =>
                        {
                            h.ConstantItem(9).Svg(PupilPassportIcons.Icon("check", Color.FromHex("#16865a"), 9));
                            h.RelativeItem().Text(T(m, "LeerlingDroom.HaveTitle")).Bold().FontSize(7.5f);
                        });
                        foreach (var h in m.DreamHaveItems.Take(3))
                        {
                            left.Item().Text("  " + NormalizePdfLine(h)).FontSize(7f);
                        }
                    });
                    r.RelativeItem().Column(right =>
                    {
                        right.Item().Row(h =>
                        {
                            h.ConstantItem(9).Svg(PupilPassportIcons.Icon("arrow", Color.FromHex("#d9452f"), 9));
                            h.RelativeItem().Text(T(m, "LeerlingDroom.NeedTitle")).Bold().FontSize(7.5f);
                        });
                        foreach (var n in m.DreamLearnItems.Take(2))
                        {
                            right.Item().Text("  " + NormalizePdfLine(n)).FontSize(7f);
                        }
                    });
                });
                col.Item().PaddingTop(3).Element(x => Route(x, m));
                if (!string.IsNullOrWhiteSpace(m.AltRouteNote))
                {
                    col.Item().PaddingTop(2).Text(m.AltRouteNote!).FontSize(6.5f).FontColor(Muted);
                }
            }
        });
    }

    private static void Route(IContainer c, PupilPassportPdfModel m)
    {
        var steps = m.RouteSteps.Take(4).ToList();
        if (steps.Count == 0)
        {
            return;
        }

        c.Height(68).Layers(layers =>
        {
            layers.Layer().PaddingTop(16).Height(40).Svg(PupilPassportIcons.RoutePathSvg("#d3c0ee"));
            layers.PrimaryLayer().PaddingTop(4).Row(row =>
            {
                for (var i = 0; i < steps.Count; i++)
                {
                    var step = steps[i];
                    var shortTitle = RouteShortTitle(step.Title, m.Language);
                    var detail = RouteShortDetail(step.Detail);
                    row.RelativeItem().Column(x =>
                    {
                        x.Item().AlignCenter().Width(18).Height(18).Background(step.IsGoal ? Coral : Colors.White)
                            .Border(2f).BorderColor(Purple).AlignCenter().AlignMiddle().Element(inner =>
                            {
                                if (step.IsGoal)
                                {
                                    inner.Svg(PupilPassportIcons.Icon("star", Colors.White, 9));
                                }
                                else
                                {
                                    var phaseKey = i switch
                                    {
                                        0 => "LeerlingPassport.Route.Phase.Now",
                                        1 => "LeerlingPassport.Route.Phase.Next",
                                        _ => "LeerlingPassport.Route.Phase.Later"
                                    };
                                    inner.Text(T(m, phaseKey)[..1]).FontSize(7).FontColor(Purple).SemiBold();
                                }
                            });
                        x.Item().PaddingTop(3).AlignCenter().Text(shortTitle).FontSize(8.5f).Bold().LineHeight(1.15f);
                        if (!string.IsNullOrWhiteSpace(detail))
                        {
                            x.Item().AlignCenter().Text(detail).FontSize(7.5f).FontColor(Muted).LineHeight(1.15f);
                        }
                    });
                }
            });
        });
    }

    private static string RouteShortTitle(string title, string language)
    {
        var part = title.Split('·', 2, StringSplitOptions.TrimEntries)[0];
        var colon = part.IndexOf(':');
        if (colon >= 0 && colon < part.Length - 1)
        {
            part = part[(colon + 1)..].Trim();
        }

        var dash = part.IndexOf(" - ", StringComparison.Ordinal);
        if (dash > 0)
        {
            part = part[..dash].Trim();
        }

        part = part.TrimEnd('!', '.');
        part = PupilPassportLocalizedCopy.RouteStepTitle(part, language);
        part = part.TrimEnd('!', '.');

        return part;
    }

    private static string? RouteShortDetail(string? detail)
    {
        if (string.IsNullOrWhiteSpace(detail))
        {
            return null;
        }

        var line = NormalizePdfLine(detail.Split('·', 2, StringSplitOptions.TrimEntries)[0]);
        return line.Length > 42 ? line[..42].TrimEnd() + "…" : line;
    }

    private static string NormalizePdfLine(string text)
        => text
            .Replace('\u2013', '-')
            .Replace('\u2014', '-')
            .Replace('\u2212', '-');

    private static void LikesBlock(IContainer c, PupilPassportPdfModel m)
    {
        c.Column(col =>
        {
            col.Item().Background(Mint).MinHeight(70).Padding(8).Column(y =>
            {
                y.Item().Text(T(m, "LeerlingPassport.Section.Likes"))
                    .FontFamily(PupilPassportFonts.Fredoka).FontSize(11).FontColor(Color.FromHex("#11774f"));
                y.Item().PaddingTop(3).Text(string.Join(" · ", m.Likes.Take(8))).FontSize(8.5f);
                if (!string.IsNullOrWhiteSpace(m.LikeOwnWord))
                {
                    y.Item().PaddingTop(2).Text(T(m, "LeerlingPassport.OwnWord") + " " + m.LikeOwnWord)
                        .FontFamily(PupilPassportFonts.Caveat).FontSize(11);
                }
            });
            col.Item().PaddingTop(4).Background(Peach).MinHeight(70).Padding(8).Column(n =>
            {
                n.Item().Text(T(m, "LeerlingPassport.Section.Dislikes"))
                    .FontFamily(PupilPassportFonts.Fredoka).FontSize(11).FontColor(Color.FromHex("#c63d28"));
                n.Item().PaddingTop(3).Text(string.Join(" · ", m.Dislikes.Take(6))).FontSize(8.5f);
                if (!string.IsNullOrWhiteSpace(m.DislikeOwnWord))
                {
                    n.Item().PaddingTop(2).Text(T(m, "LeerlingPassport.OwnWord") + " " + m.DislikeOwnWord)
                        .FontFamily(PupilPassportFonts.Caveat).FontSize(11);
                }
            });
        });
    }

    private static void Strengths(IContainer c, PupilPassportPdfModel m)
    {
        c.Column(col =>
        {
            col.Item().Text(T(m, "LeerlingPassport.Section.Strengths"))
                .FontFamily(PupilPassportFonts.Fredoka).FontSize(13).FontColor(Purple);
            col.Item().PaddingTop(4).Row(row =>
            {
                foreach (var s in m.Strengths)
                {
                    var iconColor = s.IconKey == "heart" ? Coral : Purple;
                    row.RelativeItem().PaddingHorizontal(2).Background(Color.FromHex("#f7f4ff")).Padding(7).Row(x =>
                    {
                        x.ConstantItem(20).Svg(PupilPassportIcons.Icon(s.IconKey, iconColor, 14));
                        x.RelativeItem().PaddingLeft(4).Column(t =>
                        {
                            t.Item().Text(s.Title).Bold().FontSize(7.5f).LineHeight(1.15f);
                            t.Item().Text(s.Subtitle).FontSize(7f).FontColor(Muted).ClampLines(2);
                        });
                    });
                }
            });
        });
    }

    private static void Cards(IContainer c, PupilPassportPdfModel m)
    {
        c.Column(col =>
        {
            col.Item().Text(T(m, "LeerlingPassport.Section.Cards"))
                .FontFamily(PupilPassportFonts.Fredoka).FontSize(13).FontColor(Purple);
            col.Item().Text(T(m, "LeerlingPassport.Cards.Sub")).FontSize(7.5f).FontColor(Muted);
            col.Item().PaddingTop(4).Row(row =>
            {
                foreach (var card in m.OccupationCards)
                {
                    row.RelativeItem().PaddingHorizontal(2).MinHeight(112).Border(1).BorderColor(Color.FromHex("#dcc8f4")).Padding(8)
                        .Column(x =>
                        {
                            x.Item().AlignCenter().Height(32).Svg(PupilPassportIcons.Icon(card.IconKey, Purple, 24));
                            x.Item().Text(card.DomainLabel.ToUpperInvariant()).FontSize(6.5f).FontColor(Muted);
                            x.Item().Text(card.Title).Bold().FontSize(9.5f).LineHeight(1.15f);
                            x.Item().PaddingTop(2).Text(card.Blurb).FontSize(7.2f).LineHeight(1.3f);
                            x.Item().PaddingTop(1).Text(T(m, "LeerlingPassport.Cards.Fit") + ": " +
                                                      string.Join(", ", card.FitTags)).FontSize(6.5f).FontColor(Purple);
                        });
                }
            });
        });
    }

    private static void Discover(IContainer c, PupilPassportPdfModel m)
    {
        c.Background(Color.FromHex("#f5effd")).Padding(10).Column(col =>
        {
            col.Item().Text(T(m, "LeerlingPassport.Section.Discover"))
                .FontFamily(PupilPassportFonts.Fredoka).FontSize(10).FontColor(Purple);
            foreach (var q in m.DiscoveryQuestions)
            {
                col.Item().PaddingTop(4).Row(r =>
                {
                    r.ConstantItem(11).Height(11).Border(1).BorderColor(Color.FromHex("#9c78d2"));
                    r.RelativeItem().PaddingLeft(4).Text(q).FontSize(8f).LineHeight(1.25f);
                });
            }
        });
    }

    private static void Tip(IContainer c, PupilPassportPdfModel m)
    {
        c.Row(row =>
        {
            row.ConstantItem(52).AlignBottom().Element(box =>
            {
                var bytes = PupilPassportAssets.MascotPng();
                if (bytes.Length > 0)
                {
                    box.MaxHeight(52).Image(bytes).FitHeight();
                }
            });
            row.RelativeItem().Background(Color.FromHex("#fff8e6")).Padding(10).Column(col =>
            {
                col.Item().Text(T(m, "LeerlingPassport.Section.Tip"))
                    .FontFamily(PupilPassportFonts.Fredoka).FontSize(10).FontColor(Color.FromHex("#a8640c"));
                foreach (var t in m.HomeClassTips)
                {
                    col.Item().PaddingTop(4).Text(t).FontSize(8f).LineHeight(1.3f);
                }
            });
        });
    }

    private static void Footer(IContainer c, PupilPassportPdfModel m)
    {
        c.Row(row =>
        {
            row.ConstantItem(16).Svg(PupilPassportIcons.Icon("shield", Color.FromHex("#16865a"), 12));
            row.RelativeItem().PaddingLeft(4).Text(m.FooterPrivacy).FontSize(6f).FontColor(Muted).LineHeight(1.25f);
            row.ConstantItem(55).AlignRight().Column(col =>
            {
                col.Item().AlignRight().Text("lobsy.nl").FontColor(Purple).SemiBold();
                col.Item().AlignRight().Text(T(m, "LeerlingPassport.Footer.PageLabel")).FontSize(6f).FontColor(Muted);
            });
        });
    }
}
