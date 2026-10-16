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
            container.Page(page => Compose(page, model, lang, page2: false));
            container.Page(page => Compose(page, model, lang, page2: true));
        }).GeneratePdf();

        return Task.FromResult(bytes);
    }

    private static bool IsRtl(string lang) => string.Equals(lang, "ar", StringComparison.OrdinalIgnoreCase);

    private static void Compose(PageDescriptor page, PassportPdfModel model, string lang, bool page2)
    {
        page.Size(PageSizes.A4);
        page.Margin(0);
        var rtl = IsRtl(lang);
        page.DefaultTextStyle(style =>
        {
            var text = style.FontSize(9).FontColor(Ink).LineHeight(1.25f);
            return rtl ? text.FontFamily(ArabicFontFamily) : text;
        });

        page.Header().Element(header => HeaderBlock(header, model, lang, page2));
        page.Footer().Element(footer => FooterBlock(footer, model, lang));
        page.Content().Element(body =>
        {
            var column = body.PaddingHorizontal(14).PaddingTop(4).PaddingBottom(2);
            if (rtl)
            {
                column = column.ContentFromRightToLeft();
            }

            column.Column(col =>
            {
                col.Spacing(2);
                if (page2)
                {
                    Page2(col, model, lang);
                }
                else
                {
                    Page1(col, model, lang);
                }
            });
        });
    }

    private static void HeaderBlock(IContainer container, PassportPdfModel model, string lang, bool page2)
    {
        container.Column(col =>
        {
            col.Item().Background(Purple).PaddingHorizontal(14).PaddingVertical(8).Row(row =>
            {
                row.ConstantItem(20).Height(20).AlignMiddle().Svg(LobsterSvg());
                row.ConstantItem(8);
                row.RelativeItem().AlignMiddle().Column(brand =>
                {
                    brand.Item().Text(text =>
                    {
                        text.Span("Lobsy").FontSize(14).Bold().FontColor(White);
                        text.Span("  ").FontSize(10);
                        if (page2)
                        {
                            text.Span(model.FullName).FontSize(10).SemiBold().FontColor(White);
                        }
                        else
                        {
                            text.Span(PassportPdfStrings.T(lang, "RecruiterHeader")).FontSize(9).FontColor(Color.FromHex("#e8d9f7"));
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
                            badgeRow.ConstantItem(9).Height(9).AlignMiddle().Svg(CheckSvg("#127a5c"));
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
            });
            col.Item().Width(567).Height(3).Svg(HeaderGradientSvg());
        });
    }

    private static void FooterBlock(IContainer container, PassportPdfModel model, string lang)
    {
        var hasQr = !string.IsNullOrWhiteSpace(model.QrVerifyUrl);
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
                        .FontSize(6.5f).FontColor(Muted).LineHeight(1.15f).ClampLines(3);
                }

                textCol.Item().PaddingTop(2).Text(model.GeneratedLabel).FontSize(6.5f).FontColor(Muted);
            });

            row.ConstantItem(6);
            row.AutoItem().AlignMiddle().AlignRight().Column(meta =>
            {
                meta.Item().AlignRight().Text(text =>
                {
                    text.CurrentPageNumber().FontSize(7).Bold().FontColor(Purple);
                    text.Span("/").FontSize(7).FontColor(Muted);
                    text.TotalPages().FontSize(7).FontColor(Muted);
                });
                meta.Item().AlignRight().Text("lobsy.nl").FontSize(7).FontColor(Magenta);
            });
        });
    }

    private static void Page1(ColumnDescriptor col, PassportPdfModel model, string lang)
    {
        if (!model.ShowBadge && !string.IsNullOrWhiteSpace(model.TestsPlainStatus))
        {
            col.Item().Text(model.TestsPlainStatus).FontSize(7.5f).FontColor(Muted);
        }

        Hero(col, model, lang);
        FourCardsRow(col, model, lang);
        LanguagesAndPrefs(col, model, lang);
        StrengthSection(col, model, lang);
        SectorsCompact(col, model, lang);
        ExperiencePapers(col, model, lang);
    }

    private static void Page2(ColumnDescriptor col, PassportPdfModel model, string lang)
    {
        if (model.Sectors.Count > 0)
        {
            col.Item().Column(block =>
            {
                SectionTitle(block, PassportPdfStrings.T(lang, "SectorsTitle"));
                block.Item().PaddingTop(2).Row(row =>
                {
                    for (var i = 0; i < Math.Min(3, model.Sectors.Count); i++)
                    {
                        if (i > 0)
                        {
                            row.ConstantItem(4);
                        }

                        var sector = model.Sectors[i];
                        row.RelativeItem().Background(SoftP).Border(1).BorderColor(Line).Padding(5).Column(card =>
                        {
                            card.Item().Text(sector.Title).FontSize(8.5f).Bold().FontColor(Purple).ClampLines(1);
                            if (!string.IsNullOrWhiteSpace(sector.RankSubtitle))
                            {
                                card.Item().PaddingTop(1).Text(sector.RankSubtitle).FontSize(7).FontColor(Magenta);
                            }

                            foreach (var why in sector.WhyBullets.Take(2))
                            {
                                card.Item().PaddingTop(1).Row(bullet =>
                                {
                                    bullet.ConstantItem(6).Text("•").FontSize(7).FontColor(Coral);
                                    bullet.RelativeItem().Text(why).FontSize(7).LineHeight(1.15f).ClampLines(2);
                                });
                            }

                            if (!string.IsNullOrWhiteSpace(sector.ExampleRoles))
                            {
                                card.Item().PaddingTop(2).Text(sector.ExampleRoles).FontSize(6.5f).FontColor(Muted).ClampLines(2);
                            }
                        });
                    }
                });
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

        var hasHow = model.HowWorkLines.Count > 0;
        var hasBest = model.BestOutTips.Count > 0 || model.LearningLines.Count > 0;
        if (hasHow || hasBest)
        {
            col.Item().Row(row =>
            {
                if (hasHow)
                {
                    row.RelativeItem().Column(left =>
                    {
                        SectionTitle(left, PassportPdfStrings.T(lang, "HowIWork"));
                        left.Item().PaddingTop(3).Background(SoftP).Border(1).BorderColor(Line).Padding(6).Column(list =>
                        {
                            foreach (var line in model.HowWorkLines.Take(6))
                            {
                                list.Item().PaddingBottom(2).Text(text =>
                                {
                                    text.Span(line.Lead).FontSize(8.5f).Bold();
                                    if (!string.IsNullOrWhiteSpace(line.Tail))
                                    {
                                        text.Span(" — " + line.Tail).FontSize(8.5f).FontColor(Muted);
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
                        right.Item().PaddingTop(3).Background(SoftC).Border(1).BorderColor(Line).Padding(6).Column(list =>
                        {
                            foreach (var tip in model.BestOutTips.Take(4))
                            {
                                list.Item().PaddingBottom(2).Row(tipRow =>
                                {
                                    tipRow.ConstantItem(8).Text("•").FontSize(8).FontColor(Coral);
                                    tipRow.RelativeItem().Text(tip).FontSize(8.5f);
                                });
                            }

                            if (model.LearningLines.Count > 0)
                            {
                                list.Item().PaddingTop(3).Text(PassportPdfStrings.T(lang, "LearnTitle"))
                                    .FontSize(7.5f).Bold().FontColor(Purple);
                                list.Item().PaddingTop(2).Inlined(chips =>
                                {
                                    chips.Spacing(3);
                                    chips.VerticalSpacing(3);
                                    foreach (var goal in model.LearningLines.Take(5))
                                    {
                                        chips.Item().Background(White).PaddingHorizontal(6).PaddingVertical(2)
                                            .Text(goal).FontSize(7.5f).FontColor(Ink);
                                    }
                                });
                            }
                        });
                    });
                }
            });
        }

        if (!string.IsNullOrWhiteSpace(model.OwnWords))
        {
            col.Item().Column(block =>
            {
                SectionTitle(block, PassportPdfStrings.T(lang, "OwnWords"));
                block.Item().PaddingTop(2).Background(SoftP).Border(1).BorderColor(Line).Padding(6)
                    .Text(model.OwnWords).FontSize(8f).LineHeight(1.25f).Italic().ClampLines(6);
            });
        }

        VerifiedMeansBox(col, model, lang);
    }

    private static void Hero(ColumnDescriptor col, PassportPdfModel model, string lang)
    {
        col.Item().Row(row =>
        {
            row.ConstantItem(44).Height(44).AlignMiddle().Layers(layers =>
            {
                layers.Layer().Svg(AvatarGradientSvg());
                layers.PrimaryLayer().AlignCenter().AlignMiddle()
                    .Text(model.Initials).FontSize(14).Bold().FontColor(White);
            });
            row.ConstantItem(10);
            row.RelativeItem().Column(name =>
            {
                name.Item().Text(model.FullName).FontSize(HeroNameFontSize(model.FullName)).Bold().FontColor(Ink).LineHeight(1.1f);
                if (!string.IsNullOrWhiteSpace(model.Tagline))
                {
                    name.Item().PaddingTop(2).Text(model.Tagline).FontSize(8.5f).FontColor(Muted).LineHeight(1.2f);
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
                            list.Item().PaddingBottom(3).Row(langRow =>
                            {
                                langRow.ConstantItem(72).Text(line.Label).FontSize(8.5f).Bold();
                                langRow.RelativeItem().Element(box => LanguageDots(box, line.Value, lang));
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

            block.Item().PaddingTop(3).Row(row =>
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

    private static void SectorsCompact(ColumnDescriptor col, PassportPdfModel model, string lang)
    {
        if (model.Sectors.Count == 0)
        {
            return;
        }

        col.Item().Column(block =>
        {
            SectionTitle(block, PassportPdfStrings.T(lang, "SectorsTitle"));
            block.Item().PaddingTop(3).Row(row =>
            {
                for (var i = 0; i < Math.Min(3, model.Sectors.Count); i++)
                {
                    if (i > 0)
                    {
                        row.ConstantItem(5);
                    }

                    var sector = model.Sectors[i];
                    row.RelativeItem().Background(SoftP).Border(1).BorderColor(Line).Padding(6).Column(card =>
                    {
                        card.Item().Text(sector.Title).FontSize(9).Bold().FontColor(Purple);
                        if (!string.IsNullOrWhiteSpace(sector.RankSubtitle))
                        {
                            card.Item().PaddingTop(1).Text(sector.RankSubtitle).FontSize(7).FontColor(Magenta);
                        }

                        card.Item().PaddingTop(2).Text(sector.P1Teaser).FontSize(8).LineHeight(1.2f);
                    });
                }
            });

            if (!string.IsNullOrWhiteSpace(model.SoughtLine))
            {
                block.Item().PaddingTop(4).Text(model.SoughtLine).FontSize(8.5f).Bold().FontColor(Ink);
            }
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
                            left.Item().PaddingBottom(3).Background(White).Border(1).BorderColor(Line).Padding(6).Column(card =>
                            {
                                card.Item().Row(line =>
                                {
                                    line.RelativeItem().Text(job.Title).FontSize(9).Bold().FontColor(Ink);
                                    if (!string.IsNullOrWhiteSpace(job.DurationLabel))
                                    {
                                        line.AutoItem().Text(job.DurationLabel).FontSize(8).Bold().FontColor(Magenta);
                                    }
                                });
                                if (!string.IsNullOrWhiteSpace(job.Meta))
                                {
                                    card.Item().PaddingTop(1).Text(job.Meta).FontSize(8).FontColor(Muted);
                                }

                                if (!string.IsNullOrWhiteSpace(job.Detail))
                                {
                                    card.Item().PaddingTop(1).Text(job.Detail).FontSize(8).LineHeight(1.2f);
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

    private static void VerifiedMeansBox(ColumnDescriptor col, PassportPdfModel model, string lang)
    {
        if (model.VerifiedChecked.Count == 0 && model.VerifiedNotChecked.Count == 0)
        {
            return;
        }

        col.Item().Column(block =>
        {
            SectionTitle(block, PassportPdfStrings.T(lang, "VerifiedMeans"));
            block.Item().PaddingTop(3).Row(row =>
            {
                row.RelativeItem().Background(Mint).Border(1).BorderColor(Line).Padding(7).Column(yes =>
                {
                    yes.Item().Text(PassportPdfStrings.T(lang, "VerifiedCheckedIntro")).FontSize(9).Bold().FontColor(Green);
                    foreach (var line in model.VerifiedChecked)
                    {
                        yes.Item().PaddingTop(2).Row(lineRow =>
                        {
                            lineRow.ConstantItem(10).Height(10).Svg(CheckSvg("#127a5c"));
                            lineRow.ConstantItem(4);
                            lineRow.RelativeItem().Text(line).FontSize(8);
                        });
                    }
                });
                row.ConstantItem(8);
                row.RelativeItem().Background(SoftC).Border(1).BorderColor(Line).Padding(7).Column(no =>
                {
                    no.Item().Text(PassportPdfStrings.T(lang, "VerifiedNotIntro")).FontSize(9).Bold().FontColor(Coral);
                    foreach (var line in model.VerifiedNotChecked)
                    {
                        no.Item().PaddingTop(2).Row(lineRow =>
                        {
                            lineRow.ConstantItem(8).Text("×").FontSize(9).Bold().FontColor(Coral);
                            lineRow.RelativeItem().Text(line).FontSize(8);
                        });
                    }
                });
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
        box.Background(bg).Border(1).BorderColor(Line).Padding(5).Column(col =>
        {
            col.Item().Text(layer.Nickname).FontSize(8).Bold().FontColor(Purple);
            col.Item().PaddingTop(1).Text(layer.LayerLabel).FontSize(7).FontColor(Muted);
            col.Item().PaddingTop(2).Text(layer.Body).FontSize(7.5f).LineHeight(1.2f);
            if (!string.IsNullOrWhiteSpace(layer.CompletedLabel))
            {
                col.Item().PaddingTop(2).Text(layer.CompletedLabel).FontSize(7).FontColor(Green);
            }
        });
    }

    private static void InfoCard(IContainer box, string title, Action<ColumnDescriptor> content)
    {
        box.ShowEntire().Background(White).Border(1).BorderColor(Line).Padding(6).Column(col =>
        {
            col.Item().Text(title).FontSize(8).Bold().FontColor(Purple);
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
            _ => (Line, Ink)
        };
        box.Background(bg).PaddingHorizontal(6).PaddingVertical(2).Text(chip.Text).FontSize(8).FontColor(fg);
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
        col.Item().Row(row =>
        {
            row.ConstantItem(3).Height(12).Background(Coral);
            row.ConstantItem(6);
            row.RelativeItem().AlignMiddle().Text(title).FontSize(11).Bold().FontColor(Purple);
        });
    }

    private static float HeroNameFontSize(string fullName)
        => fullName.Length switch
        {
            > 36 => 14f,
            > 30 => 16f,
            > 24 => 18f,
            _ => 20f
        };

    private static string LobsterSvg()
        => "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\">"
           + "<path d=\"M7.2 9.2c-1.8-2.4-4.4-1.6-3.6.8\" fill=\"none\" stroke=\"#f5503a\" stroke-width=\"1.6\" stroke-linecap=\"round\"/>"
           + "<path d=\"M16.8 9.2c1.8-2.4 4.4-1.6 3.6.8\" fill=\"none\" stroke=\"#f5503a\" stroke-width=\"1.6\" stroke-linecap=\"round\"/>"
           + "<ellipse cx=\"12\" cy=\"12.2\" rx=\"3.1\" ry=\"4.3\" fill=\"#f5503a\"/>"
           + "<path d=\"M9.1 15.4c.5 2.5 5.3 2.5 5.8 0\" fill=\"#f5503a\"/>"
           + "</svg>";

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
