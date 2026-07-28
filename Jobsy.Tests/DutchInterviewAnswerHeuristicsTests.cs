using Jobsy.Infrastructure.Services;

namespace Jobsy.Tests;

public class DutchInterviewAnswerHeuristicsTests
{
    [Theory]
    [InlineData("je bent een idioot")]
    [InlineData("Rot op met die vacature")]
    [InlineData("dit is kut en stomme onzin")]
    public void LooksInsulting_detects_rude_answers(string answer)
    {
        Assert.True(DutchInterviewAnswerHeuristics.LooksInsulting(answer));
    }

    [Theory]
    [InlineData("Ik heb vorige zomer bij de bakkerij geholpen met inpakken.")]
    [InlineData("Toen het druk was, hielp ik eerst de klant en daarna de voorraad.")]
    public void LooksInsulting_allows_normal_answers(string answer)
    {
        Assert.False(DutchInterviewAnswerHeuristics.LooksInsulting(answer));
    }

    [Fact]
    public void FriendlyToneRedirect_mentions_nette_reactie()
    {
        var text = DutchInterviewAnswerHeuristics.FriendlyToneRedirect("inpakken");
        Assert.Contains("geen nette reactie", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("inpakken", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LooksVague_flags_short_answers()
    {
        Assert.True(DutchInterviewAnswerHeuristics.LooksVague("Ik wil graag."));
        Assert.False(DutchInterviewAnswerHeuristics.LooksVague(
            "Toen het druk was in de winkel, deed ik eerst de kassa en hielp daarna een klant. Daardoor bleef de rij kort."));
    }

    [Fact]
    public void BuildRewriteSuggestion_for_insult_starts_with_probeer_zo()
    {
        var rewrite = DutchInterviewAnswerHeuristics.BuildRewriteSuggestion("je bent een idioot", "klantcontact");
        Assert.StartsWith("Probeer zo:", rewrite, StringComparison.Ordinal);
        Assert.Contains("klantcontact", rewrite, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExtractQuote_returns_snippet_around_star_cue()
    {
        var quote = DutchInterviewAnswerHeuristics.ExtractQuote(
            "Vorige week was het druk. Toen ik hielp bij de kassa bleef de sfeer rustig.");
        Assert.False(string.IsNullOrWhiteSpace(quote));
        Assert.Contains("Toen", quote, StringComparison.OrdinalIgnoreCase);
    }
}
