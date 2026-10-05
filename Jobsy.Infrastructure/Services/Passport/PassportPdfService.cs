using Jobsy.Core.Interfaces;
using Jobsy.Core.Passport;
using Jobsy.Core.Time;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Jobsy.Infrastructure.Services.Passport;

public sealed class PassportPdfService : IPassportPdfService
{
    private static readonly Color Purple = Color.FromHex("#5b2a9a");
    private static readonly Color Magenta = Color.FromHex("#b5309a");
    private static readonly Color Coral = Color.FromHex("#e25b3a");
    private static readonly Color Ink = Color.FromHex("#23173a");
    private static readonly Color Muted = Color.FromHex("#6d5b86");
    private static readonly Color SoftPurple = Color.FromHex("#f4effb");
    private static readonly Color SoftPeach = Color.FromHex("#fff4ee");
    private static readonly Color SoftGreen = Color.FromHex("#e8f6ee");
    private static readonly Color Line = Color.FromHex("#eadff5");
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
        var logo = _companySettings.GetBrandLogoPng();
        var lang = model.Language;

        var bytes = Document.Create(container =>
        {
            container.Page(page => Compose(page, model, logo, lang, page2: false));
            container.Page(page => Compose(page, model, logo, lang, page2: true));
        }).GeneratePdf();

        return Task.FromResult(bytes);
    }

    private static void Compose(PageDescriptor page, PassportPdfModel model, byte[]? logo, string lang, bool page2)
    {
        page.Size(PageSizes.A4);
        page.MarginHorizontal(22);
        page.MarginVertical(16);
        page.DefaultTextStyle(x => x.FontSize(9).FontColor(Ink));

        page.Content().ScaleToFit().Column(col =>
        {
            col.Spacing(8);
            if (page2)
            {
                Page2(col, model, logo, lang);
            }
            else
            {
                Page1(col, model, logo, lang);
            }
        });

        page.Footer().PaddingTop(6).Column(foot =>
        {
            foot.Item().LineHorizontal(1).LineColor(Line);
            foot.Item().PaddingTop(4).Text(PassportPdfStrings.T(lang, "FooterLive")).FontSize(7).FontColor(Muted);
            foot.Item().Text(PassportPdfStrings.T(lang, "FooterPrivacy")).FontSize(7).FontColor(Muted);
            foot.Item().Text(PassportPdfStrings.T(lang, "FooterNoScore")).FontSize(7).FontColor(Muted);
            foot.Item().PaddingTop(2).AlignRight().Text(text =>
            {
                text.CurrentPageNumber().FontSize(7).FontColor(Purple);
                text.Span(" / ").FontSize(7).FontColor(Muted);
                text.TotalPages().FontSize(7).FontColor(Muted);
            });
        });
    }

    private static void Page1(ColumnDescriptor col, PassportPdfModel model, byte[]? logo, string lang)
    {
        Header(col, model, logo, lang, page2: false);
        Hero(col, model, lang);
        QuickCards(col, model, lang);
        LanguagesAndPrefs(col, model, lang);
        Strengths(col, model, lang);
        Sought(col, model, lang);
        ExperienceTeaser(col, model, lang);
    }

    private static void Page2(ColumnDescriptor col, PassportPdfModel model, byte[]? logo, string lang)
    {
        Header(col, model, logo, lang, page2: true);
        ExperienceFull(col, model, lang);
        Dna(col, model, lang);
        HowIWork(col, model, lang);
        OwnWords(col, model, lang);
        Meaning(col, model, lang);
    }

    private static void Header(ColumnDescriptor col, PassportPdfModel model, byte[]? logo, string lang, bool page2)
    {
        col.Item().Row(row =>
        {
            if (logo is { Length: > 8 })
            {
                row.ConstantItem(28).Height(28).Image(logo).FitArea();
                row.ConstantItem(8);
            }

            row.RelativeItem().AlignMiddle().Column(title =>
            {
                title.Item().Text("Lobsy").FontSize(page2 ? 12 : 16).Bold().FontColor(Purple);
                title.Item().Text(page2
                        ? model.FullName + " · " + PassportPdfStrings.T(lang, "Title")
                        : PassportPdfStrings.T(lang, "Title") + " · " + PassportPdfStrings.T(lang, "Page1"))
                    .FontSize(8).FontColor(Magenta);
            });

            row.ConstantItem(168).AlignMiddle().AlignRight().Column(badge =>
            {
                if (model.ShowBadge)
                {
                    badge.Item().AlignRight().Background(Purple).PaddingHorizontal(8).PaddingVertical(3)
                        .Text(PassportPdfStrings.T(lang, "Badge")).FontSize(8).Bold().FontColor(White);
                    if (!string.IsNullOrWhiteSpace(model.BadgeDetail))
                    {
                        badge.Item().PaddingTop(2).AlignRight().Text(model.BadgeDetail).FontSize(7).FontColor(Muted);
                    }
                }
                else
                {
                    badge.Item().AlignRight().Text(model.TestsPlainStatus).FontSize(8).FontColor(Muted);
                }
            });
        });

        col.Item().Row(bar =>
        {
            bar.RelativeItem(3).Height(3).Background(Purple);
            bar.RelativeItem(2).Height(3).Background(Magenta);
            bar.RelativeItem(2).Height(3).Background(Coral);
        });
    }

    private static void Hero(ColumnDescriptor col, PassportPdfModel model, string lang)
    {
        col.Item().Row(row =>
        {
            row.ConstantItem(36).Height(36).Background(Purple).AlignCenter().AlignMiddle()
                .Text(model.Initials).FontSize(11).Bold().FontColor(White);
            row.ConstantItem(8);
            row.RelativeItem().AlignMiddle().Column(name =>
            {
                name.Item().Text(model.FullName).FontSize(18).Bold().FontColor(Ink);
                if (!string.IsNullOrWhiteSpace(model.Tagline))
                {
                    name.Item().Text(model.Tagline).FontSize(9).FontColor(Magenta);
                }
            });
        });

        col.Item().Row(row =>
        {
            var any = false;
            if (!string.IsNullOrWhiteSpace(model.Region))
            {
                Pill(row, model.Region, SoftPurple);
                any = true;
            }

            if (model.OpenForWork)
            {
                if (any)
                {
                    row.ConstantItem(4);
                }

                Pill(row, PassportPdfStrings.T(lang, "OpenForWork"), SoftGreen);
                any = true;
            }

            if (!string.IsNullOrWhiteSpace(model.PassportNumber))
            {
                if (any)
                {
                    row.ConstantItem(4);
                }

                Pill(row, PassportPdfStrings.F(lang, "PassportNo", model.PassportNumber), SoftPeach);
            }
        });
    }

    private static void Pill(RowDescriptor row, string text, Color background)
    {
        row.AutoItem().Background(background).PaddingHorizontal(6).PaddingVertical(2)
            .Text(text).FontSize(8).FontColor(Ink);
    }

    private static void QuickCards(ColumnDescriptor col, PassportPdfModel model, string lang)
    {
        var cards = new List<(string Title, string Body, string? Note, Color Bg)>();
        if (!string.IsNullOrWhiteSpace(model.AvailableFrom) || !string.IsNullOrWhiteSpace(model.Hours))
        {
            var body = string.Join("\n", new[] { model.AvailableFrom, model.Hours }.Where(x => !string.IsNullOrWhiteSpace(x)));
            cards.Add((PassportPdfStrings.T(lang, "Available"), body, null, SoftPurple));
        }

        if (model.Shifts.Count > 0)
        {
            var body = string.Join("  ", model.Shifts.Select(s => s.Label + " " + s.Status));
            cards.Add((PassportPdfStrings.T(lang, "Shifts"), body, model.ShiftNote, SoftPeach));
        }

        if (!string.IsNullOrWhiteSpace(model.TransportLine)
            || !string.IsNullOrWhiteSpace(model.TravelLine)
            || !string.IsNullOrWhiteSpace(model.OwnCarLine))
        {
            var body = string.Join("\n", new[] { model.TransportLine, model.TravelLine, model.OwnCarLine }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
            cards.Add((PassportPdfStrings.T(lang, "Transport"), body, null, SoftPurple));
        }

        if (!string.IsNullOrWhiteSpace(model.Email) || !string.IsNullOrWhiteSpace(model.Phone))
        {
            var bits = new List<string>();
            if (!string.IsNullOrWhiteSpace(model.Phone))
            {
                bits.Add(model.Phone);
            }

            if (model.WhatsApp)
            {
                bits.Add(PassportPdfStrings.T(lang, "WhatsApp"));
            }

            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                bits.Add(model.Email);
            }

            cards.Add((PassportPdfStrings.T(lang, "Contact"), string.Join("\n", bits), null, SoftPeach));
        }

        if (cards.Count == 0)
        {
            return;
        }

        col.Item().Row(row =>
        {
            for (var i = 0; i < cards.Count; i++)
            {
                if (i > 0)
                {
                    row.ConstantItem(6);
                }

                var card = cards[i];
                row.RelativeItem().Background(card.Bg).Padding(6).Column(inner =>
                {
                    inner.Item().Text(card.Title).FontSize(7).FontColor(Muted);
                    inner.Item().PaddingTop(2).Text(card.Body).FontSize(8).FontColor(Ink);
                    if (!string.IsNullOrWhiteSpace(card.Note))
                    {
                        inner.Item().PaddingTop(2).Text(card.Note).FontSize(7).FontColor(Coral);
                    }
                });
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
                row.RelativeItem().Border(1).BorderColor(Line).Padding(6).Column(inner =>
                {
                    inner.Item().Text(PassportPdfStrings.T(lang, "Languages")).FontSize(10).Bold().FontColor(Purple);
                    foreach (var line in model.Languages)
                    {
                        inner.Item().PaddingTop(2).Row(pair =>
                        {
                            pair.RelativeItem().Text(line.Label).FontSize(8);
                            pair.RelativeItem().AlignRight().Text(line.Value).FontSize(8).FontColor(Magenta);
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(model.DutchNote))
                    {
                        inner.Item().PaddingTop(3).Text(model.DutchNote).FontSize(7).FontColor(Muted);
                    }
                });
            }

            if (hasLang && hasPrefs)
            {
                row.ConstantItem(6);
            }

            if (hasPrefs)
            {
                row.RelativeItem().Border(1).BorderColor(Line).Padding(6).Column(inner =>
                {
                    inner.Item().Text(PassportPdfStrings.T(lang, "WorkPrefs")).FontSize(10).Bold().FontColor(Purple);
                    foreach (var line in model.WorkPreferences)
                    {
                        inner.Item().PaddingTop(2).Row(pair =>
                        {
                            pair.RelativeItem().Text(line.Label).FontSize(8);
                            pair.RelativeItem().AlignRight().Text(line.Value).FontSize(8).FontColor(Ink);
                        });
                    }
                });
            }
        });
    }

    private static void Strengths(ColumnDescriptor col, PassportPdfModel model, string lang)
    {
        if (model.Strengths.Count == 0)
        {
            return;
        }

        col.Item().Column(inner =>
        {
            inner.Item().Text(text =>
            {
                text.Span(PassportPdfStrings.T(lang, "Strengths")).FontSize(10).Bold().FontColor(Purple);
                if (!string.IsNullOrWhiteSpace(model.StrengthSource))
                {
                    text.Span("   " + model.StrengthSource).FontSize(7).FontColor(Muted);
                }
            });
            inner.Item().PaddingTop(3).Text(string.Join("   ·   ", model.Strengths)).FontSize(9);
        });
    }

    private static void Sought(ColumnDescriptor col, PassportPdfModel model, string lang)
    {
        if (string.IsNullOrWhiteSpace(model.SoughtLine))
        {
            return;
        }

        col.Item().Text(text =>
        {
            text.Span(PassportPdfStrings.T(lang, "Seeks") + "  ").FontSize(8).Bold().FontColor(Purple);
            text.Span(model.SoughtLine).FontSize(8);
        });
    }

    private static void ExperienceTeaser(ColumnDescriptor col, PassportPdfModel model, string lang)
    {
        var jobs = model.Experience.Take(2).ToList();
        var papers = model.Certificates.Take(3).ToList();
        if (jobs.Count == 0 && papers.Count == 0 && model.Educations.Count == 0)
        {
            return;
        }

        col.Item().Row(row =>
        {
            if (jobs.Count > 0)
            {
                row.RelativeItem().Column(inner =>
                {
                    inner.Item().Text(PassportPdfStrings.T(lang, "Experience")).FontSize(10).Bold().FontColor(Purple);
                    foreach (var job in jobs)
                    {
                        inner.Item().PaddingTop(2).Text(job.Title).FontSize(8).Bold();
                        if (!string.IsNullOrWhiteSpace(job.Meta))
                        {
                            inner.Item().Text(job.Meta).FontSize(7).FontColor(Muted);
                        }
                    }
                });
            }

            if ((papers.Count > 0 || model.Educations.Count > 0) && jobs.Count > 0)
            {
                row.ConstantItem(8);
            }

            if (papers.Count > 0 || model.Educations.Count > 0)
            {
                row.RelativeItem().Column(inner =>
                {
                    inner.Item().Text(PassportPdfStrings.T(lang, "Papers")).FontSize(10).Bold().FontColor(Purple);
                    foreach (var edu in model.Educations.Take(2))
                    {
                        inner.Item().PaddingTop(2).Text(edu).FontSize(8);
                    }

                    foreach (var paper in papers)
                    {
                        inner.Item().PaddingTop(2).Text(paper).FontSize(8);
                    }
                });
            }
        });
    }

    private static void ExperienceFull(ColumnDescriptor col, PassportPdfModel model, string lang)
    {
        if (model.Experience.Count == 0 && model.Certificates.Count == 0 && model.Educations.Count == 0)
        {
            return;
        }

        col.Item().Text(PassportPdfStrings.T(lang, "Experience")).FontSize(11).Bold().FontColor(Purple);
        foreach (var job in model.Experience)
        {
            col.Item().Text(text =>
            {
                text.Span(job.Title).FontSize(9).Bold();
                if (!string.IsNullOrWhiteSpace(job.Meta))
                {
                    text.Span("  " + job.Meta).FontSize(8).FontColor(Muted);
                }
            });
            if (!string.IsNullOrWhiteSpace(job.Detail))
            {
                col.Item().Text(job.Detail).FontSize(8).FontColor(Ink);
            }
        }

        if (model.ExperienceMore > 0)
        {
            col.Item().Text(PassportPdfStrings.F(lang, "More", model.ExperienceMore)).FontSize(7).FontColor(Muted);
        }

        if (model.Educations.Count > 0)
        {
            col.Item().PaddingTop(2).Text(PassportPdfStrings.T(lang, "Education")).FontSize(10).Bold().FontColor(Purple);
            foreach (var edu in model.Educations)
            {
                col.Item().Text(edu).FontSize(8);
            }
        }

        if (model.Certificates.Count > 0)
        {
            col.Item().PaddingTop(2).Text(PassportPdfStrings.T(lang, "Certificates")).FontSize(10).Bold().FontColor(Purple);
            foreach (var paper in model.Certificates)
            {
                col.Item().Text(paper).FontSize(8);
            }

            if (model.CertificatesMore > 0)
            {
                col.Item().Text(PassportPdfStrings.F(lang, "More", model.CertificatesMore)).FontSize(7).FontColor(Muted);
            }
        }
    }

    private static void Dna(ColumnDescriptor col, PassportPdfModel model, string lang)
    {
        if (model.Dna.Count == 0)
        {
            return;
        }

        col.Item().Text(PassportPdfStrings.T(lang, "DnaTitle")).FontSize(11).Bold().FontColor(Purple);
        col.Item().Row(row =>
        {
            for (var i = 0; i < model.Dna.Count; i++)
            {
                if (i > 0)
                {
                    row.ConstantItem(4);
                }

                var card = model.Dna[i];
                row.RelativeItem().Background(i % 2 == 0 ? SoftPurple : SoftPeach).Padding(5).Column(inner =>
                {
                    inner.Item().Text(card.Title).FontSize(7).FontColor(Muted);
                    inner.Item().PaddingTop(2).Text(card.Body).FontSize(8).Bold().FontColor(Ink);
                    if (!string.IsNullOrWhiteSpace(card.When))
                    {
                        inner.Item().PaddingTop(2).Text(card.When).FontSize(7).FontColor(Muted);
                    }
                });
            }
        });
    }

    private static void HowIWork(ColumnDescriptor col, PassportPdfModel model, string lang)
    {
        if (model.HowIWork.Count == 0)
        {
            return;
        }

        col.Item().Text(PassportPdfStrings.T(lang, "HowIWork")).FontSize(11).Bold().FontColor(Purple);
        foreach (var line in model.HowIWork)
        {
            col.Item().Text("·  " + line).FontSize(8);
        }
    }

    private static void OwnWords(ColumnDescriptor col, PassportPdfModel model, string lang)
    {
        if (string.IsNullOrWhiteSpace(model.OwnWords) && string.IsNullOrWhiteSpace(model.Motivation))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(model.OwnWords))
        {
            col.Item().Background(SoftPurple).Padding(8).Column(inner =>
            {
                inner.Item().Text(PassportPdfStrings.T(lang, "OwnWords")).FontSize(10).Bold().FontColor(Purple);
                inner.Item().PaddingTop(2).Text(model.OwnWords).FontSize(9).Italic();
            });
        }

        if (!string.IsNullOrWhiteSpace(model.Motivation))
        {
            col.Item().Column(inner =>
            {
                inner.Item().Text(PassportPdfStrings.T(lang, "Motivation")).FontSize(10).Bold().FontColor(Purple);
                inner.Item().Text(model.Motivation).FontSize(8);
            });
        }
    }

    private static void Meaning(ColumnDescriptor col, PassportPdfModel model, string lang)
    {
        col.Item().Border(1).BorderColor(Line).Padding(8).Column(inner =>
        {
            inner.Item().Text(PassportPdfStrings.T(lang, "Means")).FontSize(10).Bold().FontColor(Purple);
            inner.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Column(yes =>
                {
                    yes.Item().Text(PassportPdfStrings.T(lang, "Checked")).FontSize(8).Bold().FontColor(Color.FromHex("#1a7a4c"));
                    if (model.Checked.Count == 0)
                    {
                        yes.Item().Text("—").FontSize(8).FontColor(Muted);
                    }

                    foreach (var line in model.Checked)
                    {
                        yes.Item().Text("·  " + line).FontSize(7.5f);
                    }
                });
                row.ConstantItem(8);
                row.RelativeItem().Column(no =>
                {
                    no.Item().Text(PassportPdfStrings.T(lang, "NotChecked")).FontSize(8).Bold().FontColor(Coral);
                    foreach (var line in model.NotChecked)
                    {
                        no.Item().Text("·  " + line).FontSize(7.5f);
                    }
                });
            });
        });
    }

}
