using System.Globalization;
using System.Text;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.Time;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Jobsy.Infrastructure.Services;

public sealed class JobSearchActivityService(JobsyDbContext db, TimeProvider time) : IJobSearchActivityService
{
    private const string EntityApplication = "Application";
    private const string EntityInterview = "Interview";
    private const string EntityExternalVacancy = "ExternalVacancy";
    private const string EntityTrainingEnrollment = "TrainingEnrollment";

    static JobSearchActivityService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<JobSearchActivityOverviewDto> GetOverviewAsync(
        Guid candidateUserId,
        bool afterMatchEnabled,
        string cultureName,
        CancellationToken cancellationToken = default)
    {
        await SyncAutomaticActivitiesAsync(candidateUserId, afterMatchEnabled, cancellationToken);
        var agreement = await EnsureAgreementAsync(candidateUserId, cancellationToken);
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var today = TodayAmsterdam();
        var (periodStart, periodEnd) = JobSearchActivityPeriodRules.CurrentPeriod(agreement.PeriodStart, today);

        var activities = await db.JobSearchActivities.AsNoTracking()
            .Where(a => a.CandidateUserId == candidateUserId)
            .OrderByDescending(a => a.ActivityDate)
            .ThenByDescending(a => a.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var inPeriod = activities
            .Where(a => JobSearchActivityPeriodRules.IsInPeriod(a.ActivityDate, periodStart, periodEnd))
            .ToList();

        var weeks = BuildWeekGroups(activities, culture);
        var target = agreement.ActivitiesPerPeriod;
        var done = inPeriod.Count;
        var daysLeft = JobSearchActivityPeriodRules.DaysRemainingInclusive(periodEnd, today);

        return new JobSearchActivityOverviewDto(
            periodStart,
            periodEnd,
            FormatPeriodLabel(periodStart, periodEnd, culture),
            done,
            target,
            daysLeft,
            done >= target,
            weeks);
    }

    public async Task SyncAutomaticActivitiesAsync(
        Guid candidateUserId,
        bool afterMatchEnabled,
        CancellationToken cancellationToken = default)
    {
        var existing = await db.JobSearchActivities
            .Where(a => a.CandidateUserId == candidateUserId && a.Source == JobSearchActivitySource.Auto)
            .Select(a => new { a.SourceEntityType, a.SourceEntityId })
            .ToListAsync(cancellationToken);
        var existingKeys = existing
            .Where(x => x.SourceEntityType is not null && x.SourceEntityId is not null)
            .Select(x => (x.SourceEntityType!, x.SourceEntityId!.Value))
            .ToHashSet();

        var toAdd = new List<JobSearchActivity>();
        var now = time.GetUtcNow().UtcDateTime;

        var apps = await db.Applications.AsNoTracking()
            .Include(a => a.Vacancy)
            .ThenInclude(v => v.Company)
            .Where(a => a.CandidateUserId == candidateUserId)
            .ToListAsync(cancellationToken);

        foreach (var app in apps)
        {
            if (!existingKeys.Add((EntityApplication, app.Id)))
            {
                continue;
            }

            toAdd.Add(new JobSearchActivity
            {
                Id = Guid.NewGuid(),
                CandidateUserId = candidateUserId,
                ActivityDate = ToAmsterdamDate(app.CreatedAt),
                Kind = JobSearchActivityKind.Applied,
                Employer = app.Vacancy.Company?.Name ?? string.Empty,
                FunctionTitle = app.Vacancy.Title,
                ContactMethod = "Lobsy",
                Result = null,
                Source = JobSearchActivitySource.Auto,
                SourceEntityType = EntityApplication,
                SourceEntityId = app.Id,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        var externals = await db.CandidateExternalVacancies.AsNoTracking()
            .Where(e => e.CandidateUserId == candidateUserId
                        && e.Status == CandidateExternalVacancyStatus.Applied)
            .ToListAsync(cancellationToken);

        foreach (var ext in externals)
        {
            if (!existingKeys.Add((EntityExternalVacancy, ext.Id)))
            {
                continue;
            }

            toAdd.Add(new JobSearchActivity
            {
                Id = Guid.NewGuid(),
                CandidateUserId = candidateUserId,
                ActivityDate = ToAmsterdamDate(ext.SavedAtUtc),
                Kind = JobSearchActivityKind.Applied,
                Employer = ext.CompanyName,
                FunctionTitle = ext.Title,
                ContactMethod = ext.SourceHost,
                Result = null,
                Source = JobSearchActivitySource.Auto,
                SourceEntityType = EntityExternalVacancy,
                SourceEntityId = ext.Id,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        if (afterMatchEnabled)
        {
            var interviews = await db.Interviews.AsNoTracking()
                .Include(i => i.Application)
                .ThenInclude(a => a.Vacancy)
                .ThenInclude(v => v.Company)
                .Include(i => i.ChosenSlot)
                .Where(i => i.Application.CandidateUserId == candidateUserId
                            && i.Status == InterviewStatus.Done)
                .ToListAsync(cancellationToken);

            foreach (var interview in interviews)
            {
                if (!existingKeys.Add((EntityInterview, interview.Id)))
                {
                    continue;
                }

                var slotUtc = interview.ChosenSlot?.StartUtc ?? interview.UpdatedAtUtc;
                toAdd.Add(new JobSearchActivity
                {
                    Id = Guid.NewGuid(),
                    CandidateUserId = candidateUserId,
                    ActivityDate = ToAmsterdamDate(slotUtc),
                    Kind = JobSearchActivityKind.Interview,
                    Employer = interview.Application.Vacancy.Company?.Name ?? string.Empty,
                    FunctionTitle = interview.Application.Vacancy.Title,
                    ContactMethod = FormatInterviewContact(interview.Format),
                    Result = null,
                    Source = JobSearchActivitySource.Auto,
                    SourceEntityType = EntityInterview,
                    SourceEntityId = interview.Id,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                });
            }
        }

        var enrollments = await db.TrainingEnrollments.AsNoTracking()
            .Include(e => e.Offer)
            .ThenInclude(o => o.Provider)
            .Where(e => e.UserId == candidateUserId
                        && (e.Status == TrainingEnrollmentStatus.Completed
                            || e.Status == TrainingEnrollmentStatus.Verified))
            .ToListAsync(cancellationToken);

        foreach (var enrollment in enrollments)
        {
            var hasCertificate = !string.IsNullOrWhiteSpace(enrollment.CertificateTitle)
                                   || enrollment.VerifiedAtUtc is not null;
            var isWorkshop = enrollment.Offer.Type == TrainingOfferType.Workshop;
            if (!hasCertificate && !isWorkshop)
            {
                continue;
            }

            if (!existingKeys.Add((EntityTrainingEnrollment, enrollment.Id)))
            {
                continue;
            }

            var whenUtc = enrollment.VerifiedAtUtc
                          ?? enrollment.CompletedAtUtc
                          ?? enrollment.UpdatedAtUtc;
            var title = !string.IsNullOrWhiteSpace(enrollment.CertificateTitle)
                ? enrollment.CertificateTitle!
                : enrollment.Offer.Title;
            toAdd.Add(new JobSearchActivity
            {
                Id = Guid.NewGuid(),
                CandidateUserId = candidateUserId,
                ActivityDate = ToAmsterdamDate(whenUtc),
                Kind = JobSearchActivityKind.Workshop,
                Employer = enrollment.Offer.Provider.Name,
                FunctionTitle = title,
                ContactMethod = null,
                Result = null,
                Source = JobSearchActivitySource.Auto,
                SourceEntityType = EntityTrainingEnrollment,
                SourceEntityId = enrollment.Id,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        if (toAdd.Count > 0)
        {
            db.JobSearchActivities.AddRange(toAdd);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<JobSearchActivityItemDto> AddManualAsync(
        Guid candidateUserId,
        CreateManualJobSearchActivityRequest request,
        string? attachmentFileName,
        string? attachmentContentType,
        ReadOnlyMemory<byte>? attachmentContent,
        CancellationToken cancellationToken = default)
    {
        var employer = (request.Employer ?? string.Empty).Trim();
        var function = (request.FunctionTitle ?? string.Empty).Trim();
        if (employer.Length == 0 || function.Length == 0)
        {
            throw new InvalidOperationException("Employer and function are required.");
        }

        if (employer.Length > 200 || function.Length > 200)
        {
            throw new InvalidOperationException("Employer or function is too long.");
        }

        var result = string.IsNullOrWhiteSpace(request.Result) ? null : request.Result.Trim();
        if (result?.Length > 2000)
        {
            throw new InvalidOperationException("Result is too long.");
        }

        var now = time.GetUtcNow().UtcDateTime;
        var row = new JobSearchActivity
        {
            Id = Guid.NewGuid(),
            CandidateUserId = candidateUserId,
            ActivityDate = request.ActivityDate,
            Kind = request.Kind,
            Employer = employer,
            FunctionTitle = function,
            ContactMethod = null,
            Result = result,
            Source = JobSearchActivitySource.Manual,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        if (attachmentContent is { Length: > 0 })
        {
            if (!JobSearchActivityAttachmentRules.TryNormalize(
                    attachmentFileName,
                    attachmentContentType,
                    attachmentContent.Value.Span,
                    out var safeName,
                    out var normalizedType,
                    out var fileError))
            {
                throw new InvalidOperationException(fileError ?? "file_invalid");
            }

            var attachmentId = Guid.NewGuid();
            db.JobSearchActivityAttachments.Add(new JobSearchActivityAttachment
            {
                Id = attachmentId,
                CandidateUserId = candidateUserId,
                FileName = safeName,
                ContentType = normalizedType,
                SizeBytes = attachmentContent.Value.Length,
                Content = attachmentContent.Value.ToArray(),
                CreatedAtUtc = now
            });
            row.AttachmentId = attachmentId;
        }

        db.JobSearchActivities.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        return MapItem(row, isExternal: false);
    }

    public async Task<(string FileName, string ContentType, byte[] Content)?> GetAttachmentAsync(
        Guid candidateUserId,
        Guid activityId,
        CancellationToken cancellationToken = default)
    {
        var activity = await db.JobSearchActivities.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == activityId && a.CandidateUserId == candidateUserId, cancellationToken);
        if (activity?.AttachmentId is not Guid attachmentId)
        {
            return null;
        }

        var file = await db.JobSearchActivityAttachments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.CandidateUserId == candidateUserId, cancellationToken);
        if (file is null)
        {
            return null;
        }

        return (file.FileName, file.ContentType, file.Content);
    }

    public async Task UpdateAgreementAsync(
        Guid candidateUserId,
        int activitiesPerPeriod,
        CancellationToken cancellationToken = default)
    {
        var agreement = await EnsureAgreementAsync(candidateUserId, cancellationToken);
        agreement.ActivitiesPerPeriod = JobSearchActivityPeriodRules.ClampActivitiesPerPeriod(activitiesPerPeriod);
        agreement.UpdatedAtUtc = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<byte[]> BuildPeriodPdfAsync(
        Guid candidateUserId,
        bool afterMatchEnabled,
        string cultureName,
        CancellationToken cancellationToken = default)
    {
        var overview = await GetOverviewAsync(candidateUserId, afterMatchEnabled, cultureName, cancellationToken);
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var isNl = culture.TwoLetterISOLanguageName.Equals("nl", StringComparison.OrdinalIgnoreCase);

        var rows = overview.Weeks.SelectMany(w => w.Items)
            .Where(i => i.ActivityDate >= overview.PeriodStart && i.ActivityDate <= overview.PeriodEndInclusive)
            .OrderBy(i => i.ActivityDate)
            .ToList();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text(isNl ? "Overzicht sollicitatieactiviteiten" : "Job search activity overview")
                        .FontSize(16).SemiBold();
                    col.Item().PaddingTop(4).Text(overview.PeriodLabel).FontSize(11);
                });

                page.Content().PaddingTop(12).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(62);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.4f);
                        columns.RelativeColumn(1.4f);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.4f);
                    });

                    void HeaderCell(string text)
                        => table.Cell().BorderBottom(1).Padding(4).Text(text).SemiBold();

                    HeaderCell(isNl ? "Datum" : "Date");
                    HeaderCell(isNl ? "Soort" : "Type");
                    HeaderCell(isNl ? "Werkgever" : "Employer");
                    HeaderCell(isNl ? "Functie" : "Role");
                    HeaderCell(isNl ? "Contact" : "Contact");
                    HeaderCell(isNl ? "Resultaat" : "Result");

                    foreach (var row in rows)
                    {
                        table.Cell().Padding(4).Text(row.ActivityDate.ToString("dd-MM-yyyy", culture));
                        table.Cell().Padding(4).Text(KindLabel(row.Kind, culture));
                        table.Cell().Padding(4).Text(row.Employer);
                        table.Cell().Padding(4).Text(row.FunctionTitle);
                        table.Cell().Padding(4).Text(row.ContactMethod ?? "");
                        table.Cell().Padding(4).Text(row.Result ?? "");
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span(isNl
                        ? "Lobsy geeft niets door aan het UWV. Je zet dit overzicht zelf in Mijn UWV of in de Werkmap."
                        : "Lobsy does not share this with UWV. You add this overview yourself in Mijn UWV or your workbook.");
                });
            });
        }).GeneratePdf();
    }

    private async Task<ActivityAgreement> EnsureAgreementAsync(Guid candidateUserId, CancellationToken cancellationToken)
    {
        var agreement = await db.ActivityAgreements.FirstOrDefaultAsync(
            a => a.CandidateUserId == candidateUserId,
            cancellationToken);
        if (agreement is not null)
        {
            return agreement;
        }

        agreement = new ActivityAgreement
        {
            CandidateUserId = candidateUserId,
            ActivitiesPerPeriod = JobSearchActivityPeriodRules.DefaultActivitiesPerPeriod,
            PeriodStart = TodayAmsterdam(),
            UpdatedAtUtc = time.GetUtcNow().UtcDateTime
        };
        db.ActivityAgreements.Add(agreement);
        await db.SaveChangesAsync(cancellationToken);
        return agreement;
    }

    private static IReadOnlyList<JobSearchActivityWeekDto> BuildWeekGroups(
        IReadOnlyList<JobSearchActivity> activities,
        CultureInfo culture)
    {
        if (activities.Count == 0)
        {
            return Array.Empty<JobSearchActivityWeekDto>();
        }

        var groups = new Dictionary<DateOnly, List<JobSearchActivity>>();
        foreach (var activity in activities)
        {
            var weekStart = WeekStartMonday(activity.ActivityDate);
            if (!groups.TryGetValue(weekStart, out var list))
            {
                list = [];
                groups[weekStart] = list;
            }

            list.Add(activity);
        }

        return groups
            .OrderByDescending(g => g.Key)
            .Select(g => new JobSearchActivityWeekDto(
                g.Key,
                FormatWeekLabel(g.Key, culture),
                g.Value.Count,
                g.Value
                    .OrderByDescending(a => a.ActivityDate)
                    .ThenByDescending(a => a.CreatedAtUtc)
                    .Select(a => MapItem(a, a.SourceEntityType == EntityExternalVacancy))
                    .ToList()))
            .ToList();
    }

    private static JobSearchActivityItemDto MapItem(JobSearchActivity row, bool isExternal)
    {
        string? statusKey = row.Source == JobSearchActivitySource.Auto
            ? "Apps.Activiteiten.AutoTracked"
            : null;
        if (row.Kind == JobSearchActivityKind.Applied && row.Source == JobSearchActivitySource.Auto)
        {
            statusKey = isExternal ? "Apps.SourceExternal" : "Apps.Activiteiten.Sent";
        }

        return new JobSearchActivityItemDto(
            row.Id,
            row.ActivityDate,
            row.Kind,
            row.Employer,
            row.FunctionTitle,
            row.ContactMethod,
            row.Result,
            row.Source,
            row.SourceEntityType,
            row.SourceEntityId,
            isExternal,
            statusKey,
            row.AttachmentId is not null);
    }

    private static DateOnly TodayAmsterdam()
        => DateOnly.FromDateTime(AmsterdamTime.ToLocal(DateTime.UtcNow));

    private static DateOnly ToAmsterdamDate(DateTime utc)
        => DateOnly.FromDateTime(AmsterdamTime.ToLocal(utc));

    private static DateOnly WeekStartMonday(DateOnly date)
    {
        var dow = (int)date.DayOfWeek;
        var diff = (dow + 6) % 7;
        return date.AddDays(-diff);
    }

    private static string FormatPeriodLabel(DateOnly start, DateOnly end, CultureInfo culture)
    {
        var isNl = culture.TwoLetterISOLanguageName.Equals("nl", StringComparison.OrdinalIgnoreCase);
        if (isNl)
        {
            return $"{start.ToString("d MMMM", culture)} tot en met {end.ToString("d MMMM", culture)}";
        }

        return $"{start.ToString("d MMMM", culture)} through {end.ToString("d MMMM", culture)}";
    }

    private static string FormatWeekLabel(DateOnly weekStart, CultureInfo culture)
    {
        var isNl = culture.TwoLetterISOLanguageName.Equals("nl", StringComparison.OrdinalIgnoreCase);
        return isNl
            ? $"Week van {weekStart.ToString("d MMMM", culture)}"
            : $"Week of {weekStart.ToString("d MMMM", culture)}";
    }

    private static string FormatInterviewContact(InterviewFormat format)
        => format switch
        {
            InterviewFormat.OnSite => "Op locatie",
            InterviewFormat.Online => "Online",
            InterviewFormat.Phone => "Telefoon",
            _ => "Gesprek"
        };

    private static string KindLabel(JobSearchActivityKind kind, CultureInfo culture)
    {
        var isNl = culture.TwoLetterISOLanguageName.Equals("nl", StringComparison.OrdinalIgnoreCase);
        return kind switch
        {
            JobSearchActivityKind.Applied => isNl ? "Gesolliciteerd" : "Applied",
            JobSearchActivityKind.EmployerCalled => isNl ? "Werkgever gebeld" : "Called employer",
            JobSearchActivityKind.VisitedInPerson => isNl ? "Langsgegaan" : "Visited in person",
            JobSearchActivityKind.TempAgency => isNl ? "Uitzendbureau" : "Temp agency",
            JobSearchActivityKind.NetworkConversation => isNl ? "Netwerkgesprek" : "Network conversation",
            JobSearchActivityKind.Workshop => isNl ? "Workshop" : "Workshop",
            JobSearchActivityKind.Interview => isNl ? "Gesprek" : "Interview",
            _ => kind.ToString()
        };
    }
}
