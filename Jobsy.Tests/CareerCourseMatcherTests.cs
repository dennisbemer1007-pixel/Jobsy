using Jobsy.Core.Contracts;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class CareerCourseMatcherTests
{
    [Theory]
    [InlineData("Cursus Leiderschap", "leiderschap")]
    [InlineData("Workshop Roosteren", "roosteren")]
    [InlineData("Opleiding Basis Excel", "excel")]
    [InlineData("Training Module Planning", "planning")]
    public void Normalize_strips_filler_words(string input, string expected)
        => Assert.Equal(expected, CareerCourseMatcher.Normalize(input));

    [Fact]
    public void Normalize_strips_diacritics()
        => Assert.Equal("francaise", CareerCourseMatcher.Normalize("Française"));

    [Fact]
    public void IsMatch_equal_after_normalize()
        => Assert.True(CareerCourseMatcher.IsMatch(
            "Praktijkgericht leiderschap",
            "Cursus praktijkgericht leiderschap"));

    [Fact]
    public void IsMatch_containment_either_direction()
    {
        Assert.True(CareerCourseMatcher.IsMatch("Leiderschap", "Praktijkgericht leiderschap / coachmodule"));
        Assert.True(CareerCourseMatcher.IsMatch("Praktijkgericht leiderschap / coachmodule", "leiderschap"));
    }

    [Fact]
    public void IsMatch_rejects_short_tokens()
    {
        Assert.False(CareerCourseMatcher.IsMatch("HR", "HR basis"));
        Assert.False(CareerCourseMatcher.IsMatch("abc", "abcdef"));
    }

    [Fact]
    public void IsOnProfile_matches_certificate_name()
    {
        var certs = new[] { new CandidateCertificateDto("Praktijkgericht leiderschap", 2024) };
        Assert.True(CareerCourseMatcher.IsOnProfile("Cursus praktijkgericht leiderschap", certs));
        Assert.False(CareerCourseMatcher.IsOnProfile("Korte workshop roosteren", certs));
    }

    [Fact]
    public void StepMatchPercent_is_share_of_matched_courses()
    {
        var certs = new[] { new CandidateCertificateDto("Leiderschap", 2024) };
        var courses = new[] { "Praktijkgericht leiderschap", "Korte workshop roosteren" };
        Assert.Equal(50, CareerCourseMatcher.StepMatchPercent(courses, certs));
        Assert.Equal(1, CareerCourseMatcher.MatchedCount(courses, certs));
        Assert.False(CareerCourseMatcher.AllCoursesMatched(courses, certs));
    }

    [Fact]
    public void StepMatchPercent_zero_without_courses()
        => Assert.Equal(0, CareerCourseMatcher.StepMatchPercent([], []));
}
