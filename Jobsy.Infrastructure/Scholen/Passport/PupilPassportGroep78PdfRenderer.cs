using System.Globalization;
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

    private static readonly Color Navy = Color.FromHex("#0f2d5c");
    private static readonly Color NavyDeep = Color.FromHex("#0a2044");
    private static readonly Color Gold = Color.FromHex("#c9a227");
    private static readonly Color GoldLight = Color.FromHex("#e4c65a");
    private static readonly Color Cream = Color.FromHex("#fffcfa");
    private static readonly Color CreamWarm = Color.FromHex("#fffaf3");
    private static readonly Color Border = Color.FromHex("#e6d7c3");
    private static readonly Color Muted = Color.FromHex("#6b5d50");
    private static readonly Color Ink = Color.FromHex("#3d342b");
    private static readonly Color Track = Color.FromHex("#f3ebe0");
    private static readonly Color LineMuted = Color.FromHex("#d9cbb6");
    private static readonly Color FooterMuted = Color.FromHex("#8a7c6e");
    private static readonly Color Kicker = Color.FromHex("#c4552a");

    private static readonly float[] ValueBarWidths = [0.90f, 0.80f, 0.70f, 0.62f, 0.50f];

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

    private static CultureInfo DateCulture(PupilPassportPdfModel m)
        => CultureInfo.GetCultureInfo(PupilVerhaalLocalized.IsEnglish(m.Language) ? "en-GB" : "nl-NL");

    private static string DateLine(PupilPassportPdfModel m)
        => m.Date.ToString("MMMM yyyy", DateCulture(m));

    private static void CoverPage(PageDescriptor page, PupilPassportPdfModel m)
    {
        page.Size(PageSizes.A4);
        page.PageColor(Cream);
        page.MarginHorizontal(24);
        page.MarginVertical(14);
        page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor(Ink).FontFamily(PupilPassportFonts.Nunito));

        page.Header().Element(c => CoverHeader(c, m));
        page.Footer().Element(c => PageFooter(c, m, T(m, "LeerlingPassport.G78.Footer.Label.Cover")));

        page.Content().Column(col =>
        {
            col.Item().PaddingTop(18).Text(T(m, "LeerlingPassport.G78.Cover.Kicker"))
                .FontSize(10).SemiBold().FontColor(Kicker).LetterSpacing(0.8f);
            col.Item().PaddingTop(4).Text(T(m, "LeerlingPassport.G78.Cover.Title"))
                .FontFamily(PupilPassportFonts.Fredoka).FontSize(28).FontColor(Navy).LineHeight(1.1f);
            col.Item().PaddingTop(8).BorderLeft(3).BorderColor(Gold).PaddingLeft(10)
                .Text(T(m, "LeerlingPassport.G78.Cover.Intro")).FontSize(11.5f).LineHeight(1.45f);

            col.Item().PaddingTop(28).Row(row =>
            {
                row.RelativeItem().Element(c => CoverMetaBox(c, m));
                row.ConstantItem(12);
                row.RelativeItem().Element(c => CoverTocBox(c, m));
            });
        });
    }

    private static void CoverHeader(IContainer c, PupilPassportPdfModel m)
    {
        c.PaddingBottom(6).BorderBottom(1).BorderColor(Navy).Row(row =>
        {
            var logo = PupilPassportAssets.LogoPng();
            if (logo.Length > 0)
            {
                row.ConstantItem(56).MaxHeight(22).Image(logo).FitHeight();
            }

            row.RelativeItem();
            row.ConstantItem(130).AlignRight().Column(right =>
            {
                right.Item().AlignRight().Text(text =>
                {
                    text.Span(T(m, "LeerlingPassport.G78.Meta.CodeLabel") + " ").FontSize(8.5f).FontColor(Muted);
                    text.Span(m.DisplayCode).SemiBold().FontColor(Ink);
                });
                right.Item().AlignRight().Text($"{m.ClassLabel} · {DateLine(m)}")
                    .FontSize(8.5f).FontColor(Muted);
            });
        });
    }

    private static void CoverMetaBox(IContainer c, PupilPassportPdfModel m)
    {
        c.BorderTop(1.5f).BorderColor(Navy).PaddingTop(8).Column(col =>
        {
            col.Item().Text(T(m, "LeerlingPassport.G78.Cover.About"))
                .FontFamily(PupilPassportFonts.Fredoka).FontSize(12).FontColor(Navy);
            col.Item().PaddingTop(6).Element(inner => MetaRow(inner, T(m, "LeerlingPassport.G78.Cover.Meta.Code"), m.DisplayCode, bold: true));
            col.Item().PaddingTop(3).Element(inner => MetaRow(inner, T(m, "LeerlingPassport.G78.Cover.Meta.Groep"), GroepShort(m), bold: false));
            col.Item().PaddingTop(3).Element(inner => MetaRow(inner, T(m, "LeerlingPassport.G78.Cover.Meta.Datum"), DateLine(m), bold: false));
            col.Item().PaddingTop(3).Row(row =>
            {
                row.ConstantItem(72).Text(T(m, "LeerlingPassport.G78.Cover.Meta.Naam")).FontSize(9.5f).FontColor(Muted);
                row.RelativeItem().BorderBottom(1).BorderColor(LineMuted).Height(14);
            });
            col.Item().PaddingTop(4).Text(T(m, "LeerlingPassport.NamePrivacy")).FontSize(8f).FontColor(Muted);
        });
    }

    private static void CoverTocBox(IContainer c, PupilPassportPdfModel m)
    {
        c.BorderTop(1.5f).BorderColor(Navy).PaddingTop(8).Column(col =>
        {
            col.Item().Text(T(m, "LeerlingPassport.G78.Toc.Title"))
                .FontFamily(PupilPassportFonts.Fredoka).FontSize(12).FontColor(Navy);
            col.Item().PaddingTop(8).Element(x => TocLine(x, "1", T(m, "LeerlingPassport.G78.Toc.1")));
            col.Item().PaddingTop(5).Element(x => TocLine(x, "2", T(m, "LeerlingPassport.G78.Toc.2")));
            col.Item().PaddingTop(5).Element(x => TocLine(x, "3", T(m, "LeerlingPassport.G78.Toc.3")));
            col.Item().PaddingTop(5).Element(x => TocLine(x, "4", T(m, "LeerlingPassport.G78.Toc.4")));
        });
    }

    private static void MetaRow(IContainer c, string label, string value, bool bold)
    {
        c.Row(row =>
        {
            row.ConstantItem(72).Text(label).FontSize(9.5f).FontColor(Muted);
            var t = row.RelativeItem().Text(value).FontSize(9.5f);
            if (bold)
            {
                t.SemiBold();
            }
        });
    }

    private static string GroepShort(PupilPassportPdfModel m)
    {
        var label = m.ClassLabel;
        var idx = label.IndexOf('·');
        return idx > 0 ? label[..idx].Trim() : label;
    }

    private static void TocLine(IContainer c, string num, string label)
    {
        c.Row(row =>
        {
            row.ConstantItem(16).Text(num).SemiBold().FontColor(Navy);
            row.RelativeItem().Text(label).FontSize(9.5f);
        });
    }

    private static void WhoAmIPage(PageDescriptor page, PupilPassportPdfModel m, int pageNo)
    {
        InnerPage(page, m, pageNo,
            T(m, "LeerlingPassport.G78.Header.Part1"),
            T(m, "LeerlingPassport.G78.Page.Who"),
            T(m, "LeerlingPassport.G78.Footer.Label.Part1"));

        page.Content().Column(col =>
        {
            col.Item().Element(c => MirrorBox(c, m));
            col.Item().PaddingTop(8).Element(c => SectionBox(c, T(m, "LeerlingPassport.G78.Riasec.Title"),
                T(m, "LeerlingPassport.G78.Riasec.Sub"), inner => RiasecBars(inner, m)));
            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Element(c => SectionBox(c, T(m, "LeerlingPassport.G78.Culture.Title"), null,
                    inner => CultureSliders(inner, m)));
                row.ConstantItem(8);
                row.RelativeItem().Element(c => SectionBox(c, T(m, "LeerlingPassport.G78.Strengths.Title"),
                    T(m, "LeerlingPassport.G78.Strengths.Sub"), inner => StrengthChips(inner, m)));
            });
        });
    }

    private static void SubjectsPage(PageDescriptor page, PupilPassportPdfModel m, int pageNo)
    {
        InnerPage(page, m, pageNo,
            T(m, "LeerlingPassport.G78.Header.Part2"),
            T(m, "LeerlingPassport.G78.Page.Subjects"),
            T(m, "LeerlingPassport.G78.Footer.Label.Part2"));

        page.Content().Column(col =>
        {
            col.Item().Element(c => SectionBox(c, T(m, "LeerlingPassport.G78.Subjects.BlockTitle"),
                T(m, "LeerlingPassport.G78.Subjects.Sub"), inner =>
                {
                    inner.Column(box =>
                    {
                        box.Item().Element(SubjectColumns);
                        box.Item().PaddingTop(4).Text(T(m, "LeerlingPassport.G78.Subjects.Question"))
                            .Italic().FontSize(8.5f).FontColor(Navy);
                    });

                    void SubjectColumns(IContainer sc)
                    {
                        var groups = (m.SubjectGroups ?? []).Take(3).ToList();
                        sc.Row(row =>
                        {
                            for (var i = 0; i < 3; i++)
                            {
                                if (i > 0)
                                {
                                    row.ConstantItem(6);
                                }

                                row.RelativeItem().Element(g => SubjectGroupColumn(g, i < groups.Count ? groups[i] : null, i));
                            }
                        });
                    }
                }));

            var valueLabels = ValueBarLabels(m);
            if (valueLabels.Count > 0)
            {
                col.Item().PaddingTop(6).Element(c => SectionBox(c, T(m, "LeerlingPassport.G78.Values.Title"),
                    T(m, "LeerlingPassport.G78.Values.Sub"), inner =>
                    {
                        inner.Column(box =>
                        {
                            for (var i = 0; i < valueLabels.Count; i++)
                            {
                                box.Item().PaddingTop(i == 0 ? 0 : 3)
                                    .Element(x => ValueBar(x, valueLabels[i], ValueBarWidths[Math.Min(i, ValueBarWidths.Length - 1)]));
                            }

                            box.Item().PaddingTop(4).Text(T(m, "LeerlingPassport.G78.Values.Question"))
                                .Italic().FontSize(8.5f).FontColor(Navy);
                        });
                    }));
            }

            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Element(c => SectionBox(c, T(m, "LeerlingPassport.G78.Likes.Title"), null, inner =>
                {
                    inner.Column(box =>
                    {
                        box.Item().Element(LikeChips);
                        box.Item().PaddingTop(4).Text(T(m, "LeerlingPassport.G78.Likes.Question"))
                            .Italic().FontSize(8.5f).FontColor(Navy);
                    });

                    void LikeChips(IContainer lc)
                    {
                        var likes = m.Likes ?? [];
                        if (likes.Count == 0)
                        {
                            lc.Text(T(m, "LeerlingPassport.G78.Likes.Empty")).FontSize(8.5f).FontColor(Muted);
                            return;
                        }

                        lc.Row(r =>
                        {
                            foreach (var like in likes.Take(8))
                            {
                                r.AutoItem().PaddingRight(4).PaddingBottom(3)
                                    .Background(CreamWarm).Border(1).BorderColor(Gold)
                                    .PaddingHorizontal(6).PaddingVertical(3)
                                    .Text(NormalizePdfLine(like)).FontSize(8.5f);
                            }
                        });
                    }
                }));
                row.ConstantItem(8);
                row.RelativeItem().Element(c => TalkBox(c, m, FirstTalkQuestions(m)));
            });
        });
    }

    private static void DirectionsPage(PageDescriptor page, PupilPassportPdfModel m, int pageNo)
    {
        InnerPage(page, m, pageNo,
            T(m, "LeerlingPassport.G78.Header.Where"),
            T(m, "LeerlingPassport.G78.Page.Where"),
            T(m, "LeerlingPassport.G78.Footer.Label.Where"));

        page.Content().Column(col =>
        {
            col.Item().BorderLeft(3).BorderColor(Gold).PaddingLeft(8)
                .Text(T(m, "LeerlingPassport.G78.Where.Intro")).FontSize(9.5f).LineHeight(1.4f);

            col.Item().PaddingTop(8).Row(row =>
            {
                row.RelativeItem().Element(c => SectionBox(c, T(m, "LeerlingPassport.G78.Beroep.Title"), null,
                    inner => BeroepList(inner, m)));
                row.ConstantItem(8);
                row.RelativeItem().Element(c => SectionBox(c, T(m, "LeerlingPassport.G78.School.Title"),
                    T(m, "LeerlingPassport.G78.School.Sub"), inner =>
                    {
                        inner.Column(box =>
                        {
                            box.Item().Element(SchoolCards);
                            box.Item().PaddingTop(4).Text(T(m, "LeerlingPassport.G78.School.Disclaimer"))
                                .FontSize(7.5f).FontColor(Muted).Italic();
                        });

                        void SchoolCards(IContainer sc)
                        {
                            sc.Column(list =>
                            {
                                foreach (var d in m.SchoolDirections ?? [])
                                {
                                    list.Item().PaddingBottom(4).Background(CreamWarm).BorderTop(3).BorderColor(Navy)
                                        .Padding(6).Column(card =>
                                        {
                                            card.Item().Text(d.Title).Bold().FontSize(9f).FontColor(Navy);
                                            card.Item().Text(NormalizePdfLine(d.Idea)).FontSize(8f).FontColor(Muted);
                                        });
                                }
                            });
                        }
                    }));
            });

            col.Item().PaddingTop(6).Element(c => SectionBox(c, T(m, "LeerlingPassport.G78.Jobs.Title"), null,
                inner => inner.Element(JobCardsRow)));

            if (!string.IsNullOrWhiteSpace(m.DreamJobTitle))
            {
                col.Item().PaddingTop(6).Border(2).BorderColor(Gold).Padding(8).Column(dream =>
                {
                    dream.Item().Text(string.Format(
                            CultureInfo.InvariantCulture,
                            T(m, "LeerlingPassport.G78.Dream.Heading"),
                            m.DreamJobTitle!.Trim()))
                        .FontFamily(PupilPassportFonts.Fredoka).FontSize(11).FontColor(Navy);
                    dream.Item().PaddingTop(2).Text(T(m, "LeerlingPassport.G78.Dream.Sub"))
                        .FontSize(8f).FontColor(Muted);
                    dream.Item().PaddingTop(6).Element(c => DreamTimeline(c, m));
                    dream.Item().PaddingTop(4).Text(T(m, "LeerlingPassport.G78.Dream.Question"))
                        .Italic().FontSize(8.5f).FontColor(Navy);
                });
            }

            if (!string.IsNullOrWhiteSpace(m.SamenAssignment))
            {
                col.Item().PaddingTop(6).Element(c => SamenBox(c, m));
            }

            void JobCardsRow(IContainer jc)
            {
                var keys = (m.TopJobKeys ?? []).Take(5).ToList();
                jc.Row(row =>
                {
                    for (var i = 0; i < 5; i++)
                    {
                        if (i > 0)
                        {
                            row.ConstantItem(4);
                        }

                        row.RelativeItem().Element(card =>
                        {
                            if (i >= keys.Count)
                            {
                                card.MinHeight(36);
                                return;
                            }

                            var key = keys[i];
                            var title = PupilPassportLocalizedCopy.JobTitle(key, m.Language);
                            var sub = JobCardSubtitle(m, key);
                            card.Background(Colors.White).Border(1).BorderColor(Border).Padding(4).Column(inner =>
                            {
                                inner.Item().AlignCenter().Text(title).FontSize(8f).SemiBold().FontColor(Ink);
                                if (!string.IsNullOrWhiteSpace(sub))
                                {
                                    inner.Item().AlignCenter().PaddingTop(2)
                                        .Text(sub).FontSize(7f).FontColor(Muted);
                                }
                            });
                        });
                    }
                });
            }
        });
    }

    private static void UsagePage(PageDescriptor page, PupilPassportPdfModel m, int pageNo)
    {
        InnerPage(page, m, pageNo,
            T(m, "LeerlingPassport.G78.Header.Usage"),
            T(m, "LeerlingPassport.G78.Page.Usage"),
            T(m, "LeerlingPassport.G78.Footer.Label.Usage"));

        page.Content().Column(col =>
        {
            col.Item().Background(Color.FromHex("#fbf3e8")).BorderLeft(3).BorderColor(Kicker).Padding(8)
                .Text(T(m, "LeerlingPassport.G78.Usage.Notice")).FontSize(9f).LineHeight(1.45f);

            col.Item().PaddingTop(8).Row(row =>
            {
                row.RelativeItem(1.25f).Element(c => UsageColumn(c, m, parent: true));
                row.ConstantItem(10);
                row.RelativeItem().Element(c => UsageColumn(c, m, parent: false));
            });

            col.Item().PaddingTop(8).Element(c => WriteBlock(c,
                T(m, "LeerlingPassport.G78.Write.Student"),
                T(m, "LeerlingPassport.G78.Write.Student.Sub"),
                3));
            col.Item().PaddingTop(6).Element(c => WriteBlock(c,
                T(m, "LeerlingPassport.G78.Write.Parent"),
                T(m, "LeerlingPassport.G78.Write.Parent.Sub"),
                2));
            col.Item().PaddingTop(6).Column(together =>
            {
                together.Item().Element(c => WriteBlock(c,
                    T(m, "LeerlingPassport.G78.Write.Together"),
                    T(m, "LeerlingPassport.G78.Write.Together.Sub"),
                    2));
                together.Item().PaddingTop(4).Row(sign =>
                {
                    sign.RelativeItem().Element(x => SignColumn(x, T(m, "LeerlingPassport.G78.Sign.Student")));
                    sign.RelativeItem().Element(x => SignColumn(x, T(m, "LeerlingPassport.G78.Sign.Parent")));
                    sign.RelativeItem().Element(x => SignColumn(x, T(m, "LeerlingPassport.G78.Sign.Teacher")));
                });
            });
        });
    }

    private static void InnerPage(
        PageDescriptor page,
        PupilPassportPdfModel m,
        int pageNo,
        string kicker,
        string title,
        string footerLabel)
    {
        page.Size(PageSizes.A4);
        page.PageColor(Cream);
        page.MarginHorizontal(24);
        page.MarginVertical(12);
        page.DefaultTextStyle(x => x.FontSize(9f).FontColor(Ink).FontFamily(PupilPassportFonts.Nunito));
        page.Header().Element(c => InnerHeader(c, m, kicker, title));
        page.Footer().Element(c => PageFooter(c, m, footerLabel, pageNo));
    }

    private static void InnerHeader(IContainer c, PupilPassportPdfModel m, string kicker, string title)
    {
        c.PaddingBottom(5).BorderBottom(1).BorderColor(Navy).Row(row =>
        {
            row.RelativeItem().Column(left =>
            {
                left.Item().Text(kicker).FontSize(8.5f).SemiBold().FontColor(Kicker).LetterSpacing(0.5f);
                left.Item().Text(title).FontFamily(PupilPassportFonts.Fredoka).FontSize(18).FontColor(Navy);
            });
            row.ConstantItem(120).AlignRight().AlignMiddle().Column(right =>
            {
                right.Item().AlignRight().Text(text =>
                {
                    text.Span(T(m, "LeerlingPassport.G78.Meta.CodeLabel") + " ").FontSize(8f).FontColor(Muted);
                    text.Span(m.DisplayCode).SemiBold();
                });
                right.Item().AlignRight().Text($"{m.ClassLabel} · {DateLine(m)}").FontSize(8f).FontColor(Muted);
            });
        });
    }

    private static void PageFooter(IContainer c, PupilPassportPdfModel m, string label, int? pageNo = null)
    {
        c.BorderTop(1).BorderColor(Border).PaddingTop(4).Row(row =>
        {
            row.RelativeItem().Text(m.FooterPrivacy).FontSize(7f).FontColor(FooterMuted).LineHeight(1.2f);
            row.ConstantItem(90).AlignRight().Column(col =>
            {
                col.Item().AlignRight().Text(label).FontSize(7.5f).FontColor(FooterMuted);
                if (pageNo is > 0)
                {
                    col.Item().AlignRight().Text($"{T(m, "LeerlingPassport.G78.Footer.Page")} {pageNo}/5")
                        .FontSize(7f).FontColor(FooterMuted);
                }
            });
        });
    }

    private static void SectionBox(
        IContainer c,
        string heading,
        string? sub,
        Action<IContainer> body)
    {
        c.BorderTop(1.5f).BorderColor(Navy).PaddingTop(6).Column(col =>
        {
            col.Item().Text(heading).FontFamily(PupilPassportFonts.Fredoka).FontSize(11.5f).FontColor(Navy);
            if (!string.IsNullOrWhiteSpace(sub))
            {
                col.Item().PaddingTop(1).Text(sub!).FontSize(8f).FontColor(Muted);
            }

            col.Item().PaddingTop(4).Element(body);
        });
    }

    private static void MirrorBox(IContainer c, PupilPassportPdfModel m)
    {
        var body = NormalizePdfLine(m.MirrorBody ?? m.StoryBody);
        c.MinHeight(72).Layers(layers =>
        {
            layers.Layer().Svg(MirrorGradientSvg());
            layers.Layer().Padding(10).Column(col =>
            {
                col.Item().Text(T(m, "LeerlingPassport.G78.Mirror.Title"))
                    .FontFamily(PupilPassportFonts.Fredoka).FontSize(12).FontColor(GoldLight);
                col.Item().PaddingTop(4).Text(body).FontSize(10.5f).FontColor(Colors.White).LineHeight(1.55f);
                col.Item().PaddingTop(6).Text(T(m, "LeerlingPassport.G78.Mirror.Source"))
                    .FontSize(7.5f).FontColor(Color.FromHex("#b9c6da"));
            });
        });
    }

    private static string MirrorGradientSvg()
        => """
           <svg xmlns="http://www.w3.org/2000/svg" width="100%" height="100%" preserveAspectRatio="none">
             <defs>
               <linearGradient id="g78mirror" x1="0%" y1="0%" x2="100%" y2="100%">
                 <stop offset="0%" stop-color="#0f2d5c"/>
                 <stop offset="100%" stop-color="#1b4a7a"/>
               </linearGradient>
             </defs>
             <rect width="100%" height="100%" fill="url(#g78mirror)" rx="4"/>
           </svg>
           """;

    private static void RiasecBars(IContainer c, PupilPassportPdfModel m)
    {
        var bars = m.RiasecBars ?? [];
        c.Column(col =>
        {
            foreach (var bar in bars)
            {
                var tenths = Math.Clamp((int)Math.Round(bar.Score / 10.0, MidpointRounding.AwayFromZero), 0, 10);
                var ratio = tenths / 10f;
                var descKey = $"LeerlingPassport.G78.Riasec.{bar.Letter}.Desc";
                var desc = T(m, descKey);
                if (string.Equals(desc, descKey, StringComparison.Ordinal))
                {
                    desc = "";
                }

                col.Item().PaddingBottom(4).Column(row =>
                {
                    row.Item().Row(r =>
                    {
                        r.ConstantItem(88).Text($"{bar.Label}").FontSize(8.5f);
                        r.RelativeItem().Height(10).Background(Track).Row(inner =>
                        {
                            if (ratio > 0.01f)
                            {
                                inner.RelativeItem(ratio).Height(10).Background(Navy);
                            }

                            if (ratio < 0.99f)
                            {
                                inner.RelativeItem(Math.Max(0.01f, 1f - ratio));
                            }
                        });
                        r.ConstantItem(28).AlignRight().Text($"{tenths}/10").SemiBold().FontColor(Navy).FontSize(8.5f);
                    });
                    if (!string.IsNullOrWhiteSpace(desc))
                    {
                        row.Item().PaddingTop(1).Text(desc).FontSize(7.5f).FontColor(Muted);
                    }
                });
            }
        });
    }

    private static void CultureSliders(IContainer c, PupilPassportPdfModel m)
    {
        var sliders = (m.CultureSliders ?? []).Take(4).ToList();
        if (sliders.Count == 0)
        {
            c.Text(T(m, "LeerlingPassport.G78.Culture.Empty")).FontSize(8f).FontColor(Muted);
            return;
        }

        c.Column(col =>
        {
            for (var i = 0; i < sliders.Count; i += 2)
            {
                col.Item().PaddingBottom(4).Row(row =>
                {
                    row.RelativeItem().Element(x => CultureSliderCard(x, sliders[i]));
                    if (i + 1 < sliders.Count)
                    {
                        row.ConstantItem(6);
                        row.RelativeItem().Element(x => CultureSliderCard(x, sliders[i + 1]));
                    }
                    else
                    {
                        row.RelativeItem();
                    }
                });
            }
        });
    }

    private static void CultureSliderCard(IContainer c, PupilPassportCultureSlider s)
    {
        var ratio = Math.Clamp(s.Position, 0, 100) / 100f;
        c.Background(CreamWarm).Padding(6).Column(col =>
        {
            col.Item().Text(s.Label).Bold().FontSize(8.5f).FontColor(Navy);
            col.Item().PaddingTop(4).Height(10).Background(Track).Row(track =>
            {
                if (ratio > 0.02f)
                {
                    track.RelativeItem(ratio - 0.04f);
                }

                track.ConstantItem(8).Height(8).Background(Gold);
                if (ratio < 0.98f)
                {
                    track.RelativeItem(Math.Max(0.01f, 1f - ratio - 0.04f));
                }
            });
        });
    }

    private static void StrengthChips(IContainer c, PupilPassportPdfModel m)
    {
        c.Column(col =>
        {
            col.Item().Row(row =>
            {
                var i = 0;
                foreach (var s in m.Strengths.Take(8))
                {
                    var gold = i % 2 == 0;
                    row.AutoItem().PaddingRight(3).PaddingBottom(3)
                        .Background(gold ? Gold : Navy)
                        .PaddingHorizontal(6).PaddingVertical(3)
                        .Text(s.Title).FontSize(8.5f)
                        .FontColor(gold ? NavyDeep : Colors.White);
                    i++;
                }
            });
            col.Item().PaddingTop(4).Text(T(m, "LeerlingPassport.G78.Strengths.Question"))
                .Italic().FontSize(8.5f).FontColor(Navy);
        });
    }

    private static void SubjectGroupColumn(IContainer c, PupilPassportSubjectGroup? group, int index)
    {
        var topColor = index switch
        {
            0 => Gold,
            1 => Navy,
            _ => Color.FromHex("#33505f")
        };

        if (group is null)
        {
            c.MinHeight(40);
            return;
        }

        c.BorderTop(3).BorderColor(topColor).Background(Colors.White).Padding(6).Column(col =>
        {
            col.Item().Text(group.Title).Bold().FontSize(9.5f).FontColor(Navy);
            col.Item().PaddingTop(1).Text(group.Intro).FontSize(7.5f).FontColor(Muted);
            foreach (var subject in group.Subjects)
            {
                col.Item().PaddingTop(3).BorderBottom(1).BorderColor(Color.FromHex("#f0e6d8"))
                    .PaddingBottom(2).Text(subject.Label).FontSize(9f);
            }
        });
    }

    private static IReadOnlyList<string> ValueBarLabels(PupilPassportPdfModel m)
    {
        var fromTiles = (m.DnaTiles ?? [])
            .Select(t => t.Label)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Take(5)
            .ToList();
        if (fromTiles.Count > 0)
        {
            return fromTiles;
        }

        return m.Strengths.Select(s => s.Title).Where(s => !string.IsNullOrWhiteSpace(s)).Take(5).ToList();
    }

    private static void ValueBar(IContainer c, string label, float widthRatio)
    {
        c.Row(row =>
        {
            row.ConstantItem(78).Text(label).FontSize(8.5f);
            row.RelativeItem().Height(10).Background(Track).Row(inner =>
            {
                var ratio = Math.Clamp(widthRatio, 0.05f, 1f);
                inner.RelativeItem(ratio).Height(10).Background(Gold);
                if (ratio < 0.99f)
                {
                    inner.RelativeItem(Math.Max(0.01f, 1f - ratio));
                }
            });
        });
    }

    private static IReadOnlyList<string> FirstTalkQuestions(PupilPassportPdfModel m)
    {
        var group = m.SubjectGroups?.FirstOrDefault();
        return group?.ConversationQuestions.Take(3).Select(NormalizePdfLine).ToList() ?? [];
    }

    private static void TalkBox(IContainer c, PupilPassportPdfModel m, IReadOnlyList<string> questions)
    {
        c.Background(Navy).Padding(8).Column(col =>
        {
            col.Item().Text(T(m, "LeerlingPassport.G78.Talk.Heading")).Bold().FontColor(GoldLight).FontSize(9.5f);
            foreach (var q in questions)
            {
                col.Item().PaddingTop(3).Text("- " + q).FontColor(Colors.White).FontSize(9f).LineHeight(1.35f);
            }

            if (questions.Count == 0)
            {
                col.Item().PaddingTop(3).Text(T(m, "LeerlingPassport.G78.Talk.Empty")).FontColor(Colors.White).FontSize(8.5f);
            }
        });
    }

    private static void SamenBox(IContainer c, PupilPassportPdfModel m)
    {
        c.Background(Navy).Padding(8).Column(col =>
        {
            col.Item().Text(T(m, "LeerlingPassport.G78.Samen.Heading")).Bold().FontColor(GoldLight).FontSize(9.5f);
            col.Item().PaddingTop(3).Text("- " + NormalizePdfLine(m.SamenAssignment!))
                .FontColor(Colors.White).FontSize(9f).LineHeight(1.35f);
        });
    }

    private static void BeroepList(IContainer c, PupilPassportPdfModel m)
    {
        c.Column(col =>
        {
            foreach (var b in m.BeroepssoortRanks ?? [])
            {
                col.Item().PaddingBottom(3).Row(row =>
                {
                    row.ConstantItem(14).Text(b.Rank.ToString()).SemiBold().FontColor(Navy);
                    row.RelativeItem().Column(inner =>
                    {
                        inner.Item().Text(b.Title).Bold().FontSize(9f);
                        if (!string.IsNullOrWhiteSpace(b.Omdat))
                        {
                            inner.Item().Text(NormalizePdfLine(b.Omdat)).FontSize(7.5f).FontColor(Muted);
                        }
                    });
                });
            }
        });
    }

    private static void DreamTimeline(IContainer c, PupilPassportPdfModel m)
    {
        var steps = m.RouteSteps.Take(5).ToList();
        if (steps.Count == 0)
        {
            return;
        }

        c.Row(row =>
        {
            for (var i = 0; i < steps.Count; i++)
            {
                if (i > 0)
                {
                    row.ConstantItem(2);
                }

                var step = steps[i];
                var isFirst = i == 0;
                var isGoal = step.IsGoal;
                row.RelativeItem().Column(col =>
                {
                    col.Item().AlignCenter().Width(22).Height(22)
                        .Background(isFirst || isGoal ? Navy : Gold)
                        .AlignCenter().AlignMiddle()
                        .Text(isFirst
                            ? T(m, "LeerlingPassport.G78.Route.Now")
                            : isGoal
                                ? T(m, "LeerlingPassport.G78.Route.Goal")
                                : i.ToString(CultureInfo.InvariantCulture))
                        .FontSize(7.5f).SemiBold()
                        .FontColor(isFirst || isGoal ? Colors.White : NavyDeep);
                    col.Item().PaddingTop(2).AlignCenter().Text(NormalizePdfLine(step.Title))
                        .FontSize(7.5f).FontColor(Ink);
                    if (!string.IsNullOrWhiteSpace(step.Detail))
                    {
                        col.Item().AlignCenter().Text(NormalizePdfLine(step.Detail))
                            .FontSize(7f).FontColor(Muted);
                    }
                });
            }
        });
    }

    private static string JobCardSubtitle(PupilPassportPdfModel m, string jobKey)
    {
        var card = m.OccupationCards.FirstOrDefault(c =>
            string.Equals(c.Title, PupilPassportLocalizedCopy.JobTitle(jobKey, m.Language), StringComparison.OrdinalIgnoreCase));
        if (card is not null && card.FitTags.Count > 0)
        {
            return string.Join(" & ", card.FitTags.Take(2));
        }

        return PupilPassportLocalizedCopy.JobDomain(jobKey, m.Language);
    }

    private static void UsageColumn(IContainer c, PupilPassportPdfModel m, bool parent)
    {
        c.Column(col =>
        {
            col.Item().Text(parent
                    ? T(m, "LeerlingPassport.G78.Usage.Parents.Tag")
                    : T(m, "LeerlingPassport.G78.Usage.Teachers.Tag"))
                .FontSize(7.5f).SemiBold().FontColor(Kicker).LetterSpacing(0.8f);
            col.Item().PaddingTop(2).Text(parent
                    ? T(m, "LeerlingPassport.G78.Usage.Parents.Heading")
                    : T(m, "LeerlingPassport.G78.Usage.Teachers.Heading"))
                .FontFamily(PupilPassportFonts.Fredoka).FontSize(10.5f).FontColor(Navy);
            var tips = parent ? m.UsageParentTips ?? [] : m.UsageTeacherTips ?? [];
            col.Item().PaddingTop(4).Column(list =>
            {
                foreach (var tip in tips)
                {
                    list.Item().PaddingBottom(2).Row(r =>
                    {
                        r.ConstantItem(8).Text("-").FontColor(Navy);
                        r.RelativeItem().Text(NormalizePdfLine(tip)).FontSize(9f).LineHeight(1.45f);
                    });
                }
            });
        });
    }

    private static void WriteBlock(IContainer c, string title, string sub, int lines)
    {
        c.BorderTop(1.5f).BorderColor(Navy).PaddingTop(6).Column(col =>
        {
            col.Item().Text(title).FontFamily(PupilPassportFonts.Fredoka).FontSize(10.5f).FontColor(Navy);
            col.Item().PaddingTop(1).Text(sub).FontSize(8f).FontColor(Muted);
            for (var i = 0; i < lines; i++)
            {
                col.Item().PaddingTop(i == 0 ? 4 : 6).BorderBottom(1).BorderColor(LineMuted).Height(14);
            }
        });
    }

    private static void SignColumn(IContainer c, string label)
    {
        c.PaddingHorizontal(4).PaddingTop(28).BorderTop(1).BorderColor(Ink)
            .Text(label).FontSize(8.5f).FontColor(Muted);
    }

    private static string NormalizePdfLine(string text)
        => text
            .Replace('\u2013', '-')
            .Replace('\u2014', '-')
            .Replace('\u2212', '-');
}
