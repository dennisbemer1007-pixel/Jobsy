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
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontColor(Ink).FontFamily(PupilPassportFonts.Nunito));
                page.Content().Column(col =>
                {
                    col.Item().Element(c => Top(c, m));
                    col.Item().PaddingVertical(6).LineHorizontal(1).LineColor(Line);
                    col.Item().Element(c => Identity(c, m));
                    col.Item().PaddingTop(6).Element(c => InfoCards(c, m));
                    col.Item().PaddingTop(8).Element(c => Interests(c, m));
                    col.Item().PaddingTop(6).Element(c => Dna(c, m));
                    col.Item().PaddingTop(6).Element(c => Strengths(c, m));
                    if (m.SectorFits is { Count: > 0 })
                    {
                        col.Item().PaddingTop(6).Element(x => Sectors(x, m));
                    }

                    if (m.SideJobTypes is { Count: > 0 })
                    {
                        col.Item().PaddingTop(6).Element(x => SideJobs(x, m));
                    }

                    col.Item().PaddingTop(6).Element(c => Dream(c, m));
                    if (m.LearningGoals is { Count: > 0 })
                    {
                        col.Item().PaddingTop(4).Element(x => LearnGoals(x, m));
                    }

                    if (!string.IsNullOrWhiteSpace(m.OwnWords))
                    {
                        col.Item().PaddingTop(6).Element(x => Quote(x, m));
                    }

                    col.Item().PaddingTop(8).Element(c => Footer(c, m));
                });
            });
        });

    private static void Top(IContainer c, PupilPassportPdfModel m)
    {
        c.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("Lobsy").FontSize(14).SemiBold().FontColor(Purple);
                col.Item().Text(PupilPassportCopy.Get("LeerlingPassport.Header.Vo")).FontSize(9).FontColor(Muted);
            });
            row.ConstantItem(140).AlignRight().Background(Color.FromHex("#f5effd")).Padding(6).Column(col =>
            {
                col.Item().AlignRight().Text($"{m.WorldsCompleted}/{m.WorldsTotal} werelden afgerond").SemiBold().FontSize(8);
                col.Item().AlignRight().Text($"Lobsy voor scholen · {m.Date:dd MMM yyyy}").FontSize(7).FontColor(Muted);
            });
        });
    }

    private static void Identity(IContainer c, PupilPassportPdfModel m)
    {
        c.Row(row =>
        {
            if (!string.IsNullOrWhiteSpace(m.DisplayNameOnPdf))
            {
                row.ConstantItem(36).Height(36).Background(Purple).AlignCenter().AlignMiddle()
                    .Text(Initials(m.DisplayNameOnPdf!)).FontColor(Colors.White).SemiBold();
            }
            else
            {
                row.ConstantItem(36).Height(36).Border(1).BorderColor(Line);
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
                    col.Item().PaddingTop(2).BorderBottom(1).BorderColor(Line).Height(14);
                }

                if (!string.IsNullOrWhiteSpace(m.ArchetypeTagline))
                {
                    col.Item().PaddingTop(2).Text(m.ArchetypeTagline!).FontSize(9).FontColor(Purple);
                }

                col.Item().PaddingTop(4).Text(string.Join("   ·   ", new[]
                {
                    m.LevelLine,
                    m.ProfileLine,
                    m.LocationLine,
                    "Code " + m.DisplayCode
                }.Where(x => !string.IsNullOrWhiteSpace(x)))).FontSize(7.5f).FontColor(Muted);
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
            row.RelativeItem().Padding(2).Element(x => MiniCard(x, "Niveau", m.LevelLine ?? "—"));
            if (m.FavoriteSubjects is { Count: > 0 })
            {
                row.RelativeItem().Padding(2).Element(x => MiniCard(x, "Leukste vakken", string.Join(" · ", m.FavoriteSubjects)));
            }

            if (!string.IsNullOrWhiteSpace(m.SideJobAvailabilityLine))
            {
                row.RelativeItem().Padding(2).Element(x => MiniCard(x, "Bijbaan", m.SideJobAvailabilityLine!));
            }

            if (!string.IsNullOrWhiteSpace(m.TravelLine))
            {
                row.RelativeItem().Padding(2).Element(x => MiniCard(x, "Vervoer", m.TravelLine!));
            }
        });
    }

    private static void MiniCard(IContainer c, string title, string body)
    {
        c.Border(1).BorderColor(Line).Padding(6).Column(col =>
        {
            col.Item().Text(title.ToUpperInvariant()).FontSize(6.5f).FontColor(Muted).LetterSpacing(0.06f);
            col.Item().PaddingTop(2).Text(body).FontSize(8).SemiBold();
        });
    }

    private static void Interests(IContainer c, PupilPassportPdfModel m)
    {
        c.Column(col =>
        {
            col.Item().Text(PupilPassportCopy.Get("LeerlingPassport.Vo.Section.Interests")).SemiBold().FontSize(10);
            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Column(y =>
                {
                    y.Item().Text(PupilPassportCopy.Get("LeerlingPassport.Vo.Likes")).FontSize(8).SemiBold();
                    y.Item().Text(string.Join("  ·  ", m.Likes)).FontSize(8);
                });
                row.RelativeItem().Column(n =>
                {
                    n.Item().Text(PupilPassportCopy.Get("LeerlingPassport.Vo.Dislikes")).FontSize(8).SemiBold();
                    n.Item().Text(string.Join("  ·  ", m.Dislikes)).FontSize(8);
                });
            });
        });
    }

    private static void Dna(IContainer c, PupilPassportPdfModel m)
    {
        c.Column(col =>
        {
            col.Item().Text(PupilPassportCopy.Get("LeerlingPassport.Vo.DnaTitle")).SemiBold().FontSize(10);
            col.Item().PaddingTop(4).Row(row =>
            {
                foreach (var tile in m.DnaTiles)
                {
                    row.RelativeItem().Padding(2).Background(Color.FromHex("#faf8ff")).Padding(5).Column(x =>
                    {
                        x.Item().Text(tile.Label).FontSize(6.5f).FontColor(Muted);
                        x.Item().Text(tile.Value).FontSize(8).SemiBold();
                    });
                }
            });
        });
    }

    private static void Strengths(IContainer c, PupilPassportPdfModel m)
    {
        c.Column(col =>
        {
            col.Item().Text(PupilPassportCopy.Get("LeerlingPassport.Vo.Strengths")).SemiBold().FontSize(10);
            col.Item().PaddingTop(4).Row(row =>
            {
                foreach (var s in m.Strengths)
                {
                    row.RelativeItem().Padding(2).Border(1).BorderColor(Line).Padding(6).Column(x =>
                    {
                        x.Item().Text(s.Title).SemiBold().FontSize(8.5f);
                        x.Item().Text(s.Subtitle).FontSize(7.5f).FontColor(Muted);
                    });
                }
            });
        });
    }

    private static void Sectors(IContainer c, PupilPassportPdfModel m)
    {
        c.Column(col =>
        {
            col.Item().Text(PupilPassportCopy.Get("LeerlingPassport.Vo.Sectors")).SemiBold().FontSize(10);
            col.Item().PaddingTop(4).Row(row =>
            {
                foreach (var s in m.SectorFits!)
                {
                    row.RelativeItem().Padding(2).Border(1).BorderColor(Line).Padding(6).Column(x =>
                    {
                        x.Item().Text($"{s.Rank}. {s.Title}").SemiBold().FontSize(8.5f);
                        x.Item().Text(s.Subtitle).FontSize(7).FontColor(Muted);
                        foreach (var b in s.Bullets.Take(2))
                        {
                            x.Item().Text("• " + b).FontSize(7);
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
            col.Item().Text(PupilPassportCopy.Get("LeerlingPassport.Vo.SideJobs")).SemiBold().FontSize(10);
            col.Item().Text(PupilPassportCopy.Get("LeerlingPassport.Vo.SideJobs.Sub")).FontSize(7).FontColor(Muted);
            col.Item().PaddingTop(4).Row(row =>
            {
                foreach (var job in m.SideJobTypes!)
                {
                    row.RelativeItem().Padding(2).Border(1).BorderColor(Line).Padding(6).Column(x =>
                    {
                        x.Item().Text(job.Title).SemiBold().FontSize(8.5f);
                        x.Item().Text(job.Subtitle).FontSize(7).FontColor(Muted);
                        x.Item().PaddingTop(2).Text(job.Why).FontSize(7).LineHeight(1.25f);
                    });
                }
            });
        });
    }

    private static void Dream(IContainer c, PupilPassportPdfModel m)
    {
        c.Column(col =>
        {
            col.Item().Text(PupilPassportCopy.Get("LeerlingPassport.Vo.Dream")).SemiBold().FontSize(10);
            if (!string.IsNullOrWhiteSpace(m.DreamJobTitle))
            {
                col.Item().Text(m.DreamJobTitle!).FontSize(11).FontColor(Purple);
            }

            col.Item().PaddingTop(4).Column(col2 =>
            {
                var i = 1;
                foreach (var step in m.RouteSteps.Take(4))
                {
                    col2.Item().Row(r =>
                    {
                        r.ConstantItem(14).Text(i.ToString()).SemiBold().FontColor(Purple);
                        r.RelativeItem().Text(step.Title + (string.IsNullOrWhiteSpace(step.Detail) ? "" : " — " + step.Detail))
                            .FontSize(8);
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
            col.Item().Text(PupilPassportCopy.Get("LeerlingPassport.Vo.Learn")).SemiBold().FontSize(10);
            foreach (var g in m.LearningGoals!)
            {
                col.Item().Text("⚑ " + g).FontSize(8);
            }
        });
    }

    private static void Quote(IContainer c, PupilPassportPdfModel m)
    {
        c.Border(1).BorderColor(Line).Padding(8).Column(col =>
        {
            col.Item().Text(PupilPassportCopy.Get("LeerlingPassport.Vo.OwnWords")).SemiBold().FontSize(9);
            col.Item().PaddingTop(4).Text(m.OwnWords!).FontSize(8.5f).LineHeight(1.35f);
            col.Item().PaddingTop(2).Text(PupilPassportCopy.Get("LeerlingPassport.Vo.OwnWords.Note")).FontSize(7).FontColor(Muted);
        });
    }

    private static void Footer(IContainer c, PupilPassportPdfModel m)
    {
        c.Row(row =>
        {
            row.RelativeItem().Text(m.FooterPrivacy).FontSize(6.5f).FontColor(Muted).LineHeight(1.3f);
            row.ConstantItem(60).AlignRight().Text("pagina 1/1\nlobsy.nl").FontSize(6.5f).FontColor(Muted);
        });
    }
}
