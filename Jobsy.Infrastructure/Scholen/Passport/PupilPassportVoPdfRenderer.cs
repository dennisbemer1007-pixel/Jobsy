using Jobsy.Core.Scholen.PupilPassport;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Jobsy.Infrastructure.Scholen.Passport;

internal static class PupilPassportVoPdfRenderer
{
    private static readonly Color Purple = Color.FromHex("#5b2a9a");
    private static readonly Color Ink = Color.FromHex("#2f2445");
    private static readonly Color Muted = Color.FromHex("#6b6280");
    private static readonly Color Line = Color.FromHex("#e8e2f0");
    private static readonly Color Coral = Color.FromHex("#f5503a");
    private static readonly Color SoftPeach = Color.FromHex("#fff5f0");
    private static readonly Color SoftPurple = Color.FromHex("#f5effd");

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
                page.MarginHorizontal(22);
                page.MarginVertical(18);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontColor(Ink).FontFamily(PupilPassportFonts.Nunito));
                page.Content().Column(col =>
                {
                    col.Spacing(5);
                    col.Item().Element(c => Top(c, m));
                    col.Item().Height(3).Svg(GradientRule());
                    col.Item().Element(c => Identity(c, m));
                    col.Item().Element(c => InfoCards(c, m));
                    col.Item().Element(c => Interests(c, m));
                    col.Item().Element(c => Dna(c, m));
                    col.Item().Element(c => Strengths(c, m));
                    if (m.SectorFits is { Count: > 0 })
                    {
                        col.Item().Element(x => Sectors(x, m));
                    }

                    if (m.SideJobTypes is { Count: > 0 })
                    {
                        col.Item().Element(x => SideJobs(x, m));
                    }

                    col.Item().Element(c => Dream(c, m));
                    col.Item().Element(x => LearnGoals(x, m));
                    col.Item().Element(x => Quote(x, m));
                    col.Item().Element(c => Footer(c, m));
                });
            });
        });

    private static string GradientRule()
        => """<svg xmlns="http://www.w3.org/2000/svg" width="100%" height="3"><defs><linearGradient id="g" x1="0" y1="0" x2="1" y2="0"><stop offset="0%" stop-color="#5b2a9a"/><stop offset="50%" stop-color="#c94aa8"/><stop offset="100%" stop-color="#f5503a"/></linearGradient></defs><rect width="100%" height="3" fill="url(#g)" rx="1.5"/></svg>""";

    private static void Top(IContainer c, PupilPassportPdfModel m)
    {
        c.Row(row =>
        {
            var logo = PupilPassportAssets.LogoPng();
            if (logo.Length > 0)
            {
                row.ConstantItem(32).Height(32).Image(logo).FitArea();
            }

            row.RelativeItem().PaddingLeft(6).Column(col =>
            {
                col.Item().Text("Lobsy").FontSize(14).SemiBold().FontColor(Purple);
                col.Item().Text(PupilPassportCopy.Get("LeerlingPassport.Header.Vo")).FontSize(8.5f).FontColor(Muted);
            });
            row.ConstantItem(130).AlignRight().Background(SoftPurple).Padding(6).Row(badge =>
            {
                badge.ConstantItem(16).AlignMiddle().Svg(PupilPassportIcons.Icon("check", Purple, 14));
                badge.RelativeItem().PaddingLeft(4).Column(col =>
                {
                    col.Item().Text($"{m.WorldsCompleted}/{m.WorldsTotal} werelden afgerond").SemiBold().FontSize(8);
                    col.Item().Text($"Lobsy voor scholen · {m.Date:dd MMM yyyy}").FontSize(7).FontColor(Muted);
                });
            });
        });
    }

    private static void Identity(IContainer c, PupilPassportPdfModel m)
    {
        c.Row(row =>
        {
            if (!string.IsNullOrWhiteSpace(m.DisplayNameOnPdf))
            {
                row.ConstantItem(40).Height(40).Background(Color.FromHex("#7a2a9e")).AlignCenter().AlignMiddle()
                    .Text(Initials(m.DisplayNameOnPdf!)).FontColor(Colors.White).SemiBold().FontSize(12);
            }
            else
            {
                row.ConstantItem(40).Height(40).Border(1).BorderColor(Line);
            }

            row.RelativeItem().PaddingLeft(8).Column(col =>
            {
                if (!string.IsNullOrWhiteSpace(m.DisplayNameOnPdf))
                {
                    col.Item().Text(m.DisplayNameOnPdf!).FontSize(16).SemiBold();
                }
                else
                {
                    col.Item().Text(PupilPassportCopy.Get("LeerlingPassport.Vo.NamePrompt")).FontSize(9).FontColor(Muted);
                    col.Item().PaddingTop(4).BorderBottom(1).BorderColor(Purple).Height(16);
                }

                if (!string.IsNullOrWhiteSpace(m.ArchetypeTagline))
                {
                    col.Item().PaddingTop(2).Text(m.ArchetypeTagline!).FontSize(9).FontColor(Purple);
                }

                col.Item().PaddingTop(4).Row(pills =>
                {
                    void Pill(string text, bool outline = false)
                    {
                        pills.AutoItem().PaddingRight(4).PaddingBottom(2)
                            .Background(outline ? Colors.White : SoftPurple).Border(1).BorderColor(outline ? Line : Color.FromHex("#dcc8f4"))
                            .PaddingHorizontal(6).PaddingVertical(2).Text(text).FontSize(7f).FontColor(Ink);
                    }

                    if (!string.IsNullOrWhiteSpace(m.LevelLine))
                    {
                        Pill(m.LevelLine!);
                    }

                    if (!string.IsNullOrWhiteSpace(m.ProfileLine))
                    {
                        Pill(m.ProfileLine!);
                    }

                    if (!string.IsNullOrWhiteSpace(m.LocationLine))
                    {
                        Pill(m.LocationLine!, outline: true);
                    }

                    Pill("Code " + m.DisplayCode, outline: true);
                });
            });
        });
    }

    private static string Initials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return "?";
        }

        if (parts.Length == 1)
        {
            return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();
        }

        return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[^1][0])}";
    }

    private static void InfoCards(IContainer c, PupilPassportPdfModel m)
    {
        c.Row(row =>
        {
            row.RelativeItem().Padding(2).Element(x => MiniCard(x, "Niveau", m.LevelLine ?? "—", m.LevelInfoSubline, SoftPeach));
            row.RelativeItem().Padding(2).Element(x => MiniCard(x, "Leukste vakken",
                m.FavoriteSubjects is { Count: > 0 } ? string.Join(" · ", m.FavoriteSubjects) : "—",
                m.FavoriteSubjectsSubline, SoftPurple));
            row.RelativeItem().Padding(2).Element(x => MiniCard(x, "Bijbaan",
                m.SideJobAvailabilityLine ?? "—", m.SideJobInfoSubline, SoftPeach));
            row.RelativeItem().Padding(2).Element(x => MiniCard(x, "Vervoer",
                m.TravelLine ?? "—", m.TravelInfoSubline, SoftPurple));
        });
    }

    private static void MiniCard(IContainer c, string title, string body, string? subline, Color bg)
    {
        c.Background(bg).Border(1).BorderColor(Line).Padding(6).Column(col =>
        {
            col.Item().Text(title.ToUpperInvariant()).FontSize(6.5f).FontColor(Muted).LetterSpacing(0.06f);
            col.Item().PaddingTop(2).Text(body).FontSize(8.5f).SemiBold();
            if (!string.IsNullOrWhiteSpace(subline))
            {
                col.Item().Text(subline!).FontSize(7).FontColor(Muted);
            }
        });
    }

    private static void Interests(IContainer c, PupilPassportPdfModel m)
    {
        c.Border(1).BorderColor(Line).Padding(8).Column(col =>
        {
            col.Item().Text(PupilPassportCopy.Get("LeerlingPassport.Vo.Section.Interests")).SemiBold().FontSize(10);
            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Column(y =>
                {
                    y.Item().Text(PupilPassportCopy.Get("LeerlingPassport.Vo.Likes")).FontSize(8).SemiBold().FontColor(Purple);
                    y.Item().PaddingTop(2).Text(string.Join("  ·  ", m.Likes)).FontSize(8);
                });
                row.RelativeItem().Column(n =>
                {
                    n.Item().Text(PupilPassportCopy.Get("LeerlingPassport.Vo.Dislikes")).FontSize(8).SemiBold().FontColor(Coral);
                    n.Item().PaddingTop(2).Text(string.Join("  ·  ", m.Dislikes)).FontSize(8);
                });
            });
        });
    }

    private static void Dna(IContainer c, PupilPassportPdfModel m)
    {
        c.Border(1).BorderColor(Line).Padding(8).Column(col =>
        {
            col.Item().Text(PupilPassportCopy.Get("LeerlingPassport.Vo.DnaTitle")).SemiBold().FontSize(10);
            col.Item().PaddingTop(4).Column(dnaCol =>
            {
                for (var row = 0; row < 2; row++)
                {
                    dnaCol.Item().Row(dnaRow =>
                    {
                        for (var colIdx = 0; colIdx < 2; colIdx++)
                        {
                            var i = row * 2 + colIdx;
                            if (i >= m.DnaTiles.Count)
                            {
                                continue;
                            }

                            var tile = m.DnaTiles[i];
                            var bg = i is 1 or 2 ? SoftPurple : SoftPeach;
                            dnaRow.RelativeItem().Padding(2).Background(bg).Padding(6).Column(x =>
                            {
                                x.Item().Row(r =>
                                {
                                    r.ConstantItem(14).Svg(PupilPassportIcons.Icon(PupilPassportIcons.DnaLayerIcon(i), Purple, 12));
                                    r.RelativeItem().PaddingLeft(3).Text(tile.Label.ToUpperInvariant()).FontSize(6.5f).FontColor(Muted);
                                });
                                x.Item().PaddingTop(2).Text(tile.Value).FontSize(8.5f).SemiBold().LineHeight(1.2f);
                            });
                        }
                    });
                }
            });
        });
    }

    private static void Strengths(IContainer c, PupilPassportPdfModel m)
    {
        c.Column(col =>
        {
            col.Item().Row(h =>
            {
                h.ConstantItem(18).Svg(PupilPassportIcons.Icon("star", Purple, 14));
                h.RelativeItem().PaddingLeft(4).Text(PupilPassportCopy.Get("LeerlingPassport.Vo.Strengths")).SemiBold().FontSize(10);
            });
            col.Item().PaddingTop(4).Row(row =>
            {
                foreach (var s in m.Strengths)
                {
                    row.RelativeItem().Padding(2).Background(SoftPurple).Border(1).BorderColor(Line).Padding(6).Column(x =>
                    {
                        x.Item().Row(r =>
                        {
                            r.ConstantItem(12).Svg(PupilPassportIcons.Icon(s.IconKey, Purple, 11));
                            r.RelativeItem().PaddingLeft(3).Text(s.Title).SemiBold().FontSize(8.5f);
                        });
                        x.Item().PaddingTop(2).Text(s.Subtitle).FontSize(7.5f).FontColor(Muted).LineHeight(1.2f);
                    });
                }
            });
        });
    }

    private static void Sectors(IContainer c, PupilPassportPdfModel m)
    {
        c.Column(col =>
        {
            col.Item().Row(h =>
            {
                h.ConstantItem(18).Svg(PupilPassportIcons.Icon("arrow", Purple, 14));
                h.RelativeItem().PaddingLeft(4).Text(PupilPassportCopy.Get("LeerlingPassport.Vo.Sectors")).SemiBold().FontSize(10);
            });
            col.Item().PaddingTop(4).Row(row =>
            {
                foreach (var s in m.SectorFits!)
                {
                    row.RelativeItem().Padding(2).Border(1).BorderColor(Line).Padding(6).Column(x =>
                    {
                        x.Item().Text($"{s.Rank}. {s.Title}").SemiBold().FontSize(8.5f);
                        foreach (var b in s.Bullets.Take(3))
                        {
                            x.Item().PaddingTop(2).Row(r =>
                            {
                                r.ConstantItem(6).Svg(PupilPassportIcons.Icon("bullet", Coral, 5));
                                r.RelativeItem().Text(b).FontSize(7).LineHeight(1.25f);
                            });
                        }
                    });
                }
            });
        });
    }

    private static void SideJobs(IContainer c, PupilPassportPdfModel m)
    {
        c.Column(col =>
        {
            col.Item().Row(h =>
            {
                h.ConstantItem(18).Svg(PupilPassportIcons.Icon("brief", Purple, 14));
                h.RelativeItem().PaddingLeft(4).Column(x =>
                {
                    x.Item().Text(PupilPassportCopy.Get("LeerlingPassport.Vo.SideJobs")).SemiBold().FontSize(10);
                    x.Item().Text(PupilPassportCopy.Get("LeerlingPassport.Vo.SideJobs.Sub")).FontSize(7).FontColor(Muted);
                });
            });
            col.Item().PaddingTop(4).Row(row =>
            {
                foreach (var job in m.SideJobTypes!)
                {
                    row.RelativeItem().Padding(2).Border(1).BorderColor(Line).Padding(6).Column(x =>
                    {
                        x.Item().Text(job.Title).SemiBold().FontSize(8.5f);
                        x.Item().Text(job.Subtitle).FontSize(7).FontColor(Muted);
                        if (job.MetaPills is { Count: > 0 })
                        {
                            x.Item().PaddingTop(3).Row(pills =>
                            {
                                foreach (var pill in job.MetaPills)
                                {
                                    pills.AutoItem().PaddingRight(3).Background(Color.FromHex("#f5f3f8"))
                                        .PaddingHorizontal(5).PaddingVertical(2).Text(pill).FontSize(7).SemiBold();
                                }
                            });
                        }

                        x.Item().PaddingTop(3).Text(text =>
                        {
                            text.Span(PupilPassportCopy.Get("LeerlingPassport.Vo.SideJob.Meta.WhyPrefix") + " ")
                                .SemiBold().FontColor(Purple);
                            text.Span(job.Why).FontSize(7).LineHeight(1.25f);
                        });
                    });
                }
            });
        });
    }

    private static void Dream(IContainer c, PupilPassportPdfModel m)
    {
        c.Border(1).BorderColor(Line).Padding(8).Column(col =>
        {
            col.Item().Text(PupilPassportCopy.Get("LeerlingPassport.Vo.Dream")).SemiBold().FontSize(10);
            if (!string.IsNullOrWhiteSpace(m.DreamJobTitle))
            {
                col.Item().Text(m.DreamJobTitle!).FontSize(12).SemiBold().FontColor(Purple);
            }

            col.Item().PaddingTop(4).Column(col2 =>
            {
                var i = 1;
                foreach (var step in m.RouteSteps.Take(4))
                {
                    col2.Item().PaddingBottom(3).Row(r =>
                    {
                        var circle = r.ConstantItem(18).Height(18).Border(2).BorderColor(step.IsGoal ? Coral : Purple)
                            .Background(step.IsGoal ? Coral : Colors.White).AlignCenter().AlignMiddle();
                        if (step.IsGoal)
                        {
                            circle.Svg(PupilPassportIcons.Icon("star", Colors.White, 10));
                        }
                        else
                        {
                            circle.Text(i.ToString()).SemiBold().FontSize(8).FontColor(Purple);
                        }

                        r.RelativeItem().PaddingLeft(6).Column(t =>
                        {
                            t.Item().Text(step.Title).SemiBold().FontSize(8.5f);
                            if (!string.IsNullOrWhiteSpace(step.Detail))
                            {
                                t.Item().Text(step.Detail).FontSize(7.5f).FontColor(Muted);
                            }
                        });
                    });
                    i++;
                }
            });
        });
    }

    private static void LearnGoals(IContainer c, PupilPassportPdfModel m)
    {
        c.Column(col =>
        {
            col.Item().Row(h =>
            {
                h.ConstantItem(14).Svg(PupilPassportIcons.Icon("flag", Purple, 12));
                h.RelativeItem().PaddingLeft(4).Text(PupilPassportCopy.Get("LeerlingPassport.Vo.Learn")).SemiBold().FontSize(10);
            });
            if (m.LearningGoals is { Count: > 0 })
            {
                foreach (var g in m.LearningGoals)
                {
                    col.Item().PaddingTop(2).Row(r =>
                    {
                        r.ConstantItem(10).Svg(PupilPassportIcons.Icon("flag", Coral, 10));
                        r.RelativeItem().Text(g).FontSize(8);
                    });
                }
            }
            else
            {
                col.Item().PaddingTop(2).Text(PupilPassportCopy.Get("LeerlingPassport.Vo.Learn.Placeholder"))
                    .FontSize(8).FontColor(Muted).Italic();
            }
        });
    }

    private static void Quote(IContainer c, PupilPassportPdfModel m)
    {
        c.Border(1).BorderColor(Line).Padding(8).Row(row =>
        {
            row.ConstantItem(20).Svg(PupilPassportIcons.Icon("quote", Color.FromHex("#d9c7f0"), 18));
            row.RelativeItem().PaddingLeft(6).Column(col =>
            {
                col.Item().Text(PupilPassportCopy.Get("LeerlingPassport.Vo.OwnWords")).SemiBold().FontSize(9);
                if (!string.IsNullOrWhiteSpace(m.OwnWords))
                {
                    col.Item().PaddingTop(4).Text(m.OwnWords!).FontSize(8.5f).LineHeight(1.35f);
                }
                else
                {
                    col.Item().PaddingTop(4).Text(PupilPassportCopy.Get("LeerlingPassport.Vo.OwnWords.Placeholder"))
                        .FontSize(8.5f).FontColor(Muted).Italic();
                }

                col.Item().PaddingTop(2).Text(PupilPassportCopy.Get("LeerlingPassport.Vo.OwnWords.Note")).FontSize(7).FontColor(Muted);
            });
        });
    }

    private static void Footer(IContainer c, PupilPassportPdfModel m)
    {
        c.BorderTop(1).BorderColor(Line).PaddingTop(6).Row(row =>
        {
            row.ConstantItem(20).Height(20).Background(Color.FromHex("#e9f7f0")).AlignCenter().AlignMiddle()
                .Svg(PupilPassportIcons.Icon("shield", Color.FromHex("#14825a"), 11));
            row.RelativeItem().PaddingLeft(6).Text(m.FooterPrivacy).FontSize(6.5f).FontColor(Muted).LineHeight(1.3f);
            row.ConstantItem(60).AlignRight().Text("pagina 1/1\nlobsy.nl").FontSize(6.5f).FontColor(Muted);
        });
    }
}
