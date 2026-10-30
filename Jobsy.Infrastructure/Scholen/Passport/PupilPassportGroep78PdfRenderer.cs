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
    private static readonly Color Cover = Color.FromHex("#241538");
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
            doc.Page(p => CoverPage(p, m));
            doc.Page(p => WhoAmIPage(p, m, 2));
            doc.Page(p => SubjectsPage(p, m, 3));
            doc.Page(p => DirectionsPage(p, m, 4));
            doc.Page(p => UsagePage(p, m, 5));
        });

    private static void CoverPage(PageDescriptor page, PupilPassportPdfModel m)
    {
        page.Size(PageSizes.A4);
        page.PageColor(Cover);
        page.Margin(28);
        page.DefaultTextStyle(x => x.FontSize(10f).FontColor(Colors.White).FontFamily(PupilPassportFonts.Nunito));

        page.Content().Column(col =>
        {
            col.Item().Row(row =>
            {
                var logo = PupilPassportAssets.LogoPng();
                if (logo.Length > 0)
                {
                    row.ConstantItem(72).MaxHeight(28).Image(logo).FitHeight();
                }

                row.RelativeItem();
            });
            col.Item().PaddingTop(40).Text(T(m, "LeerlingPassport.G78.Cover.Title"))
                .FontFamily(PupilPassportFonts.Fredoka).FontSize(22);
            col.Item().PaddingTop(8).Text(T(m, "LeerlingPassport.G78.Cover.Sub"))
                .FontSize(11).FontColor(Color.FromHex("#c9b8e8"));
            col.Item().PaddingTop(24).Text(T(m, "LeerlingPassport.NameIntro"))
                .FontSize(10);
            col.Item().PaddingTop(4).BorderBottom(1).BorderColor(Color.FromHex("#6a4a9a")).Height(20);
            col.Item().PaddingTop(6).Text(m.DisplayCode).FontFamily(Fonts.CourierNew).FontSize(11);
            col.Item().PaddingTop(32).Text(T(m, "LeerlingPassport.G78.Toc.Title"))
                .FontFamily(PupilPassportFonts.Fredoka).FontSize(14);
            col.Item().PaddingTop(10).Element(c => TocLine(c, "1", T(m, "LeerlingPassport.G78.Toc.1")));
            col.Item().PaddingTop(6).Element(c => TocLine(c, "2", T(m, "LeerlingPassport.G78.Toc.2")));
            col.Item().PaddingTop(6).Element(c => TocLine(c, "3", T(m, "LeerlingPassport.G78.Toc.3")));
            col.Item().PaddingTop(6).Element(c => TocLine(c, "4", T(m, "LeerlingPassport.G78.Toc.4")));
        });
    }

    private static void TocLine(IContainer c, string num, string label)
    {
        c.Row(row =>
        {
            row.ConstantItem(22).Text(num).SemiBold();
            row.RelativeItem().Text(label);
        });
    }

    private static void WhoAmIPage(PageDescriptor page, PupilPassportPdfModel m, int pageNo)
    {
        CommonPage(page, m, pageNo);
        page.Content().Column(col =>
        {
            col.Item().Text(T(m, "LeerlingPassport.G78.Page.Who"))
                .FontFamily(PupilPassportFonts.Fredoka).FontSize(16).FontColor(Purple);
            col.Item().PaddingTop(8).Background(Color.FromHex("#fffdf5")).Border(1).BorderColor(Color.FromHex("#f3e6bf"))
                .Padding(10).Text(NormalizePdfLine(m.MirrorBody ?? m.StoryBody)).LineHeight(1.35f);
            col.Item().PaddingTop(10).Text(T(m, "LeerlingPassport.G78.Riasec.Title"))
                .FontFamily(PupilPassportFonts.Fredoka).FontSize(12).FontColor(Purple);
            col.Item().PaddingTop(4).Element(c => RiasecBars(c, m));
            col.Item().PaddingTop(10).Text(T(m, "LeerlingPassport.G78.Culture.Title"))
                .FontFamily(PupilPassportFonts.Fredoka).FontSize(12).FontColor(Purple);
            col.Item().PaddingTop(4).Element(c => CultureSliders(c, m));
            col.Item().PaddingTop(10).Element(c => Strengths(c, m));
        });
    }

    private static void SubjectsPage(PageDescriptor page, PupilPassportPdfModel m, int pageNo)
    {
        CommonPage(page, m, pageNo);
        page.Content().Column(col =>
        {
            col.Item().Text(T(m, "LeerlingPassport.G78.Page.Subjects"))
                .FontFamily(PupilPassportFonts.Fredoka).FontSize(16).FontColor(Purple);
            var groups = m.SubjectGroups ?? [];
            foreach (var group in groups)
            {
                col.Item().PaddingTop(8).Text(group.Title).Bold().FontSize(11).FontColor(Purple);
                col.Item().PaddingTop(2).Text(group.Intro).FontSize(8.5f).FontColor(Muted);
                col.Item().PaddingTop(3).Text(string.Join(" · ", group.Subjects.Select(s => s.Label))).FontSize(9f);
                foreach (var trait in group.TraitLines)
                {
                    col.Item().PaddingTop(2).Text(NormalizePdfLine(trait)).FontSize(8f);
                }

                col.Item().PaddingTop(3).Text(T(m, "LeerlingPassport.G78.Talk.Title")).SemiBold().FontSize(8.5f);
                foreach (var q in group.ConversationQuestions.Take(2))
                {
                    col.Item().PaddingTop(2).Row(r =>
                    {
                        r.ConstantItem(10).Height(10).Border(1).BorderColor(Purple);
                        r.RelativeItem().PaddingLeft(4).Text(NormalizePdfLine(q)).FontSize(8f);
                    });
                }
            }
        });
    }

    private static void DirectionsPage(PageDescriptor page, PupilPassportPdfModel m, int pageNo)
    {
        CommonPage(page, m, pageNo);
        page.Content().Column(col =>
        {
            col.Item().Text(T(m, "LeerlingPassport.G78.Page.Where"))
                .FontFamily(PupilPassportFonts.Fredoka).FontSize(16).FontColor(Purple);
            col.Item().PaddingTop(6).Text(T(m, "LeerlingPassport.G78.Beroep.Title")).SemiBold().FontSize(11);
            foreach (var b in m.BeroepssoortRanks ?? [])
            {
                col.Item().PaddingTop(4).Row(row =>
                {
                    row.ConstantItem(16).Text(b.Rank.ToString()).SemiBold().FontColor(Purple);
                    row.RelativeItem().Column(inner =>
                    {
                        inner.Item().Text(b.Title).Bold().FontSize(9.5f);
                        inner.Item().Text(NormalizePdfLine(b.Omdat)).FontSize(8f).FontColor(Muted);
                    });
                });
            }

            col.Item().PaddingTop(10).Text(T(m, "LeerlingPassport.G78.School.Title")).SemiBold().FontSize(11);
            foreach (var d in m.SchoolDirections ?? [])
            {
                col.Item().PaddingTop(3).Text(d.Title).Bold().FontSize(9f);
                col.Item().Text(NormalizePdfLine(d.Idea)).FontSize(8f).FontColor(Muted);
            }

            col.Item().PaddingTop(6).Text(T(m, "LeerlingPassport.G78.School.Disclaimer"))
                .FontSize(7.5f).FontColor(Muted).Italic();

            col.Item().PaddingTop(10).Text(T(m, "LeerlingPassport.G78.Jobs.Title")).SemiBold().FontSize(11);
            var jobs = (m.TopJobKeys ?? []).Take(5).ToList();
            for (var i = 0; i < jobs.Count; i++)
            {
                col.Item().PaddingTop(2).Text($"{i + 1}. {PupilPassportLocalizedCopy.JobTitle(jobs[i], m.Language)}")
                    .FontSize(9f);
            }

            if (!string.IsNullOrWhiteSpace(m.DreamJobTitle))
            {
                col.Item().PaddingTop(8).Text(T(m, "LeerlingPassport.Section.Droom")).SemiBold();
                col.Item().Text(m.DreamJobTitle!).FontFamily(PupilPassportFonts.Caveat).FontSize(14).FontColor(Purple);
                col.Item().PaddingTop(4).Element(c => Route(c, m));
            }

            if (!string.IsNullOrWhiteSpace(m.SamenAssignment))
            {
                col.Item().PaddingTop(8).Text(T(m, "LeerlingPassport.G78.Samen.Title")).SemiBold().FontSize(10);
                col.Item().PaddingTop(3).Text(NormalizePdfLine(m.SamenAssignment)).FontSize(8f).LineHeight(1.3f);
            }
        });
    }

    private static void UsagePage(PageDescriptor page, PupilPassportPdfModel m, int pageNo)
    {
        CommonPage(page, m, pageNo);
        page.Content().Column(col =>
        {
            col.Item().Text(T(m, "LeerlingPassport.G78.Page.Usage"))
                .FontFamily(PupilPassportFonts.Fredoka).FontSize(16).FontColor(Purple);
            col.Item().PaddingTop(8).Text(T(m, "LeerlingPassport.G78.Usage.Lead")).FontSize(9f).LineHeight(1.35f);
            col.Item().PaddingTop(10).Text(T(m, "LeerlingPassport.G78.Usage.Parents")).SemiBold();
            foreach (var tip in m.UsageParentTips ?? [])
            {
                col.Item().PaddingTop(3).Text(NormalizePdfLine(tip)).FontSize(8.5f);
            }

            col.Item().PaddingTop(8).Text(T(m, "LeerlingPassport.G78.Usage.Teachers")).SemiBold();
            foreach (var tip in m.UsageTeacherTips ?? [])
            {
                col.Item().PaddingTop(3).Text(NormalizePdfLine(tip)).FontSize(8.5f);
            }

            col.Item().PaddingTop(12).Text(T(m, "LeerlingPassport.G78.Sign.Title")).SemiBold().FontSize(10);
            col.Item().PaddingTop(16).Element(c => SignLine(c, T(m, "LeerlingPassport.G78.Sign.Student")));
            col.Item().PaddingTop(12).Element(c => SignLine(c, T(m, "LeerlingPassport.G78.Sign.Parent")));
            col.Item().PaddingTop(12).Element(c => SignLine(c, T(m, "LeerlingPassport.G78.Sign.Teacher")));
            col.Item().PaddingTop(16).Element(c => Footer(c, m, pageNo));
        });
    }

    private static void SignLine(IContainer c, string label)
    {
        c.Row(row =>
        {
            row.RelativeItem(2).BorderBottom(1).BorderColor(Color.FromHex("#dcc8f4")).Height(16);
            row.RelativeItem(3).PaddingLeft(8).AlignBottom().Text(label).FontSize(8.5f);
        });
    }

    private static void CommonPage(PageDescriptor page, PupilPassportPdfModel m, int pageNo)
    {
        page.Size(PageSizes.A4);
        page.PageColor(Color.FromHex("#fffefb"));
        page.MarginHorizontal(24);
        page.MarginVertical(12);
        page.DefaultTextStyle(x => x.FontSize(9f).FontColor(Ink).FontFamily(PupilPassportFonts.Nunito));
        if (pageNo > 2)
        {
            page.Footer().AlignRight().Text(text =>
            {
                text.Span(T(m, "LeerlingPassport.G78.Footer.Page")).FontSize(7f).FontColor(Muted);
                text.Span($" {pageNo}/5").FontSize(7f).FontColor(Muted);
            });
        }
    }

    private static void RiasecBars(IContainer c, PupilPassportPdfModel m)
    {
        var bars = m.RiasecBars ?? [];
        c.Column(col =>
        {
            foreach (var bar in bars)
            {
                col.Item().PaddingBottom(3).Row(row =>
                {
                    row.ConstantItem(14).Text(bar.Letter).SemiBold().FontColor(Purple);
                    row.ConstantItem(70).Text(bar.Label).FontSize(7.5f);
                    row.RelativeItem().Height(8).Background(Color.FromHex("#ede5f8")).Row(inner =>
                    {
                        var ratio = Math.Clamp(bar.Score, 0, 100) / 100f;
                        if (ratio > 0.01f)
                        {
                            inner.RelativeItem(ratio).Height(8).Background(Purple);
                        }

                        if (ratio < 0.99f)
                        {
                            inner.RelativeItem(Math.Max(0.01f, 1f - ratio));
                        }
                    });
                });
            }
        });
    }

    private static void CultureSliders(IContainer c, PupilPassportPdfModel m)
    {
        var sliders = m.CultureSliders ?? [];
        if (sliders.Count == 0)
        {
            c.Text(T(m, "LeerlingPassport.G78.Culture.Empty")).FontSize(8f).FontColor(Muted);
            return;
        }

        c.Column(col =>
        {
            foreach (var s in sliders)
            {
                col.Item().PaddingBottom(4).Row(row =>
                {
                    row.RelativeItem(2).Text(s.Label).FontSize(8f);
                    row.RelativeItem(3).Height(8).Background(Color.FromHex("#e6f8ef")).Row(inner =>
                    {
                        var ratio = Math.Clamp(s.Position, 0, 100) / 100f;
                        if (ratio > 0.01f)
                        {
                            inner.RelativeItem(ratio).Height(8).Background(Color.FromHex("#16865a"));
                        }

                        if (ratio < 0.99f)
                        {
                            inner.RelativeItem(Math.Max(0.01f, 1f - ratio));
                        }
                    });
                });
            }
        });
    }

    private static void Strengths(IContainer c, PupilPassportPdfModel m)
    {
        c.Column(col =>
        {
            col.Item().Text(T(m, "LeerlingPassport.Section.Strengths"))
                .FontFamily(PupilPassportFonts.Fredoka).FontSize(12).FontColor(Purple);
            col.Item().PaddingTop(4).Row(row =>
            {
                foreach (var s in m.Strengths)
                {
                    row.RelativeItem().PaddingHorizontal(2).Background(Color.FromHex("#f7f4ff")).Padding(6).Column(t =>
                    {
                        t.Item().Text(s.Title).Bold().FontSize(8f);
                        t.Item().Text(s.Subtitle).FontSize(7f).FontColor(Muted).ClampLines(2);
                    });
                }
            });
        });
    }

    private static void Route(IContainer c, PupilPassportPdfModel m)
    {
        var steps = m.RouteSteps.Take(4).ToList();
        if (steps.Count == 0)
        {
            return;
        }

        c.Column(col =>
        {
            foreach (var step in steps)
            {
                col.Item().PaddingTop(2).Text(NormalizePdfLine(step.Title)).FontSize(8f).Bold();
                if (!string.IsNullOrWhiteSpace(step.Detail))
                {
                    col.Item().Text(NormalizePdfLine(step.Detail)).FontSize(7.5f).FontColor(Muted);
                }
            }
        });
    }

    private static void Footer(IContainer c, PupilPassportPdfModel m, int pageNo)
    {
        c.Row(row =>
        {
            row.ConstantItem(16).Svg(PupilPassportIcons.Icon("shield", Color.FromHex("#16865a"), 12));
            row.RelativeItem().PaddingLeft(4).Text(m.FooterPrivacy).FontSize(6f).FontColor(Muted).LineHeight(1.25f);
            row.ConstantItem(55).AlignRight().Column(col =>
            {
                col.Item().AlignRight().Text("lobsy.nl").FontColor(Purple).SemiBold();
                col.Item().AlignRight().Text($"{pageNo}/5").FontSize(6f).FontColor(Muted);
            });
        });
    }

    private static string NormalizePdfLine(string text)
        => text
            .Replace('\u2013', '-')
            .Replace('\u2014', '-')
            .Replace('\u2212', '-');
}
