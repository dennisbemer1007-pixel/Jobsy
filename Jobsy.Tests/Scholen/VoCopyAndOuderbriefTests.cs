using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Infrastructure.Scholen;
using Jobsy.Web.Localization;

namespace Jobsy.Tests.Scholen;

public class VoCopyAndOuderbriefTests
{
    [Fact]
    public void Parental_info_version_is_october_2026_v2()
        => Assert.Equal("ouders-2026-10-v2", ParentalInfoTexts.CurrentVersion);

    [Fact]
    public void Ouderbrief_variants_are_separate_and_drop_kindvriendelijke()
    {
        var g78 = OuderbriefTemplate.For(PupilQuestionSet.Groep78);
        var vo = OuderbriefTemplate.For(PupilQuestionSet.Vo);
        Assert.Contains("60 korte vragen", g78, StringComparison.Ordinal);
        Assert.Contains("100 korte vragen", vo, StringComparison.Ordinal);
        Assert.Contains("twee lesdelen", vo, StringComparison.Ordinal);
        Assert.DoesNotContain("60 kindvriendelijke", g78, StringComparison.Ordinal);
        Assert.DoesNotContain("60 kindvriendelijke", vo, StringComparison.Ordinal);
        Assert.DoesNotContain("middelbare", g78, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(g78, vo);
    }

    [Fact]
    public void PupilCopy_vo_keys_and_docent_messages()
    {
        Assert.Equal("Leerling.Login.Error.Invalid.Vo", PupilCopy.Key(PupilQuestionSet.Vo, "Leerling.Login.Error.Invalid"));
        Assert.Equal("Leerling.Login.Error.Invalid", PupilCopy.Key(PupilQuestionSet.Groep78, "Leerling.Login.Error.Invalid"));
        Assert.Contains("docent", PupilCopy.LoginInvalid(PupilQuestionSet.Vo), StringComparison.Ordinal);
        Assert.Contains("leraar", PupilCopy.LoginInvalid(PupilQuestionSet.Groep78), StringComparison.Ordinal);
        Assert.DoesNotContain("leraar", PupilCopy.LoginInvalid(PupilQuestionSet.Vo), StringComparison.Ordinal);
    }

    [Fact]
    public void Ui_vo_login_error_says_docent()
    {
        var nl = new Dictionary<string, string>(StringComparer.Ordinal);
        UiStringsScholen.MergeNl(nl);
        Assert.Contains("docent", nl["Leerling.Login.Error.Invalid.Vo"], StringComparison.Ordinal);
        Assert.Contains("leraar", nl["Leerling.Login.Error.Invalid"], StringComparison.Ordinal);
        Assert.Equal(PupilCopy.LoginInvalid(PupilQuestionSet.Vo), nl["Leerling.Login.Error.Invalid.Vo"]);
        Assert.Equal(PupilCopy.LoginCooldown(PupilQuestionSet.Vo), nl["Leerling.Login.Error.Cooldown.Vo"]);
        Assert.Equal(PupilCopy.LoginWindow(PupilQuestionSet.Vo), nl["Leerling.Login.Error.Window.Vo"]);
        Assert.Equal(PupilCopy.AnswersSavedWindow(PupilQuestionSet.Vo), nl["Leerling.WindowClosed.Body.Vo"]);
    }

    [Fact]
    public void Vo_cheer_keys_are_used_for_vo_def()
    {
        var registry = new PupilQuestionSetRegistry();
        var vo = registry.Get(PupilQuestionSet.Vo);
        var g78 = registry.Get(PupilQuestionSet.Groep78);
        Assert.StartsWith("LeerlingQ.Vo.Cheer.", vo.CheerKey(0), StringComparison.Ordinal);
        Assert.StartsWith("LeerlingQ.Cheer.", g78.CheerKey(0), StringComparison.Ordinal);
        Assert.NotEqual(vo.CheerKey(0), g78.CheerKey(0));
    }
}
