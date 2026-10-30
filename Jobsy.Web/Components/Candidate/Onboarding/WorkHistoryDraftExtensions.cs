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
            OnlyForMe = job.OnlyForMe,
            Skills = job.Skills?.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? []
        };
}
