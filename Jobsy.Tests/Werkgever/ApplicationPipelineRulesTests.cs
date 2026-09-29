using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Models;
using Jobsy.Web.Werkgever;

namespace Jobsy.Tests.Werkgever;

public class ApplicationPipelineRulesTests
{
    [Theory]
    [InlineData("Pending", false, false)]
    [InlineData("Accepted", true, false)]
    [InlineData("EmployerContacting", true, false)]
    [InlineData("Hired", true, true)]
    public void Privacy_stages_match_LobsyCvAccessRules(string status, bool pii, bool contact)
    {
        var item = Item(status, name: "Priya Sanders", email: "p@x.nl", phone: "0612345678");
        Assert.Equal(pii, ApplicationPipelineRules.IsPiiStage(item));
        Assert.Equal(contact, ApplicationPipelineRules.IsContactStage(item));
        Assert.Equal(pii, LobsyCvAccessRules.IsPiiRevealed(Enum.Parse<ApplicationStatus>(status)));
        Assert.Equal(contact, LobsyCvAccessRules.IsDirectContactRevealed(Enum.Parse<ApplicationStatus>(status)));
    }

    [Fact]
    public void DisplayName_ignores_leaked_name_on_Pending()
    {
        var pending = Item("Pending", name: "Priya Sanders");
        var label = ApplicationPipelineRules.DisplayName(pending, "Kandidaat #{0}");
        Assert.StartsWith("Kandidaat #", label);
        Assert.DoesNotContain("Priya", label);
    }

    [Fact]
    public void DisplayName_uses_api_name_after_accept()
    {
        var accepted = Item("Accepted", name: "Priya Sanders");
        Assert.Equal("Priya Sanders", ApplicationPipelineRules.DisplayName(accepted, "Kandidaat #{0}"));
    }

    [Fact]
    public void Column_mapping_includes_rejected_summary_statuses()
    {
        var items = new[]
        {
            Item("Rejected"),
            Item("FilledElsewhere"),
            Item("Withdrawn"),
            Item("Pending")
        };
        var rejectedCol = ApplicationPipelineRules.Columns
            .Single(c => c.Column == ApplicationPipelineRules.PipelineColumn.Afgewezen);
        Assert.Equal(3, items.Count(rejectedCol.Matches));
        var (r, e, w) = ApplicationPipelineRules.RejectedSummary(items);
        Assert.Equal(1, r);
        Assert.Equal(1, e);
        Assert.Equal(1, w);
    }

    [Fact]
    public void Overdue_marker_only_for_pending_over_48h()
    {
        var now = DateTime.UtcNow;
        var late = Item("Pending", created: now.AddHours(-60));
        var fresh = Item("Pending", created: now.AddHours(-10));
        var acceptedLate = Item("Accepted", created: now.AddHours(-60));
        Assert.True(ApplicationPipelineRules.IsOverdue(late, now));
        Assert.False(ApplicationPipelineRules.IsOverdue(fresh, now));
        Assert.False(ApplicationPipelineRules.IsOverdue(acceptedLate, now));
    }

    [Theory]
    [InlineData("Pending", "WgApp.Action.Accept", true)]
    [InlineData("Accepted", "WgApp.Action.Invite", true)]
    [InlineData("EmployerContacting", "WgApp.Action.Hire", true)]
    [InlineData("Hired", null, false)]
    [InlineData("Rejected", null, false)]
    public void Footer_primary_and_reject_per_stage(string status, string? primary, bool reject)
    {
        var item = Item(status);
        Assert.Equal(primary, ApplicationPipelineRules.PrimaryActionKey(item));
        Assert.Equal(reject, ApplicationPipelineRules.ShowReject(item));
    }

    [Theory]
    [InlineData("Pending", true, false, false)]
    [InlineData("Accepted", true, true, false)]
    [InlineData("EmployerContacting", true, true, false)]
    [InlineData("Hired", true, true, true)]
    public void Wat_je_ziet_rows_per_stage(string status, bool anon, bool nameCv, bool contact)
    {
        var rows = ApplicationPipelineRules.VisibilityRows(Item(status));
        Assert.Equal(anon, rows[0].Unlocked);
        Assert.Equal(nameCv, rows[1].Unlocked);
        Assert.Equal(contact, rows[2].Unlocked);
    }

    [Theory]
    [InlineData("Pending", 0)]
    [InlineData("Accepted", 1)]
    [InlineData("EmployerContacting", 2)]
    [InlineData("Hired", 3)]
    public void Stepper_marks_current_step(string status, int currentIndex)
    {
        var steps = ApplicationPipelineRules.Stepper(Item(status));
        Assert.Equal(ApplicationPipelineRules.StepperState.Current, steps[currentIndex]);
        for (var i = 0; i < currentIndex; i++)
        {
            Assert.Equal(ApplicationPipelineRules.StepperState.Done, steps[i]);
        }
    }

    [Fact]
    public void Rm_cannot_act_bm_and_vm_can()
    {
        Assert.False(ApplicationPipelineRules.CanAct(Jobsy.Web.Navigation.EmployerRole.Regiomanager));
        Assert.True(ApplicationPipelineRules.CanAct(Jobsy.Web.Navigation.EmployerRole.Bedrijfsmanager));
        Assert.True(ApplicationPipelineRules.CanAct(Jobsy.Web.Navigation.EmployerRole.Vestigingsmanager));
    }

    [Fact]
    public void No_employer_note_field_so_interne_notitie_deferred()
        => Assert.False(ApplicationPipelineRules.HasEmployerNoteField());

    [Fact]
    public void Terminology_keys_use_Aangenomen_and_Uitgenodigd()
    {
        Assert.Equal("WgApp.Status.Hired", ApplicationPipelineRules.StatusLabelKey("Hired"));
        Assert.Equal("WgApp.Status.Invited", ApplicationPipelineRules.StatusLabelKey("EmployerContacting"));
        Assert.Equal("Aangenomen", Jobsy.Web.Localization.UiStrings.Get("WgApp.Status.Hired", "nl"));
        Assert.Equal("Uitgenodigd", Jobsy.Web.Localization.UiStrings.Get("WgApp.Status.Invited", "nl"));
        Assert.DoesNotContain("Gematcht", Jobsy.Web.Localization.UiStrings.Get("WgApp.Action.Hire", "nl"));
        Assert.DoesNotContain("Contact opgenomen", Jobsy.Web.Localization.UiStrings.Get("WgApp.Status.Invited", "nl"));
    }

    private static EmployerApplicationItem Item(
        string status,
        string? name = null,
        string? email = null,
        string? phone = null,
        DateTime? created = null)
        => new()
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            VacancyId = Guid.NewGuid(),
            VacancyTitle = "Oogst",
            CompanyName = "Naaldwijk",
            Status = status,
            CandidateName = name,
            CandidateEmail = email,
            CandidatePhone = phone,
            CreatedAt = created ?? DateTime.UtcNow.AddDays(-1),
            PiiRevealed = status is "Accepted" or "EmployerContacting" or "Hired",
            CvPdfAvailable = status is "Accepted" or "EmployerContacting" or "Hired"
        };
}
