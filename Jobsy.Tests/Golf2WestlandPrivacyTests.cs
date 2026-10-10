using Jobsy.Core.Entities;
using Jobsy.Core.Golf2;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class Golf2WestlandPrivacyTests
{
    [Fact]
    public void Cohort_below_10_is_suppressed_without_rounding()
    {
        var (status, value) = WestlandPilotPrivacy.SuppressCount(9);
        Assert.Equal(WestlandPilotPrivacy.StatusInsufficient, status);
        Assert.Null(value);

        var (okStatus, okValue) = WestlandPilotPrivacy.SuppressCount(12);
        Assert.Equal(WestlandPilotPrivacy.StatusOk, okStatus);
        Assert.Equal(12, okValue);
    }

    [Fact]
    public void Count_of_10_is_returned_exactly_not_rounded()
    {
        var (status, value) = WestlandPilotPrivacy.SuppressCount(10);
        Assert.Equal(WestlandPilotPrivacy.StatusOk, status);
        Assert.Equal(10, value);
        Assert.Equal(13, WestlandPilotPrivacy.SuppressCount(13).Value);
    }

    [Fact]
    public void Gespreksblad_rejects_forbidden_field_keys()
    {
        var errors = ConversationSheetRules.ValidateFieldKeys(["strengths", "scores", "motivation"]);
        Assert.Contains(errors, e => e.Contains("scores", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(errors, e => e.Contains("strengths", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Gespreksblad_rejects_scores_test_results_and_birthdate_in_text()
    {
        Assert.NotEmpty(ConversationSheetRules.ValidateFreeText("Mijn score is 87%"));
        Assert.NotEmpty(ConversationSheetRules.ValidateFreeText("Mijn RIASEC testresultaat was interessant"));
        Assert.NotEmpty(ConversationSheetRules.ValidateFreeText("Geboortedatum 12-03-1990"));
        Assert.Empty(ConversationSheetRules.ValidateFreeText("Ik wil graag in de logistiek werken."));
    }

    [Fact]
    public void Gespreksblad_custom_text_uses_expected_label()
    {
        Assert.Equal("Eigen toevoeging", ConversationSheetRules.CustomTextLabel);
    }

    [Fact]
    public void Passport_next_step_uses_career_plan_dream_not_role_fit()
    {
        Assert.Equal("Hovenier", PassportNextStepBuilder.Build("Hovenier", "Magazijnmedewerker"));
        Assert.Null(PassportNextStepBuilder.Build(null, "Magazijnmedewerker"));
        Assert.Equal("Kok", PassportNextStepBuilder.Build("Kok", null));
    }

    [Fact]
    public void Pilot_csv_has_no_open_answer_columns()
    {
        var header = string.Join(',', WestlandPilotReportingService.CsvHeaderColumns);
        Assert.True(WestlandPilotReportingService.CsvIsAggregateOnly(header + "\n"));
        foreach (var forbidden in WestlandPilotReportingService.ForbiddenCsvColumns)
        {
            Assert.False(WestlandPilotReportingService.CsvIsAggregateOnly(forbidden + ",task_id\n"));
        }
    }

    [Fact]
    public void Pilot_csv_header_lists_only_aggregate_fields()
    {
        Assert.Equal(
            ["task_id", "occupation_title", "task_title", "choice_count", "count_status"],
            WestlandPilotReportingService.CsvHeaderColumns);
    }

    [Fact]
    public async Task Pilot_csv_counts_only_gecontroleerde_tasks()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new JobsyDbContext(options);

        var occupation = new WestlandOccupation
        {
            Id = Guid.NewGuid(),
            EscoId = Guid.NewGuid().ToString("D"),
            TitleNl = "Logistiek",
            SortOrder = 1,
            IsPublished = true
        };
        var checkedTask = new WestlandOccupationTask
        {
            Id = Guid.NewGuid(),
            OccupationId = occupation.Id,
            Occupation = occupation,
            TitleNl = "Orders picken",
            SortOrder = 1,
            Gecontroleerd = true
        };
        var draftTask = new WestlandOccupationTask
        {
            Id = Guid.NewGuid(),
            OccupationId = occupation.Id,
            Occupation = occupation,
            TitleNl = "Nog niet gecontroleerd",
            SortOrder = 2,
            Gecontroleerd = false
        };
        occupation.Tasks.Add(checkedTask);
        occupation.Tasks.Add(draftTask);
        db.WestlandOccupations.Add(occupation);

        var userId = Guid.NewGuid();
        db.CandidateWestlandTaskChoices.AddRange(
            new CandidateWestlandTaskChoice
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TaskId = checkedTask.Id,
                Task = checkedTask,
                ChosenAtUtc = DateTime.UtcNow
            },
            new CandidateWestlandTaskChoice
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TaskId = draftTask.Id,
                Task = draftTask,
                ChosenAtUtc = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var reporting = new WestlandPilotReportingService(db);
        var csv = await reporting.BuildTaskChoicesCsvAsync();

        Assert.Contains("Orders picken", csv, StringComparison.Ordinal);
        Assert.DoesNotContain("Nog niet gecontroleerd", csv, StringComparison.Ordinal);
        Assert.DoesNotContain(draftTask.Id.ToString("D"), csv, StringComparison.Ordinal);
    }
}
