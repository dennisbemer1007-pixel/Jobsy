using System.Globalization;
using System.Text;
using Jobsy.Core.Contracts;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Jobsy.Infrastructure.Services;

public sealed class LobsyCvPdfService : ILobsyCvPdfService
{
    private static readonly Color BrandNavy = Color.FromHex("#0f2d5c");
    private static readonly Color BrandDeep = Color.FromHex("#0a2044");
    private static readonly Color SoftSky = Color.FromHex("#e8f3fa");
    private static readonly Color SoftMint = Color.FromHex("#e7f6ef");
    private static readonly Color WarmSand = Color.FromHex("#f6f0e7");
    private static readonly Color AccentTeal = Color.FromHex("#1a7a6d");
    private static readonly Color AccentCoral = Color.FromHex("#c45c3e");
    private static readonly Color SoftCoral = Color.FromHex("#f8ebe6");
    private static readonly Color Slate = Color.FromHex("#2c3a4a");
    private static readonly Color Muted = Color.FromHex("#5a6a7a");
    private static readonly Color Line = Color.FromHex("#d5e3ec");
    private static readonly Color CellOn = Color.FromHex("#1a7a6d");
    private static readonly Color CellOff = Color.FromHex("#f2f6f8");

    private readonly IPlatformCompanySettingsService _companySettings;
    private readonly ICandidateMapImageService _mapImages;

    static LobsyCvPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public LobsyCvPdfService(
        IPlatformCompanySettingsService companySettings,
        ICandidateMapImageService mapImages)
    {
        _companySettings = companySettings;
        _mapImages = mapImages;
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

        byte[]? mapPng = null;
        if (model.Latitude is double lat && model.Longitude is double lng)
        {
            mapPng = await _mapImages.RenderAsync(
                lat,
                lng,
                width: 360,
                height: 160,
                zoom: 15,
                markerLogoPng: logo,
                cancellationToken: cancellationToken);
        }

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(28);
                page.MarginVertical(24);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Slate));

                page.Header().Column(header =>
                {
                    header.Item().Background(SoftSky).Padding(14).Row(row =>
                    {
                        if (logo is { Length: > 0 })
                        {
                            row.ConstantItem(40).Height(26).Image(logo).FitArea();
                            row.ConstantItem(8);
                        }

                        row.RelativeItem().AlignMiddle().Column(title =>
                        {
                            title.Item().Text(brand).FontSize(18).Bold().FontColor(BrandNavy);
                            title.Item().Text("Persoonlijk Lobsy-visitekaartje")
                                .FontSize(9).FontColor(AccentTeal);
                        });

                        row.ConstantItem(118).AlignMiddle().AlignRight().Background(Colors.White)
                            .PaddingHorizontal(8).PaddingVertical(6).Column(meta =>
                            {
                                meta.Item().Text("Lobsy-CV").FontSize(9).Bold().FontColor(AccentCoral);
                                meta.Item().Text(generatedLocal.ToString("d MMM yyyy", culture))
                                    .FontSize(8).FontColor(Muted);
                            });
                    });
                    header.Item().Height(3).Background(AccentCoral);
                });

                page.Content().PaddingTop(14).Column(body =>
                {
                    body.Spacing(12);

                    body.Item().Row(hero =>
                    {
                        hero.RelativeItem().Column(person =>
                        {
                            person.Item().Text("Hallo, ik ben")
                                .FontSize(9).FontColor(AccentCoral);
                            person.Item().Text(string.IsNullOrWhiteSpace(model.FullName) ? "Kandidaat" : model.FullName)
                                .FontSize(24).Bold().FontColor(BrandNavy);
                            var ageLine = FormatDateOfBirthAndAge(
                                model.IncludeContactDetails ? model.DateOfBirth : null,
                                model.AgeYears,
                                culture);
                            if (!string.IsNullOrWhiteSpace(ageLine))
                            {
                                person.Item().PaddingTop(2).Text(ageLine)
                                    .FontSize(10).FontColor(Slate);
                            }
                            if (!string.IsNullOrWhiteSpace(model.City))
                            {
                                person.Item().Text($"Klaar voor werk dichtbij {model.City}")
                                    .FontSize(10).FontColor(BrandDeep);
                            }
                            else
                            {
                                person.Item().Text("Klaar voor werk dichterbij dan je denkt")
                                    .FontSize(10).FontColor(BrandDeep);
                            }
                        });

                        if (!string.IsNullOrWhiteSpace(model.VacancyTitle) || model.MatchPercent is not null)
                        {
                            hero.ConstantItem(150).Background(WarmSand).Padding(10).Column(ctx =>
                            {
                                ctx.Spacing(2);
                                ctx.Item().Text("Voor deze rol").FontSize(8).FontColor(AccentTeal);
                                if (!string.IsNullOrWhiteSpace(model.VacancyTitle))
                                {
                                    ctx.Item().Text(model.VacancyTitle!).FontSize(11).Bold().FontColor(BrandNavy);
                                }

                                if (!string.IsNullOrWhiteSpace(model.CompanyName))
                                {
                                    ctx.Item().Text(model.CompanyName!).FontSize(9).FontColor(Slate);
                                }

                                if (model.MatchPercent is int pct)
                                {
                                    ctx.Item().PaddingTop(4).Text($"{pct}% match")
                                        .FontSize(12).Bold().FontColor(AccentTeal);
                                }
                            });
                        }
                    });

                    if (model.IncludeContactDetails)
                    {
                        body.Item().Background(SoftMint).Padding(12).Column(contact =>
                        {
                            contact.Spacing(4);
                            contact.Item().Text("Contactgegevens").FontSize(12).Bold().FontColor(BrandNavy);
                            contact.Item().Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("E-mail").FontSize(8).FontColor(Muted);
                                    col.Item().Text(string.IsNullOrWhiteSpace(model.Email) ? "—" : model.Email!)
                                        .FontSize(10).Bold();
                                });
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("Telefoon").FontSize(8).FontColor(Muted);
                                    col.Item().Text(string.IsNullOrWhiteSpace(model.PhoneNumber) ? "—" : model.PhoneNumber!)
                                        .FontSize(10).Bold();
                                });
                            });
                            if (model.DateOfBirth is DateOnly dob || model.AgeYears is int)
                            {
                                contact.Item().PaddingTop(4).Row(row =>
                                {
                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text("Geboortedatum").FontSize(8).FontColor(Muted);
                                        col.Item().Text(model.DateOfBirth is DateOnly d
                                                ? d.ToString("d MMMM yyyy", culture)
                                                : "—")
                                            .FontSize(10).Bold();
                                    });
                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text("Leeftijd").FontSize(8).FontColor(Muted);
                                        col.Item().Text(model.AgeYears is int age
                                                ? $"{age} jaar"
                                                : "—")
                                            .FontSize(10).Bold();
                                    });
                                });
                            }
                            if (model.WhatsAppContactAllowed && !string.IsNullOrWhiteSpace(model.PhoneNumber))
                            {
                                contact.Item().PaddingTop(2)
                                    .Text("WhatsApp mag: bel of app gerust voor een snelle kennismaking.")
                                    .FontSize(9).FontColor(AccentTeal);
                            }
                        });
                    }

                    Section(body, "Over mij", model.AboutMe);
                    Section(body, "Motivatie", model.Motivation);

                    body.Item().Row(split =>
                    {
                        split.RelativeItem().Background(SoftSky).Padding(8).Column(avail =>
                        {
                            avail.Spacing(4);
                            avail.Item().Text("Beschikbaarheid").FontSize(9).Bold().FontColor(BrandNavy);

                            var hours = FormatHours(model.MinHoursPerWeek, model.MaxHoursPerWeek);
                            if (!string.IsNullOrWhiteSpace(hours))
                            {
                                avail.Item().Text($"{hours} u/week").FontSize(8).FontColor(Muted);
                            }

                            if (model.FlexibleTimes)
                            {
                                avail.Item().Background(WarmSand).Padding(6)
                                    .Text("Tijden in overleg")
                                    .FontSize(8).FontColor(BrandDeep);
                            }
                            else
                            {
                                DrawAvailabilityMatrix(avail, model.AvailabilitySlots);
                            }
                        });

                        split.ConstantItem(8);

                        split.RelativeItem().Background(SoftCoral).Padding(8).Column(loc =>
                        {
                            loc.Spacing(4);
                            loc.Item().Text("Locatie").FontSize(9).Bold().FontColor(BrandNavy);

                            // Map only when coordinates are present (candidate opted in via ShowAddressOnCv).
                            if (mapPng is { Length: > 0 })
                            {
                                loc.Item().Height(92).Image(mapPng).FitArea();
                            }
                            else if (!string.IsNullOrWhiteSpace(model.City))
                            {
                                loc.Item().Height(48).Background(Colors.White).AlignMiddle().AlignCenter()
                                    .Text(model.City!)
                                    .FontSize(9).FontColor(BrandDeep);
                            }
                            else
                            {
                                loc.Item().Height(48).Background(Colors.White).AlignMiddle().AlignCenter()
                                    .Text(model.IncludeContactDetails
                                        ? "Niet op CV"
                                        : "Adres na acceptatie")
                                    .FontSize(8).FontColor(Muted);
                            }

                            if (model.IncludeFullAddress && !string.IsNullOrWhiteSpace(model.Address))
                            {
                                loc.Item().Text(model.Address!).FontSize(8).Bold().FontColor(BrandNavy);
                            }
                            else if (!string.IsNullOrWhiteSpace(model.City))
                            {
                                loc.Item().Text(model.City!).FontSize(8).Bold().FontColor(BrandNavy);
                            }
                        });
                    });

                    body.Item().Row(travel =>
                    {
                        travel.RelativeItem().Background(SoftSky).Padding(10).Column(col =>
                        {
                            col.Item().Text("Vervoer").FontSize(8).FontColor(Muted);
                            col.Item().Text(string.IsNullOrWhiteSpace(model.PreferredTransport) ? "—" : model.PreferredTransport!)
                                .FontSize(11).Bold().FontColor(BrandNavy);
                        });
                        travel.ConstantItem(8);
                        travel.RelativeItem().Background(SoftCoral).Padding(10).Column(col =>
                        {
                            col.Item().Text("Reistijd").FontSize(8).FontColor(Muted);
                            if (model.EstimatedTravelMinutes is int est)
                            {
                                col.Item().Text($"{est} min naar vacature").FontSize(11).Bold().FontColor(BrandNavy);
                            }
                            else if (model.MaxTravelMinutes is int max)
                            {
                                col.Item().Text($"Max. {max} min").FontSize(11).Bold().FontColor(BrandNavy);
                            }
                            else
                            {
                                col.Item().Text("—").FontSize(11).Bold().FontColor(BrandNavy);
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
                            exp.Spacing(3);
                            exp.Item().Text("Werkervaring").FontSize(11).Bold().FontColor(BrandNavy);
                            foreach (var employer in model.Employers)
                            {
                                exp.Item().Background(SoftSky).Padding(7).Column(card =>
                                {
                                    card.Spacing(1);
                                    card.Item().Row(row =>
                                    {
                                        var title = employer.EmployerName;
                                        if (!string.IsNullOrWhiteSpace(employer.Role))
                                        {
                                            title += $" — {employer.Role}";
                                        }

                                        row.RelativeItem().Text(title).FontSize(9).Bold().FontColor(BrandNavy);
                                        var period = LobsyCvModelFactory.FormatEmployerPeriod(
                                            employer.StartMonth, employer.EndMonth, employer.Years);
                                        if (!string.IsNullOrWhiteSpace(period))
                                        {
                                            row.ConstantItem(88).AlignRight()
                                                .Text(period).FontSize(8).FontColor(Muted);
                                        }
                                    });
                                    if (!string.IsNullOrWhiteSpace(employer.Description))
                                    {
                                        card.Item().Text(employer.Description!).FontSize(8).FontColor(Slate);
                                    }
                                });
                            }
                        });
                    }

                    if (model.Certificates.Count > 0)
                    {
                        body.Item().Column(certs =>
                        {
                            certs.Spacing(3);
                            certs.Item().Text("Certificaten & cursussen").FontSize(11).Bold().FontColor(BrandNavy);
                            foreach (var cert in model.Certificates)
                            {
                                certs.Item().Row(row =>
                                {
                                    row.RelativeItem().Text(cert.Name).FontSize(9).FontColor(BrandNavy);
                                    if (cert.Year is int year)
                                    {
                                        row.ConstantItem(40).AlignRight()
                                            .Text(year.ToString(CultureInfo.InvariantCulture))
                                            .FontSize(9).FontColor(Muted);
                                    }
                                });
                            }
                        });
                    }
                });

                page.Footer().AlignCenter().Column(footer =>
                {
                    footer.Item().PaddingTop(6).LineHorizontal(0.5f).LineColor(Line);
                    footer.Item().PaddingTop(5).Text(text =>
                    {
                        text.Span("Gegenereerd door Lobsy · visitekaartje van de kandidaat, geen upload-CV")
                            .FontSize(7.5f).FontColor(Muted);
                        if (!string.IsNullOrWhiteSpace(model.ConsentVersion))
                        {
                            text.Span($" · consent {model.ConsentVersion}")
                                .FontSize(7.5f).FontColor(Muted);
                        }
                    });
                });
            });
        }).GeneratePdf();
    }

    private static void DrawAvailabilityMatrix(
        ColumnDescriptor avail,
        IReadOnlyDictionary<string, string[]>? slots)
    {
        var map = slots ?? new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        var hasAny = map.Values.Any(v => v is { Length: > 0 });
        if (!hasAny)
        {
            avail.Item().Text("Nog niet ingevuld").FontSize(8).FontColor(Muted);
            return;
        }

        avail.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(22);
                foreach (var _ in DayPartMatrix.DayPartCodes)
                {
                    columns.RelativeColumn();
                }
            });

            table.Header(header =>
            {
                header.Cell().Element(CellHeader).Text("").FontSize(6);
                foreach (var part in DayPartMatrix.DayPartCodes)
                {
                    // Compact: first letter only to keep the matrix small beside the map.
                    var shortLabel = part.Length > 0 ? part[..1] : part;
                    header.Cell().Element(CellHeader).AlignCenter()
                        .Text(shortLabel).FontSize(6).Bold();
                }
            });

            foreach (var day in DayPartMatrix.DayCodes)
            {
                table.Cell().Element(CellBody).AlignMiddle().Text(day).FontSize(6.5f).Bold();
                map.TryGetValue(day, out var selected);
                selected ??= [];
                foreach (var part in DayPartMatrix.DayPartCodes)
                {
                    var on = selected.Contains(part, StringComparer.OrdinalIgnoreCase);
                    table.Cell().Element(c => CellSlot(c, on))
                        .AlignCenter().AlignMiddle()
                        .Text(on ? "●" : "")
                        .FontSize(6.5f)
                        .FontColor(on ? Colors.White : Muted);
                }
            }
        });

        avail.Item().Text("O=ochtend · M=middag · A=avond · N=nacht")
            .FontSize(6.5f).FontColor(Muted);
    }

    private static IContainer CellHeader(IContainer container)
        => container.Background(Colors.White).Border(0.4f).BorderColor(Line).Padding(1.5f);

    private static IContainer CellBody(IContainer container)
        => container.Background(Colors.White).Border(0.4f).BorderColor(Line).Padding(1.5f);

    private static IContainer CellSlot(IContainer container, bool on)
        => container.Background(on ? CellOn : Colors.White).Border(0.4f).BorderColor(Line).PaddingVertical(2);

    private static void Section(ColumnDescriptor body, string title, string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        body.Item().Column(col =>
        {
            col.Spacing(3);
            col.Item().Text(title).FontSize(12).Bold().FontColor(BrandNavy);
            col.Item().Text(content.Trim());
        });
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

    private static string? FormatDateOfBirthAndAge(DateOnly? dateOfBirth, int? ageYears, CultureInfo culture)
    {
        if (dateOfBirth is DateOnly dob && ageYears is int age)
        {
            return $"{dob.ToString("d MMMM yyyy", culture)} · {age} jaar";
        }

        if (dateOfBirth is DateOnly onlyDob)
        {
            return onlyDob.ToString("d MMMM yyyy", culture);
        }

        return ageYears is int onlyAge ? $"{onlyAge} jaar" : null;
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
