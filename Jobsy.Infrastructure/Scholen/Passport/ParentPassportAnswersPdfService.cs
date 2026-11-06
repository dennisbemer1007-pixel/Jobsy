using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.PupilPassport;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Jobsy.Infrastructure.Scholen.Passport;

public sealed class ParentPassportAnswersPdfService : IParentPassportAnswersPdfService
{
    private static readonly Color Purple = Color.FromHex("#5b2a9a");
    private static readonly Color Ink = Color.FromHex("#3a2d52");
    private static readonly Color Muted = Color.FromHex("#8d83a0");
    private static readonly Color Line = Color.FromHex("#e6dac3");

    static ParentPassportAnswersPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        PupilPassportFonts.EnsureRegistered();
    }

    public byte[] Render(ParentPassportAnswersPdfModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (!model.Enabled || model.Sections.Count == 0)
        {
            return [];
        }

        var dateCulture = PupilVerhaalLocalized.IsEnglish(model.Language)
            ? System.Globalization.CultureInfo.GetCultureInfo("en-GB")
            : System.Globalization.CultureInfo.GetCultureInfo("nl-NL");

        return Document.Create(doc =>
        {
            foreach (var section in model.Sections)
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.PageColor(Color.FromHex("#fffefb"));
                    page.MarginHorizontal(24);
                    page.MarginVertical(18);
                    page.DefaultTextStyle(x => x.FontSize(9f).FontColor(Ink).FontFamily(PupilPassportFonts.Nunito));

                    page.Header().Element(c => Header(c, model, dateCulture));
                    page.Content().PaddingTop(8).Column(col =>
                    {
                        col.Item().Text(model.Intro).FontSize(8.5f).FontColor(Muted).LineHeight(1.35f);
                        col.Item().PaddingTop(10).Text(section.Title).FontFamily(PupilPassportFonts.Fredoka)
                            .FontSize(14).FontColor(Purple);
                        col.Item().Text(section.Subtitle).FontSize(8.5f).FontColor(Muted);
                        col.Item().PaddingTop(8).Element(c => Items(c, model, section));
                    });
                    page.Footer().AlignCenter().Text(model.Footer).FontSize(7f).FontColor(Muted).LineHeight(1.25f);
                });
            }
        }).GeneratePdf();
    }

    private static void Header(IContainer c, ParentPassportAnswersPdfModel m, System.Globalization.CultureInfo dateCulture)
    {
        c.Background(Purple).Padding(10).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("LOBSY").FontFamily(PupilPassportFonts.Fredoka).FontSize(8).FontColor(Colors.White)
                    .LetterSpacing(0.12f);
                col.Item().Text(m.HeaderTitle).FontFamily(PupilPassportFonts.Fredoka).FontSize(15)
                    .FontColor(Colors.White);
            });
            row.ConstantItem(110).AlignMiddle().Column(col =>
            {
                col.Item().AlignRight().Text(m.ClassLabel).FontSize(8f).FontColor(Colors.White);
                col.Item().AlignRight().Text(m.SchoolName).FontSize(7.5f).FontColor(Colors.White).LineHeight(1.15f);
                col.Item().AlignRight().Text(m.Date.ToString("d MMM yyyy", dateCulture)).FontSize(7f)
                    .FontColor(Colors.White);
            });
        });
    }

    private static void Items(IContainer c, ParentPassportAnswersPdfModel model, ParentPassportAnswersPdfSection section)
    {
        c.Column(col =>
        {
            var n = 1;
            foreach (var item in section.Items)
            {
                col.Item().PaddingTop(n == 1 ? 0 : 8).BorderBottom(0.5f).BorderColor(Line).PaddingBottom(6)
                    .Column(block =>
                    {
                        block.Item().Text($"{n}. {item.Question}").SemiBold().FontSize(9f).LineHeight(1.35f);
                        block.Item().PaddingTop(3).Text(text =>
                        {
                            text.Span($"{model.AnswerLabel}: ").FontColor(Muted).FontSize(8f);
                            text.Span(item.Answer).SemiBold().FontSize(9f);
                        });
                    });
                n++;
            }
        });
    }
}
