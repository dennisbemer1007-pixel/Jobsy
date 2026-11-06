using Jobsy.Web.Scholen;

namespace Jobsy.Tests.Scholen;

public class SpokenTextTests
{
    [Fact]
    public void Join_adds_a_sentence_break_only_when_the_previous_part_has_none()
    {
        Assert.Equal(
            "Zo werkt het. Je gaat op reis door 4 werelden.",
            SpokenText.Join("Zo werkt het", "Je gaat op reis door 4 werelden."));
        Assert.Equal(
            "Wat wil jij later worden? Je helpt graag anderen.",
            SpokenText.Join("Wat wil jij later worden?", "Je helpt graag anderen."));
        Assert.Equal(
            "Goed gedaan! Je antwoorden zijn bewaard.",
            SpokenText.Join("Goed gedaan!", "Je antwoorden zijn bewaard."));
        Assert.Equal(
            "Wil je stoppen? Druk op pauze.",
            SpokenText.Join("Wil je stoppen?", "Druk op pauze."));
        Assert.Equal(
            "Stel je voor… je komt in een nieuwe klas.",
            SpokenText.Join("Stel je voor…", "je komt in een nieuwe klas."));
    }

    [Fact]
    public void Join_skips_blank_parts()
    {
        Assert.Equal("Klaar. Verder.", SpokenText.Join("Klaar.", "  ", null, "Verder."));
    }

    [Fact]
    public void WithoutRepeatedLead_drops_a_heading_that_the_body_repeats()
    {
        Assert.Equal(
            "met je code. Ga verder of stop.",
            SpokenText.WithoutRepeatedLead(
                "Je bent al ingelogd",
                "Je bent al ingelogd met je code. Ga verder of stop."));
        Assert.Equal(
            "je komt in een nieuwe klas.",
            SpokenText.WithoutRepeatedLead(
                "Stel je voor…",
                "Stel je voor… je komt in een nieuwe klas."));
        Assert.Equal(
            "een vriendin zegt dat het prima gaat.",
            SpokenText.WithoutRepeatedLead(
                "Stel je voor…",
                "Stel je voor: een vriendin zegt dat het prima gaat."));
        Assert.Equal(
            "Je helpt graag anderen.",
            SpokenText.WithoutRepeatedLead("Wat wil jij later worden?", "Je helpt graag anderen."));
    }
}
