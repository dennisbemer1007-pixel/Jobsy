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
    private static readonly Color SoftGoldTint = Color.FromHex("#fff8e6");
    private static readonly Color MockChrome = Color.FromHex("#c4552a");

    private static float Mm(float mm) => mm * 72f / 25.4f;

    /// <summary>Calm highlight: warm cream fill, thin border, coloured left accent strip.</summary>
    private static void SoftAccentPanel(
        IContainer c,
        Color accent,
        Color? fill = null,
        Action<IContainer>? body = null)
    {
        c.CornerRadius(Mm(1.5f)).Row(row =>
        {
            row.ConstantItem(Mm(1.2f)).Background(accent);
            row.RelativeItem().MinWidth(0)
                .Background(fill ?? CreamWarm)
                .Border(1).BorderColor(Border)
                .PaddingVertical(Mm(3)).PaddingHorizontal(Mm(4))
                .Element(inner => body?.Invoke(inner));
        });
    }

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

        var coverKreeft = PupilPassportAssets.Groep78ArtPng(PupilPassportG78Art.KreeftKaart);
        page.Content().Layers(layers =>
        {
            layers.Layer().AlignBottom().AlignRight().Width(Mm(95)).Height(Mm(32))
                .Svg(PupilPassportG78BijlagenVisuals.CoverSparkleDecor()).FitArea();

            layers.PrimaryLayer().Column(col =>
            {
                col.Item().Height(Mm(3)).Background(SoftGoldTint);
                col.Item().PaddingTop(Mm(41)).Text(T(m, "LeerlingPassport.G78.Cover.Kicker"))
                    .FontSize(10).SemiBold().FontColor(Kicker).LetterSpacing(1.2f);
                col.Item().PaddingTop(4).Text(T(m, "LeerlingPassport.G78.Cover.Title"))
                    .Bold().FontSize(34).FontColor(Navy).LineHeight(1.08f);
                col.Item().PaddingTop(Mm(3)).Width(Mm(30)).Height(2).Background(Gold);
                col.Item().PaddingTop(8).BorderLeft(3).BorderColor(Gold).PaddingLeft(10)
                    .Text(T(m, "LeerlingPassport.G78.Cover.Intro")).FontSize(12).LineHeight(1.45f);

                col.Item().PaddingTop(Mm(30)).Row(row =>
                {
                    row.RelativeItem().Element(c => CoverMetaBox(c, m));
                    row.ConstantItem(Mm(4.5f));
                    row.RelativeItem().Element(c => CoverTocBox(c, m));
                });
            });

            if (coverKreeft.Length > 0)
            {
                layers.Layer().AlignBottom().AlignRight().PaddingBottom(Mm(10))
                    .Width(Mm(50)).Height(Mm(50))
                    .Image(coverKreeft).FitArea();
            }
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
            col.Item().PaddingTop(10).Element(x => TocLine(x, "1", T(m, "LeerlingPassport.G78.Toc.1")));
            col.Item().PaddingTop(7).Element(x => TocLine(x, "2", T(m, "LeerlingPassport.G78.Toc.2")));
            col.Item().PaddingTop(7).Element(x => TocLine(x, "3", T(m, "LeerlingPassport.G78.Toc.3")));
            col.Item().PaddingTop(7).Element(x => TocLine(x, "4", T(m, "LeerlingPassport.G78.Toc.4")));
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
            compactHeader: true);

        page.Content().Column(col =>
        {
            col.Spacing(Mm(2));
            col.Item().Element(c => MirrorBox(c, m));

            col.Item().Element(c => SectionBox(c, T(m, "LeerlingPassport.G78.Riasec.Title"),
                T(m, "LeerlingPassport.G78.Riasec.Sub"), inner =>
                {
                    if (m.G78RiasecPending)
                    {
                        inner.Text(T(m, "LeerlingPassport.G78.Riasec.Pending"))
                            .FontSize(9.5f).Italic().FontColor(Muted).LineHeight(1.45f);
                    }
                    else
                    {
                        inner.Element(x => RiasecBars(x, m, spacious: false));
                    }
                }, goldRule: true, compact: true));
            col.Item().Element(c => SectionBox(c, T(m, "LeerlingPassport.G78.Strengths.Title"),
                T(m, "LeerlingPassport.G78.Strengths.Sub"), inner => StrengthChips(inner, m, spacious: false, askQuestion: false),
                goldRule: true, compact: true));

            if (m.DnaTiles is { Count: > 0 })
            {
                col.Item().PaddingTop(Mm(1)).Element(c => G78PassportTileGrid(c, m));
            }
        });
    }

    private static void G78PassportTileGrid(IContainer c, PupilPassportPdfModel m)
    {
        var tiles = m.DnaTiles.Take(4).ToList();
        c.Column(outer =>
        {
            for (var i = 0; i < tiles.Count; i += 2)
            {
                var rowIndex = i;
                outer.Item().PaddingBottom(Mm(1.5f)).Row(row =>
                {
                    row.RelativeItem().MinWidth(0).PaddingRight(Mm(1.5f))
                        .Element(left => G78PassportTileCard(left, tiles[rowIndex]));
                    if (rowIndex + 1 < tiles.Count)
                    {
                        row.RelativeItem().MinWidth(0).PaddingLeft(Mm(1.5f))
                            .Element(right => G78PassportTileCard(right, tiles[rowIndex + 1]));
                    }
                });
            }
        });
    }

    private static void G78PassportTileCard(IContainer c, (string Label, string Value) tile)
    {
        c.Background(CreamWarm)
            .Border(1).BorderColor(Gold)
            .CornerRadius(Mm(1.2f))
            .Padding(Mm(1.5f))
            .Column(col =>
        {
            col.Item().Text(TruncateLabel(tile.Label, 28)).Bold().FontSize(8.5f).FontColor(Navy);
            col.Item().PaddingTop(1).Text(NormalizePdfLine(tile.Value))
                .FontSize(7.75f).FontColor(Ink).LineHeight(1.35f);
        });
    }

    private static void G78PassportGrowthCard(IContainer c, PupilPassportPdfModel m)
    {
        var items = m.G78GrowthItems!.Take(2).ToList();
        var foot = items.Select(i => i.Why).FirstOrDefault(l => !string.IsNullOrWhiteSpace(l));
        c.Background(CreamWarm)
            .Border(1).BorderColor(Gold)
            .CornerRadius(Mm(1.2f))
            .Padding(Mm(1.5f))
            .Column(col =>
            {
                col.Item().Text(T(m, "LeerlingPassport.G78.Growth.Title"))
                    .Bold().FontSize(9f).FontColor(Navy);
                col.Item().PaddingTop(2).Row(row =>
                {
                    for (var i = 0; i < items.Count; i++)
                    {
                        if (i > 0)
                        {
                            row.ConstantItem(Mm(2));
                        }

                        row.RelativeItem().MinWidth(0).Text(TruncateLabel(items[i].Label, 28))
                            .SemiBold().FontSize(8.25f).FontColor(Ink).LineHeight(1.2f);
                    }
                });
                if (!string.IsNullOrWhiteSpace(foot))
                {
                    col.Item().PaddingTop(2).Text(NormalizePdfLine(foot))
                        .FontSize(7.5f).FontColor(Muted).LineHeight(1.25f);
                }
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
            var compact = m.G78GrowthItems is { Count: > 0 };
            var gap = compact ? Mm(2.5f) : Mm(4);
            col.Spacing(gap);
            col.Item().PaddingBottom(Mm(1)).BorderBottom(0.75f).BorderColor(Gold);
            col.Item().PaddingTop(compact ? Mm(2) : Mm(4)).Element(c => CultureSlidersFullWidth(c, m, compact));
            col.Item().PaddingTop(gap).Element(c => G78LeukBelangrijkColumnsEqual(c, m));
            var talk = FirstTalkQuestions(m);
            if (talk.Count > 0)
            {
                col.Item().PaddingTop(gap).Element(c => TalkBox(c, m, talk));
            }

            var showGrowth = m.G78GrowthItems is { Count: > 0 };
            if (m.SubjectGroups is { Count: > 0 } && !showGrowth)
            {
                col.Item().PaddingTop(gap).Element(c => SubjectGroupsCompact(c, m));
            }

            if (showGrowth)
            {
                col.Item().PaddingTop(Mm(1.5f)).Element(c => G78PassportGrowthCard(c, m));
            }
        });
    }

    private static void CultureSlidersFullWidth(IContainer c, PupilPassportPdfModel m, bool compact = false)
    {
        var sliders = (m.CultureSliders ?? []).Take(3).ToList();
        if (sliders.Count == 0)
        {
            c.Text(T(m, "LeerlingPassport.G78.Culture.Empty")).FontSize(8f).FontColor(Muted);
            return;
        }

        c.Row(row =>
        {
            for (var i = 0; i < sliders.Count; i++)
            {
                if (i > 0)
                {
                    row.ConstantItem(compact ? Mm(2.5f) : Mm(4));
                }

                row.RelativeItem().MinWidth(0).Element(x => CultureSliderCard(x, sliders[i], spacious: !compact));
            }
        });
    }

    private static void G78LeukBelangrijkColumnsEqual(IContainer c, PupilPassportPdfModel m)
    {
        var school = m.G78SchoolLeukItems ?? [];
        var hobby = m.G78HobbyLeukItems ?? m.Likes ?? [];
        var belangrijk = m.G78BelangrijkItems ?? [];
        var showBelangrijk = belangrijk.Count > 0;

        c.Row(row =>
        {
            row.RelativeItem().MinWidth(0)
                .Element(left => LeukGroupedColumn(left, m, school, hobby));
            if (showBelangrijk)
            {
                row.ConstantItem(Mm(4));
                row.RelativeItem().MinWidth(0).Element(right =>
                    PreferencesColumnWrapped(right, T(m, "LeerlingPassport.G78.Col.Belangrijk"), belangrijk));
            }
        });
    }

    private static void SubjectGroupsCompact(IContainer c, PupilPassportPdfModel m)
    {
        var groups = m.SubjectGroups ?? [];
        if (groups.Count == 0)
        {
            c.Text(T(m, "LeerlingPassport.G78.Subjects.Empty"))
                .FontSize(9.5f).FontColor(Muted).Italic();
            return;
        }

        var pair = groups.Take(2).ToList();
        c.Row(row =>
        {
            for (var i = 0; i < pair.Count; i++)
            {
                if (i > 0)
                {
                    row.ConstantItem(Mm(4));
                }

                row.RelativeItem().MinWidth(0).Element(x => SubjectGroupCard(x, pair[i]));
            }
        });
    }

    private static void SubjectGroupCard(IContainer c, PupilPassportSubjectGroup g)
    {
        c.Background(CreamWarm)
            .Border(1).BorderColor(Gold)
            .CornerRadius(Mm(1.5f))
            .Padding(Mm(3.5f))
            .Column(col =>
            {
                col.Item().Text(g.Title).Bold().FontSize(10.5f).FontColor(Navy);
                if (!string.IsNullOrWhiteSpace(g.Intro))
                {
                    col.Item().PaddingTop(1).Text(g.Intro).FontSize(8.5f).FontColor(Muted);
                }

                foreach (var line in g.Subjects.Take(6))
                {
                    col.Item().PaddingTop(3).Row(row =>
                    {
                        row.ConstantItem(8).Text("·").FontSize(10f).FontColor(Gold);
                        row.RelativeItem().MinWidth(0).Text(TruncateLabel(line.Label, 36))
                            .FontSize(9f).FontColor(Ink).LineHeight(1.25f);
                        row.ConstantItem(Mm(24)).AlignMiddle().AlignRight()
                            .Element(pill => SubjectScorePill(pill, line.Leuk, line.Goed));
                    });
                }
            });
    }

    private static void SubjectScorePill(IContainer c, int leuk, int goed)
    {
        c.Background(Track)
            .Border(1).BorderColor(Border)
            .CornerRadius(Mm(1))
            .PaddingHorizontal(4).PaddingVertical(2)
            .Text($"leuk {leuk} · goed {goed}")
            .FontSize(7f).FontColor(Muted);
    }

    private static void DirectionsPage(PageDescriptor page, PupilPassportPdfModel m, int pageNo)
    {
        InnerPage(page, m, pageNo,
            T(m, "LeerlingPassport.G78.Header.Where"),
            T(m, "LeerlingPassport.G78.Page.Where"),
            T(m, "LeerlingPassport.G78.Footer.Label.Where"));

        page.Content().Column(col =>
        {
            col.Item().PaddingTop(Mm(2)).BorderLeft(3).BorderColor(Gold).PaddingLeft(Mm(4))
                .Text(T(m, "LeerlingPassport.G78.Where.Intro")).FontSize(10.5f).LineHeight(1.45f);

            col.Item().PaddingTop(Mm(3)).Row(row =>
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
                                    list.Item().PaddingBottom(4).Background(CreamWarm).BorderTop(3).BorderColor(Gold)
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

            col.Item().PaddingTop(Mm(3)).Element(c =>
            {
                var jobCards = (m.OccupationCards ?? []).Take(5).ToList();
                var jobsTitle = jobCards.Count is > 0 and < 5
                    ? T(m, "LeerlingPassport.G78.Jobs.TitleFew")
                    : T(m, "LeerlingPassport.G78.Jobs.Title");
                var jobsSub = jobCards.Count is > 0 and < 5
                    ? T(m, "LeerlingPassport.G78.Jobs.FewSub")
                    : null;
                SectionBox(c, jobsTitle, jobsSub, inner => inner.Element(jc => JobCardsRow(jc, m, jobCards)));
            });

            col.Item().PaddingTop(Mm(3)).Column(tail =>
            {
                if (!string.IsNullOrWhiteSpace(m.DreamJobTitle))
                {
                    tail.Item().Element(c => DreamBox(c, m));
                }

                if (!string.IsNullOrWhiteSpace(m.SamenAssignment))
                {
                    tail.Item().PaddingTop(Mm(3)).Element(c => SamenBox(c, m));
                }
            });

            void JobCardsRow(IContainer jc, PupilPassportPdfModel model, IReadOnlyList<PupilPassportOccupationCard> cards)
            {
                if (cards.Count == 0)
                {
                    jc.Text(T(model, "LeerlingPassport.G78.Jobs.FewSub"))
                        .FontSize(9f).Italic().FontColor(Muted);
                    return;
                }

                var spacious = cards.Count <= 2;
                var iconH = spacious ? 26f : 20f;
                var pad = spacious ? Mm(4.5f) : Mm(3f);
                jc.Row(row =>
                {
                    for (var i = 0; i < cards.Count; i++)
                    {
                        if (i > 0)
                        {
                            row.ConstantItem(spacious ? 8 : 4);
                        }

                        var occ = cards[i];
                        var jobKey = (model.TopJobKeys ?? []).ElementAtOrDefault(i);
                        if (string.IsNullOrWhiteSpace(jobKey))
                        {
                            return;
                        }

                        row.RelativeItem().MinWidth(0).Element(card =>
                        {
                            var sub = !string.IsNullOrWhiteSpace(occ.DomainLabel)
                                ? occ.DomainLabel
                                : occ.FitTags.Count > 0
                                    ? string.Join(" & ", occ.FitTags.Take(2))
                                    : JobCardSubtitle(model, jobKey);
                            card.Background(CreamWarm).Border(1).BorderColor(Border).CornerRadius(Mm(1))
                                .Padding(pad).Column(inner =>
                                {
                                    inner.Item().AlignCenter().Height(iconH)
                                        .Svg(PupilPassportG78BijlagenVisuals.JobEmojiGlyph(jobKey)).FitArea();
                                    if (occ.MatchPercent is > 0)
                                    {
                                        inner.Item().AlignCenter().PaddingTop(2)
                                            .Text(FormatJobMatchPercent(model, occ.MatchPercent.Value))
                                            .FontSize(spacious ? 8.5f : 8f).SemiBold().FontColor(Kicker);
                                    }

                                    inner.Item().AlignCenter().PaddingTop(2)
                                        .Text(TruncateLabel(occ.Title, spacious ? 26 : 22))
                                        .FontSize(spacious ? 10f : 9f).SemiBold()
                                        .FontColor(Ink).LineHeight(1.15f);
                                    if (!string.IsNullOrWhiteSpace(sub))
                                    {
                                        inner.Item().AlignCenter().PaddingTop(2)
                                            .Text(TruncateLabel(sub, spacious ? 32 : 28))
                                            .FontSize(7f).FontColor(Muted).LineHeight(1.1f);
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

            col.Item().PaddingTop(Mm(5)).Row(row =>
            {
                row.RelativeItem(1.25f).Element(c => UsageColumn(c, m, parent: true));
                row.ConstantItem(10);
                row.RelativeItem().Element(c => UsageColumn(c, m, parent: false));
            });

            col.Item().PaddingTop(Mm(5)).Column(blocks =>
            {
                blocks.Item().Element(c => WriteBlock(c,
                T(m, "LeerlingPassport.G78.Write.Student"),
                T(m, "LeerlingPassport.G78.Write.Student.Sub"),
                3));
                blocks.Item().PaddingTop(Mm(5)).Element(c => WriteBlock(c,
                T(m, "LeerlingPassport.G78.Write.Parent"),
                T(m, "LeerlingPassport.G78.Write.Parent.Sub"),
                2));
                blocks.Item().PaddingTop(Mm(5)).Column(together =>
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
        });
    }

    private static void DreamBox(IContainer c, PupilPassportPdfModel m)
    {
        SoftAccentPanel(c, Gold, SoftGoldTint, inner =>
        {
            inner.Column(dream =>
            {
                dream.Item().Text(string.Format(
                        CultureInfo.InvariantCulture,
                        T(m, "LeerlingPassport.G78.Dream.Heading"),
                        m.DreamJobTitle!.Trim()))
                    .Bold().FontSize(11.5f).FontColor(Navy);
                dream.Item().PaddingTop(2).Text(T(m, "LeerlingPassport.G78.Dream.Sub"))
                    .FontSize(8.5f).FontColor(Muted);
                dream.Item().PaddingTop(Mm(3)).Element(x => DreamTimeline(x, m));
                dream.Item().PaddingTop(Mm(2)).Text(T(m, "LeerlingPassport.G78.Dream.Question"))
                    .Italic().FontSize(9f).FontColor(Navy);
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
        bool compactHeader = false)
    {
        ApplyPageShell(page, m);
        page.Header().Element(c => InnerHeader(c, m, kicker, title, compactHeader));
        page.Footer().Element(c => PageFooter(c, m, footerLabel, pageNo));
    }

    private static void InnerHeader(
        IContainer c,
        PupilPassportPdfModel m,
        string kicker,
        string title,
        bool compact = false)
    {
        c.PaddingBottom(compact ? Mm(2.5f) : Mm(4)).BorderBottom(0.75f).BorderColor(Gold).Row(row =>
        {
            row.RelativeItem().MinWidth(0).Column(left =>
            {
                left.Item().Text(kicker).FontSize(10).SemiBold().FontColor(Kicker).LetterSpacing(0.6f);
                left.Item().Text(title).Bold().FontSize(compact ? 20 : 22).FontColor(Navy);
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
        Action<IContainer> body,
        bool goldRule = false,
        bool compact = false)
    {
        var ruleColor = goldRule ? Gold : Navy;
        var ruleSize = goldRule ? 0.75f : 1.5f;
        var padBottom = compact ? Mm(2) : Mm(5);
        var padTop = compact ? Mm(2) : Mm(3);
        var bodyGap = compact ? Mm(2) : Mm(4);
        var titleSize = compact ? 11.5f : 12.5f;
        c.PaddingBottom(padBottom).BorderTop(ruleSize).BorderColor(ruleColor).PaddingTop(padTop).Column(col =>
        {
            col.Item().Text(heading).Bold().FontSize(titleSize).FontColor(Navy);
            if (!string.IsNullOrWhiteSpace(sub))
            {
                col.Item().PaddingTop(1).Text(sub!).FontSize(compact ? 8.5f : 9f).FontColor(Muted);
            }

            col.Item().PaddingTop(bodyGap).Element(body);
        });
    }

    private static void LeukGroupedColumn(
        IContainer c,
        PupilPassportPdfModel m,
        IReadOnlyList<string> school,
        IReadOnlyList<string> hobby)
    {
        c.Background(CreamWarm)
            .Border(1).BorderColor(Gold)
            .CornerRadius(Mm(1.5f))
            .Padding(Mm(3.5f))
            .Column(col =>
            {
                col.Item().Text(T(m, "LeerlingPassport.G78.Col.Leuk")).Bold().FontSize(10.5f).FontColor(Navy);
                if (school.Count > 0)
                {
                    col.Item().PaddingTop(4).Text(T(m, "LeerlingPassport.G78.Col.OpSchool"))
                        .FontSize(8.5f).SemiBold().FontColor(Muted);
                    foreach (var item in school.Take(5))
                    {
                        col.Item().PaddingTop(2).Row(line => PreferenceBullet(line, item));
                    }
                }

                if (hobby.Count > 0)
                {
                    col.Item().PaddingTop(school.Count > 0 ? 5 : 4)
                        .Text(T(m, "LeerlingPassport.G78.Col.VrijeTijd"))
                        .FontSize(8.5f).SemiBold().FontColor(Muted);
                    foreach (var item in hobby.Take(6))
                    {
                        col.Item().PaddingTop(2).Row(line => PreferenceBullet(line, item));
                    }
                }
            });
    }

    private static void PreferenceBullet(RowDescriptor line, string item, bool truncate = true)
    {
        line.ConstantItem(8).Text("·").FontSize(10f).FontColor(Gold);
        var text = truncate ? TruncateLabel(item) : NormalizePdfLine(item);
        line.RelativeItem().Text(text).FontSize(9.5f).FontColor(Ink).LineHeight(1.35f);
    }

    private static void PreferencesColumnWrapped(IContainer c, string heading, IReadOnlyList<string> items, bool truncateItems = false)
    {
        c.Background(CreamWarm)
            .Border(1).BorderColor(Gold)
            .CornerRadius(Mm(1.5f))
            .Padding(Mm(3.5f))
            .Column(col =>
            {
                col.Item().Text(heading).Bold().FontSize(10.5f).FontColor(Navy);
                foreach (var item in items.Take(6))
                {
                    col.Item().PaddingTop(3).Row(line => PreferenceBullet(line, item, truncate: truncateItems));
                }
            });
    }

    private static void MirrorBox(IContainer c, PupilPassportPdfModel m)
    {
        var raw = NormalizePdfLine(m.MirrorBody ?? m.StoryBody);
        var flat = raw.Contains(PupilG78FactStory.ParagraphBreak, StringComparison.Ordinal)
            ? raw.Replace(PupilG78FactStory.ParagraphBreak, " ", StringComparison.Ordinal)
            : raw;
        var body = TrimMirrorSentences(flat);

        SoftAccentPanel(c, Gold, CreamWarm, inner =>
        {
            inner.Column(col =>
            {
                col.Item().Text(T(m, "LeerlingPassport.G78.Mirror.Title"))
                    .Bold().FontSize(12.5f).FontColor(Navy);
                col.Item().PaddingTop(Mm(1.5f)).Text(body)
                    .FontFamily(MirrorFont).FontSize(10f).FontColor(Ink).LineHeight(1.4f);
            });
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
            .Take(2)
            .ToList();

        if (parts.Count == 0)
        {
            return raw.Length > 720 ? raw[..717] + "…" : raw;
        }

        return string.Join(' ', parts);
    }

    private static void RiasecBars(IContainer c, PupilPassportPdfModel m, bool spacious = false)
    {
        var bars = (m.RiasecBars ?? []).Take(4).ToList();
        var barHeight = Mm(spacious ? 4.2f : 3.2f);
        var rowGap = spacious ? 6 : 2;
        c.Column(col =>
        {
            foreach (var bar in bars)
            {
                var tenths = Math.Clamp((int)Math.Round(bar.Score / 10.0, MidpointRounding.AwayFromZero), 0, 10);
                var ratio = tenths / 10f;
                var desc = bar.Description;
                if (string.IsNullOrWhiteSpace(desc))
                {
                    var descKey = $"LeerlingPassport.G78.Riasec.{bar.Letter}.Desc";
                    desc = T(m, descKey);
                    if (string.Equals(desc, descKey, StringComparison.Ordinal))
                    {
                        desc = "";
                    }
                }

                col.Item().PaddingBottom(rowGap).Column(row =>
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
                        row.Item().PaddingLeft(92).PaddingTop(0).Text(desc).FontSize(7.75f).FontColor(Muted)
                            .LineHeight(1.15f);
                    }
                });
            }
        });
    }

    private static void CultureSliderCard(IContainer c, PupilPassportCultureSlider s, bool spacious = false)
    {
        var ratio = Math.Clamp(s.Position, 0, 100) / 100f;
        var endpoints = SliderEndpoints(s.Label);
        var pad = spacious ? Mm(4) : Mm(3);
        var trackH = spacious ? 14f : 12f;
        var dot = spacious ? 14f : 12f;
        c.Background(CreamWarm).CornerRadius(Mm(1)).Padding(pad).Column(col =>
        {
            col.Item().Text(TruncateLabel(s.Label, 32)).Bold().FontSize(9f).FontColor(Navy);
            col.Item().PaddingTop(Mm(2)).Height(trackH).Background(Track).AlignMiddle().Row(track =>
            {
                track.RelativeItem(Math.Max(0.001f, ratio - 0.05f));
                track.ConstantItem(dot).Height(dot).Background(Gold);
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
        if (parts.Length == 2)
        {
            return (parts[0].ToLowerInvariant(), parts[1].ToLowerInvariant());
        }

        parts = label.Split(',', 2, StringSplitOptions.TrimEntries);
        return parts.Length == 2
            ? (parts[0].ToLowerInvariant(), parts[1].ToLowerInvariant())
            : (label.ToLowerInvariant(), "");
    }

    private static void StrengthChips(IContainer c, PupilPassportPdfModel m, bool spacious = false, bool askQuestion = true)
    {
        var chipPadV = spacious ? 4 : 3;
        c.Column(col =>
        {
            col.Item().Row(row =>
            {
                var i = 0;
                foreach (var s in m.Strengths.Take(5))
                {
                    var highlight = i < 2;
                    row.AutoItem().PaddingRight(3).PaddingBottom(chipPadV)
                        .CornerRadius(Mm(1))
                        .Background(highlight ? SoftGoldTint : CreamWarm)
                        .Border(1).BorderColor(highlight ? Gold : Border)
                        .PaddingHorizontal(6).PaddingVertical(3)
                        .Text(TruncateLabel(s.Title)).FontSize(9f)
                        .FontColor(Ink);
                    i++;
                }
            });
            if (askQuestion)
            {
                col.Item().PaddingTop(spacious ? 6 : 4).Text(T(m, "LeerlingPassport.G78.Strengths.Question"))
                    .Italic().FontSize(8.5f).FontColor(Navy);
            }
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
        c.Background(CreamWarm)
            .Border(1).BorderColor(Gold)
            .CornerRadius(Mm(1.5f))
            .Padding(Mm(3.5f))
            .Column(col =>
            {
                col.Item().Text(T(m, "LeerlingPassport.G78.Talk.Heading")).Bold().FontColor(Navy).FontSize(10.5f);
                foreach (var q in questions)
                {
                    col.Item().PaddingTop(3).Text("• " + q).FontColor(Ink).FontSize(9.5f).LineHeight(1.5f);
                }

                if (questions.Count == 0)
                {
                    col.Item().PaddingTop(3).Text(T(m, "LeerlingPassport.G78.Talk.Empty")).FontColor(Muted).FontSize(9.5f);
                }
            });
    }

    private static void SamenBox(IContainer c, PupilPassportPdfModel m)
    {
        SoftAccentPanel(c, Gold, SoftGoldTint, inner =>
        {
            inner.Column(col =>
            {
                col.Item().Text(T(m, "LeerlingPassport.G78.Samen.Heading")).Bold().FontColor(Navy).FontSize(10f);
                col.Item().PaddingTop(3).Text("• " + NormalizePdfLine(m.SamenAssignment!))
                    .FontColor(Ink).FontSize(9.5f).LineHeight(1.45f);
            });
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

        var circle = Mm(10);
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
                            .Background(isGoal ? Navy : Gold)
                            .AlignCenter().AlignMiddle()
                            .Text(isFirst
                                ? T(m, "LeerlingPassport.G78.Route.Now")
                                : isGoal
                                    ? T(m, "LeerlingPassport.G78.Route.Goal")
                                    : i.ToString(CultureInfo.InvariantCulture))
                            .FontSize(7f).SemiBold()
                            .FontColor(isGoal ? Colors.White : NavyDeep);

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

    private static string FormatJobMatchPercent(PupilPassportPdfModel m, int percent)
        => string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            T(m, "LeerlingPassport.G78.Jobs.MatchSuggest"),
            percent);

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
                col.Item().PaddingTop(i == 0 ? 4 : 7).BorderBottom(1).BorderColor(LineMuted).Height(Mm(9));
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
