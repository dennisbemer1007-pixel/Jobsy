using System.Text.RegularExpressions;
using Jobsy.Core.Diagnostics;
using Jobsy.Web.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Jobsy.Tests.Errors;

public class SupportCodeTests
{
    private static readonly Regex Shape = new("^LB-[2-9A-HJKMNP-TV-Z]{4}$", RegexOptions.Compiled);

    [Fact]
    public void Alphabet_excludes_the_characters_people_misread()
    {
        foreach (var forbidden in "01ILOU")
        {
            Assert.False(
                SupportCodeGenerator.Alphabet.Contains(forbidden, StringComparison.Ordinal),
                $"Alphabet must not contain '{forbidden}'.");
        }
    }

    [Fact]
    public void Ten_thousand_codes_all_match_the_published_shape()
    {
        for (var i = 0; i < 10_000; i++)
        {
            var code = SupportCodeGenerator.Create();
            Assert.Equal(7, code.Length);
            Assert.True(Shape.IsMatch(code), $"Unexpected support code: {code}");
            Assert.True(SupportCodeGenerator.IsValid(code));
        }
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("LB-7Q3K", true)]
    [InlineData("lb-7q3k", false)]
    [InlineData("LB-7Q3", false)]
    [InlineData("LB-7Q3KX", false)]
    [InlineData("LB-0O1I", false)]
    public void Validation_matches_the_generator(string? code, bool valid)
        => Assert.Equal(valid, SupportCodeGenerator.IsValid(code));

    [Fact]
    public void One_request_gets_exactly_one_code()
    {
        var http = new DefaultHttpContext();

        var first = SupportCode.GetOrCreate(http);
        var second = SupportCode.GetOrCreate(http);

        Assert.Equal(first, second);
        Assert.Equal(first, SupportCode.Peek(http));
    }

    [Fact]
    public void Different_requests_get_different_codes()
    {
        var codes = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < 200; i++)
        {
            codes.Add(SupportCode.GetOrCreate(new DefaultHttpContext()));
        }

        // 30^4 ≈ 810k combinations: a handful of collisions in 200 draws would be a broken RNG.
        Assert.True(codes.Count > 190, $"Only {codes.Count} unique codes in 200 draws.");
    }

    [Fact]
    public void Missing_http_context_still_yields_a_code()
        => Assert.True(SupportCodeGenerator.IsValid(SupportCode.GetOrCreate(null)));
}
