using Jobsy.Core.Careers;
using Jobsy.Core.Reports;
using Jobsy.Core.Reports.Career;
using Jobsy.Core.Reports.Competence;
using Jobsy.Core.Reports.Culture;
using Jobsy.Core.Reports.Values;
using Jobsy.Core.Rules;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Compact (~4 A4) layout for paid personal deep-test PDFs. Same facts as the long layout,
/// drawn tighter with a bar chart. No new advice and no new scores.
/// </summary>
internal static class CompactDeepReportPdf
{
    private static readonly Color Purple = Color.FromHex("#5B2A9A");
    private static readonly Color Orange = Color.FromHex("#E36A2C");
    private static readonly Color Ink = Color.FromHex("#23173A");
    private static readonly Color Muted = Color.FromHex("#5C5168");
    private static readonly Color SoftPurple = Color.FromHex("#F4EFFB");
    private static readonly Color SoftCoral = Color.FromHex("#FFF1EC");
    private static readonly Color Track = Color.FromHex("#E4D8F2");
    private static readonly Color Card = Color.FromHex("#FFFBFA");
    private static readonly Color Line = Color.FromHex("#E7DCEC");

    internal static byte[] Career(
        string brand, byte[] logo, string fullName, string generated,
        CareerDeepReport report, string lang, string? uiLang, string? education = null)
    {
        var en = ReportLanguage.IsEnglish(lang);
        var title = DeepReportCatalog.Get("title.career", lang);
        var ordered = report.Domains.OrderByDescending(d => d.Score).ToList();
        var topLabels = ordered.Take(3).Select(d => DeepReportCatalog.RiasecLabel(d.Domain, lang)).ToList();
        var top = string.Join(", ", topLabels);
        var places = CareerDeepReportBuilder.TypicalPlaces(report, lang, education);
        var jobs = CareerDeepReportBuilder.ShownOccupations(report, education);
        var percentLang = string.IsNullOrWhiteSpace(uiLang) ? lang : uiLang;
        var summary = report.Summary.Resolve(lang);
        if (!string.IsNullOrWhiteSpace(top))
        {
            summary += en ? $" Strongest directions: {top}." : $" Sterkste richtingen: {top}.";
        }

        if (!string.IsNullOrWhiteSpace(places))
        {
            summary += en
                ? $" Jobs on the next pages: {places}."
                : $" Beroepen op de volgende pagina's: {places}.";
        }

        return Compose(brand, logo, fullName, generated, title, lang, col =>
        {
            Intro(col, title, summary);
            Chips(col, uiLang, topLabels);
            Chart(col, uiLang, en
                ? "Each line is how often that kind of work showed up in your 200 answers. A higher percent is a direction to explore, not a grade and not a promise of a job."
                : "Elke regel laat zien hoe vaak dat soort werk in je 200 antwoorden zat. Een hoger percentage is een richting om te verkennen, geen cijfer en geen belofte van een baan.",
                ordered.Select(d => (
                    DeepReportCatalog.RiasecLabel(d.Domain, lang),
                    d.Score,
                    d.NormMean is double n ? $"{d.Score}% (Ø {Math.Round(n)}%)" : $"{d.Score}%")));

            Heading(col, DeepReportCatalog.Get("holland.title", lang));
            col.Item().Text(DeepReportCatalog.Format("holland.body", lang, report.HollandCode, top, places)).FontSize(9);
            col.Item().Text(en
                ? "Read the letters as a direction. A higher score means that kind of work showed up more often in your answers."
                : "Lees de letters als een richting. Een hogere score betekent dat dat soort werk vaker in je antwoorden zat.")
                .FontSize(8).FontColor(Muted);

            Heading(col, CompactPdfCopy.WhatItMeans(uiLang));
            foreach (var domain in ordered)
            {
                var label = DeepReportCatalog.RiasecLabel(domain.Domain, lang);
                MeaningCard(col, $"{label}: {domain.Score}%", AssessmentReportPdfService.CareerScoreSense(domain.Domain, en));
            }

            Heading(col, en ? "Jobs that fit you" : "Beroepen die bij je passen");
            if (jobs.Count == 0)
            {
                col.Item().Text(en
                    ? "Your answers do not point to one job yet. Use the directions above as a start."
                    : "Je antwoorden wijzen nog niet naar één beroep. Gebruik de richtingen hierboven als start.")
                    .FontSize(9).FontColor(Muted);
            }
            else
            {
                foreach (var job in jobs)
                {
                    JobCard(col,
                        $"{job.Title(lang)} — {CareerCompassBuilder.FormatPercent(job.MatchPercent, percentLang)}%",
                        job.Reason(lang));
                }
            }

            ActionPlan(col, lang, report.ActionPlan);
            Strengths(col, lang, report.StrengthKeys, report.PitfallKeys, "riasec");

            col.Item().ShowEntire().Column(tail =>
            {
                tail.Spacing(4);
                Heading(tail, en ? "How to read your scores" : "Zo lees je je scores");
                tail.Item().Text(en
                    ? "A higher percent means that direction showed up more often in your answers. It is a starting point, not a grade."
                    : "Een hoger percentage betekent dat die richting vaker in je antwoorden zat. Het is een startpunt, geen cijfer.")
                    .FontSize(9).FontColor(Muted);

                Heading(tail, en ? "What you can do next" : "Wat je hiermee kunt doen");
                tail.Item().Text(en
                    ? "This week, talk to one person who does a job from your list. Ask what a normal Tuesday looks like, and which task they would skip if they could."
                    : "Praat deze week met één persoon die een beroep uit jouw lijst doet. Vraag hoe een gewone dinsdag eruitziet, en welke taak die persoon zou overslaan als dat mocht.")
                    .FontSize(9);
                tail.Item().Text(en
                    ? "Write down three tasks that gave you energy. Put them next to the jobs on the page before this one. Keep the job whose day looks most like those tasks."
                    : "Schrijf drie taken op die je energie gaven. Leg ze naast de beroepen op de vorige pagina. Houd het beroep waarvan de dag het meest op die taken lijkt.")
                    .FontSize(9);
                if (!string.IsNullOrWhiteSpace(places))
                {
                    tail.Item().Text(en
                        ? $"Places and jobs to start with: {places}."
                        : $"Plekken en beroepen om mee te beginnen: {places}.")
                        .FontSize(9).FontColor(Muted);
                }

                tail.Item().Text(en
                    ? "Your answers stay yours. A workplace only sees that a direction fits, not your raw scores."
                    : "Je antwoorden blijven van jou. Een werkplek ziet alleen dat een richting past, niet je ruwe scores.")
                    .FontSize(8).FontColor(Muted);
                Disclaimer(tail, lang);
            });
        });
    }

    private static string TraitJudgement(CompetenceDeepTraitReport trait)
        => !string.IsNullOrWhiteSpace(trait.NormBand) ? trait.NormBand! : trait.Level;

    internal static byte[] Competence(
        string brand, byte[] logo, string fullName, string generated,
        CompetenceDeepReport report, string? uiLang)
    {
        const string lang = "nl";
        var title = "Uitgebreid rapport – Competentietest";
        var traits = report.Traits.ToList();
        var top = traits.OrderByDescending(t => t.Score).Take(3).Select(t => t.LabelNl).ToList();

        return Compose(brand, logo, fullName, generated, title, lang, col =>
        {
            col.Item().Text("150 vragen · Big Five (IPIP)").FontSize(8).FontColor(Orange);
            Intro(col, "Jij in het kort", report.Summary);
            Chips(col, uiLang, top);
            Chart(col, uiLang,
                report.NormSourceLine ?? "",
                traits.Select(t => (
                    t.LabelNl,
                    t.Score,
                    string.IsNullOrWhiteSpace(TraitJudgement(t)) ? $"{t.Score}/100" : $"{t.Score}/100 · {TraitJudgement(t)}")));

            if (!string.IsNullOrWhiteSpace(report.NormSourceLine))
            {
                col.Item().Text(report.NormSourceLine!).FontSize(7.5f).FontColor(Muted).Italic();
            }

            Heading(col, CompactPdfCopy.WhatItMeans(uiLang));
            foreach (var trait in traits)
            {
                col.Item().ShowEntire().Border(1).BorderColor(Line).Background(Card).Padding(6).Column(box =>
                {
                    box.Spacing(3);
                    box.Item().Row(r =>
                    {
                        r.RelativeItem().Text(trait.LabelNl).FontSize(11).Bold().FontColor(Purple);
                        r.ConstantItem(170).AlignRight().AlignMiddle()
                            .Text($"{trait.Score}/100 · {TraitJudgement(trait)}").FontSize(8).SemiBold().FontColor(Orange);
                    });

                    box.Item().Element(e => Bar(e, trait.Score, Purple, trait.NormMean));
                    if (trait.NormMean is double avg)
                    {
                        box.Item().Text($"Gemiddelde van de normgroep: {Math.Round(avg)}/100").FontSize(7.5f).FontColor(Muted);
                    }

                    if (trait.Facets.Count > 0)
                    {
                        box.Item().PaddingTop(2).Text("Facetten").FontSize(8).Bold().FontColor(Ink);
                        foreach (var facet in trait.Facets)
                        {
                            box.Item().Row(r =>
                            {
                                r.RelativeItem(4).AlignMiddle().Text(facet.LabelNl).FontSize(7.5f);
                                r.RelativeItem(5).AlignMiddle().Element(e => Bar(e, facet.Score, Orange, facet.NormMean));
                                r.ConstantItem(22).AlignRight().AlignMiddle().Text($"{facet.Score}").FontSize(7.5f).Bold().FontColor(Ink);
                            });
                        }
                    }

                    Mini(box, "Wat betekent dit?", trait.Meaning);
                    Mini(box, "Zo zie je het op je werk", trait.WorkQuote);
                    box.Item().Row(r =>
                    {
                        r.RelativeItem().Background(SoftCoral).Padding(4).Column(c =>
                        {
                            c.Item().Text("Valkuil").FontSize(8).Bold().FontColor(Orange);
                            c.Item().Text(trait.Pitfall).FontSize(8);
                        });
                        r.ConstantItem(6);
                        r.RelativeItem().Background(SoftPurple).Padding(4).Column(c =>
                        {
                            c.Item().Text("Tip").FontSize(8).Bold().FontColor(Purple);
                            c.Item().Text(trait.Tip).FontSize(8);
                        });
                    });
                    Mini(box, "Sterk in", trait.Strength);
                });
            }

            var strongest = traits.OrderByDescending(t => t.Score).FirstOrDefault();
            if (strongest is not null)
            {
                Heading(col, "Werk dat bij je past");
                col.Item().Row(r =>
                {
                    FitCard(r, "Hier bloei je op", strongest.ThriveAtWork, SoftPurple);
                    r.ConstantItem(6);
                    FitCard(r, "Leidinggevende die past", strongest.FittingManager, SoftCoral);
                    r.ConstantItem(6);
                    FitCard(r, "Jij in een team", strongest.InTeam, Card);
                });
            }

            Heading(col, "Beroepen die bij je passen");
            if (report.Occupations.Count == 0)
            {
                col.Item().Text("Vul de vragenlijst volledig in voor persoonlijke beroepssuggesties.").FontSize(9).FontColor(Muted);
            }

            foreach (var job in report.Occupations)
            {
                var heading = OccupationCatalog.Shared.Resolve(job.Title) is { IsListable: true }
                    ? $"{job.Title} — {CareerCompassBuilder.FormatPercent(job.MatchPercent, "nl")}%"
                    : job.Title;
                JobCard(col, heading, job.Reason);
            }

            Heading(col, "Jouw actieplan");
            var step = 1;
            foreach (var action in report.ActionPlan.Take(3))
            {
                col.Item().ShowEntire().Background(SoftPurple).Padding(6).Column(box =>
                {
                    box.Spacing(2);
                    box.Item().Text($"{step}. {action.Title}").FontSize(10).Bold().FontColor(Purple);
                    box.Item().Text(action.Body).FontSize(8.5f);
                    box.Item().Text("☐ Gedaan op: ____").FontSize(8).FontColor(Muted);
                });
                step++;
            }

            Heading(col, "Over deze test");
            col.Item().Text("150 vragen · Big Five (IPIP).").FontSize(8);
            col.Item().Text(
                    "Goldberg, L. R., Johnson, J. A., Eber, H. W., Hogan, R., Ashton, M. C., Cloninger, C. R., " +
                    "& Gough, H. G. (2006). The International Personality Item Pool and the future of " +
                    "public-domain personality measures. Journal of Research in Personality, 40(1), 84–96. ipip.ori.org")
                .FontSize(7).FontColor(Muted);
            if (!string.IsNullOrWhiteSpace(report.NormSourceLine))
            {
                col.Item().Text(report.NormSourceLine!).FontSize(7).FontColor(Muted).Italic();
            }

            col.Item().PaddingTop(2).Text("Dit is geen diagnose.").FontSize(8).Italic().FontColor(Muted);
            Disclaimer(col, lang);
        });
    }

    internal static byte[] Culture(
        string brand, byte[] logo, string fullName, string generated,
        CultureDeepReport report, string lang, string? uiLang, bool employersOn)
    {
        var en = ReportLanguage.IsEnglish(lang);
        var title = DeepReportCatalog.Get("title.culture", lang);
        var cultureAxes = report.Domains
            .Where(d => CulturePersonalityCatalog.CultureDimensionCodes.Contains(d.Domain, StringComparer.OrdinalIgnoreCase))
            .OrderByDescending(d => d.Score)
            .ToList();
        var facets = report.Domains
            .Where(d => CulturePersonalityCatalog.PersonalityFacetCodes.Contains(d.Domain, StringComparer.OrdinalIgnoreCase))
            .OrderByDescending(d => d.Score)
            .ToList();
        if (cultureAxes.Count == 0)
        {
            cultureAxes = report.Domains.OrderByDescending(d => d.Score).ToList();
        }

        return Compose(brand, logo, fullName, generated, title, lang, col =>
        {
            Intro(col, title, report.Summary.Resolve(lang));
            Chips(col, uiLang, cultureAxes.Take(3).Select(d => DeepReportCatalog.CultureLabel(d.Domain, lang)));
            Chart(col, uiLang, en
                ? "A higher percent means that way of working showed up more often. It is a starting point, not a grade."
                : "Een hoger percentage betekent dat die manier van werken vaker in je antwoorden zat. Het is een startpunt, geen cijfer.",
                cultureAxes.Select(d => BarPoint(DeepReportCatalog.CultureLabel(d.Domain, lang), d)));

            if (employersOn && report.Employers.Count > 0)
            {
                Heading(col, en ? "Workplaces that fit you" : "Werkplekken die bij je passen");
                foreach (var employer in report.Employers.Take(6))
                {
                    JobCard(col,
                        DeepReportCatalog.Get($"org.{employer.OrgTypeKey}", lang),
                        DeepReportCatalog.Get($"org.{employer.OrgTypeKey}.why", lang));
                }
            }
            else
            {
                Heading(col, en ? "How you like to work" : "Hoe jij graag werkt");
                col.Item().Text(en
                    ? "Look for a place where these ways of working show up in a normal week."
                    : "Zoek een plek waar deze manieren van werken in een gewone week zichtbaar zijn.")
                    .FontSize(9).FontColor(Muted);
                foreach (var axis in cultureAxes.Take(3))
                {
                    col.Item().Text($"{DeepReportCatalog.CultureLabel(axis.Domain, lang)} — {axis.Score}%").FontSize(9).SemiBold();
                }
            }

            if (facets.Count > 0)
            {
                Heading(col, en ? "How you show up in a team" : "Hoe jij in een team past");
                Chart(col, uiLang, en
                    ? "A higher percent means that way of working showed up more often. It is a starting point, not a grade."
                    : "Een hoger percentage betekent dat die manier van werken vaker in je antwoorden zat. Het is een startpunt, geen cijfer.",
                    facets.Select(d => BarPoint(DeepReportCatalog.CultureLabel(d.Domain, lang), d)));
            }

            ActionPlan(col, lang, report.ActionPlan);
            Strengths(col, lang, report.StrengthKeys, report.PitfallKeys, "culture");
            Heading(col, en ? "How to read your scores" : "Zo lees je je scores");
            col.Item().Text(en
                ? "A higher percent means that way of working showed up more often. It is a starting point, not a grade."
                : "Een hoger percentage betekent dat die manier van werken vaker in je antwoorden zat. Het is een startpunt, geen cijfer.")
                .FontSize(9).FontColor(Muted);
            Heading(col, en ? "What you can do next" : "Wat je hiermee kunt doen");
            col.Item().Text(en
                ? "Use this picture when you look at a workplace. Your answers stay yours."
                : "Gebruik dit beeld als je naar een werkplek kijkt. Je antwoorden blijven van jou.")
                .FontSize(9);
            Disclaimer(col, lang);
        });
    }

    internal static byte[] Values(
        string brand, byte[] logo, string fullName, string generated,
        ValuesDeepReport report, string lang, string? uiLang, bool employersOn)
    {
        var en = ReportLanguage.IsEnglish(lang);
        var title = DeepReportCatalog.Get("title.values", lang);
        var ordered = report.Domains.ToList();

        return Compose(brand, logo, fullName, generated, title, lang, col =>
        {
            Intro(col, title, report.Summary.Resolve(lang));
            Chips(col, uiLang, ordered.Take(3).Select(d => DeepReportCatalog.ValueLabel(d.Domain, lang)));
            Chart(col, uiLang, en
                ? "A higher percent means that value weighed more in your answers. Ask in a conversation how it shows up in a normal week."
                : "Een hoger percentage betekent dat die waarde zwaarder woog in je antwoorden. Vraag in een gesprek hoe dat in een gewone week zichtbaar is.",
                ordered.Select(d => BarPoint(DeepReportCatalog.ValueLabel(d.Domain, lang), d)));

            Heading(col, DeepReportCatalog.Get("values.rank.title", lang));
            col.Item().Text(DeepReportCatalog.Get("values.rank.lead", lang)).FontSize(8).Italic().FontColor(Muted);
            var rank = 1;
            foreach (var domain in ordered)
            {
                MeaningCard(col,
                    $"{rank}. {DeepReportCatalog.ValueLabel(domain.Domain, lang)} — {domain.Score}%",
                    AssessmentReportPdfService.ChooseLine(domain.Domain, lang));
                rank++;
            }

            if (employersOn && report.Employers.Count > 0)
            {
                Heading(col, en ? "Workplaces that fit these values" : "Werkplekken die bij deze waarden passen");
                foreach (var employer in report.Employers.Take(6))
                {
                    JobCard(col,
                        DeepReportCatalog.Get($"org.{employer.OrgTypeKey}", lang),
                        DeepReportCatalog.Get($"org.{employer.OrgTypeKey}.why", lang));
                }
            }
            else
            {
                Heading(col, en ? "What this means for your work" : "Wat dit voor je werk betekent");
                col.Item().Text(en
                    ? "Use your top values when you choose tasks and a team. You do not need a company name for that."
                    : "Gebruik je topwaarden als je taken en een team kiest. Daar heb je geen bedrijfsnaam voor nodig.")
                    .FontSize(9);
            }

            ActionPlan(col, lang, report.ActionPlan);
            Strengths(col, lang, report.StrengthKeys, report.PitfallKeys, "value");
            Heading(col, en ? "How to read this and what is next" : "Zo lees je dit, en wat daarna");
            col.Item().Text(en
                ? "A higher percent means that value weighed more in your answers. Ask in a conversation how it shows up in a normal week."
                : "Een hoger percentage betekent dat die waarde zwaarder woog in je antwoorden. Vraag in een gesprek hoe dat in een gewone week zichtbaar is.")
                .FontSize(9).FontColor(Muted);
            Disclaimer(col, lang);
        });
    }

    private static (string Label, int Score, string Trailing) BarPoint(string label, DeepDomainScore domain)
    {
        var trailing = domain.NormMean is double n
            ? $"{domain.Score}% (Ø {Math.Round(n)}%)"
            : $"{domain.Score}%";
        return (label, domain.Score, trailing);
    }

    private static byte[] Compose(
        string brand, byte[] logo, string fullName, string generated, string title,
        string lang, Action<ColumnDescriptor> body)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(18);
                page.MarginVertical(14);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Ink).LineHeight(1.3f));
                Header(page, brand, logo, title, fullName, generated, lang);
                page.Content().PaddingTop(8).Column(col =>
                {
                    col.Spacing(7);
                    body(col);
                });
                Footer(page, brand, lang);
            });
        }).GeneratePdf();
    }

    private static void Header(
        PageDescriptor page, string brand, byte[] logo, string title, string fullName, string generated, string lang)
    {
        var confidential = DeepReportCatalog.Get("pdf.confidential", lang);
        page.Header().Column(header =>
        {
            header.Item().Background(Purple).PaddingHorizontal(8).PaddingVertical(5).Row(row =>
            {
                if (logo is { Length: > 0 })
                {
                    row.ConstantItem(26).Height(16).Image(logo).FitArea();
                    row.ConstantItem(6);
                }

                row.RelativeItem().AlignMiddle().Text(brand).FontSize(11).Bold().FontColor(Colors.White);
                row.RelativeItem(2).AlignMiddle().AlignRight().Text(title).FontSize(8).FontColor(Colors.White);
            });
            header.Item().Background(SoftCoral).PaddingHorizontal(8).PaddingVertical(3).Row(row =>
            {
                row.RelativeItem().Text(fullName).FontSize(8).SemiBold().FontColor(Ink);
                row.ConstantItem(88).AlignRight().Text(generated).FontSize(8).FontColor(Muted);
                row.ConstantItem(72).AlignRight().Text(confidential).FontSize(8).SemiBold().FontColor(Orange);
            });
        });
    }

    private static void Footer(PageDescriptor page, string brand, string lang)
    {
        var pageWord = DeepReportCatalog.Get("pdf.page", lang);
        page.Footer().PaddingTop(3).BorderTop(1).BorderColor(Line).PaddingTop(3).Row(row =>
        {
            row.RelativeItem().Text($"{brand} · persoonlijk rapport").FontSize(7.5f).FontColor(Muted);
            row.ConstantItem(88).AlignRight().Text(text =>
            {
                text.Span(pageWord + " ").FontSize(7.5f).FontColor(Muted);
                text.CurrentPageNumber().FontSize(7.5f).FontColor(Muted);
                text.Span(" / ").FontSize(7.5f).FontColor(Muted);
                text.TotalPages().FontSize(7.5f).FontColor(Muted);
            });
        });
    }

    private static void Intro(ColumnDescriptor col, string title, string summary)
    {
        col.Item().Text(title).FontSize(16).Bold().FontColor(Purple);
        if (!string.IsNullOrWhiteSpace(summary))
        {
            col.Item().Text(summary).FontSize(9.5f);
        }
    }

    private static void Chips(ColumnDescriptor col, string? uiLang, IEnumerable<string> labels)
    {
        var chips = labels.Where(l => !string.IsNullOrWhiteSpace(l)).Take(3).ToList();
        if (chips.Count == 0)
        {
            return;
        }

        col.Item().Text(CompactPdfCopy.TopThree(uiLang)).FontSize(8).SemiBold().FontColor(Orange);
        col.Item().Row(row =>
        {
            for (var i = 0; i < chips.Count; i++)
            {
                if (i > 0)
                {
                    row.ConstantItem(4);
                }

                row.RelativeItem().Background(SoftCoral).Border(1).BorderColor(Orange)
                    .PaddingHorizontal(4).PaddingVertical(3)
                    .AlignCenter().Text(chips[i]).FontSize(8).SemiBold().FontColor(Ink);
            }
        });
    }

    private static void Chart(
        ColumnDescriptor col,
        string? uiLang,
        string lead,
        IEnumerable<(string Label, int Score, string Trailing)> rows)
    {
        var items = rows.ToList();
        if (items.Count == 0)
        {
            return;
        }

        var highlight = items.Max(i => i.Score);
        col.Item().Background(SoftPurple).Border(1).BorderColor(Line).Padding(6).Column(box =>
        {
            box.Spacing(3);
            box.Item().Text(CompactPdfCopy.Profile(uiLang)).FontSize(12).Bold().FontColor(Purple);
            if (!string.IsNullOrWhiteSpace(lead))
            {
                box.Item().Text(lead).FontSize(7.5f).FontColor(Muted);
            }
            foreach (var item in items)
            {
                var strong = item.Score == highlight && highlight > 0;
                box.Item().Row(r =>
                {
                    r.RelativeItem(3).AlignMiddle().Text(item.Label).FontSize(8).SemiBold().FontColor(Ink);
                    r.RelativeItem(4).AlignMiddle().Element(e => Bar(e, item.Score, strong ? Orange : Purple, marker: null));
                    r.ConstantItem(78).AlignMiddle().AlignRight().Text(item.Trailing).FontSize(7.5f).SemiBold()
                        .FontColor(strong ? Orange : Purple);
                });
            }
        });
    }

    private static void Bar(IContainer container, double scorePercent, Color fill, double? marker)
    {
        var score = (float)Math.Clamp(scorePercent, 0, 100);
        var left = Math.Max(score, 0.01f);
        var right = Math.Max(100f - score, 0.01f);
        container.Height(8).Layers(layers =>
        {
            layers.PrimaryLayer().Background(Track).Row(row =>
            {
                row.RelativeItem(left).Background(fill);
                row.RelativeItem(right);
            });
            if (marker is double markerRaw)
            {
                var mark = (float)Math.Clamp(markerRaw, 0, 100);
                var markerLeft = Math.Max(mark, 0.01f);
                var markerRight = Math.Max(100f - mark, 0.01f);
                layers.Layer().Row(row =>
                {
                    row.RelativeItem(markerLeft);
                    row.ConstantItem(2).Background(Ink);
                    row.RelativeItem(markerRight);
                });
            }
        });
    }

    private static void Heading(ColumnDescriptor col, string text)
        => col.Item().PaddingTop(3).Text(text).FontSize(12).Bold().FontColor(Purple);

    private static void MeaningCard(ColumnDescriptor col, string title, string body)
    {
        col.Item().ShowEntire().Background(Card).BorderLeft(3).BorderColor(Purple).PaddingLeft(6).PaddingVertical(3).Column(box =>
        {
            box.Item().Text(title).FontSize(9).SemiBold().FontColor(Ink);
            if (!string.IsNullOrWhiteSpace(body))
            {
                box.Item().Text(body).FontSize(8).FontColor(Muted);
            }
        });
    }

    private static void JobCard(ColumnDescriptor col, string title, string reason)
    {
        col.Item().ShowEntire().Background(Card).Border(1).BorderColor(Line).Padding(5).Column(box =>
        {
            box.Item().Text(title).FontSize(9).SemiBold().FontColor(Ink);
            if (!string.IsNullOrWhiteSpace(reason))
            {
                box.Item().Text(reason).FontSize(8).FontColor(Muted);
            }
        });
    }

    private static void Mini(ColumnDescriptor col, string label, string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return;
        }

        col.Item().Column(box =>
        {
            box.Item().Text(label).FontSize(8).Bold().FontColor(Purple);
            box.Item().Text(body).FontSize(8);
        });
    }

    private static void FitCard(RowDescriptor row, string title, string body, Color background)
    {
        row.RelativeItem().Background(background).Border(1).BorderColor(Line).Padding(4).Column(box =>
        {
            box.Item().Text(title).FontSize(8).Bold().FontColor(Purple);
            box.Item().Text(body).FontSize(7.5f);
        });
    }

    private static void ActionPlan(ColumnDescriptor col, string lang, IReadOnlyList<DeepActionStep> plan)
    {
        Heading(col, DeepReportCatalog.Get("pdf.actionPlan", lang));
        col.Item().Text(DeepReportCatalog.Get("action.lead", lang)).FontSize(8).Italic().FontColor(Muted);
        var n = 1;
        foreach (var step in plan.Take(3))
        {
            col.Item().ShowEntire().Background(SoftCoral).Padding(6).Column(box =>
            {
                box.Item().Text($"{n}. {step.Title.Resolve(lang)}").FontSize(9).SemiBold().FontColor(Ink);
                box.Item().Text(step.Body.Resolve(lang)).FontSize(8).FontColor(Muted);
            });
            n++;
        }
    }

    private static void Strengths(
        ColumnDescriptor col, string lang, IReadOnlyList<string> strengths, IReadOnlyList<string> pitfalls, string label)
    {
        Heading(col, DeepReportCatalog.Get("pdf.strengths", lang));
        col.Item().Text(DeepReportCatalog.Get("strength.lead", lang)).FontSize(8).Italic().FontColor(Muted);
        foreach (var key in strengths.Take(3))
        {
            col.Item().Text("• " + AssessmentReportPdfService.StrengthSentence(key, label, lang)).FontSize(8.5f);
        }

        foreach (var key in pitfalls.Take(3))
        {
            col.Item().Text("△ " + AssessmentReportPdfService.StrengthSentence(key, label, lang)).FontSize(8.5f).FontColor(Orange);
        }
    }

    private static void Disclaimer(ColumnDescriptor col, string lang)
    {
        col.Item().PaddingTop(2).Text(DeepReportCatalog.Get("pdf.disclaimer", lang)).FontSize(7.5f).Italic().FontColor(Muted);
    }
}
