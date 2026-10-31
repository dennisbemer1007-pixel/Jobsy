using System.Globalization;
using System.Text.RegularExpressions;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.PupilPassport;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Jobsy.Infrastructure.Scholen.Passport;

internal static class PupilPassportGroep78PdfRenderer
{
    private const string BodyFont = "Helvetica";
    private const string MirrorFont = "Times New Roman";

    private static string T(PupilPassportPdfModel m, string key)
        => PupilPassportLocalizedCopy.Get(key, m.Language);

    private static readonly Color Navy = Color.FromHex("#0f2d5c");
    private static readonly Color NavyDeep = Color.FromHex("#0a2044");
    private static readonly Color Gold = Color.FromHex("#c9a227");
    private static readonly Color Cream = Color.FromHex("#fffcfa");
    private static readonly Color CreamWarm = Color.FromHex("#fffaf3");
    private static readonly Color Border = Color.FromHex("#e6d7c3");
    private static readonly Color Muted = Color.FromHex("#6b5d50");
    private static readonly Color Ink = Color.FromHex("#3d342b");
    private static readonly Color Track = Color.FromHex("#f3ebe0");
    private static readonly Color LineMuted = Color.FromHex("#d9cbb6");
    private static readonly Color FooterMuted = Color.FromHex("#8a7c6e");
    private static readonly Color Kicker = Color.FromHex("#c4552a");
    private static readonly Color NoticeBg = Color.FromHex("#fbf3e8");
    private static readonly Color MirrorGold = Color.FromHex("#ffd966");
    private static readonly Color MirrorSource = Color.FromHex("#b9c6da");
    private static readonly Color MockChrome = Color.FromHex("#c4552a");

    private static readonly float[] ValueBarWidths = [0.90f, 0.80f, 0.70f, 0.62f, 0.50f];

    private static float Mm(float mm) => mm * 72f / 25.4f;

    internal static byte[] Render(PupilPassportPdfModel m)
    {
        PupilPassportFonts.EnsureRegistered();
        return BuildDocument(m).GeneratePdf();
    }

    internal static byte[] RenderFirstPagePng(PupilPassportPdfModel m, int dpi)
    {
        PupilPassportFonts.EnsureRegistered();
        return BuildDocument(m).GenerateImages(new ImageGenerationSettings
        {
            RasterDpi = dpi,
            ImageFormat = ImageFormat.Png
        }).First();
    }

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

    private static void ApplyPageShell(PageDescriptor page, PupilPassportPdfModel m)
    {
        page.Size(PageSizes.A4);
        page.PageColor(Cream);
        page.MarginHorizontal(Mm(15));
        page.MarginVertical(Mm(14));
        page.DefaultTextStyle(x => x.FontSize(10f).FontColor(Ink).FontFamily(BodyFont));
        if (m.G78BijlagenMockChrome)
        {
            page.Background().AlignTop().AlignRight().PaddingTop(Mm(2)).PaddingRight(Mm(1))
                .Text(MockChromeLabel(m))
                .FontSize(7.5f).SemiBold().FontColor(MockChrome).LetterSpacing(0.6f);
        }
    }

    private static string MockChromeLabel(PupilPassportPdfModel m)
    {
        const string key = "LeerlingPassport.G78.MockChrome";
        var localized = T(m, key);
        return string.Equals(localized, key, StringComparison.Ordinal)
            ? "VOORBEELDDATA · mockup"
            : localized;
    }

    private static void CoverPage(PageDescriptor page, PupilPassportPdfModel m)
    {
        ApplyPageShell(page, m);
        page.Header().Element(c => CoverHeader(c, m));
        page.Footer().Element(c => PageFooter(c, m, T(m, "LeerlingPassport.G78.Footer.Label.Cover")));

        page.Content().Column(col =>
        {
            col.Item().PaddingTop(Mm(55)).Text(T(m, "LeerlingPassport.G78.Cover.Kicker"))
                .FontSize(10).SemiBold().FontColor(Kicker).LetterSpacing(1.2f);
            col.Item().PaddingTop(4).Text(T(m, "LeerlingPassport.G78.Cover.Title"))
                .Bold().FontSize(34).FontColor(Navy).LineHeight(1.08f);
            col.Item().PaddingTop(8).BorderLeft(3).BorderColor(Gold).PaddingLeft(10)
                .Text(T(m, "LeerlingPassport.G78.Cover.Intro")).FontSize(12).LineHeight(1.45f);

            col.Item().PaddingTop(Mm(30)).Row(row =>
            {
                row.RelativeItem().Element(c => CoverMetaBox(c, m));
                row.ConstantItem(Mm(4));
                row.RelativeItem().Element(c => CoverTocBox(c, m));
            });
        });
    }

    private static void CoverHeader(IContainer c, PupilPassportPdfModel m)
    {
        c.PaddingBottom(6).BorderBottom(1).BorderColor(Navy).Row(row =>
        {
            var logoSvg = PupilPassportAssets.LogoHorizontalSvg();
            if (!string.IsNullOrWhiteSpace(logoSvg))
            {
                row.ConstantItem(Mm(42)).Height(Mm(16)).Svg(logoSvg).FitArea();
            }

            row.RelativeItem();
            row.ConstantItem(Mm(46)).AlignRight().Column(right =>
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
                .Bold().FontSize(12.5f).FontColor(Navy);
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
                .Bold().FontSize(12.5f).FontColor(Navy);
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
            T(m, "LeerlingPassport.G78.Footer.Label.Part1"),
            PupilPassportG78Art.KreeftKijker,
            artHeightMm: 22);

        page.Content().Column(col =>
        {
            col.Item().Element(c => MirrorBox(c, m));
            col.Item().PaddingTop(Mm(6)).Element(c => SectionBox(c, T(m, "LeerlingPassport.G78.Riasec.Title"),
                T(m, "LeerlingPassport.G78.Riasec.Sub"), inner => RiasecBars(inner, m)));
            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().MinWidth(0).Element(c => SectionBox(c, T(m, "LeerlingPassport.G78.Culture.Title"), null,
                    inner => CultureSliders(inner, m)));
                row.ConstantItem(8);
                row.RelativeItem().MinWidth(0).Element(c => SectionBox(c, T(m, "LeerlingPassport.G78.Strengths.Title"),
                    T(m, "LeerlingPassport.G78.Strengths.Sub"), inner => StrengthChips(inner, m)));
            });
        });
    }

    private static void SubjectsPage(PageDescriptor page, PupilPassportPdfModel m, int pageNo)
    {
        InnerPage(page, m, pageNo,
            T(m, "LeerlingPassport.G78.Header.Part2"),
            T(m, "LeerlingPassport.G78.Page.Subjects"),
            T(m, "LeerlingPassport.G78.Footer.Label.Part2"),
            PupilPassportG78Art.KreeftSchild,
            artHeightMm: 22);

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

                                var weight = i == 0 ? 1.2f : 1f;
                                row.RelativeItem(weight).MinWidth(0)
                                    .Element(g => SubjectGroupColumn(g, i < groups.Count ? groups[i] : null, i));
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
                row.RelativeItem().MinWidth(0).Element(c => SectionBox(c, T(m, "LeerlingPassport.G78.Likes.Title"), null, inner =>
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
                            foreach (var like in likes.Take(5))
                            {
                                r.AutoItem().PaddingRight(4).PaddingBottom(3)
                                    .Background(CreamWarm).Border(1).BorderColor(Gold)
                                    .CornerRadius(Mm(1))
                                    .PaddingHorizontal(6).PaddingVertical(3)
                                    .Text(TruncateLabel(like)).FontSize(9.5f);
                            }
                        });
                    }
                }));
                row.ConstantItem(8);
                row.RelativeItem().MinWidth(0).Element(c => TalkBox(c, m, FirstTalkQuestions(m)));
            });
        });
    }

    private static void DirectionsPage(PageDescriptor page, PupilPassportPdfModel m, int pageNo)
    {
        InnerPage(page, m, pageNo,
            T(m, "LeerlingPassport.G78.Header.Where"),
            T(m, "LeerlingPassport.G78.Page.Where"),
            T(m, "LeerlingPassport.G78.Footer.Label.Where"),
            PupilPassportG78Art.KreeftKaart,
            artHeightMm: 22);

        page.Content().Column(col =>
        {
            col.Item().PaddingTop(Mm(5)).BorderLeft(3).BorderColor(Gold).PaddingLeft(Mm(4))
                .Text(T(m, "LeerlingPassport.G78.Where.Intro")).FontSize(10.5f).LineHeight(1.45f);

            col.Item().PaddingTop(Mm(5)).Row(row =>
            {
                row.RelativeItem().MinWidth(0).Element(c => SectionBox(c, T(m, "LeerlingPassport.G78.Beroep.Title"), null,
                    inner => BeroepList(inner, m)));
                row.ConstantItem(8);
                row.RelativeItem().MinWidth(0).Element(c => SectionBox(c, T(m, "LeerlingPassport.G78.School.Title"),
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
                                        .CornerRadius(Mm(1)).Padding(Mm(3)).Column(card =>
                                        {
                                            card.Item().Text(TruncateLabel(d.Title, 40)).Bold().FontSize(9f).FontColor(Navy);
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
                col.Item().PaddingTop(6).Border(2).BorderColor(Gold).CornerRadius(Mm(1)).Padding(Mm(4)).Column(dream =>
                {
                    dream.Item().Text(string.Format(
                            CultureInfo.InvariantCulture,
                            T(m, "LeerlingPassport.G78.Dream.Heading"),
                            m.DreamJobTitle!.Trim()))
                        .Bold().FontSize(11).FontColor(Navy);
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
                var cards = (m.OccupationCards ?? []).Take(5).ToList();
                jc.Row(row =>
                {
                    for (var i = 0; i < 5; i++)
                    {
                        if (i > 0)
                        {
                            row.ConstantItem(4);
                        }

                        row.RelativeItem().MinWidth(0).Element(card =>
                        {
                            if (i >= keys.Count)
                            {
                                card.MinHeight(48);
                                return;
                            }

                            var key = keys[i];
                            var occ = i < cards.Count ? cards[i] : null;
                            var title = occ?.Title ?? PupilPassportLocalizedCopy.JobTitle(key, m.Language);
                            var sub = occ is not null && occ.FitTags.Count > 0
                                ? string.Join(" & ", occ.FitTags.Take(2))
                                : JobCardSubtitle(m, key);
                            card.Background(Colors.White).Border(1).BorderColor(Border).CornerRadius(Mm(1))
                                .Padding(Mm(2.5f)).Column(inner =>
                                {
                                    inner.Item().AlignCenter().Height(18)
                                        .Svg(PupilPassportG78BijlagenVisuals.JobEmojiGlyph(key)).FitArea();
                                    inner.Item().AlignCenter().PaddingTop(2).Text(TruncateLabel(title, 22)).FontSize(9f).SemiBold()
                                    .FontColor(Ink).LineHeight(1.15f);
                                if (!string.IsNullOrWhiteSpace(sub))
                                {
                                    inner.Item().AlignCenter().PaddingTop(2)
                                        .Text(TruncateLabel(sub, 28)).FontSize(7f).FontColor(Muted).LineHeight(1.1f);
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
            col.Item().Background(NoticeBg).BorderLeft(3).BorderColor(Kicker)
                .PaddingVertical(Mm(3)).PaddingHorizontal(Mm(4))
                .Text(T(m, "LeerlingPassport.G78.Usage.Notice")).FontSize(9f).LineHeight(1.5f);

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
        string footerLabel,
        PupilPassportG78Art art = PupilPassportG78Art.None,
        float? artHeightMm = null)
    {
        ApplyPageShell(page, m);
        page.Header().Element(c => InnerHeader(c, m, kicker, title, art, artHeightMm));
        page.Footer().Element(c => PageFooter(c, m, footerLabel, pageNo));
    }

    private static void InnerHeader(
        IContainer c,
        PupilPassportPdfModel m,
        string kicker,
        string title,
        PupilPassportG78Art art,
        float? artHeightMm)
    {
        var artHeight = Mm(artHeightMm ?? 18);
        c.PaddingBottom(Mm(4)).BorderBottom(1).BorderColor(Navy).Row(row =>
        {
            row.RelativeItem().MinWidth(0).Column(left =>
            {
                left.Item().Text(kicker).FontSize(10).SemiBold().FontColor(Kicker).LetterSpacing(0.6f);
                left.Item().Text(title).Bold().FontSize(22).FontColor(Navy);
            });
            row.ConstantItem(Mm(40)).AlignRight().AlignMiddle().Column(right =>
            {
                right.Item().AlignRight().Text(text =>
                {
                    text.Span(T(m, "LeerlingPassport.G78.Meta.CodeLabel") + " ").FontSize(8f).FontColor(Muted);
                    text.Span(m.DisplayCode).SemiBold();
                });
                right.Item().AlignRight().Text($"{m.ClassLabel} · {DateLine(m)}").FontSize(8f).FontColor(Muted);
            });

            var artBytes = PupilPassportAssets.Groep78ArtPng(art);
            if (artBytes.Length > 0)
            {
                row.ConstantItem(artHeight).Height(artHeight).Image(artBytes).FitArea();
            }
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
        c.PaddingBottom(Mm(5)).BorderTop(1.5f).BorderColor(Navy).PaddingTop(Mm(3)).Column(col =>
        {
            col.Item().Text(heading).Bold().FontSize(12.5f).FontColor(Navy);
            if (!string.IsNullOrWhiteSpace(sub))
            {
                col.Item().PaddingTop(1).Text(sub!).FontSize(9f).FontColor(Muted);
            }

            col.Item().PaddingTop(Mm(4)).Element(body);
        });
    }

    private static void MirrorBox(IContainer c, PupilPassportPdfModel m)
    {
        var raw = NormalizePdfLine(m.MirrorBody ?? m.StoryBody);
        var body = TrimMirrorSentences(raw);

        c.MinHeight(Mm(38)).CornerRadius(Mm(1.5f)).Background(Navy)
            .PaddingVertical(Mm(5)).PaddingHorizontal(Mm(6)).Column(col =>
            {
                col.Item().Text(T(m, "LeerlingPassport.G78.Mirror.Title"))
                    .Bold().FontSize(13).FontColor(MirrorGold);
                col.Item().PaddingTop(Mm(2.5f)).Text(body)
                    .FontFamily(MirrorFont).FontSize(12.5f).FontColor(Colors.White).LineHeight(1.65f);
                col.Item().PaddingTop(Mm(3)).Text(T(m, "LeerlingPassport.G78.Mirror.Source"))
                    .FontSize(7.5f).FontColor(MirrorSource);
            });
    }

    private static string TrimMirrorSentences(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return raw;
        }

        var parts = Regex.Split(raw.Trim(), @"(?<=[.!?])\s+")
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .Take(PupilG78MirrorTemplate.MaxSentences)
            .ToList();

        if (parts.Count == 0)
        {
            return raw.Length > 720 ? raw[..717] + "…" : raw;
        }

        return string.Join(' ', parts);
    }

    private static void RiasecBars(IContainer c, PupilPassportPdfModel m)
    {
        var bars = (m.RiasecBars ?? []).Take(6).ToList();
        var barHeight = Mm(3.5f);
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

                col.Item().PaddingBottom(3).Column(row =>
                {
                    row.Item().Row(r =>
                    {
                        r.ConstantItem(14).AlignMiddle().Height(12)
                            .Svg(PupilPassportG78BijlagenVisuals.RiasecGlyph(bar.Letter)).FitArea();
                        r.ConstantItem(78).AlignMiddle()
                            .Text(TruncateLabel(bar.Label, 24)).FontSize(9.5f).LineHeight(1.1f);
                        r.RelativeItem().AlignMiddle().Height(barHeight).Background(Track).Row(inner =>
                        {
                            if (ratio > 0.01f)
                            {
                                inner.RelativeItem(ratio).Height(barHeight).Background(Navy);
                            }

                            if (ratio < 0.99f)
                            {
                                inner.RelativeItem(Math.Max(0.01f, 1f - ratio));
                            }
                        });
                        r.ConstantItem(26).AlignMiddle().AlignRight().Text($"{tenths}/10").SemiBold()
                            .FontColor(Navy).FontSize(8.5f);
                    });
                    if (!string.IsNullOrWhiteSpace(desc))
                    {
                        row.Item().PaddingLeft(92).PaddingTop(1).Text(desc).FontSize(8.3f).FontColor(Muted)
                            .LineHeight(1.2f);
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
        var endpoints = SliderEndpoints(s.Label);
        c.Background(CreamWarm).CornerRadius(Mm(1)).Padding(Mm(3)).Column(col =>
        {
            col.Item().Text(TruncateLabel(s.Label, 32)).Bold().FontSize(9f).FontColor(Navy);
            col.Item().PaddingTop(Mm(2)).Height(12).Background(Track).AlignMiddle().Row(track =>
            {
                track.RelativeItem(Math.Max(0.001f, ratio - 0.05f));
                track.ConstantItem(12).Height(12).Background(Gold);
                track.RelativeItem(Math.Max(0.001f, 1f - ratio - 0.05f));
            });
            col.Item().PaddingTop(3).Row(lr =>
            {
                lr.RelativeItem().Text(endpoints.left).FontSize(7.5f).FontColor(Muted);
                lr.RelativeItem().AlignRight().Text(endpoints.right).FontSize(7.5f).FontColor(Muted);
            });
        });
    }

    private static (string left, string right) SliderEndpoints(string label)
    {
        var parts = label.Split(" of ", 2, StringSplitOptions.TrimEntries);
        if (parts.Length == 2)
        {
            return (parts[0].ToLowerInvariant(), parts[1].ToLowerInvariant());
        }

        parts = label.Split(" or ", 2, StringSplitOptions.TrimEntries);
        return parts.Length == 2
            ? (parts[0].ToLowerInvariant(), parts[1].ToLowerInvariant())
            : (label, "");
    }

    private static void StrengthChips(IContainer c, PupilPassportPdfModel m)
    {
        c.Column(col =>
        {
            col.Item().Row(row =>
            {
                var i = 0;
                foreach (var s in m.Strengths.Take(5))
                {
                    var gold = i < 2;
                    row.AutoItem().PaddingRight(3).PaddingBottom(3)
                        .CornerRadius(Mm(1))
                        .Background(gold ? Gold : Navy)
                        .PaddingHorizontal(6).PaddingVertical(3)
                        .Text(TruncateLabel(s.Title)).FontSize(9f)
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

        c.BorderTop(3).BorderColor(topColor).Background(Colors.White).CornerRadius(Mm(1))
            .Padding(Mm(3.5f)).Column(col =>
        {
            col.Item().Text(group.Title).Bold().FontSize(9.5f).FontColor(Navy).LineHeight(1.1f);
            col.Item().PaddingTop(1).Text(group.Intro).FontSize(7.5f).FontColor(Muted).LineHeight(1.15f);
            foreach (var subject in group.Subjects.Take(3))
            {
                col.Item().PaddingTop(3).BorderBottom(1).BorderColor(Color.FromHex("#f0e6d8"))
                    .PaddingBottom(2).Row(line =>
                    {
                        line.ConstantItem(12).AlignMiddle().Height(10)
                            .Svg(PupilPassportG78BijlagenVisuals.SubjectGlyph(subject.SubjectKey)).FitArea();
                        line.RelativeItem().MinWidth(0).AlignMiddle()
                            .Text(TruncateLabel(subject.Label)).FontSize(8.5f).LineHeight(1.15f);
                    });
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
            row.ConstantItem(78).Text(TruncateLabel(label)).FontSize(8.5f);
            row.RelativeItem().Height(Mm(3.5f)).Background(Track).Row(inner =>
            {
                var ratio = Math.Clamp(widthRatio, 0.05f, 1f);
                inner.RelativeItem(ratio).Height(Mm(3.5f)).Background(Gold);
                if (ratio < 0.99f)
                {
                    inner.RelativeItem(Math.Max(0.01f, 1f - ratio));
                }
            });
        });
    }

    private static IReadOnlyList<string> FirstTalkQuestions(PupilPassportPdfModel m)
    {
        if (m.SubjectGroups is not { Count: > 0 } groups)
        {
            return [];
        }

        var group = groups[0];
        return group.ConversationQuestions.Take(3).Select(NormalizePdfLine).ToList();
    }

    private static void TalkBox(IContainer c, PupilPassportPdfModel m, IReadOnlyList<string> questions)
    {
        c.MinWidth(0).CornerRadius(Mm(1.5f)).Background(Navy)
            .PaddingVertical(Mm(4)).PaddingHorizontal(Mm(5)).Column(col =>
        {
            col.Item().Text(T(m, "LeerlingPassport.G78.Talk.Heading")).Bold().FontColor(MirrorGold).FontSize(10f);
            foreach (var q in questions)
            {
                col.Item().PaddingTop(3).Text("• " + q).FontColor(Colors.White).FontSize(10f).LineHeight(1.5f);
            }

            if (questions.Count == 0)
            {
                col.Item().PaddingTop(3).Text(T(m, "LeerlingPassport.G78.Talk.Empty")).FontColor(Colors.White).FontSize(10f);
            }
        });
    }

    private static void SamenBox(IContainer c, PupilPassportPdfModel m)
    {
        c.CornerRadius(Mm(1.5f)).Background(Navy).Padding(8).Column(col =>
        {
            col.Item().Text(T(m, "LeerlingPassport.G78.Samen.Heading")).Bold().FontColor(MirrorGold).FontSize(9.5f);
            col.Item().PaddingTop(3).Text("- " + NormalizePdfLine(m.SamenAssignment!))
                .FontColor(Colors.White).FontSize(9f).LineHeight(1.35f);
        });
    }

    private static void BeroepList(IContainer c, PupilPassportPdfModel m)
    {
        c.Column(col =>
        {
            foreach (var b in (m.BeroepssoortRanks ?? []).Take(10))
            {
                col.Item().PaddingBottom(2).Row(row =>
                {
                    row.ConstantItem(14).Text(b.Rank.ToString()).SemiBold().FontColor(Navy);
                    row.RelativeItem().MinWidth(0).Column(inner =>
                    {
                        inner.Item().Text(TruncateLabel(b.Title, 36)).Bold().FontSize(8.5f).LineHeight(1.15f);
                        if (!string.IsNullOrWhiteSpace(b.Omdat))
                        {
                            inner.Item().Text(NormalizePdfLine(b.Omdat)).FontSize(7.5f).FontColor(Muted)
                                .LineHeight(1.15f);
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

        var circle = Mm(11);
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
                row.RelativeItem().MinWidth(0).Column(col =>
                {
                    col.Item().Row(line =>
                    {
                        if (i > 0)
                        {
                            line.RelativeItem().AlignMiddle().Height(4)
                                .Svg(PupilPassportG78BijlagenVisuals.StepDashLine()).FitArea();
                        }

                        line.ConstantItem(circle).Height(circle)
                            .Background(isFirst || isGoal ? Navy : Gold)
                            .AlignCenter().AlignMiddle()
                            .Text(isFirst
                                ? T(m, "LeerlingPassport.G78.Route.Now")
                                : isGoal
                                    ? T(m, "LeerlingPassport.G78.Route.Goal")
                                    : i.ToString(CultureInfo.InvariantCulture))
                            .FontSize(7f).SemiBold()
                            .FontColor(isFirst || isGoal ? Colors.White : NavyDeep);

                        if (i < steps.Count - 1)
                        {
                            line.RelativeItem().AlignMiddle().Height(4)
                                .Svg(PupilPassportG78BijlagenVisuals.StepDashLine()).FitArea();
                        }
                    });
                    col.Item().PaddingTop(2).AlignCenter().Text(TruncateLabel(NormalizePdfLine(step.Title), 20))
                        .FontSize(7.5f).FontColor(Ink).LineHeight(1.1f);
                    if (!string.IsNullOrWhiteSpace(step.Detail))
                    {
                        col.Item().AlignCenter().Text(TruncateLabel(NormalizePdfLine(step.Detail), 24))
                            .FontSize(7f).FontColor(Muted).LineHeight(1.1f);
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
                .Bold().FontSize(10.5f).FontColor(Navy);
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
            col.Item().Text(title).Bold().FontSize(10.5f).FontColor(Navy);
            col.Item().PaddingTop(1).Text(sub).FontSize(8f).FontColor(Muted);
            for (var i = 0; i < lines; i++)
            {
                col.Item().PaddingTop(i == 0 ? 4 : 6).BorderBottom(1).BorderColor(LineMuted).Height(Mm(8));
            }
        });
    }

    private static void SignColumn(IContainer c, string label)
    {
        c.PaddingHorizontal(4).PaddingTop(Mm(10)).BorderTop(1).BorderColor(Ink)
            .Text(label).FontSize(8.5f).FontColor(Muted);
    }

    private static string TruncateLabel(string text, int maxChars = 26)
    {
        var normalized = NormalizePdfLine(text);
        if (normalized.Length <= maxChars)
        {
            return normalized;
        }

        return normalized[..Math.Max(0, maxChars - 1)] + "…";
    }

    internal static string NormalizePdfLine(string text)
        => text
            .Replace('\u2013', '-')
            .Replace('\u2014', '-')
            .Replace('\u2212', '-');
}
