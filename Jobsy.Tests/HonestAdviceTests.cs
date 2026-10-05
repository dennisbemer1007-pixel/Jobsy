using System.Text;
using System.Text.Json;
using Jobsy.Core.Careers;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Web.Admin;
using Jobsy.Web.Localization;

namespace Jobsy.Tests;

public class HonestAdviceTests
{
    private const string CloudId = "781a6350-e686-45b9-b075-e4c8d5a05ff7";

    [Fact]
    public void Flag_defaults_off_and_admin_copy_exists()
    {
        Assert.False(FeatureFlagSnapshot.Defaults.HonestAdviceEnabled);
        Assert.False(new FeatureFlagSnapshot(false, true).IsEnabled(PlatformFeature.HonestAdvice));
        Assert.True(new FeatureFlagSnapshot(false, true, HonestAdviceEnabled: true)
            .IsEnabled(PlatformFeature.HonestAdvice));

        var entry = PlatformSettingsCatalog.Entries.Single(item => item.Key == "HonestAdviceEnabled");
        Assert.False(entry.Read(new PlatformFeatureSnapshot(false, true, "http://localhost", null)) is true);
        foreach (var lang in new[] { "nl", "en", "pl", "ro", "ar" })
        {
            var title = UiStrings.Get("Advice.Title", lang);
            var admin = UiStrings.Get("AdminSettings.HonestAdvice.Enabled.Title", lang);
            Assert.False(string.IsNullOrWhiteSpace(title));
            Assert.False(string.IsNullOrWhiteSpace(admin));
        }
    }

    [Fact]
    public void Get_is_offline_and_returns_the_seed_for_a_software_job()
    {
        var source = File.ReadAllText(Path.Combine(
            TestRepo.FindRoot(), "Jobsy.Core/Careers/HonestAdviceService.cs"));
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("api.openai.com", source, StringComparison.Ordinal);

        var advice = HonestAdviceService.Shared.Get(CloudId);
        Assert.NotNull(advice);
        Assert.Contains("AI", advice!.Text, StringComparison.Ordinal);
        Assert.Contains("peildatum", advice.Source, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(advice.Peildatum));
        Assert.Null(HonestAdviceService.Shared.Get("niet-een-beroep"));
        Assert.Null(HonestAdviceService.Shared.Get(null));
    }

    [Fact]
    public void Seed_is_grounded_and_avoids_forbidden_words()
    {
        Assert.True(HonestAdviceService.Shared.Ids.Count >= 5);
        foreach (var id in HonestAdviceService.Shared.Ids)
        {
            var entry = HonestAdviceService.Shared.Find(id);
            var facts = HonestAdviceFacts.TryFor(id);
            Assert.NotNull(entry);
            Assert.NotNull(facts);
            Assert.Null(HonestAdviceValidator.RejectionReason(entry!.Text, facts!));
            Assert.Equal(facts!.SourceHash, entry.SourceHash);
            Assert.NotNull(HonestAdviceService.Shared.Get(id));
            foreach (var word in new[] { "verdwijnt", "verdwijnen", "geen toekomst", "overbodig", "ontslag", "werkloos" })
            {
                Assert.DoesNotContain(word, entry.Text, StringComparison.OrdinalIgnoreCase);
            }

            var dutchHash = HonestAdviceFacts.Hash(entry.Text);
            foreach (var lang in new[] { "en", "pl", "ro", "ar" })
            {
                Assert.True(entry.Translations.ContainsKey(lang));
                var locale = entry.Translations[lang];
                Assert.Equal(dutchHash, locale.SourceHash);
                Assert.Null(HonestAdviceValidator.RejectionReason(locale.Text, facts, lang));
                var served = HonestAdviceService.Shared.Get(id, lang);
                Assert.Equal(lang, served!.Language);
                Assert.Equal(locale.Text, served.Text);
            }
        }
    }

    [Fact]
    public void Get_serves_dutch_when_the_locale_is_missing_or_stale()
    {
        var dutch = HonestAdviceService.Shared.Get(CloudId);
        Assert.Equal("nl", dutch!.Language);
        Assert.Equal(dutch.Text, HonestAdviceService.Shared.Get(CloudId, "nl")!.Text);
        Assert.Equal("nl", HonestAdviceService.Shared.Get(CloudId, "fr")!.Language);
        Assert.Equal(dutch.Text, HonestAdviceService.Shared.Get(CloudId, "fr")!.Text);

        var entry = HonestAdviceService.Shared.Find(CloudId)!;
        var stale = JsonSerializer.Serialize(new
        {
            entries = new Dictionary<string, object>
            {
                [CloudId] = new
                {
                    text = entry.Text,
                    generatedAt = entry.GeneratedAt,
                    model = entry.Model,
                    peildatum = entry.Peildatum,
                    sourceHash = entry.SourceHash,
                    translations = new Dictionary<string, object>
                    {
                        ["en"] = new
                        {
                            text = "Many tasks can change because of AI. The work stays useful if you learn to work with those tools.",
                            generatedAt = "2026-10-05T19:00:00Z",
                            model = "seed",
                            sourceHash = "not-the-dutch-text"
                        }
                    }
                }
            }
        });
        using var staleStream = new MemoryStream(Encoding.UTF8.GetBytes(stale));
        var staleService = HonestAdviceService.Load(staleStream);
        var staleResult = staleService.Get(CloudId, "en");
        Assert.Equal("nl", staleResult!.Language);
        Assert.Equal(entry.Text, staleResult.Text);

        var invented = JsonSerializer.Serialize(new
        {
            entries = new Dictionary<string, object>
            {
                [CloudId] = new
                {
                    text = entry.Text,
                    generatedAt = entry.GeneratedAt,
                    model = entry.Model,
                    peildatum = entry.Peildatum,
                    sourceHash = entry.SourceHash,
                    translations = new Dictionary<string, object>
                    {
                        ["en"] = new
                        {
                            text = "There are 99 new jobs here. The work stays useful if you learn to work with AI.",
                            generatedAt = "2026-10-05T19:00:00Z",
                            model = "seed",
                            sourceHash = HonestAdviceFacts.Hash(entry.Text)
                        }
                    }
                }
            }
        });
        using var inventedStream = new MemoryStream(Encoding.UTF8.GetBytes(invented));
        var inventedResult = HonestAdviceService.Load(inventedStream).Get(CloudId, "en");
        Assert.Equal("nl", inventedResult!.Language);
        Assert.Equal(entry.Text, inventedResult.Text);
    }

    [Theory]
    [InlineData("en", "This work will disappear soon. People can still learn new tasks.")]
    [InlineData("pl", "Ta praca znika szybko. Ludzie mogą się dalej uczyć nowych zadań.")]
    [InlineData("ro", "Această muncă dispare curând. Oamenii pot învăța în continuare.")]
    [InlineData("ar", "هذا العمل يختفي قريباً. يمكن للناس أن يتعلموا مهاماً جديدة.")]
    public void Validator_rejects_doom_words_in_stored_languages(string language, string text)
    {
        Assert.Equal("forbidden-word", HonestAdviceValidator.RejectionReason(text, Grounded(), language));
    }

    [Theory]
    [InlineData("verdwijnt")]
    [InlineData("verdwijnen")]
    [InlineData("geen toekomst")]
    [InlineData("overbodig")]
    [InlineData("ontslag")]
    [InlineData("werkloos")]
    public void Validator_rejects_forbidden_words(string word)
    {
        var facts = Grounded();
        var text = $"Dit werk {word} niet zomaar. Je kunt nog leren. Mensen blijven nodig.";
        Assert.Equal("forbidden-word", HonestAdviceValidator.RejectionReason(text, facts));
    }

    [Fact]
    public void Validator_rejects_empty_length_and_invented_numbers()
    {
        var facts = Grounded();
        Assert.Equal("empty", HonestAdviceValidator.RejectionReason("  ", facts));
        Assert.Equal("too-few-sentences", HonestAdviceValidator.RejectionReason("Alleen één zin over dit werk.", facts));
        Assert.Equal("too-long", HonestAdviceValidator.RejectionReason(new string('a', 200) + ". " + new string('b', 300) + ".", facts));
        Assert.Equal(
            "invented-number",
            HonestAdviceValidator.RejectionReason(
                "Er komen 40 banen vrij. Je kunt dit werk leren. Mensen blijven nodig.",
                facts));
        Assert.Equal(
            "invented-claim",
            HonestAdviceValidator.RejectionReason(
                "Je hebt een diploma nodig. Je kunt dit leren. Mensen blijven nodig.",
                facts));
        Assert.Null(HonestAdviceValidator.RejectionReason(
            "Veel taken kunnen door AI veranderen. Het blijft nuttig als je leert met AI werken. Er is vraag naar mensen.",
            facts));
    }

    [Fact]
    public void Thin_outlook_may_only_store_the_short_honest_line()
    {
        var thin = new HonestAdviceFacts("x", "Beroep", "Korte omschrijving.", null, null, [], [], [], "1 september 2026");
        Assert.False(thin.EnoughToAdvise);
        Assert.Null(HonestAdviceValidator.RejectionReason(HonestAdviceValidator.TooLittle, thin));
        Assert.Equal(
            "ungrounded",
            HonestAdviceValidator.RejectionReason(
                "Dit werk blijft nuttig. Je kunt het leren. Er is veel vraag.",
                thin));
    }

    [Fact]
    public void Coach_may_quote_stored_advice_and_not_a_parallel_line()
    {
        var stored = HonestAdviceService.Shared.Get(CloudId)!;
        var sheet = CandidateFactSheet.Personal([], [], [], [stored.Text]);
        Assert.Equal(
            "unknown-advice",
            CandidateFactGuard.RejectionReason(
                "Het is slim om met AI te leren werken in dit beroep.",
                sheet));

        sheet.RememberHonestAdvice([stored.Text]);
        Assert.Null(CandidateFactGuard.RejectionReason(stored.Text, sheet));
        Assert.Equal(
            "unknown-advice",
            CandidateFactGuard.RejectionReason(
                "AI verandert alles en je moet iets anders kiezen.",
                sheet));
    }

    [Fact]
    public void Pages_show_the_block_next_to_the_outlook()
    {
        var root = TestRepo.FindRoot();
        foreach (var relative in new[]
                 {
                     "Jobsy.Web/Components/Candidate/CareerCompassPanel.razor",
                     "Jobsy.Web/Components/Candidate/RoleFitCheckPanel.razor",
                     "Jobsy.Web/Components/Candidate/Passport/PassportFitTab.razor",
                     "Jobsy.Web/Components/Candidate/CurrentJobTipBlock.razor"
                 })
        {
            var source = File.ReadAllText(Path.Combine(root, relative));
            Assert.Contains("HonestAdviceBlock", source, StringComparison.Ordinal);
            Assert.Contains("PlatformFeature.HonestAdvice", source, StringComparison.Ordinal);
            Assert.Contains("Culture.Language", source, StringComparison.Ordinal);
        }

        var prompt = File.ReadAllText(Path.Combine(root, "tools/occupations/HonestAdviceGen/Program.cs"));
        Assert.Contains("MISTRAL_API_KEY", prompt, StringComparison.Ordinal);
        Assert.Contains("OPENAI_API_KEY", prompt, StringComparison.Ordinal);
        Assert.Contains("HonestAdvicePrompt.System", prompt, StringComparison.Ordinal);
        Assert.Contains("--translate", prompt, StringComparison.Ordinal);
        Assert.Contains("HonestAdvicePrompt.TranslateSystem", prompt, StringComparison.Ordinal);
        Assert.Contains("B1", HonestAdvicePrompt.System, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", File.ReadAllText(Path.Combine(root, "Jobsy.Core/Careers/HonestAdviceService.cs")), StringComparison.Ordinal);
    }

    private static HonestAdviceFacts Grounded()
        => new(
            "test",
            "softwareontwikkelaar",
            "Bouwt software.",
            "Tot 2030 hebben werkgevers naar verwachting moeite om genoeg mensen te vinden voor dit soort werk.",
            "Veel taken kunnen door AI veranderen. Het werk gaat er waarschijnlijk anders uitzien.",
            [],
            [],
            ["softwarearchitectuur definiëren"],
            "1 september 2026");
}
