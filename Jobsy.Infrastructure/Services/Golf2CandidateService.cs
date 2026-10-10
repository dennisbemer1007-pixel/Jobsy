using Jobsy.Core.Entities;
using Jobsy.Core.Golf2;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed record ConversationSheetDto(
    string StrengthsText,
    string MotivationText,
    string CustomText,
    string CustomTextLabel);

public sealed record OutsideWorkDto(Guid Id, string ActivityTitle, string Description, int? HoursPerWeek, int SortOrder);

public sealed record FourTestsFeedbackDto(int HelpfulnessRating, string OpenAnswer, bool ShareWithPilot);

public sealed record WestlandTaskDto(Guid Id, Guid OccupationId, string OccupationTitle, string TitleNl, bool Gecontroleerd);

public sealed class Golf2CandidateService(JobsyDbContext db, TimeProvider time)
{
    public async Task<ConversationSheetDto?> GetConversationSheetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var row = await db.CandidateConversationSheets.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        return row is null
            ? null
            : new ConversationSheetDto(
                row.StrengthsText,
                row.MotivationText,
                row.CustomText,
                ConversationSheetRules.CustomTextLabel);
    }

    public async Task<(bool Ok, IReadOnlyList<string> Errors, ConversationSheetDto? Sheet)> SaveConversationSheetAsync(
        Guid userId,
        ConversationSheetDto request,
        IReadOnlyDictionary<string, string?>? extraFields,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        if (extraFields is not null)
        {
            errors.AddRange(ConversationSheetRules.ValidateFieldKeys(extraFields.Keys));
        }

        errors.AddRange(ConversationSheetRules.ValidateFreeText(request.StrengthsText));
        errors.AddRange(ConversationSheetRules.ValidateFreeText(request.MotivationText));
        errors.AddRange(ConversationSheetRules.ValidateFreeText(request.CustomText));
        if (errors.Count > 0)
        {
            return (false, errors, null);
        }

        var now = time.GetUtcNow().UtcDateTime;
        var row = await db.CandidateConversationSheets
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        if (row is null)
        {
            row = new CandidateConversationSheet
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UpdatedAtUtc = now
            };
            db.CandidateConversationSheets.Add(row);
        }

        row.StrengthsText = request.StrengthsText.Trim();
        row.MotivationText = request.MotivationText.Trim();
        row.CustomText = request.CustomText.Trim();
        row.UpdatedAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);

        return (true, [], new ConversationSheetDto(
            row.StrengthsText,
            row.MotivationText,
            row.CustomText,
            ConversationSheetRules.CustomTextLabel));
    }

    public async Task<IReadOnlyList<OutsideWorkDto>> ListOutsideWorkAsync(Guid userId, CancellationToken cancellationToken = default)
        => await db.CandidateOutsideWorkExperiences.AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.SortOrder)
            .Select(e => new OutsideWorkDto(e.Id, e.ActivityTitle, e.Description, e.HoursPerWeek, e.SortOrder))
            .ToListAsync(cancellationToken);

    public async Task<OutsideWorkDto> UpsertOutsideWorkAsync(
        Guid userId,
        Guid? id,
        string activityTitle,
        string description,
        int? hoursPerWeek,
        int sortOrder,
        CancellationToken cancellationToken = default)
    {
        var now = time.GetUtcNow().UtcDateTime;
        CandidateOutsideWorkExperience row;
        if (id is Guid existingId)
        {
            row = await db.CandidateOutsideWorkExperiences
                .FirstOrDefaultAsync(e => e.Id == existingId && e.UserId == userId, cancellationToken)
                ?? throw new InvalidOperationException("Activiteit niet gevonden.");
        }
        else
        {
            row = new CandidateOutsideWorkExperience
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAtUtc = now
            };
            db.CandidateOutsideWorkExperiences.Add(row);
        }

        row.ActivityTitle = activityTitle.Trim();
        row.Description = description.Trim();
        row.HoursPerWeek = hoursPerWeek is < 0 or > 80 ? null : hoursPerWeek;
        row.SortOrder = sortOrder;
        row.UpdatedAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);
        return new OutsideWorkDto(row.Id, row.ActivityTitle, row.Description, row.HoursPerWeek, row.SortOrder);
    }

    public async Task DeleteOutsideWorkAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        var row = await db.CandidateOutsideWorkExperiences
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId, cancellationToken);
        if (row is null)
        {
            return;
        }

        db.CandidateOutsideWorkExperiences.Remove(row);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<FourTestsFeedbackDto?> GetFourTestsFeedbackAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var row = await db.PassportFourTestsFeedbacks.AsNoTracking()
            .FirstOrDefaultAsync(f => f.UserId == userId, cancellationToken);
        return row is null
            ? null
            : new FourTestsFeedbackDto(row.HelpfulnessRating, row.OpenAnswer, row.ShareWithPilot);
    }

    public async Task<FourTestsFeedbackDto> SaveFourTestsFeedbackAsync(
        Guid userId,
        int helpfulnessRating,
        string openAnswer,
        bool shareWithPilot,
        CancellationToken cancellationToken = default)
    {
        var rating = Math.Clamp(helpfulnessRating, 1, 5);
        var now = time.GetUtcNow().UtcDateTime;
        var row = await db.PassportFourTestsFeedbacks
            .FirstOrDefaultAsync(f => f.UserId == userId, cancellationToken);
        if (row is null)
        {
            row = new PassportFourTestsFeedback
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAtUtc = now
            };
            db.PassportFourTestsFeedbacks.Add(row);
        }

        row.HelpfulnessRating = rating;
        row.OpenAnswer = openAnswer.Trim();
        row.ShareWithPilot = shareWithPilot;
        row.UpdatedAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);
        return new FourTestsFeedbackDto(row.HelpfulnessRating, row.OpenAnswer, row.ShareWithPilot);
    }

    public async Task<IReadOnlyList<WestlandTaskDto>> ListPublishedTasksAsync(CancellationToken cancellationToken = default)
        => await db.WestlandOccupationTasks.AsNoTracking()
            .Where(t => t.Occupation.IsPublished)
            .OrderBy(t => t.Occupation.SortOrder)
            .ThenBy(t => t.SortOrder)
            .Select(t => new WestlandTaskDto(
                t.Id,
                t.OccupationId,
                t.Occupation.TitleNl,
                t.TitleNl,
                t.Gecontroleerd))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetTaskChoicesAsync(Guid userId, CancellationToken cancellationToken = default)
        => await db.CandidateWestlandTaskChoices.AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => c.TaskId)
            .ToListAsync(cancellationToken);

    public async Task SetTaskChoicesAsync(Guid userId, IReadOnlyList<Guid> taskIds, CancellationToken cancellationToken = default)
    {
        var published = await db.WestlandOccupationTasks.AsNoTracking()
            .Where(t => t.Occupation.IsPublished && taskIds.Contains(t.Id))
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);
        var allowed = published.ToHashSet();

        var existing = await db.CandidateWestlandTaskChoices
            .Where(c => c.UserId == userId)
            .ToListAsync(cancellationToken);
        db.CandidateWestlandTaskChoices.RemoveRange(existing.Where(c => !allowed.Contains(c.TaskId)));

        var have = existing.Select(c => c.TaskId).ToHashSet();
        var now = time.GetUtcNow().UtcDateTime;
        foreach (var taskId in allowed.Where(id => !have.Contains(id)))
        {
            db.CandidateWestlandTaskChoices.Add(new CandidateWestlandTaskChoice
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TaskId = taskId,
                ChosenAtUtc = now
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<string?> GetPassportNextStepAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var dream = await db.CandidateCareerPlans.AsNoTracking()
            .Where(p => p.UserId == userId && p.Status == CareerPlanStatuses.Active)
            .Select(p => p.DreamTitle)
            .FirstOrDefaultAsync(cancellationToken);

        var roleFit = await db.CandidateRoleFitChecks.AsNoTracking()
            .Where(r => r.UserId == userId)
            .Select(r => r.JobTitle)
            .FirstOrDefaultAsync(cancellationToken);

        return PassportNextStepBuilder.Build(dream, roleFit);
    }
}
