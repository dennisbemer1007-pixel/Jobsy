using Jobsy.Core.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Jobsy.Infrastructure.Services.CandidateExternalVacancies;

public sealed class ExternalVacancyApplicationLetterPdfBuilder : IExternalVacancyApplicationLetterPdfBuilder
{
    static ExternalVacancyApplicationLetterPdfBuilder()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Build(
        string candidateName,
        string vacancyTitle,
        string companyName,
        string motivation,
        IReadOnlyList<(string Label, string Value)> sharedFacts)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11));
                page.Header().Text("Sollicitatie via Lobsy").SemiBold().FontSize(16).FontColor(Colors.Blue.Darken3);
                page.Content().Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Text($"Beste {companyName},");
                    col.Item().Text($"Ik solliciteer voor: {vacancyTitle}.");
                    col.Item().Text("Motivatie:").SemiBold();
                    col.Item().Text(motivation);
                    if (sharedFacts.Count > 0)
                    {
                        col.Item().PaddingTop(8).Text("Gedeelde feiten:").SemiBold();
                        foreach (var (label, value) in sharedFacts)
                        {
                            col.Item().Text($"• {label}: {value}");
                        }
                    }

                    col.Item().PaddingTop(16).Text($"Met vriendelijke groet,\n{candidateName}");
                });
            });
        });

        return doc.GeneratePdf();
    }
}
