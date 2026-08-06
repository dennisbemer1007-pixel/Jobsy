using System.Globalization;
using System.Text;
using Jobsy.Core.Contracts;
using Jobsy.Core.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Jobsy.Infrastructure.Services;

public sealed class LobsyCvPdfService : ILobsyCvPdfService
{
    private static readonly Color BrandNavy = Color.FromHex("#0f2d5c");
    private static readonly Color BrandDeep = Color.FromHex("#0a2044");
    private static readonly Color SoftSky = Color.FromHex("#dceef8");
    private static readonly Color SoftMint = Color.FromHex("#e8f5ef");
    private static readonly Color WarmSand = Color.FromHex("#f7f1e6");
    private static readonly Color AccentTeal = Color.FromHex("#1a7a6d");
    private static readonly Color Slate = Color.FromHex("#2c3a4a");
    private static readonly Color Muted = Color.FromHex("#5a6a7a");

    private readonly IPlatformCompanySettingsService _companySettings;

    static LobsyCvPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public LobsyCvPdfService(IPlatformCompanySettingsService companySettings)
    {
        _companySettings = companySettings;
    }

    public string BuildFileName(LobsyCvModel model)
    {
        var initials = Initials(model.FullName);
        var date = model.GeneratedAtUtc.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        return $"Lobsy-CV-{initials}-{date}.pdf";
    }

    public async Task<byte[]> RenderAsync(LobsyCvModel model, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        var platform = await _companySettings.GetAsync(cancellationToken);
        var brand = string.IsNullOrWhiteSpace(platform.CompanyName) ? "Lobsy" : platform.CompanyName.Trim();
        var logo = _companySettings.GetBrandLogoPng();
        var culture = CultureInfo.GetCultureInfo("nl-NL");
        var generatedLocal = DateTime.SpecifyKind(model.GeneratedAtUtc, DateTimeKind.Utc);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(10.5f).FontColor(Slate));

                page.Header().Column(header =>
                {
                    header.Item().Background(SoftSky).Padding(16).Row(row =>
                    {
                        if (logo is { Length: > 0 })
                        {
                            row.ConstantItem(44).Height(28).Image(logo).FitArea();
                            row.ConstantItem(10);
                        }

                        row.RelativeItem().AlignMiddle().Column(title =>
                        {
                            title.Item().Text(brand).FontSize(20).Bold().FontColor(BrandNavy);
                            title.Item().Text("Lobsy-CV").FontSize(12).FontColor(AccentTeal);
                        });

                        row.ConstantItem(120).AlignMiddle().AlignRight().Column(meta =>
                        {
                            meta.Item().Text("Automatisch gegenereerd")
                                .FontSize(8).FontColor(Muted);
                            meta.Item().Text(generatedLocal.ToString("d MMM yyyy", culture))
                                .FontSize(9).FontColor(BrandDeep);
                        });
                    });
                    header.Item().Height(4).Background(AccentTeal);
                });

                page.Content().PaddingTop(18).Column(body =>
                {
                    body.Spacing(14);

                    body.Item().Column(person =>
                    {
                        person.Item().Text(string.IsNullOrWhiteSpace(model.FullName) ? "Kandidaat" : model.FullName)
                            .FontSize(22).Bold().FontColor(BrandNavy);
                        if (model.IncludeContactEmail && !string.IsNullOrWhiteSpace(model.Email))
                        {
                            person.Item().Text(model.Email!).FontSize(10).FontColor(Muted);
                        }

                        var location = BuildLocationLine(model);
                        if (!string.IsNullOrWhiteSpace(location))
                        {
                            person.Item().Text(location).FontSize(10).FontColor(Slate);
                        }
                    });

                    if (!string.IsNullOrWhiteSpace(model.VacancyTitle) || !string.IsNullOrWhiteSpace(model.CompanyName))
                    {
                        body.Item().Background(WarmSand).Padding(12).Column(ctx =>
                        {
                            ctx.Spacing(2);
                            ctx.Item().Text("Sollicitatie").FontSize(9).Bold().FontColor(AccentTeal);
                            if (!string.IsNullOrWhiteSpace(model.VacancyTitle))
                            {
                                ctx.Item().Text(model.VacancyTitle!).FontSize(12).Bold().FontColor(BrandNavy);
                            }

                            if (!string.IsNullOrWhiteSpace(model.CompanyName))
                            {
                                ctx.Item().Text(model.CompanyName!).FontSize(10).FontColor(Slate);
                            }

                            if (model.MatchPercent is int pct)
                            {
                                ctx.Item().Text($"Match: {pct}%").FontSize(10).FontColor(BrandDeep);
                            }
                        });
                    }

                    Section(body, "Over mij", model.AboutMe);
                    Section(body, "Motivatie", model.Motivation);

                    body.Item().Column(avail =>
                    {
                        avail.Spacing(4);
                        avail.Item().Text("Beschikbaarheid").FontSize(13).Bold().FontColor(BrandNavy);
                        avail.Item().Background(SoftMint).Padding(10).Column(box =>
                        {
                            box.Spacing(3);
                            var hours = FormatHours(model.MinHoursPerWeek, model.MaxHoursPerWeek);
                            if (!string.IsNullOrWhiteSpace(hours))
                            {
                                box.Item().Text($"Uren per week: {hours}");
                            }

                            if (model.FlexibleTimes)
                            {
                                box.Item().Text("Tijden in overleg");
                            }
                            else if (!string.IsNullOrWhiteSpace(model.AvailabilitySummary))
                            {
                                box.Item().Text(model.AvailabilitySummary!);
                            }
                            else
                            {
                                box.Item().Text("Nog niet ingevuld").FontColor(Muted);
                            }
                        });
                    });

                    body.Item().Column(travel =>
                    {
                        travel.Spacing(4);
                        travel.Item().Text("Vervoer & reistijd").FontSize(13).Bold().FontColor(BrandNavy);
                        travel.Item().Column(lines =>
                        {
                            lines.Spacing(2);
                            if (!string.IsNullOrWhiteSpace(model.PreferredTransport))
                            {
                                lines.Item().Text($"Voorkeur: {model.PreferredTransport}");
                            }

                            if (model.EstimatedTravelMinutes is int est)
                            {
                                lines.Item().Text($"Geschatte reistijd naar vacature: {est} minuten");
                            }
                            else if (model.MaxTravelMinutes is int max)
                            {
                                lines.Item().Text($"Maximale reistijd: {max} minuten");
                            }
                        });
                    });

                    if (model.DrivingLicenses.Count > 0)
                    {
                        Section(body, "Rijbewijs", string.Join(", ", model.DrivingLicenses));
                    }

                    if (model.Educations.Count > 0)
                    {
                        Section(body, "Opleiding", string.Join(", ", model.Educations));
                    }

                    if (model.Employers.Count > 0)
                    {
                        body.Item().Column(exp =>
                        {
                            exp.Spacing(6);
                            exp.Item().Text("Werkervaring").FontSize(13).Bold().FontColor(BrandNavy);
                            foreach (var employer in model.Employers)
                            {
                                exp.Item().PaddingBottom(4).Column(card =>
                                {
                                    var title = employer.EmployerName;
                                    if (!string.IsNullOrWhiteSpace(employer.Role))
                                    {
                                        title += $" — {employer.Role}";
                                    }

                                    card.Item().Text(title).Bold();
                                    if (employer.Years is int years)
                                    {
                                        card.Item().Text($"{years} jaar").FontSize(9).FontColor(Muted);
                                    }

                                    if (!string.IsNullOrWhiteSpace(employer.Description))
                                    {
                                        card.Item().Text(employer.Description!).FontSize(10);
                                    }
                                });
                            }
                        });
                    }
                });

                page.Footer().AlignCenter().Column(footer =>
                {
                    footer.Item().PaddingTop(8).LineHorizontal(0.5f).LineColor(SoftSky);
                    footer.Item().PaddingTop(6).Text(text =>
                    {
                        text.Span("Gegenereerd door Lobsy · niet door de kandidaat geüpload")
                            .FontSize(8).FontColor(Muted);
                        if (!string.IsNullOrWhiteSpace(model.ConsentVersion))
                        {
                            text.Span($" · consent {model.ConsentVersion}")
                                .FontSize(8).FontColor(Muted);
                        }
                    });
                });
            });
        }).GeneratePdf();
    }

    private static void Section(ColumnDescriptor body, string title, string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        body.Item().Column(col =>
        {
            col.Spacing(4);
            col.Item().Text(title).FontSize(13).Bold().FontColor(BrandNavy);
            col.Item().Text(content.Trim());
        });
    }

    private static string? BuildLocationLine(LobsyCvModel model)
    {
        if (model.IncludeFullAddress && !string.IsNullOrWhiteSpace(model.Address))
        {
            return model.Address.Trim();
        }

        if (!string.IsNullOrWhiteSpace(model.City))
        {
            return model.City.Trim();
        }

        if (!string.IsNullOrWhiteSpace(model.Address))
        {
            // Live preview: show city-ish last segment when full address not released to third parties.
            var parts = model.Address.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[^1] : model.Address.Trim();
        }

        return null;
    }

    private static string? FormatHours(decimal? min, decimal? max)
    {
        if (min is null && max is null)
        {
            return null;
        }

        if (min is not null && max is not null)
        {
            return $"{min:0.#}–{max:0.#}";
        }

        return (min ?? max)!.Value.ToString("0.#", CultureInfo.GetCultureInfo("nl-NL"));
    }

    private static string Initials(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return "XX";
        }

        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var sb = new StringBuilder();
        foreach (var part in parts.Take(3))
        {
            sb.Append(char.ToUpperInvariant(part[0]));
        }

        return sb.Length == 0 ? "XX" : sb.ToString();
    }
}
