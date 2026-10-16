using System.Globalization;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Passport;
using Jobsy.Core.Time;
using QRCoder;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Jobsy.Infrastructure.Services.Passport;

public sealed class PassportPdfService : IPassportPdfService
{
    private static readonly Color Purple = Color.FromHex("#5b2a9a");
    private static readonly Color Magenta = Color.FromHex("#b5309a");
    private static readonly Color Coral = Color.FromHex("#f5503a");
    private static readonly Color Ink = Color.FromHex("#23173a");
    private static readonly Color Muted = Color.FromHex("#776e8c");
    private static readonly Color SoftP = Color.FromHex("#f4effb");
    private static readonly Color SoftC = Color.FromHex("#fff1ec");
    private static readonly Color Mint = Color.FromHex("#e8f6f0");
    private static readonly Color Green = Color.FromHex("#127a5c");
    private static readonly Color Line = Color.FromHex("#e8e1f2");
    private static readonly Color White = Colors.White;
    private const string ArabicFontFamily = "Noto Sans Arabic";

    private readonly IPlatformCompanySettingsService _companySettings;

    static PassportPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        RegisterEmbeddedFont("Jobsy.Infrastructure.Assets.Fonts.NotoSansArabic-Regular.ttf");
        RegisterEmbeddedFont("Jobsy.Infrastructure.Assets.Fonts.NotoSansArabic-Bold.ttf");
    }

    private static void RegisterEmbeddedFont(string resourceName)
    {
        using var stream = typeof(PassportPdfService).Assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return;
        }

        FontManager.RegisterFont(stream);
    }

    public PassportPdfService(IPlatformCompanySettingsService companySettings)
    {
        _companySettings = companySettings;
    }

    public string BuildFileName(PassportPdfModel model)
    {
        var date = AmsterdamTime.ToLocal(model.GeneratedAtUtc);
        return $"Lobsy-DNA-paspoort-{model.Initials}-{date:yyyyMMdd}.pdf";
    }

    public Task<byte[]> RenderAsync(PassportPdfModel model, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        cancellationToken.ThrowIfCancellationRequested();
        _ = _companySettings.GetBrandLogoPng();
        var lang = model.Language;

        var bytes = Document.Create(container =>
        {
            if (model.SinglePageLayout)
            {
                container.Page(page => Compose(page, model, lang, page2: false, singlePage: true));
            }
            else
            {
                container.Page(page => Compose(page, model, lang, page2: false, singlePage: false));
                container.Page(page => Compose(page, model, lang, page2: true, singlePage: false));
            }
        }).GeneratePdf();

        return Task.FromResult(bytes);
    }

    public Task<IReadOnlyList<byte[]>> RenderPageImagesAsync(
        PassportPdfModel model,
        int dpi,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        cancellationToken.ThrowIfCancellationRequested();
        var lang = model.Language;
        var images = Document.Create(container =>
        {
            if (model.SinglePageLayout)
            {
                container.Page(page => Compose(page, model, lang, page2: false, singlePage: true));
            }
            else
            {
                container.Page(page => Compose(page, model, lang, page2: false, singlePage: false));
                container.Page(page => Compose(page, model, lang, page2: true, singlePage: false));
            }
        }).GenerateImages(new ImageGenerationSettings
        {
            RasterDpi = dpi,
            ImageFormat = ImageFormat.Png
        });
        return Task.FromResult((IReadOnlyList<byte[]>)images);
    }

    private static bool IsRtl(string lang) => string.Equals(lang, "ar", StringComparison.OrdinalIgnoreCase);

    private static void Compose(PageDescriptor page, PassportPdfModel model, string lang, bool page2, bool singlePage)
    {
        page.Size(PageSizes.A4);
        page.Margin(0);
        var rtl = IsRtl(lang);
        page.DefaultTextStyle(style =>
        {
            var text = style.FontSize(9.25f).FontColor(Ink).LineHeight(1.28f);
            return rtl ? text.FontFamily(ArabicFontFamily) : text;
        });

        page.Header().Element(header => HeaderBlock(header, model, lang, page2));
        page.Footer().Element(footer => FooterBlock(footer, model, lang, page2));
        page.Content().Element(body =>
        {
            var column = body.PaddingHorizontal(16).PaddingTop(6).PaddingBottom(4);
            if (rtl)
            {
                column = column.ContentFromRightToLeft();
            }

            column.Column(col =>
            {
                col.Spacing(page2 ? 7 : 7);
                if (page2)
                {
                    Page2(col, model, lang);
                }
                else
                {
                    Page1(col, model, lang, singlePage);
                }
            });
        });
    }

    private static void HeaderBlock(IContainer container, PassportPdfModel model, string lang, bool page2)
    {
        container.Column(col =>
        {
            col.Item().Background(Purple).PaddingHorizontal(16).PaddingVertical(8).Row(row =>
            {
                row.ConstantItem(36).Width(36).Height(36).AlignMiddle().Svg(LobsterSvg());
                row.ConstantItem(8);
                row.RelativeItem().AlignMiddle().Column(brand =>
                {
                    brand.Item().Text(text =>
                    {
                        text.Span("Lobsy").FontSize(16).Bold().FontColor(White);
                        text.Span("  ").FontSize(10);
                        if (page2)
                        {
                            text.Span(model.FullName).FontSize(10.5f).SemiBold().FontColor(White);
                        }
                        else
                        {
                            var headerKey = model.StudentLayout ? "StudentHeader" : "RecruiterHeader";
                            text.Span(PassportPdfStrings.T(lang, headerKey)).FontSize(9.5f).FontColor(Color.FromHex("#e8d9f7"));
                        }
                    });
                    if (page2 && !string.IsNullOrWhiteSpace(model.PassportNumber))
                    {
                        brand.Item().PaddingTop(1).Text(PassportPdfStrings.F(lang, "DeepeningHeader", model.PassportNumber))
                            .FontSize(7.5f).FontColor(Color.FromHex("#d5c4ef"));
                    }
                });

                if (model.ShowBadge)
                {
                    row.ConstantItem(6);
                    row.AutoItem().AlignMiddle().Background(White).PaddingHorizontal(6).PaddingVertical(3).Column(badge =>
                    {
                        badge.Item().Row(badgeRow =>
                        {
                            badgeRow.ConstantItem(12).Width(12).Height(12).AlignMiddle().Svg(VerifiedBadgeSvg());
                            badgeRow.ConstantItem(3);
                            badgeRow.AutoItem().AlignMiddle().Text(PassportPdfStrings.T(lang, "BadgeVerified"))
                                .FontSize(7f).Bold().FontColor(Purple);
                        });
                        if (!string.IsNullOrWhiteSpace(model.BadgeDetail))
                        {
                            badge.Item().PaddingTop(1).Text(model.BadgeDetail!).FontSize(6.5f).FontColor(Muted);
                        }
                    });
                }
                else if (!string.IsNullOrWhiteSpace(model.TestsPlainStatus))
                {
                    row.ConstantItem(6);
                    row.AutoItem().AlignMiddle().Background(White).PaddingHorizontal(7).PaddingVertical(4)
                        .Text(model.TestsPlainStatus).FontSize(7f).Bold().FontColor(Purple);
                }
            });
            col.Item().Width(595).Height(4).Svg(HeaderGradientSvg());
        });
    }

    private static void FooterBlock(IContainer container, PassportPdfModel model, string lang, bool page2)
    {
        var hasQr = !page2 && !string.IsNullOrWhiteSpace(model.QrVerifyUrl);
        byte[]? qrPng = null;
        if (hasQr)
        {
            qrPng = QrPng(model.QrVerifyUrl!, pixelsPerModule: 4);
        }

        container.BorderTop(1).BorderColor(Line).PaddingTop(4).Row(row =>
        {
            if (qrPng is { Length: > 0 })
            {
                row.ConstantItem(52).Height(52).Padding(2).Background(White).Border(1).BorderColor(Line)
                    .AlignMiddle().AlignCenter().Width(48).Image(qrPng).FitWidth();
                row.ConstantItem(8);
            }

            row.RelativeItem().AlignMiddle().Column(textCol =>
            {
                if (hasQr)
                {
                    textCol.Item().Text(PassportPdfStrings.T(lang, "ScanLive")).FontSize(7.5f).Bold().FontColor(Purple);
                    if (!string.IsNullOrWhiteSpace(model.QrValidUntilLabel))
                    {
                        textCol.Item().PaddingTop(1).Text(model.QrValidUntilLabel).FontSize(6.5f).FontColor(Muted);
                    }
                }

                if (!string.IsNullOrWhiteSpace(model.ConversationFooter))
                {
                    textCol.Item().PaddingTop(hasQr ? 2 : 0).Text(model.ConversationFooter)
                        .FontSize(6.5f).FontColor(Muted).LineHeight(1.15f).ClampLines(2);
                }

                if (!string.IsNullOrWhiteSpace(model.FooterOmissionLine))
                {
                    textCol.Item().PaddingTop(1).Text(model.FooterOmissionLine!)
                        .FontSize(6.5f).FontColor(Muted).LineHeight(1.15f).ClampLines(1);
                }
            });

            row.ConstantItem(6);
            row.AutoItem().AlignMiddle().AlignRight().Column(meta =>
            {
                meta.Item().AlignRight().Text(PassportPdfStrings.F(lang, "FooterMade", model.GeneratedLabel))
                    .FontSize(6.5f).FontColor(Muted);
                meta.Item().PaddingTop(1).AlignRight().Text(text =>
                {
                    text.Span("pagina ").FontSize(6.5f).FontColor(Muted);
                    text.CurrentPageNumber().FontSize(7).Bold().FontColor(Purple);
                    text.Span("/").FontSize(7).FontColor(Muted);
                    text.TotalPages().FontSize(7).FontColor(Muted);
                });
                meta.Item().PaddingTop(1).AlignRight().Text("lobsy.nl").FontSize(7).Bold().FontColor(Magenta);
            });
        });
    }

    private static void Page1(ColumnDescriptor col, PassportPdfModel model, string lang, bool singlePage)
    {
        Hero(col, model, lang);
        FourCardsRow(col, model, lang);
        LanguagesAndPrefs(col, model, lang);
        StrengthSection(col, model, lang);
        if (model.StudentLayout)
        {
            StageVacanciesSection(col, model, lang);
        }
        else
        {
            SectorsCompact(col, model, lang);
        }

        ExperiencePapers(col, model, lang);

        if (singlePage)
        {
            SparseTodoBlock(col, model, lang);
            VerifiedMeansBox(col, model, lang, qrBesideVerified: false);
        }
    }

    private static void Page2(ColumnDescriptor col, PassportPdfModel model, string lang)
    {
        if (model.StudentLayout && model.Direction.Count > 0)
        {
            StudyDirectionSection(col, model, lang);
        }

        if (model.Sectors.Count > 0)
        {
            col.Item().Column(block =>
            {
                SectionTitle(block, PassportPdfStrings.T(lang, "SectorsTitle"));
                if (model.SingleSectorFullWidth)
                {
                    block.Item().PaddingTop(2).Element(box => SectorDetailCard(box, model.Sectors[0], lang, fullWidth: true));
                }
                else
                {
                    block.Item().PaddingTop(2).Row(row =>
                    {
                        for (var i = 0; i < Math.Min(3, model.Sectors.Count); i++)
                        {
                            if (i > 0)
                            {
                                row.ConstantItem(4);
                            }

                            row.RelativeItem().Element(box => SectorDetailCard(box, model.Sectors[i], lang, fullWidth: false));
                        }
                    });
                }
            });
        }

        if (model.ShowDnaBlocks && model.DnaLayers.Count > 0)
        {
            col.Item().Column(block =>
            {
                SectionTitle(block, PassportPdfStrings.T(lang, "DnaTitle"));
                block.Item().PaddingTop(3).Row(row =>
                {
                    for (var i = 0; i < model.DnaLayers.Count; i++)
                    {
                        if (i > 0)
                        {
                            row.ConstantItem(5);
                        }

                        var layer = model.DnaLayers[i];
                        row.RelativeItem().Element(box => DnaLayerCard(box, layer));
                    }
                });
            });
        }

        var hasHow = model.ShowDnaBlocks && model.HowWorkLines.Count > 0;
        var hasBest = model.ShowDnaBlocks && model.BestOutTips.Count > 0;
        if (hasHow || hasBest)
        {
            col.Item().Row(row =>
            {
                if (hasHow)
                {
                    row.RelativeItem().Column(left =>
                    {
                        SectionTitle(left, PassportPdfStrings.T(lang, "HowIWork"));
                        left.Item().PaddingTop(4).Background(SoftP).Border(1).BorderColor(Line).Padding(9).Column(list =>
                        {
                            foreach (var line in model.HowWorkLines.Take(6))
                            {
                                list.Item().PaddingBottom(3).Text(text =>
                                {
                                    text.Span(line.Lead).FontSize(9f).Bold();
                                    if (!string.IsNullOrWhiteSpace(line.Tail))
                                    {
                                        text.Span(" — " + line.Tail).FontSize(9f).FontColor(Muted);
                                    }
                                });
                            }
                        });
                    });
                }

                if (hasHow && hasBest)
                {
                    row.ConstantItem(8);
                }

                if (hasBest)
                {
                    row.RelativeItem().Column(right =>
                    {
                        var first = model.FirstName ?? model.FullName.Split(' ').FirstOrDefault() ?? model.FullName;
                        SectionTitle(right, PassportPdfStrings.F(lang, "BestOutTitle", first));
                        right.Item().PaddingTop(4).Background(SoftC).Border(1).BorderColor(Line).Padding(9).Column(list =>
                        {
                            foreach (var tip in model.BestOutTips.Take(4))
                            {
                                list.Item().PaddingBottom(3).Row(tipRow =>
                                {
                                    tipRow.ConstantItem(8).Text("•").FontSize(8.5f).FontColor(Coral);
                                    tipRow.RelativeItem().Text(tip).FontSize(9f).LineHeight(1.25f);
                                });
                            }
                        });
                    });
                }
            });
        }

        if (model.ShowDnaBlocks && model.LearningLines.Count > 0)
        {
            col.Item().Column(block =>
            {
                SectionTitle(block, PassportPdfStrings.T(lang, "LearnTitle"));
                block.Item().PaddingTop(4).Background(SoftP).Border(1).BorderColor(Line).Padding(10).Inlined(chips =>
                {
                    chips.Spacing(5);
                    chips.VerticalSpacing(5);
                    foreach (var goal in model.LearningLines.Take(5))
                    {
                        chips.Item().Background(White).Border(1).BorderColor(Line).PaddingHorizontal(10).PaddingVertical(4)
                            .Text(goal).FontSize(8.5f).FontColor(Purple);
                    }
                });
            });
        }

        if (!string.IsNullOrWhiteSpace(model.OwnWords))
        {
            col.Item().Column(block =>
            {
                SectionTitle(block, PassportPdfStrings.T(lang, "OwnWords"));
                block.Item().PaddingTop(4).Background(SoftP).Border(1).BorderColor(Line).Padding(12)
                    .Text(model.OwnWords).FontSize(10.5f).LineHeight(1.35f).Italic().FontColor(Ink).ClampLines(8);
            });
        }

        VerifiedMeansBox(col, model, lang, qrBesideVerified: true);
    }

    private static void Hero(ColumnDescriptor col, PassportPdfModel model, string lang)
    {
        col.Item().Row(row =>
        {
            if (model.ShowHeroAvatar)
            {
                row.ConstantItem(56).Width(56).Height(56).Element(avatar =>
                {
                    avatar.Layers(layers =>
                    {
                        layers.Layer().Svg(AvatarGradientSvg());
                        layers.PrimaryLayer().Width(56).Height(56).AlignCenter().AlignMiddle()
                            .Text(model.Initials).FontSize(17).Bold().FontColor(White);
                    });
                });
                row.ConstantItem(12);
            }

            row.RelativeItem().Column(name =>
            {
                name.Item().Text(model.FullName).FontSize(HeroNameFontSize(model.FullName)).Bold().FontColor(Ink).LineHeight(1.08f);
                if (!string.IsNullOrWhiteSpace(model.Tagline))
                {
                    name.Item().PaddingTop(2).Text(model.Tagline).FontSize(9f).FontColor(Magenta).LineHeight(1.25f);
                }

                name.Item().PaddingTop(4).Inlined(chips =>
                {
                    chips.Spacing(4);
                    chips.VerticalSpacing(3);
                    foreach (var chip in model.Chips)
                    {
                        chips.Item().Element(box => Chip(box, chip));
                    }

                    if (!string.IsNullOrWhiteSpace(model.PassportNumber))
                    {
                        chips.Item().Background(SoftP).Border(1).BorderColor(Line).PaddingHorizontal(6).PaddingVertical(2)
                            .Text(PassportPdfStrings.F(lang, "PassportNo", model.PassportNumber)).FontSize(7.5f).FontColor(Purple);
                    }
                });
            });
        });
    }

    private static void FourCardsRow(ColumnDescriptor col, PassportPdfModel model, string lang)
    {
        var hasAvail = !string.IsNullOrWhiteSpace(model.AvailableFrom) || !string.IsNullOrWhiteSpace(model.Hours);
        var hasShifts = model.Shifts.Count > 0 || !string.IsNullOrWhiteSpace(model.ShiftNote);
        var hasTransport = !string.IsNullOrWhiteSpace(model.TransportLine)
                           || !string.IsNullOrWhiteSpace(model.TravelLine)
                           || !string.IsNullOrWhiteSpace(model.OwnCarLine);
        var hasContact = !string.IsNullOrWhiteSpace(model.Email)
                         || !string.IsNullOrWhiteSpace(model.Phone)
                         || model.WhatsApp;

        if (!hasAvail && !hasShifts && !hasTransport && !hasContact)
        {
            return;
        }

        col.Item().Row(row =>
        {
            if (hasAvail)
            {
                row.RelativeItem().Element(box => InfoCard(box, PassportPdfStrings.T(lang, "Available"), inner =>
                {
                    if (!string.IsNullOrWhiteSpace(model.AvailableFrom))
                    {
                        inner.Item().Text(model.AvailableFrom).FontSize(9).Bold().FontColor(Ink);
                    }

                    if (!string.IsNullOrWhiteSpace(model.Hours))
                    {
                        inner.Item().PaddingTop(1).Text(model.Hours).FontSize(8).FontColor(Muted);
                    }
                }));
            }

            if (hasShifts)
            {
                if (hasAvail)
                {
                    row.ConstantItem(5);
                }

                row.RelativeItem().Element(box => InfoCard(box, PassportPdfStrings.T(lang, "Shifts"), inner =>
                {
                    if (model.Shifts.Count > 0)
                    {
                        inner.Item().Inlined(chips =>
                        {
                            chips.Spacing(3);
                            chips.VerticalSpacing(3);
                            foreach (var shift in model.Shifts)
                            {
                                chips.Item().Element(chip => ShiftChip(chip, shift));
                            }
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(model.ShiftNote))
                    {
                        inner.Item().PaddingTop(2).Text(model.ShiftNote).FontSize(7.5f).FontColor(Muted);
                    }
                }));
            }

            if (hasTransport)
            {
                if (hasAvail || hasShifts)
                {
                    row.ConstantItem(5);
                }

                row.RelativeItem().Element(box => InfoCard(box, PassportPdfStrings.T(lang, "Transport"), inner =>
                {
                    if (!string.IsNullOrWhiteSpace(model.TransportLine))
                    {
                        inner.Item().Text(model.TransportLine).FontSize(8.5f);
                    }

                    if (!string.IsNullOrWhiteSpace(model.TravelLine))
                    {
                        inner.Item().PaddingTop(1).Text(model.TravelLine).FontSize(8).FontColor(Muted);
                    }

                    if (!string.IsNullOrWhiteSpace(model.OwnCarLine))
                    {
                        inner.Item().PaddingTop(1).Text(model.OwnCarLine).FontSize(8).FontColor(Muted);
                    }
                }));
            }

            if (hasContact)
            {
                if (hasAvail || hasShifts || hasTransport)
                {
                    row.ConstantItem(5);
                }

                row.RelativeItem().Element(box => InfoCard(box, PassportPdfStrings.T(lang, "Contact"), inner =>
                {
                    if (!string.IsNullOrWhiteSpace(model.Email))
                    {
                        inner.Item().Text(model.Email).FontSize(8.5f);
                    }

                    if (!string.IsNullOrWhiteSpace(model.Phone))
                    {
                        inner.Item().PaddingTop(1).Text(model.Phone).FontSize(8.5f);
                    }

                    if (model.WhatsApp)
                    {
                        inner.Item().PaddingTop(1).Text(PassportPdfStrings.T(lang, "WhatsApp")).FontSize(8).FontColor(Green);
                    }
                }));
            }
        });
    }

    private static void LanguagesAndPrefs(ColumnDescriptor col, PassportPdfModel model, string lang)
    {
        var hasLang = model.Languages.Count > 0;
        var hasPrefs = model.WorkPreferences.Count > 0;
        if (!hasLang && !hasPrefs)
        {
            return;
        }

        col.Item().Row(row =>
        {
            if (hasLang)
            {
                row.RelativeItem().Column(left =>
                {
                    SectionTitle(left, PassportPdfStrings.T(lang, "Languages"));
                    left.Item().PaddingTop(3).Background(SoftP).Border(1).BorderColor(Line).Padding(6).Column(list =>
                    {
                        foreach (var line in model.Languages.Take(6))
                        {
                            list.Item().PaddingBottom(4).Row(langRow =>
                            {
                                langRow.ConstantItem(72).Text(line.Label).FontSize(9f).Bold();
                                langRow.RelativeItem().Row(levelRow =>
                                {
                                    levelRow.RelativeItem().Element(box => LanguageDots(box, line.Value, lang));
                                    var levelLabel = LanguageLevelLabel(line.Value, lang);
                                    if (!string.IsNullOrWhiteSpace(levelLabel))
                                    {
                                        levelRow.ConstantItem(8);
                                        levelRow.AutoItem().AlignMiddle().Text(levelLabel).FontSize(8.5f).FontColor(Magenta);
                                    }
                                });
                            });
                        }

                        if (!string.IsNullOrWhiteSpace(model.DutchNote))
                        {
                            list.Item().PaddingTop(2).Text(model.DutchNote).FontSize(7.5f).FontColor(Muted).Italic();
                        }
                    });
                });
            }

            if (hasLang && hasPrefs)
            {
                row.ConstantItem(8);
            }

            if (hasPrefs)
            {
                row.RelativeItem().Column(right =>
                {
                    SectionTitle(right, PassportPdfStrings.T(lang, "WorkPrefs"));
                    right.Item().PaddingTop(3).Background(SoftC).Border(1).BorderColor(Line).Padding(6).Column(list =>
                    {
                        foreach (var pref in model.WorkPreferences.Take(8))
                        {
                            list.Item().PaddingBottom(2).Text(text =>
                            {
                                text.Span(pref.Label).FontSize(8.5f).Bold();
                                text.Span("  ").FontSize(8);
                                text.Span(pref.Value).FontSize(8.5f).FontColor(Muted);
                            });
                        }
                    });
                });
            }
        });
    }

    private static void StrengthSection(ColumnDescriptor col, PassportPdfModel model, string lang)
    {
        if (model.StrengthCards.Count == 0)
        {
            return;
        }

        col.Item().Column(block =>
        {
            SectionTitle(block, PassportPdfStrings.T(lang, "Strengths"));
            if (!string.IsNullOrWhiteSpace(model.StrengthSource))
            {
                block.Item().PaddingTop(1).Text(model.StrengthSource).FontSize(7.5f).FontColor(Muted);
            }

            block.Item().PaddingTop(5).Row(row =>
            {
                for (var i = 0; i < Math.Min(3, model.StrengthCards.Count); i++)
                {
                    if (i > 0)
                    {
                        row.ConstantItem(5);
                    }

                    var card = model.StrengthCards[i];
                    row.RelativeItem().Background(SoftC).Border(1).BorderColor(Line).Padding(6).Column(item =>
                    {
                        item.Item().Text(card.Title).FontSize(9).Bold().FontColor(Coral);
                        item.Item().PaddingTop(2).Text(card.Subtitle).FontSize(8).LineHeight(1.25f);
                    });
                }
            });

            if (model.StrengthTags.Count > 0)
            {
                block.Item().PaddingTop(4).Inlined(chips =>
                {
                    chips.Spacing(4);
                    chips.VerticalSpacing(3);
                    foreach (var tag in model.StrengthTags)
                    {
                        chips.Item().Background(SoftP).PaddingHorizontal(7).PaddingVertical(3)
                            .Text(tag).FontSize(8).FontColor(Ink);
                    }
                });
            }
        });
    }

    private static void SectorDetailCard(IContainer box, PassportSectorCard sector, string lang, bool fullWidth)
    {
        box.Background(SoftP).Border(1).BorderColor(Line).Padding(10).Column(card =>
        {
            card.Item().Text(sector.Title).FontSize(fullWidth ? 11f : 9.5f).Bold().FontColor(Purple).ClampLines(1);
            if (!string.IsNullOrWhiteSpace(sector.RankSubtitle))
            {
                card.Item().PaddingTop(2).Text(sector.RankSubtitle!).FontSize(7.5f).FontColor(Magenta);
            }

            if (!string.IsNullOrWhiteSpace(sector.P1Teaser))
            {
                card.Item().PaddingTop(3).Text(sector.P1Teaser).FontSize(8.5f).LineHeight(1.25f);
            }

            card.Item().PaddingTop(4).Text(PassportPdfStrings.T(lang, "Sector.WhyHeading"))
                .FontSize(6.5f).Bold().FontColor(Muted);
            foreach (var why in sector.WhyBullets.Take(fullWidth ? 3 : 4))
            {
                card.Item().PaddingTop(2).Row(bullet =>
                {
                    bullet.ConstantItem(6).Text("•").FontSize(7.5f).FontColor(Coral);
                    bullet.RelativeItem().Text(why).FontSize(8f).LineHeight(1.2f).ClampLines(fullWidth ? 3 : 2);
                });
            }

            if (!string.IsNullOrWhiteSpace(sector.ExampleRoles))
            {
                card.Item().PaddingTop(4).Text(PassportPdfStrings.T(lang, "Sector.ExampleRolesLabel"))
                    .FontSize(6.5f).Bold().FontColor(Muted);
                card.Item().PaddingTop(1).Text(sector.ExampleRoles!).FontSize(7.5f).FontColor(Ink).LineHeight(1.2f).ClampLines(3);
            }
        });
    }

    private static void StageVacanciesSection(ColumnDescriptor col, PassportPdfModel model, string lang)
    {
        col.Item().Column(block =>
        {
            SectionTitle(block, PassportPdfStrings.T(lang, "StageVacanciesTitle"));
            if (model.StageVacancyMatches.Count == 0)
            {
                block.Item().PaddingTop(4).Background(SoftP).Border(1).BorderColor(Line).Padding(10)
                    .Text(model.StageVacanciesEmpty ?? PassportPdfStrings.T(lang, "StageVacanciesEmpty"))
                    .FontSize(8.5f).FontColor(Muted).LineHeight(1.3f);
                return;
            }

            block.Item().PaddingTop(4).Row(row =>
            {
                for (var i = 0; i < Math.Min(3, model.StageVacancyMatches.Count); i++)
                {
                    if (i > 0)
                    {
                        row.ConstantItem(5);
                    }

                    var fit = model.StageVacancyMatches[i];
                    row.RelativeItem().Background(White).Border(1).BorderColor(Line).Padding(9).Column(card =>
                    {
                        card.Item().Text(fit.Title).FontSize(9.5f).Bold().FontColor(Purple).LineHeight(1.15f).ClampLines(2);
                        card.Item().PaddingTop(4).Text(fit.Why).FontSize(8f).FontColor(Ink).LineHeight(1.25f).ClampLines(4);
                    });
                }
            });
        });
    }

    private static void StudyDirectionSection(ColumnDescriptor col, PassportPdfModel model, string lang)
    {
        col.Item().Column(block =>
        {
            SectionTitle(block, PassportPdfStrings.T(lang, "StudyDirectionTitle"));
            block.Item().PaddingTop(4).Background(SoftP).Border(1).BorderColor(Line).Padding(10).Column(list =>
            {
                for (var i = 0; i < model.Direction.Count; i++)
                {
                    var step = model.Direction[i];
                    var isLast = i == model.Direction.Count - 1;
                    list.Item().PaddingBottom(isLast ? 0 : 6).Row(row =>
                    {
                        row.ConstantItem(22).AlignTop().Element(circle =>
                        {
                            circle.Width(18).Height(18).Background(isLast ? Coral : White).Border(1.5f).BorderColor(Purple)
                                .AlignCenter().AlignMiddle().Text((i + 1).ToString()).FontSize(8f).Bold().FontColor(isLast ? White : Purple);
                        });
                        row.RelativeItem().PaddingLeft(6).Column(text =>
                        {
                            text.Item().Text(step.Title).FontSize(9.5f).Bold().FontColor(Ink);
                            if (!string.IsNullOrWhiteSpace(step.State))
                            {
                                text.Item().PaddingTop(1).Text(step.State).FontSize(8f).FontColor(Magenta);
                            }
                        });
                    });
                }
            });
        });
    }

    private static void SectorsCompact(ColumnDescriptor col, PassportPdfModel model, string lang)
    {
        if (model.Sectors.Count == 0)
        {
            return;
        }

        col.Item().Column(block =>
        {
            SectionTitle(block, PassportPdfStrings.T(lang, "SectorsTitle"));
            block.Item().PaddingTop(5).Row(row =>
            {
                for (var i = 0; i < Math.Min(3, model.Sectors.Count); i++)
                {
                    if (i > 0)
                    {
                        row.ConstantItem(5);
                    }

                    var sector = model.Sectors[i];
                    row.RelativeItem().Background(SoftP).Border(1).BorderColor(Line).Padding(8).Column(card =>
                    {
                        card.Item().Text(sector.Title).FontSize(10).Bold().FontColor(Purple);
                        if (!string.IsNullOrWhiteSpace(sector.RankSubtitle))
                        {
                            card.Item().PaddingTop(2).Text(sector.RankSubtitle).FontSize(7.5f).FontColor(Magenta);
                        }

                        card.Item().PaddingTop(3).Text(sector.P1Teaser).FontSize(8.5f).LineHeight(1.25f);
                        if (!string.IsNullOrWhiteSpace(sector.ExampleRoles))
                        {
                            card.Item().PaddingTop(3).Text(sector.ExampleRoles).FontSize(7.5f).FontColor(Muted).LineHeight(1.2f).ClampLines(2);
                        }
                    });
                }
            });
        });
    }

    private static void ExperiencePapers(ColumnDescriptor col, PassportPdfModel model, string lang)
    {
        var jobs = model.Experience.Take(2).ToList();
        var papers = model.Educations.Concat(model.Certificates).Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
        if (jobs.Count == 0 && papers.Count == 0)
        {
            return;
        }

        col.Item().Column(block =>
        {
            SectionTitle(block, PassportPdfStrings.T(lang, "ExperiencePapers"));
            block.Item().PaddingTop(3).Row(row =>
            {
                if (jobs.Count > 0)
                {
                    row.RelativeItem().Column(left =>
                    {
                        foreach (var job in jobs)
                        {
                            left.Item().PaddingBottom(5).Background(White).Border(1).BorderColor(Line).Padding(9).Column(card =>
                            {
                                card.Item().Row(line =>
                                {
                                    line.RelativeItem().Text(job.Title).FontSize(9.5f).Bold().FontColor(Ink);
                                    if (!string.IsNullOrWhiteSpace(job.DurationLabel))
                                    {
                                        line.AutoItem().Text(job.DurationLabel).FontSize(8.5f).Bold().FontColor(Magenta);
                                    }
                                });
                                if (!string.IsNullOrWhiteSpace(job.Meta))
                                {
                                    card.Item().PaddingTop(2).Text(job.Meta).FontSize(8.5f).FontColor(Muted);
                                }

                                if (job.Duties is { Count: > 0 })
                                {
                                    foreach (var duty in job.Duties.Take(2))
                                    {
                                        card.Item().PaddingTop(2).Row(bullet =>
                                        {
                                            bullet.ConstantItem(8).Text("•").FontSize(8).FontColor(Coral);
                                            bullet.RelativeItem().Text(duty).FontSize(8.5f).LineHeight(1.25f);
                                        });
                                    }
                                }
                                else if (!string.IsNullOrWhiteSpace(job.Detail))
                                {
                                    card.Item().PaddingTop(2).Text(job.Detail).FontSize(8.5f).LineHeight(1.25f);
                                }
                            });
                        }

                        if (model.ExperienceMore > 0)
                        {
                            left.Item().Text(PassportPdfStrings.F(lang, "More", model.ExperienceMore))
                                .FontSize(7.5f).FontColor(Muted);
                        }
                    });
                }

                if (jobs.Count > 0 && papers.Count > 0)
                {
                    row.ConstantItem(8);
                }

                if (papers.Count > 0)
                {
                    row.RelativeItem().Background(SoftP).Border(1).BorderColor(Line).Padding(6).Column(right =>
                    {
                        if (model.Educations.Count > 0)
                        {
                            right.Item().Text(PassportPdfStrings.T(lang, "Education")).FontSize(8).Bold().FontColor(Purple);
                            foreach (var edu in model.Educations.Take(4))
                            {
                                right.Item().PaddingTop(2).Text("•  " + edu).FontSize(8);
                            }
                        }

                        if (model.Certificates.Count > 0)
                        {
                            right.Item().PaddingTop(model.Educations.Count > 0 ? 4 : 0)
                                .Text(PassportPdfStrings.T(lang, "Certificates")).FontSize(8).Bold().FontColor(Purple);
                            foreach (var cert in model.Certificates.Take(4))
                            {
                                right.Item().PaddingTop(2).Text("•  " + cert).FontSize(8);
                            }
                        }

                        var more = model.CertificatesMore;
                        if (more > 0)
                        {
                            right.Item().PaddingTop(2).Text(PassportPdfStrings.F(lang, "More", more))
                                .FontSize(7.5f).FontColor(Muted);
                        }
                    });
                }
            });
        });
    }

    private static void VerifiedMeansBox(ColumnDescriptor col, PassportPdfModel model, string lang, bool qrBesideVerified)
    {
        if (model.VerifiedChecked.Count == 0 && model.VerifiedNotChecked.Count == 0)
        {
            return;
        }

        byte[]? qrPng = null;
        if (qrBesideVerified && !string.IsNullOrWhiteSpace(model.QrVerifyUrl))
        {
            qrPng = QrPng(model.QrVerifyUrl!, pixelsPerModule: 4);
        }

        col.Item().Column(block =>
        {
            SectionTitle(block, PassportPdfStrings.T(lang, "VerifiedMeans"));
            block.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Row(cols =>
                {
                    cols.RelativeItem().Background(Mint).Border(1).BorderColor(Line).Padding(9).Column(yes =>
                    {
                        yes.Item().Text(PassportPdfStrings.T(lang, "VerifiedCheckedIntro")).FontSize(9.5f).Bold().FontColor(Green);
                        foreach (var line in model.VerifiedChecked)
                        {
                            var pending = line == PassportPdfStrings.T(lang, "NothingVerifiedYet");
                            yes.Item().PaddingTop(3).Row(lineRow =>
                            {
                                if (pending)
                                {
                                    lineRow.ConstantItem(10).Width(10).AlignMiddle().Text("○").FontSize(9).FontColor(Muted);
                                }
                                else
                                {
                                    lineRow.ConstantItem(10).Width(10).Height(10).Svg(CheckSvg("#127a5c"));
                                }

                                lineRow.ConstantItem(4);
                                lineRow.RelativeItem().Text(line).FontSize(8.5f).LineHeight(1.2f)
                                    .FontColor(pending ? Muted : Ink);
                            });
                        }
                    });
                    cols.ConstantItem(8);
                    cols.RelativeItem().Background(SoftC).Border(1).BorderColor(Line).Padding(9).Column(no =>
                    {
                        no.Item().Text(PassportPdfStrings.T(lang, "VerifiedNotIntro")).FontSize(9.5f).Bold().FontColor(Coral);
                        foreach (var line in model.VerifiedNotChecked)
                        {
                            no.Item().PaddingTop(3).Row(lineRow =>
                            {
                                lineRow.ConstantItem(8).Text("×").FontSize(9).Bold().FontColor(Coral);
                                lineRow.RelativeItem().Text(line).FontSize(8.5f).LineHeight(1.2f);
                            });
                        }
                    });
                });

                if (qrPng is { Length: > 0 })
                {
                    row.ConstantItem(10);
                    row.ConstantItem(62).Column(qrCol =>
                    {
                        qrCol.Item().Width(56).Height(56).Padding(2).Background(White).Border(1).BorderColor(Line)
                            .AlignMiddle().AlignCenter().Image(qrPng).FitWidth();
                        qrCol.Item().PaddingTop(3).Text(PassportPdfStrings.T(lang, "ScanLive"))
                            .FontSize(7f).Bold().FontColor(Purple).LineHeight(1.15f);
                    });
                }
            });
        });
    }

    private static void SparseTodoBlock(ColumnDescriptor col, PassportPdfModel model, string lang)
    {
        if (model.SparseTodoLines.Count == 0)
        {
            return;
        }

        col.Item().PaddingTop(4).Column(block =>
        {
            SectionTitle(block, PassportPdfStrings.T(lang, "SparseTodoTitle"));
            block.Item().PaddingTop(4).Background(White).Border(1).BorderColor(Line).Padding(14).Column(list =>
            {
                if (!string.IsNullOrWhiteSpace(model.SparseCandidateNote))
                {
                    list.Item().PaddingBottom(8).Text(model.SparseCandidateNote!)
                        .FontSize(9.5f).LineHeight(1.35f).FontColor(Muted);
                }

                foreach (var line in model.SparseTodoLines)
                {
                    list.Item().PaddingBottom(5).Row(item =>
                    {
                        item.ConstantItem(8).Text("•").FontSize(10).FontColor(Coral);
                        item.RelativeItem().Text(line).FontSize(10f).LineHeight(1.3f);
                    });
                }
            });
        });
    }

    private static void DnaLayerCard(IContainer box, PassportDnaLayerCard layer)
    {
        var bg = layer.CardTone switch
        {
            "coral" => SoftC,
            "mint" => Mint,
            "sand" => Color.FromHex("#faf6f0"),
            _ => SoftP
        };
        box.Background(bg).Border(1).BorderColor(Line).Padding(8).Column(col =>
        {
            col.Item().Row(title =>
            {
                title.ConstantItem(14).Width(14).Height(14).Svg(DnaLayerIconSvg(layer.CardTone));
                title.ConstantItem(4);
                title.RelativeItem().Column(labels =>
                {
                    labels.Item().Text(layer.Nickname).FontSize(8.5f).Bold().FontColor(Purple);
                    labels.Item().PaddingTop(1).Text(layer.LayerLabel).FontSize(7f).FontColor(Muted);
                });
            });
            col.Item().PaddingTop(3).Text(layer.Body).FontSize(8f).LineHeight(1.25f);
            if (!string.IsNullOrWhiteSpace(layer.CompletedLabel))
            {
                col.Item().PaddingTop(3).Row(done =>
                {
                    done.ConstantItem(10).Width(10).Height(10).AlignMiddle().Svg(CheckSvg("#127a5c"));
                    done.ConstantItem(3);
                    done.RelativeItem().AlignMiddle().Text(layer.CompletedLabel!).FontSize(7f).FontColor(Green);
                });
            }
        });
    }

    private static void InfoCard(IContainer box, string title, Action<ColumnDescriptor> content)
    {
        box.ShowEntire().Background(White).Border(1).BorderColor(Line).Padding(8).Column(col =>
        {
            col.Item().Text(title).FontSize(8.5f).Bold().FontColor(Purple);
            col.Item().PaddingTop(3).Column(content);
        });
    }

    private static void Chip(IContainer box, PassportChip chip)
    {
        var (bg, fg) = chip.Tone switch
        {
            "open" => (SoftC, Coral),
            "role" => (SoftP, Purple),
            "available" => (Mint, Green),
            "seek" => (White, Ink),
            "study" => (SoftP, Purple),
            "internship" => (SoftC, Coral),
            _ => (Line, Ink)
        };
        if (chip.Tone == "seek")
        {
            box.Background(bg).Border(1).BorderColor(Line).PaddingHorizontal(8).PaddingVertical(4).MaxWidth(280)
                .Text(chip.Text).FontSize(7.5f).FontColor(fg).LineHeight(1.2f);
            return;
        }

        box.Background(bg).PaddingHorizontal(7).PaddingVertical(3)
            .Text(chip.Text).FontSize(8f).FontColor(fg);
    }

    private static void ShiftChip(IContainer box, PassportShiftChip shift)
    {
        var (bg, fg) = shift.Kind switch
        {
            PassportShiftKind.Yes => (Mint, Green),
            PassportShiftKind.Consult => (SoftC, Coral),
            _ => (Line, Muted)
        };
        box.Background(bg).PaddingHorizontal(5).PaddingVertical(2).Column(col =>
        {
            col.Item().Text(shift.Label).FontSize(7.5f).Bold().FontColor(fg);
            col.Item().Text(shift.Status).FontSize(7).FontColor(Muted);
        });
    }

    private static void LanguageDots(IContainer box, string? value, string lang)
    {
        var filled = LanguageLevelDots(value, lang);
        box.AlignMiddle().Row(row =>
        {
            for (var i = 1; i <= 5; i++)
            {
                if (i > 1)
                {
                    row.ConstantItem(3);
                }

                var on = i <= filled;
                row.ConstantItem(8).Height(8).Background(on ? Magenta : Line);
            }

            if (filled == 0 && !string.IsNullOrWhiteSpace(value) && value != "—")
            {
                row.ConstantItem(6);
                row.RelativeItem().Text(value).FontSize(7.5f).FontColor(Muted);
            }
        });
    }

    private static int LanguageLevelDots(string? value, string lang)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "—")
        {
            return 0;
        }

        var raw = value.Trim().ToLowerInvariant();
        if (raw is "b1" or "b2")
        {
            return raw == "b1" ? 3 : 4;
        }

        foreach (var level in new[] { "beginner", "basis", "goed", "vloeiend", "moedertaal" })
        {
            if (raw == level || raw == PassportPdfStrings.T(lang, "Dutch." + level).ToLowerInvariant())
            {
                return level switch
                {
                    "beginner" => 1,
                    "basis" => 2,
                    "goed" => 3,
                    "vloeiend" => 4,
                    _ => 5
                };
            }
        }

        return 0;
    }

    private static void SectionTitle(ColumnDescriptor col, string title)
    {
        col.Item().PaddingBottom(2).Row(row =>
        {
            row.ConstantItem(4).Height(14).Background(Coral);
            row.ConstantItem(8);
            row.RelativeItem().AlignMiddle().Text(title).FontSize(13).Bold().FontColor(Purple);
        });
    }

    private static float HeroNameFontSize(string fullName)
        => fullName.Length switch
        {
            > 36 => 14f,
            > 30 => 16f,
            > 24 => 18f,
            _ => 22f
        };

    private static string? LanguageLevelLabel(string? value, string lang)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "—")
        {
            return null;
        }

        var raw = value.Trim();
        if (raw.Length <= 4 && raw.Any(char.IsDigit))
        {
            return raw.ToUpperInvariant();
        }

        foreach (var level in new[] { "beginner", "basis", "goed", "vloeiend", "moedertaal" })
        {
            if (raw.Equals(level, StringComparison.OrdinalIgnoreCase)
                || raw.Equals(PassportPdfStrings.T(lang, "Dutch." + level), StringComparison.OrdinalIgnoreCase))
            {
                return level switch
                {
                    "beginner" => "A1",
                    "basis" => "A2",
                    "goed" => "B1",
                    "vloeiend" => "C1",
                    _ => PassportPdfStrings.T(lang, "Dutch.moedertaal")
                };
            }
        }

        return raw.Length <= 12 ? raw : null;
    }

    private static string LobsterSvg()
        => "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\">"
           + "<path d=\"M7.2 9.2c-1.8-2.4-4.4-1.6-3.6.8\" fill=\"none\" stroke=\"#f5503a\" stroke-width=\"1.6\" stroke-linecap=\"round\"/>"
           + "<path d=\"M16.8 9.2c1.8-2.4 4.4-1.6 3.6.8\" fill=\"none\" stroke=\"#f5503a\" stroke-width=\"1.6\" stroke-linecap=\"round\"/>"
           + "<ellipse cx=\"12\" cy=\"12.2\" rx=\"3.1\" ry=\"4.3\" fill=\"#f5503a\"/>"
           + "<path d=\"M9.1 15.4c.5 2.5 5.3 2.5 5.8 0\" fill=\"#f5503a\"/>"
           + "</svg>";

    private static string VerifiedBadgeSvg()
        => "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 18 18\">"
           + "<circle cx=\"9\" cy=\"9\" r=\"9\" fill=\"#5b2a9a\"/>"
           + "<path d=\"M5 9.2 L7.8 12 L13 6.5\" fill=\"none\" stroke=\"#ffffff\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>"
           + "</svg>";

    private static string DnaLayerIconSvg(string tone)
    {
        var fill = tone switch
        {
            "coral" => "#f5503a",
            "mint" => "#127a5c",
            "sand" => "#c4a574",
            _ => "#5b2a9a"
        };
        return "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 14 14\">"
               + $"<circle cx=\"7\" cy=\"7\" r=\"6\" fill=\"{fill}\" opacity=\"0.25\"/>"
               + $"<circle cx=\"7\" cy=\"7\" r=\"2.5\" fill=\"{fill}\"/>"
               + "</svg>";
    }

    private static string HeaderGradientSvg()
        => "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 600 3\" preserveAspectRatio=\"none\">"
           + "<defs><linearGradient id=\"g\" x1=\"0\" y1=\"0\" x2=\"1\" y2=\"0\">"
           + "<stop offset=\"0\" stop-color=\"#5b2a9a\"/>"
           + "<stop offset=\"0.5\" stop-color=\"#b5309a\"/>"
           + "<stop offset=\"1\" stop-color=\"#f5503a\"/>"
           + "</linearGradient></defs>"
           + "<rect width=\"600\" height=\"3\" fill=\"url(#g)\"/>"
           + "</svg>";

    private static string AvatarGradientSvg()
        => "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 44 44\">"
           + "<defs><linearGradient id=\"a\" x1=\"0\" y1=\"0\" x2=\"1\" y2=\"1\">"
           + "<stop offset=\"0\" stop-color=\"#5b2a9a\"/>"
           + "<stop offset=\"1\" stop-color=\"#f5503a\"/>"
           + "</linearGradient></defs>"
           + "<circle cx=\"22\" cy=\"22\" r=\"22\" fill=\"url(#a)\"/>"
           + "</svg>";

    private static byte[] QrPng(string url, int pixelsPerModule)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url.Trim(), QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        return png.GetGraphic(pixelsPerModule);
    }

    private static string CheckSvg(string strokeHex)
        => "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 12 12\">"
           + "<path d=\"M2 6.2 L5 9.2 L10 3.2\" fill=\"none\" stroke=\"" + strokeHex + "\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>"
           + "</svg>";
}
