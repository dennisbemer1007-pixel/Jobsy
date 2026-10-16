using Jobsy.Core.Passport;

namespace Jobsy.Infrastructure.Services.Passport;

internal static class StudentPassportPdfModelMapper
{
    internal static StudentPassportPdfModel FromPassport(PassportPdfModel m)
    {
        var dna = m.Dna.Select((d, i) => new StudentPassportDnaCard(
            IconKey: i switch { 0 => "heart", 1 => "dna", 2 => "flag", _ => "shield" },
            Label: d.Title,
            Value: d.Body,
            Sub: d.When)).ToList();

        var vacancies = m.JobFits.Select(j => new StudentPassportVacancyCard(
            j.Title,
            "",
            [],
            [j.Why])).ToList();

        var direction = m.Direction.Select((d, i) => new StudentPassportDirectionStep(
            i + 1,
            d.Title,
            d.State,
            i == m.Direction.Count - 1)).ToList();

        return new StudentPassportPdfModel(
            FullName: m.FullName,
            Initials: m.Initials,
            StudyLine: m.Educations.Count > 0 ? m.Educations[0] : "Student",
            YearLine: m.HeroMeta ?? "",
            Tagline: m.Tagline,
            Region: m.Region,
            DnaCards: dna,
            Strengths: m.Strengths,
            Vacancies: vacancies,
            Experience: m.Experience.Select(e => new StudentPassportExperienceLine(
                e.Title,
                e.Period ?? e.Meta ?? "",
                e.Detail)).ToList(),
            LearningGoals: m.LearningLines.ToList(),
            DirectionSteps: direction,
            OwnWords: m.OwnWords,
            Motivation: m.Motivation,
            HowIWork: m.HowIWork.ToList(),
            Certificates: m.Certificates.ToList(),
            FooterNote: m.GeneratedLabel,
            Sparse: string.IsNullOrWhiteSpace(m.FullName));
    }
}
