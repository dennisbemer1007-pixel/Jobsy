using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class VacancyCategoryDefaultsPopupBadgeTests
{
    [Fact]
    public void ResolveMapPopupTypeBadge_returns_stage_volunteer_inclusief_uitzend()
    {
        Assert.Equal(
            ("Stageplek", "#0EA5E9"),
            VacancyCategoryDefaults.ResolveMapPopupTypeBadge(VacancyCategoryDefaults.InternshipId, false));
        Assert.Equal(
            ("Vrijwilligers", "#10B981"),
            VacancyCategoryDefaults.ResolveMapPopupTypeBadge(VacancyCategoryDefaults.VolunteerId, false));
        Assert.Equal(
            ("Inclusieve vacature", "#8B5CF6"),
            VacancyCategoryDefaults.ResolveMapPopupTypeBadge(VacancyCategoryDefaults.InclusiefId, false));
        Assert.Equal(
            (VacancyCategoryDefaults.UitzendbureauLabel, VacancyCategoryDefaults.UitzendbureauColorHex),
            VacancyCategoryDefaults.ResolveMapPopupTypeBadge(VacancyCategoryDefaults.UitzendbureauId, false));
    }

    [Fact]
    public void ResolveMapPopupTypeBadge_returns_65plus_for_category_or_flag()
    {
        Assert.Equal(
            (VacancyCategoryDefaults.SuitableFor65PlusLabel, VacancyCategoryDefaults.SeniorPlusColorHex),
            VacancyCategoryDefaults.ResolveMapPopupTypeBadge(VacancyCategoryDefaults.SeniorLightId, false));
        Assert.Equal(
            (VacancyCategoryDefaults.SuitableFor65PlusLabel, VacancyCategoryDefaults.SeniorPlusColorHex),
            VacancyCategoryDefaults.ResolveMapPopupTypeBadge(VacancyCategoryDefaults.RegulierId, true));
    }

    [Fact]
    public void ResolveMapPopupTypeBadge_regular_and_highlight_have_no_label()
    {
        Assert.Equal((null, null), VacancyCategoryDefaults.ResolveMapPopupTypeBadge(VacancyCategoryDefaults.RegulierId, false));
        Assert.Equal((null, null), VacancyCategoryDefaults.ResolveMapPopupTypeBadge(VacancyCategoryDefaults.HighlightId, false));
    }

    [Fact]
    public void ResolveMapPopupTypeBadge_kind_fallback_without_category()
    {
        Assert.Equal(
            ("Stageplek", "#0EA5E9"),
            VacancyCategoryDefaults.ResolveMapPopupTypeBadge(null, false, VacancyKind.Internship));
        Assert.Equal(
            ("Vrijwilligers", "#10B981"),
            VacancyCategoryDefaults.ResolveMapPopupTypeBadge(null, false, VacancyKind.Volunteer));
    }
}
