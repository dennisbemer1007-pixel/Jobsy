using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class SuitableFor65PlusRulesTests
{
    [Fact]
    public void Dedicated_65plus_category_always_matches_filter()
    {
        Assert.True(VacancyCategoryDefaults.MatchesSuitableFor65PlusFilter(
            VacancyCategoryDefaults.SeniorLightId,
            suitableFor65Plus: false));
    }

    [Fact]
    public void Flagged_regular_vacancy_matches_filter()
    {
        Assert.True(VacancyCategoryDefaults.MatchesSuitableFor65PlusFilter(
            VacancyCategoryDefaults.RegulierId,
            suitableFor65Plus: true));
    }

    [Fact]
    public void Unflagged_regular_and_other_categories_do_not_match()
    {
        Assert.False(VacancyCategoryDefaults.MatchesSuitableFor65PlusFilter(
            VacancyCategoryDefaults.RegulierId,
            suitableFor65Plus: false));
        Assert.False(VacancyCategoryDefaults.MatchesSuitableFor65PlusFilter(
            VacancyCategoryDefaults.InternshipId,
            suitableFor65Plus: true));
        Assert.False(VacancyCategoryDefaults.MatchesSuitableFor65PlusFilter(
            VacancyCategoryDefaults.VolunteerId,
            suitableFor65Plus: false));
    }

    [Fact]
    public void Senior_plus_brand_color_and_name_are_geschikt_voor_65plus()
    {
        Assert.Equal("#5B21B6", VacancyCategoryDefaults.SeniorPlusColorHex);
        var senior = Assert.Single(
            VacancyCategoryDefaults.All,
            c => c.Id == VacancyCategoryDefaults.SeniorLightId);
        Assert.Equal(VacancyCategoryDefaults.SeniorPlusColorHex, senior.ColorHex);
        Assert.Equal("Geschikt voor 65+", VacancyCategoryDefaults.SuitableFor65PlusLabel);
        Assert.Equal(VacancyCategoryDefaults.SuitableFor65PlusLabel, senior.Name);
    }

    [Fact]
    public void Selecting_65plus_category_includes_flagged_regulier_vacancies()
    {
        var filter = new HashSet<Guid> { VacancyCategoryDefaults.SeniorLightId };
        Assert.True(VacancyCategoryDefaults.MatchesSelectedCategories(
            VacancyCategoryDefaults.SeniorLightId, false, filter));
        Assert.True(VacancyCategoryDefaults.MatchesSelectedCategories(
            VacancyCategoryDefaults.RegulierId, true, filter));
        Assert.False(VacancyCategoryDefaults.MatchesSelectedCategories(
            VacancyCategoryDefaults.RegulierId, false, filter));
        Assert.False(VacancyCategoryDefaults.MatchesSelectedCategories(
            VacancyCategoryDefaults.InternshipId, true, filter));
    }
}
