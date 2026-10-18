using Jobsy.Core.Rules;
using Jobsy.Web.Models;

namespace Jobsy.Web.Components.Candidate.Onboarding;

public static class WorkHistoryDraftExtensions
{
    public static bool IsBlank(this CandidateEmployerHistory job)
        => OnboardingWorkHistoryRules.IsBlank(
            job.EmployerName,
            job.Role,
            job.StartMonth,
            job.EndMonth,
            job.IsCurrent);

    public static bool HasEngagedJob(this OnboardingProfileDraft draft)
        => draft.Jobs.Any(job => !job.IsBlank());

    public static void EnsureFirstJobSlot(this OnboardingProfileDraft draft)
    {
        if (draft.NoWork || draft.Jobs.Count > 0)
        {
            return;
        }

        draft.Jobs.Add(new CandidateEmployerHistory { IsCurrent = false });
    }

    public static List<CandidateEmployerHistory> JobsForSave(this OnboardingProfileDraft draft)
        => draft.Jobs
            .Where(job => !job.IsBlank())
            .Select(CopyForSave)
            .ToList();

    public static bool HasAnyField(this CandidateEmployerJobReference reference)
        => !string.IsNullOrWhiteSpace(reference.ContactName)
           || !string.IsNullOrWhiteSpace(reference.Relationship)
           || !string.IsNullOrWhiteSpace(reference.Email)
           || !string.IsNullOrWhiteSpace(reference.Phone);

    private static CandidateEmployerHistory CopyForSave(CandidateEmployerHistory job)
        => new()
        {
            EmployerName = job.EmployerName.Trim(),
            Role = string.IsNullOrWhiteSpace(job.Role) ? null : job.Role.Trim(),
            Years = job.Years,
            StartMonth = string.IsNullOrWhiteSpace(job.StartMonth) ? null : job.StartMonth.Trim(),
            EndMonth = string.IsNullOrWhiteSpace(job.EndMonth) ? null : job.EndMonth.Trim(),
            Description = string.IsNullOrWhiteSpace(job.Description) ? null : job.Description.Trim(),
            IsCurrent = job.IsCurrent,
            EscoId = string.IsNullOrWhiteSpace(job.EscoId) ? null : job.EscoId.Trim(),
            Reference = job.Reference?.ForSave()
        };

    private static CandidateEmployerJobReference? ForSave(this CandidateEmployerJobReference reference)
    {
        if (!reference.ConsentConfirmed || !reference.HasAnyField())
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(reference.ContactName))
        {
            return null;
        }

        var hasEmail = !string.IsNullOrWhiteSpace(reference.Email);
        var hasPhone = !string.IsNullOrWhiteSpace(reference.Phone);
        if (!hasEmail && !hasPhone)
        {
            return null;
        }

        return new CandidateEmployerJobReference
        {
            ContactName = reference.ContactName.Trim(),
            Relationship = string.IsNullOrWhiteSpace(reference.Relationship) ? null : reference.Relationship.Trim(),
            Email = hasEmail ? reference.Email!.Trim() : null,
            Phone = hasPhone ? reference.Phone!.Trim() : null,
            ConsentConfirmed = true
        };
    }
}
