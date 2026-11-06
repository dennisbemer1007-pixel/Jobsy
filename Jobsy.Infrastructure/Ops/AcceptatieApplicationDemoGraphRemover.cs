using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Ops;

/// <summary>Deletes applications and dependent rows used by acceptatie demo reset (interviews, messages, notifications).</summary>
internal static class AcceptatieApplicationDemoGraphRemover
{
    private const string EntityApplication = "Application";

    public static async Task RemoveApplicationsAsync(
        JobsyDbContext db,
        IReadOnlyCollection<Guid> applicationIds,
        CancellationToken cancellationToken = default)
    {
        if (applicationIds.Count == 0)
        {
            return;
        }

        var interviews = await db.Interviews
            .Where(i => applicationIds.Contains(i.ApplicationId))
            .ToListAsync(cancellationToken);
        foreach (var interview in interviews)
        {
            interview.ChosenSlotId = null;
        }

        if (interviews.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        var interviewIds = interviews.Select(i => i.Id).ToList();
        if (interviewIds.Count > 0)
        {
            db.InterviewSlots.RemoveRange(
                await db.InterviewSlots.Where(s => interviewIds.Contains(s.InterviewId)).ToListAsync(cancellationToken));
            db.InterviewChanges.RemoveRange(
                await db.InterviewChanges.Where(c => interviewIds.Contains(c.InterviewId)).ToListAsync(cancellationToken));
            db.InterviewReminders.RemoveRange(
                await db.InterviewReminders.Where(r => interviewIds.Contains(r.InterviewId)).ToListAsync(cancellationToken));
            db.Interviews.RemoveRange(interviews);
        }

        db.ApplicationMessages.RemoveRange(
            await db.ApplicationMessages.Where(m => applicationIds.Contains(m.ApplicationId)).ToListAsync(cancellationToken));
        db.ApplicationOutcomes.RemoveRange(
            await db.ApplicationOutcomes.Where(o => applicationIds.Contains(o.ApplicationId)).ToListAsync(cancellationToken));
        db.MaqqieHoursWeeks.RemoveRange(
            await db.MaqqieHoursWeeks.Where(w => applicationIds.Contains(w.ApplicationId)).ToListAsync(cancellationToken));
        db.UserNotifications.RemoveRange(
            await db.UserNotifications
                .Where(n => n.RelatedEntityType == EntityApplication
                            && n.RelatedEntityId != null
                            && applicationIds.Contains(n.RelatedEntityId.Value))
                .ToListAsync(cancellationToken));
        db.JobSearchActivities.RemoveRange(
            await db.JobSearchActivities
                .Where(a => a.SourceEntityType == EntityApplication
                            && a.SourceEntityId != null
                            && applicationIds.Contains(a.SourceEntityId.Value))
                .ToListAsync(cancellationToken));

        var apps = await db.Applications.Where(a => applicationIds.Contains(a.Id)).ToListAsync(cancellationToken);
        db.Applications.RemoveRange(apps);
        await db.SaveChangesAsync(cancellationToken);
    }
}
