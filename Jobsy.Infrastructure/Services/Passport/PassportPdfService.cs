using System.Globalization;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Passport;
using Jobsy.Core.Time;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Jobsy.Infrastructure.Services.Passport;

public sealed class PassportPdfService : IPassportPdfService
{
    private static readonly Color Navy = Color.FromHex("#0F2D5C");
    private static readonly Color Ink = Color.FromHex("#122033");
    private static readonly Color Muted = Color.FromHex("#5A6A7D");
    private static readonly Color Orange = Color.FromHex("#F54A1B");
    private static readonly Color Soft = Color.FromHex("#F7F4F0");
    private static readonly Color Peach = Color.FromHex("#FFE8E0");
    private static readonly Color Sand = Color.FromHex("#F3EBE3");
    private static readonly Color Green = Color.FromHex("#ECFDF3");
    private static readonly Color GreenInk = Color.FromHex("#15803D");
    private static readonly Color White = Colors.White;

    private readonly IPlatformCompanySettingsService _companySettings;

    static PassportPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
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

    private static void Compose(PageDescriptor page, PassportPdfModel model, string lang, bool page2)
    {
        page.Size(PageSizes.A4);
        page.Margin(0);
        page.DefaultTextStyle(style => style.FontSize(9).FontColor(Ink).LineHeight(1.25f));

        page.Header().Height(40).Background(Navy).Layers(layers =>
        {
            layers.Layer().AlignRight().PaddingRight(132).AlignMiddle().Width(168).Height(40).Svg(HelixSvg());
            layers.PrimaryLayer().PaddingHorizontal(16).AlignMiddle().Row(row =>
            {
                row.ConstantItem(18).Height(18).AlignMiddle().Svg(LobsterSvg());
                row.ConstantItem(7);
                row.RelativeItem().AlignMiddle().Text(text =>
                {
                    text.Span("Lobsy").FontSize(15).Bold().FontColor(White);
                    text.Span("    ").FontSize(11);
                    text.Span(PassportPdfStrings.T(lang, "Title")).FontSize(11).FontColor(Color.FromHex("#D5E2F2"));
                });
                row.ConstantItem(140).AlignMiddle().AlignRight().Text(page2
                        ? PassportPdfStrings.T(lang, "Page2")
                        : PassportPdfStrings.T(lang, "Page1"))
                    .FontSize(11).FontColor(White);
            });
        });

        page.Content().PaddingHorizontal(16).PaddingTop(8).PaddingBottom(4).ScaleToFit().Column(col =>
        {
            col.Spacing(7);
            if (page2)
            {
                Page2(col, model, lang);
            }
            else
            {
                Page1(col, model, lang);
            }
        });

        page.Footer().PaddingHorizontal(16).PaddingBottom(8).PaddingTop(2).Row(row =>
        {
            row.RelativeItem().AlignMiddle().Text(PassportPdfStrings.T(lang, "FooterOne")).FontSize(7).FontColor(Muted);
            row.ConstantItem(36).AlignMiddle().AlignRight().Text(text =>
            {
                text.CurrentPageNumber().FontSize(7).FontColor(Navy);
                text.Span("/").FontSize(7).FontColor(Muted);
                text.TotalPages().FontSize(7).FontColor(Muted);
            });
        });
    }

    private static void Page1(ColumnDescriptor col, PassportPdfModel model, string lang)
    {
        Hero(col, model);
        if (!string.IsNullOrWhiteSpace(model.Story))
        {
            col.Item().Column(block =>
            {
                SectionTitle(block, PassportPdfStrings.T(lang, "StoryTitle"));
                block.Item().PaddingTop(3).Text(model.Story).FontSize(8.5f).LineHeight(1.3f);
            });
        }

        var hasStoryColumn = model.Traits.Count > 0 || model.HomeLines.Count > 0;
        if (hasStoryColumn)
        {
            col.Item().Row(row =>
            {
                row.RelativeItem(1.12f).Column(left => ProfileColumn(left, model, lang));
                row.ConstantItem(10);
                row.RelativeItem().Column(right => RadarBlock(right, model, lang));
            });
        }
        else
        {
            RadarBlock(col, model, lang);
        }

        LegendRow(col, model);

        if (model.ValueLines.Count > 0 || model.JobFits.Count > 0)
        {
            col.Item().Row(row =>
            {
                if (model.ValueLines.Count > 0)
                {
                    row.RelativeItem(1.12f).Column(left => ValuesBlock(left, model, lang));
                }

                if (model.ValueLines.Count > 0 && model.JobFits.Count > 0)
                {
                    row.ConstantItem(10);
                }

                if (model.JobFits.Count > 0)
                {
                    row.RelativeItem().Column(right => JobsBlock(right, model, lang));
                }
            });
        }

        Practical(col, model, lang);
    }

    private static void Page2(ColumnDescriptor col, PassportPdfModel model, string lang)
    {
        var paid = OfKind(model, "paid");
        var volunteer = OfKind(model, "volunteer");
        var care = OfKind(model, "care");

        SectionTitle(col, PassportPdfStrings.T(lang, "WorkExperience"));
        if (paid.Count == 0 && volunteer.Count == 0 && care.Count == 0)
        {
            col.Item().Text(PassportPdfStrings.T(lang, "NoExperience")).FontSize(8.5f).FontColor(Muted);
        }
        else if (paid.Count == 0)
        {
            col.Item().Text(PassportPdfStrings.T(lang, "NoPaid")).FontSize(8.5f).FontColor(Muted);
        }
        else
        {
            ExperienceCards(col, paid);
        }

        if (model.ExperienceMore > 0)
        {
            col.Item().Text(PassportPdfStrings.F(lang, "More", model.ExperienceMore)).FontSize(7.5f).FontColor(Muted);
        }

        if (volunteer.Count > 0)
        {
            SectionTitle(col, PassportPdfStrings.T(lang, "VolunteerTitle"));
            ExperienceCards(col, volunteer);
        }

        if (care.Count > 0)
        {
            SectionTitle(col, PassportPdfStrings.T(lang, "CareTitle"));
            ExperienceCards(col, care);
        }

        SectionTitle(col, PassportPdfStrings.T(lang, "PapersTitle"));
        var papers = model.Educations.Concat(model.Certificates).Where(item => !string.IsNullOrWhiteSpace(item)).Take(8).ToList();
        if (papers.Count == 0)
        {
            col.Item().Text(PassportPdfStrings.T(lang, "NoPapers")).FontSize(8.5f).FontColor(Muted);
        }
        else
        {
            col.Item().Inlined(row =>
            {
                row.Spacing(4);
                row.VerticalSpacing(4);
                foreach (var paper in papers)
                {
                    row.Item().Background(Peach).PaddingHorizontal(7).PaddingVertical(3)
                        .Text(paper).FontSize(8).FontColor(Ink);
                }
            });
        }

        if (model.LearningLines.Count > 0)
        {
            SectionTitle(col, PassportPdfStrings.T(lang, "LearnTitle"));
            col.Item().Inlined(row =>
            {
                row.Spacing(4);
                row.VerticalSpacing(4);
                foreach (var goal in model.LearningLines)
                {
                    row.Item().Background(Sand).PaddingHorizontal(7).PaddingVertical(3)
                        .Text(goal).FontSize(8).FontColor(Ink);
                }
            });
        }

        SectionTitle(col, PassportPdfStrings.T(lang, "ReferencesTitle"));
        if (model.Quotes.Count == 0)
        {
            col.Item().Text(PassportPdfStrings.T(lang, "NoReferences")).FontSize(8.5f).FontColor(Muted);
        }
        else if (model.Quotes.Count == 1)
        {
            QuoteCard(col, model.Quotes[0], Green, greenTitle: true);
        }
        else
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Element(box => QuoteCard(box, model.Quotes[0], Green, greenTitle: true));
                row.ConstantItem(8);
                row.RelativeItem().Element(box => QuoteCard(box, model.Quotes[1], Peach, greenTitle: false));
            });
        }

        if (model.Direction.Count > 0)
        {
            SectionTitle(col, PassportPdfStrings.T(lang, "DirectionTitle"));
            col.Item().Background(Soft).Padding(8).Row(row =>
            {
                for (var i = 0; i < model.Direction.Count; i++)
                {
                    if (i > 0)
                    {
                        row.ConstantItem(4);
                    }

                    var step = model.Direction[i];
                    var number = (i + 1).ToString(CultureInfo.InvariantCulture);
                    row.RelativeItem().Column(stepCol =>
                    {
                        stepCol.Item().AlignCenter().Width(18).Height(18).Layers(layers =>
                        {
                            layers.Layer().Svg(AvatarSvg());
                            layers.PrimaryLayer().AlignCenter().AlignMiddle()
                                .Text(number).FontSize(8).Bold().FontColor(White);
                        });
                        stepCol.Item().PaddingTop(3).AlignCenter().Text(step.Title).FontSize(7.5f).Bold().FontColor(Navy);
                        stepCol.Item().AlignCenter().Text(step.State).FontSize(7).FontColor(Orange);
                    });
                }
            });
        }

        col.Item().Background(Soft).Padding(8).Row(row =>
        {
            row.RelativeItem().Column(yes =>
            {
                yes.Item().Text(PassportPdfStrings.T(lang, "CheckedTitle")).FontSize(9).Bold().FontColor(GreenInk);
                if (model.Checked.Count == 0)
                {
                    yes.Item().PaddingTop(2).Text("—").FontSize(8).FontColor(Muted);
                }

                foreach (var line in model.Checked)
                {
                    yes.Item().PaddingTop(2).Text("✓  " + line).FontSize(8);
                }
            });
            row.ConstantItem(8);
            row.RelativeItem().Column(no =>
            {
                no.Item().Text(PassportPdfStrings.T(lang, "NotOnPassport")).FontSize(9).Bold().FontColor(Orange);
                foreach (var line in model.NotChecked)
                {
                    no.Item().PaddingTop(2).Text("·  " + line).FontSize(8);
                }
            });
        });

        col.Item().Background(Orange).PaddingVertical(8).PaddingHorizontal(10).AlignCenter().Text(text =>
        {
            text.Span(PassportPdfStrings.T(lang, "ShareCta")).FontSize(10).Bold().FontColor(White);
            text.Span("   ·   ").FontSize(9).FontColor(Color.FromHex("#FFE4D8"));
            text.Span(PassportPdfStrings.T(lang, "ShareCtaTail")).FontSize(9).FontColor(White);
        });
    }

    private static void Hero(ColumnDescriptor col, PassportPdfModel model)
    {
        col.Item().Row(row =>
        {
            row.RelativeItem().Column(name =>
            {
                name.Item().Text(model.FullName).FontSize(20).Bold().FontColor(Navy);
                if (!string.IsNullOrWhiteSpace(model.HeroMeta))
                {
                    name.Item().PaddingTop(1).Text(model.HeroMeta).FontSize(8).FontColor(Muted);
                }

                if (model.Chips.Count > 0)
                {
                    name.Item().PaddingTop(4).Inlined(chips =>
                    {
                        chips.Spacing(4);
                        foreach (var chip in model.Chips)
                        {
                            var bg = chip.Tone switch
                            {
                                "open" => Peach,
                                "role" => Sand,
                                _ => Soft
                            };
                            var color = chip.Tone == "open" ? Orange : Ink;
                            chips.Item().Background(bg).PaddingHorizontal(6).PaddingVertical(2)
                                .Text(chip.Text).FontSize(8).FontColor(color);
                        }
                    });
                }
            });
            row.ConstantItem(8);
            row.ConstantItem(34).Height(34).AlignMiddle().Layers(layers =>
            {
                layers.Layer().Svg(AvatarSvg());
                layers.PrimaryLayer().AlignCenter().AlignMiddle()
                    .Text(AvatarLetter(model)).FontSize(13).Bold().FontColor(White);
            });
        });
    }

    private static void ProfileColumn(ColumnDescriptor col, PassportPdfModel model, string lang)
    {
        col.Spacing(8);
        if (model.Traits.Count > 0)
        {
            col.Item().Column(block =>
            {
                SectionTitle(block, PassportPdfStrings.T(lang, "TraitsTitle"));
                foreach (var line in model.Traits)
                {
                    block.Item().PaddingTop(3).Row(row =>
                    {
                        row.ConstantItem(8).PaddingTop(1).Text("●").FontSize(6).FontColor(Orange);
                        row.RelativeItem().Text(text =>
                        {
                            text.Span(line.Label).FontSize(8.5f).Bold();
                            text.Span("  —  " + line.Gloss).FontSize(8.5f).FontColor(Ink);
                        });
                    });
                }
            });
        }

        if (model.HomeLines.Count > 0)
        {
            col.Item().Column(block =>
            {
                SectionTitle(block, PassportPdfStrings.T(lang, "HomeTitle"));
                foreach (var line in model.HomeLines)
                {
                    block.Item().PaddingTop(3).Text("·  " + line).FontSize(8.5f);
                }
            });
        }
    }

    private static void ValuesBlock(ColumnDescriptor col, PassportPdfModel model, string lang)
    {
        col.Item().Column(block =>
        {
            SectionTitle(block, PassportPdfStrings.T(lang, "ValuesTitle"));
            foreach (var line in model.ValueLines)
            {
                block.Item().PaddingTop(3).Text(text =>
                {
                    text.Span(line.Label).FontSize(8.5f).Bold().FontColor(Orange);
                    text.Span("  —  " + line.Gloss).FontSize(8.5f).FontColor(Ink);
                });
            }
        });
    }

    private static void RadarBlock(ColumnDescriptor col, PassportPdfModel model, string lang)
    {
        col.Item().Column(block =>
        {
            SectionTitle(block, PassportPdfStrings.T(lang, "RadarTitle"));
            block.Item().PaddingTop(1).Text(PassportPdfStrings.T(lang, "RadarNote")).FontSize(7.5f).FontColor(Muted);
            if (model.ShowRadar)
            {
                block.Item().PaddingTop(2).Element(box => Radar(box, model));
            }
            else if (model.Dna.Any(card => card.Present))
            {
                block.Item().PaddingTop(2).Text(PassportPdfStrings.T(lang, "RadarSkipped")).FontSize(8).FontColor(Muted);
            }
        });
    }

    private static void LegendRow(ColumnDescriptor col, PassportPdfModel model)
    {
        if (model.Dna.Count == 0)
        {
            return;
        }

        col.Item().Row(row =>
        {
            for (var i = 0; i < model.Dna.Count; i++)
            {
                if (i > 0)
                {
                    row.ConstantItem(6);
                }

                var card = model.Dna[i];
                row.RelativeItem().Column(item =>
                {
                    item.Item().Text(card.Title).FontSize(7.5f).Bold().FontColor(card.Present ? Navy : Muted);
                    item.Item().Text(card.Body).FontSize(7.5f).FontColor(card.Present ? Ink : Muted);
                });
            }
        });
    }

    private static void JobsBlock(ColumnDescriptor col, PassportPdfModel model, string lang)
    {
        col.Item().Column(block =>
        {
            SectionTitle(block, PassportPdfStrings.T(lang, "JobsTitle"));
            foreach (var job in model.JobFits)
            {
                block.Item().PaddingTop(3).Column(card =>
                {
                    card.Item().Text(text =>
                    {
                        text.Span("●  ").FontSize(7).FontColor(Orange);
                        text.Span(job.Title).FontSize(8.5f).Bold().FontColor(Navy);
                    });
                    card.Item().PaddingLeft(12).Text(job.Why).FontSize(7.5f).FontColor(Muted);
                });
            }
        });
    }

    private static void Practical(ColumnDescriptor col, PassportPdfModel model, string lang)
    {
        col.Item().Background(Soft).Padding(7).Column(block =>
        {
            block.Item().Text(PassportPdfStrings.T(lang, "PracticalTitle")).FontSize(9).Bold().FontColor(Navy);
            if (!string.IsNullOrWhiteSpace(model.PracticalLine))
            {
                block.Item().PaddingTop(2).Text(model.PracticalLine).FontSize(8);
            }

            if (!string.IsNullOrWhiteSpace(model.PracticalSeek))
            {
                block.Item().PaddingTop(1).Text(model.PracticalSeek).FontSize(8).FontColor(Muted);
            }
        });
    }

    private static void Radar(IContainer box, PassportPdfModel model)
    {
        var competence = Present(model, 0);
        var career = Present(model, 1);
        var culture = Present(model, 2);
        var values = Present(model, 3);
        box.AlignCenter().Width(214).Height(132).Column(col =>
        {
            col.Item().Height(12).AlignCenter().Text(Axis(model, 3)).FontSize(7).FontColor(values ? Navy : Muted);
            col.Item().Height(108).Row(row =>
            {
                row.ConstantItem(52).AlignMiddle().AlignRight().Text(Axis(model, 0)).FontSize(7).FontColor(competence ? Navy : Muted);
                row.RelativeItem().AlignMiddle().Svg(RadarSvg(competence, career, culture, values)).FitArea();
                row.ConstantItem(48).AlignMiddle().Text(Axis(model, 1)).FontSize(7).FontColor(career ? Navy : Muted);
            });
            col.Item().Height(12).AlignCenter().Text(Axis(model, 2)).FontSize(7).FontColor(culture ? Navy : Muted);
        });
    }

    private static bool Present(PassportPdfModel model, int index)
        => index < model.Dna.Count && model.Dna[index].Present;

    private static string Axis(PassportPdfModel model, int index)
        => index < model.Dna.Count ? model.Dna[index].Title : "";

    private static string RadarSvg(bool competence, bool career, bool culture, bool values)
    {
        static double Radius(bool on) => on ? 78 : 0;
        static string At(double radius, double degrees)
        {
            var rad = degrees * Math.PI / 180d;
            var x = 100 + (radius * Math.Cos(rad));
            var y = 100 + (radius * Math.Sin(rad));
            return string.Create(CultureInfo.InvariantCulture, $"{x:0.##},{y:0.##}");
        }

        string Ring(double radius) => string.Join(" ", new[] { -90d, 0d, 90d, 180d }.Select(angle => At(radius, angle)));
        var shape = string.Join(" ", new[]
        {
            At(Radius(values), -90),
            At(Radius(career), 0),
            At(Radius(culture), 90),
            At(Radius(competence), 180)
        });
        return "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 200 200\">"
               + "<polygon points=\"" + Ring(78) + "\" fill=\"none\" stroke=\"#E4DDD4\" stroke-width=\"1.2\"/>"
               + "<polygon points=\"" + Ring(48) + "\" fill=\"none\" stroke=\"#E4DDD4\" stroke-width=\"1\"/>"
               + "<line x1=\"100\" y1=\"22\" x2=\"100\" y2=\"178\" stroke=\"#E4DDD4\" stroke-width=\"1\"/>"
               + "<line x1=\"22\" y1=\"100\" x2=\"178\" y2=\"100\" stroke=\"#E4DDD4\" stroke-width=\"1\"/>"
               + "<polygon points=\"" + shape + "\" fill=\"#FFE8E0\" stroke=\"#F54A1B\" stroke-width=\"2.2\"/>"
               + "</svg>";
    }

    private static string AvatarSvg()
        => "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 36 36\"><circle cx=\"18\" cy=\"18\" r=\"18\" fill=\"#0F2D5C\"/></svg>";

    private static string AvatarLetter(PassportPdfModel model)
    {
        var letter = model.FullName.FirstOrDefault(char.IsLetter);
        return letter == default ? model.Initials[..1] : char.ToUpperInvariant(letter).ToString();
    }

    private static List<PassportExperienceLine> OfKind(PassportPdfModel model, string kind)
        => model.Experience.Where(line => string.Equals(line.Kind ?? "paid", kind, StringComparison.Ordinal)).ToList();

    private static void ExperienceCards(ColumnDescriptor col, IReadOnlyList<PassportExperienceLine> jobs)
    {
        foreach (var job in jobs)
        {
            col.Item().Background(Soft).Padding(7).Row(row =>
            {
                row.ConstantItem(3).Background(Orange);
                row.ConstantItem(8);
                row.RelativeItem().Column(card =>
                {
                    card.Item().Row(line =>
                    {
                        line.RelativeItem().Text(job.Title).FontSize(10).Bold().FontColor(Navy);
                        if (!string.IsNullOrWhiteSpace(job.Period))
                        {
                            line.ConstantItem(108).AlignRight().Text(job.Period).FontSize(8).Bold().FontColor(Orange);
                        }
                    });
                    if (!string.IsNullOrWhiteSpace(job.Place))
                    {
                        card.Item().PaddingTop(1).Text(job.Place).FontSize(8).FontColor(Muted);
                    }

                    foreach (var duty in job.Duties ?? [])
                    {
                        card.Item().PaddingTop(1).Text("–  " + duty).FontSize(8);
                    }
                });
            });
        }
    }

    private static string HelixSvg()
        => "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 168 40\" fill=\"none\">"
           + "<path d=\"M0 14 C 14 14, 14 26, 28 26 S 42 14, 56 14 S 70 26, 84 26 S 98 14, 112 14 S 126 26, 140 26 S 154 14, 168 14\" stroke=\"#D5E2F2\" stroke-width=\"1.2\" opacity=\"0.55\"/>"
           + "<path d=\"M0 26 C 14 26, 14 14, 28 14 S 42 26, 56 26 S 70 14, 84 14 S 98 26, 112 26 S 126 14, 140 14 S 154 26, 168 26\" stroke=\"#F54A1B\" stroke-width=\"1\" opacity=\"0.45\"/>"
           + "</svg>";

    private static string LobsterSvg()
        => "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\">"
           + "<path d=\"M7.2 9.2c-1.8-2.4-4.4-1.6-3.6.8\" fill=\"none\" stroke=\"#F54A1B\" stroke-width=\"1.6\" stroke-linecap=\"round\"/>"
           + "<path d=\"M16.8 9.2c1.8-2.4 4.4-1.6 3.6.8\" fill=\"none\" stroke=\"#F54A1B\" stroke-width=\"1.6\" stroke-linecap=\"round\"/>"
           + "<ellipse cx=\"12\" cy=\"12.2\" rx=\"3.1\" ry=\"4.3\" fill=\"#F54A1B\"/>"
           + "<path d=\"M9.1 15.4c.5 2.5 5.3 2.5 5.8 0\" fill=\"#F54A1B\"/>"
           + "</svg>";

    private static void SectionTitle(ColumnDescriptor col, string title)
    {
        col.Item().Row(row =>
        {
            row.ConstantItem(3).Height(11).Background(Orange);
            row.ConstantItem(6);
            row.RelativeItem().AlignMiddle().Text(title).FontSize(11).Bold().FontColor(Navy);
        });
    }

    private static void QuoteCard(ColumnDescriptor col, PassportReferenceQuote quote, Color background, bool greenTitle)
    {
        col.Item().Element(box => QuoteCard(box, quote, background, greenTitle));
    }

    private static void QuoteCard(IContainer box, PassportReferenceQuote quote, Color background, bool greenTitle)
    {
        var title = greenTitle ? GreenInk : Orange;
        box.Background(background).Padding(7).Column(card =>
        {
            card.Item().Text(quote.Attribution).FontSize(8).Bold().FontColor(title);
            card.Item().PaddingTop(2).Text("“" + quote.Quote + "”").FontSize(8).Italic();
        });
    }
}
