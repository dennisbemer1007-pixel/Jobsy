using System.Net;
using System.Text;
using Jobsy.Core.Careers;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Tests.Uat;
using Jobsy.Web.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class OccupationDayInLifeTests
{
    [Fact]
    public void Prompt_forbids_invented_employers_cities_wages_and_diplomas()
    {
        var prompt = OccupationDayInLifePrompt.System;
        Assert.Contains("Verzin geen werkgever", prompt, StringComparison.Ordinal);
        Assert.Contains("geen stad", prompt, StringComparison.Ordinal);
        Assert.Contains("geen salaris", prompt, StringComparison.Ordinal);
        Assert.Contains("geen diploma", prompt, StringComparison.Ordinal);
        Assert.Contains("B1", prompt, StringComparison.Ordinal);
        Assert.Contains("Bron is dun", prompt, StringComparison.Ordinal);
        Assert.Contains("blocks", prompt, StringComparison.Ordinal);
        Assert.Contains("start", prompt, StringComparison.Ordinal);
        Assert.Contains("Pauze", prompt, StringComparison.Ordinal);
        Assert.Contains("geen klant met een naam", prompt, StringComparison.Ordinal);
        Assert.Contains("Afronden", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("kandidaatprofiel", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Facts_come_from_the_occupation_catalogue_without_wages()
    {
        var job = OccupationCatalog.Shared.All.First(item =>
            item.Desc.Length > 40
            && !item.Desc.Contains('€', StringComparison.Ordinal)
            && !item.Desc.Contains("salaris", StringComparison.OrdinalIgnoreCase)
            && !item.Desc.Contains("euro", StringComparison.OrdinalIgnoreCase));
        var facts = OccupationDayFacts.For(job.Id);
        Assert.NotNull(facts);
        var prompt = facts!.ToPrompt();
        Assert.Contains(job.Nl, prompt, StringComparison.Ordinal);
        Assert.Contains(job.Desc[..40], prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("€", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("salaris", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validator_accepts_a_short_day_grounded_in_the_source()
    {
        var facts = SampleFacts();
        Assert.True(OccupationDayInLifeValidator.TryValidate(CleanDraft(), facts, out var reasons), string.Join(", ", reasons));
    }

    [Theory]
    [InlineData("werkgever", " Je werkt bij Albert Heijn.")]
    [InlineData("stad", " Je werkt in Rotterdam.")]
    [InlineData("salaris", " Het loon is € 14 per uur.")]
    [InlineData("diploma", " Er is een mbo-diploma nodig.")]
    [InlineData("kandidaat", " Dit gaat over jouw ervaring.")]
    public void Validator_rejects_invented_facts(string reason, string extra)
    {
        var facts = SampleFacts();
        var clean = CleanDraft();
        var draft = clean with { Morning = clean.Morning + extra };
        Assert.False(OccupationDayInLifeValidator.TryValidate(draft, facts, out var reasons));
        Assert.Contains(reason, reasons);
    }

    [Fact]
    public void Validator_allows_a_city_that_is_already_in_the_source()
    {
        var facts = OccupationDayFacts.Create(
            SampleFacts().EscoId,
            SampleFacts().Uri,
            "orderpicker",
            SampleFacts().Description + " Het werk kan in Rotterdam liggen.",
            ["orders verzamelen", "tellen"],
            ["Producten uit het magazijn halen"]);
        var clean = CleanDraft();
        var draft = clean with { Morning = clean.Morning + " Soms is dat in Rotterdam." };
        Assert.True(OccupationDayInLifeValidator.TryValidate(draft, facts, out var reasons), string.Join(", ", reasons));
    }

    [Fact]
    public void Validator_accepts_six_grounded_blocks()
    {
        var facts = SampleFacts();
        Assert.True(OccupationDayInLifeValidator.TryValidate(BlockDraft(), facts, out var reasons), string.Join(", ", reasons));
    }

    [Fact]
    public void Validator_rejects_a_city_inside_a_block_and_a_short_timeline()
    {
        var facts = SampleFacts();
        var city = BlockDraft();
        var blocks = city.Blocks!.ToList();
        blocks[0] = blocks[0] with { Text = blocks[0].Text + " Dat is in Rotterdam." };
        Assert.False(OccupationDayInLifeValidator.TryValidate(city with { Blocks = blocks }, facts, out var cityReasons));
        Assert.Contains("stad", cityReasons);

        var shortDay = BlockDraft() with { Blocks = BlockDraft().Blocks!.Take(2).ToList() };
        Assert.False(OccupationDayInLifeValidator.TryValidate(shortDay, facts, out var shortReasons));
        Assert.Contains("blok", shortReasons);
    }

    [Fact]
    public void Gate_defaults_on_and_can_be_turned_off()
    {
        Assert.True(OccupationDayInLifeGate.IsEnabled(null));
        Assert.True(OccupationDayInLifeGate.IsEnabled(""));
        Assert.True(OccupationDayInLifeGate.IsEnabled("true"));
        Assert.False(OccupationDayInLifeGate.IsEnabled("false"));
    }

    [Fact]
    public async Task Lookup_returns_stored_text_and_empty_when_missing()
    {
        var id = OccupationCatalog.Shared.All[0].Id;
        await using var db = NewDb();
        var reader = new OccupationDayInLifeReader(db, Options.Create(new OccupationDayInLifeOptions()));
        var missing = await reader.GetAsync(id);
        Assert.True(missing.KnownOccupation);
        Assert.True(missing.Enabled);
        Assert.Null(missing.Day);

        db.OccupationDayInLives.Add(Stored(id, "Je start de ochtend met de eerste taak uit de lijst."));
        await db.SaveChangesAsync();
        var found = await reader.GetAsync(id);
        Assert.Equal("Je start de ochtend met de eerste taak uit de lijst.", found.Day?.Morning);

        var unknown = await reader.GetAsync("00000000-0000-0000-0000-000000000099");
        Assert.False(unknown.KnownOccupation);
    }

    [Fact]
    public async Task Disabled_flag_hides_stored_text()
    {
        var id = OccupationCatalog.Shared.All[0].Id;
        await using var db = NewDb();
        db.OccupationDayInLives.Add(Stored(id, "Dit mag niet naar buiten als de pagina uit staat."));
        await db.SaveChangesAsync();
        var reader = new OccupationDayInLifeReader(db, Options.Create(new OccupationDayInLifeOptions { Enabled = false }));
        var result = await reader.GetAsync(id);
        Assert.True(result.KnownOccupation);
        Assert.False(result.Enabled);
        Assert.Null(result.Day);
    }

    [Fact]
    public async Task Generate_skips_existing_rows_and_does_not_call_the_writer()
    {
        var id = OccupationCatalog.Shared.All[0].Id;
        await using var db = NewDb();
        db.OccupationDayInLives.Add(Stored(id, "Deze tekst blijft staan."));
        await db.SaveChangesAsync();
        var writer = new ScriptWriter("{ }", "should-not-run");
        var generator = Generator(db, writer);
        var only = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { id };
        var result = await generator.GenerateMissingAsync(5, skipEscoIds: null, only, CancellationToken.None);
        Assert.Equal(0, writer.Calls);
        var row = await db.OccupationDayInLives.SingleAsync();
        Assert.Equal("Deze tekst blijft staan.", row.Morning);
        Assert.Equal(1, result.Generated);
        Assert.True(OccupationDayTranslations.IsComplete(row.TranslationsJson, row.ContentHash));
    }

    [Fact]
    public async Task Generate_stores_a_valid_day_and_a_second_run_skips_it()
    {
        var id = OccupationCatalog.Shared.All[0].Id;
        var name = Guid.NewGuid().ToString("N");
        var json = CleanJson();
        await using (var db = NewDb(name))
        {
            var writer = new ScriptWriter(json);
            var generator = Generator(db, writer);
            var only = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { id };
            var result = await generator.GenerateMissingAsync(1, null, only, CancellationToken.None);
            Assert.Equal(1, result.Generated);
            Assert.Equal(1, writer.Calls);
            Assert.False(result.KeyMissing);
        }

        await using (var db = NewDb(name))
        {
            var writer = new ScriptWriter(json);
            var generator = Generator(db, writer);
            var only = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { id };
            var again = await generator.GenerateMissingAsync(1, null, only, CancellationToken.None);
            Assert.Equal(0, again.Generated);
            Assert.Equal(0, writer.Calls);
            Assert.Equal(1, await db.OccupationDayInLives.CountAsync());
        }
    }

    [Fact]
    public async Task Generate_copies_catalog_tasks_and_drops_model_tasks()
    {
        var job = OccupationCatalog.Shared.All.First(item =>
        {
            var known = OccupationDayFacts.For(item.Id);
            return known is not null && (known.Tasks.Count > 0 || known.Skills.Count > 0);
        });
        var facts = OccupationDayFacts.For(job.Id)!;
        var json = CleanJson().TrimEnd('}') + ",\"tasks\":[\"Verzonnen eis\"],\"skills\":[\"Nog een verzinsel\"]}";
        await using var db = NewDb();
        var generator = Generator(db, new ScriptWriter(json));
        var only = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { job.Id };
        var result = await generator.GenerateMissingAsync(1, null, only, CancellationToken.None);
        Assert.Equal(1, result.Generated);
        var row = await db.OccupationDayInLives.SingleAsync();
        Assert.DoesNotContain("Verzonnen eis", row.TasksJson, StringComparison.Ordinal);
        Assert.DoesNotContain("Nog een verzinsel", row.SkillsJson, StringComparison.Ordinal);
        if (facts.Tasks.Count > 0)
        {
            Assert.Contains(facts.Tasks[0], row.TasksJson, StringComparison.Ordinal);
        }
        else
        {
            Assert.Contains(facts.Skills[0], row.SkillsJson, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Export_roundtrip_imports_once_and_skips_the_same_hash()
    {
        var id = OccupationCatalog.Shared.All[1].Id;
        var facts = OccupationDayFacts.For(id)!;
        var draft = CleanDraft() with { TitleNl = facts.TitleNl };
        var hash = OccupationDayInLifeHash.Compute(facts.EscoId, draft);
        var document = new OccupationDayExportDocument
        {
            Locale = "nl",
            ExportedAtUtc = DateTime.UtcNow,
            Rows =
            [
                new OccupationDayExportRow
                {
                    EscoId = facts.EscoId,
                    Uri = facts.Uri,
                    TitleNl = facts.TitleNl,
                    Morning = draft.Morning,
                    Midday = draft.Midday,
                    Afternoon = draft.Afternoon,
                    Closing = draft.Closing,
                    Highlights = draft.Highlights.ToList(),
                    VariesNote = draft.VariesNote,
                    SourceModel = "gpt-4o-mini",
                    GeneratedAtUtc = DateTime.UtcNow,
                    ContentHash = hash,
                    Locale = "nl",
                    ThinSource = facts.IsThin
                }
            ]
        };

        var name = Guid.NewGuid().ToString("N");
        await using (var db = NewDb(name))
        {
            var generator = Generator(db, new ScriptWriter("{}"));
            var first = await generator.ImportAsync(document);
            Assert.Equal(1, first.Inserted);
            Assert.Equal(0, first.Rejected);
        }

        await using (var db = NewDb(name))
        {
            var generator = Generator(db, new ScriptWriter("{}"));
            var second = await generator.ImportAsync(document);
            Assert.Equal(0, second.Inserted);
            Assert.Equal(1, second.Unchanged);
            var exported = await generator.ExportAsync();
            Assert.Equal(hash, exported.Rows.Single().ContentHash);
            Assert.Equal(draft.Morning, exported.Rows.Single().Morning);
        }
    }

    [Fact]
    public async Task OpenAi_writer_stays_on_openai_even_when_the_base_url_is_mistral()
    {
        var handler = new CaptureHandler();
        var services = new ServiceCollection();
        services.AddHttpClient(OccupationDayInLifeOpenAiWriter.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        await using var provider = services.BuildServiceProvider();
        var writer = new OccupationDayInLifeOpenAiWriter(
            provider.GetRequiredService<IHttpClientFactory>(),
            new StubCredentials(apiKey: null, baseUrl: "https://api.eu.mistral.ai/v1/"),
            Options.Create(new OpenAiOptions
            {
                ApiKey = "sk-test",
                BaseUrl = "https://api.eu.mistral.ai/v1/",
                Model = "mistral-small-latest"
            }),
            Options.Create(new OccupationDayInLifeOptions { Model = "mistral-small-latest" }),
            NullLogger<OccupationDayInLifeOpenAiWriter>.Instance);

        var result = await writer.CompleteAsync(OccupationDayInLifePrompt.System, "Beroep: kok", CancellationToken.None);
        Assert.True(result.Ok);
        Assert.Equal("gpt-4o-mini", result.Model);
        Assert.NotNull(handler.Request);
        Assert.Equal("api.openai.com", handler.Request!.RequestUri!.Host);
        Assert.EndsWith("chat/completions", handler.Request.RequestUri.AbsolutePath, StringComparison.Ordinal);
        Assert.Equal("Bearer", handler.Request.Headers.Authorization?.Scheme);
        Assert.Equal("sk-test", handler.Request.Headers.Authorization?.Parameter);
        Assert.Contains("gpt-4o-mini", handler.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("mistral", handler.Body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Verzin geen werkgever", handler.Body, StringComparison.Ordinal);
    }

    [Fact]
    public void Translation_targets_every_supported_ui_language_except_dutch()
    {
        Assert.Equal(["en", "pl", "ro", "ar"], OccupationDayTranslations.TargetLanguages);
        var prompt = OccupationDayTranslationPrompt.System("pl");
        Assert.Contains("B1", prompt, StringComparison.Ordinal);
        Assert.Contains("Do not add", prompt, StringComparison.Ordinal);
        Assert.Contains("city", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("wage", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("diploma", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Translation_validator_rejects_a_city_missing_from_the_dutch_source()
    {
        var source = CleanDraft();
        var translated = source with
        {
            TitleNl = "order picker",
            Morning = "You start with your crate and your list. Then you walk to the first rack in Rotterdam."
        };
        Assert.False(OccupationDayInLifeValidator.TryValidateTranslation(source, translated, out var reasons));
        Assert.Contains("stad", reasons);
    }

    [Fact]
    public async Task Reader_uses_a_stored_translation_and_falls_back_to_dutch()
    {
        var id = OccupationCatalog.Shared.All[0].Id;
        await using var db = NewDb();
        var row = Stored(id, "Je start de ochtend met de eerste taak uit de lijst.");
        row.ContentHash = "hash-nl";
        row.TranslationsJson = OccupationDayTranslations.Serialize(new Dictionary<string, OccupationDayStoredTranslation>
        {
            ["pl"] = new()
            {
                Title = "kompletacja zamówień",
                Morning = "Rano bierzesz listę i zbierasz pierwsze produkty.",
                Midday = "W środku dnia liczysz produkty i wkładasz je do skrzynki.",
                Afternoon = "Po południu sprawdzasz listę i uzupełniasz braki.",
                Closing = "Na końcu oddajesz skrzynki i odkładasz rzeczy.",
                Highlights = ["Zbieranie", "Liczenie"],
                Varies = "Kolejność zadań różni się u każdego pracodawcy.",
                SourceHash = "hash-nl"
            }
        });
        db.OccupationDayInLives.Add(row);
        await db.SaveChangesAsync();
        var reader = new OccupationDayInLifeReader(db, Options.Create(new OccupationDayInLifeOptions()));

        var polish = await reader.GetAsync(id, "pl");
        Assert.Equal("Rano bierzesz listę i zbierasz pierwsze produkty.", polish.Day?.Morning);
        Assert.Equal("kompletacja zamówień", polish.Day?.TitleNl);

        var english = await reader.GetAsync(id, "en");
        Assert.Equal(row.Morning, english.Day?.Morning);

        var dutch = await reader.GetAsync(id, "nl");
        Assert.Equal(row.Morning, dutch.Day?.Morning);
    }

    [Fact]
    public async Task Generate_stores_each_language_once()
    {
        var id = OccupationCatalog.Shared.All[2].Id;
        var name = Guid.NewGuid().ToString("N");
        var translator = new EchoTranslator();
        await using (var db = NewDb(name))
        {
            var writer = new ScriptWriter(CleanJson());
            var generator = Generator(db, writer, translator);
            var only = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { id };
            var result = await generator.GenerateMissingAsync(1, null, only, CancellationToken.None);
            Assert.Equal(1, result.Generated);
            Assert.Equal(1, writer.Calls);
            Assert.False(result.KeyMissing);
            Assert.Equal(OccupationDayTranslations.TargetLanguages.Count, translator.Calls);
            var row = await db.OccupationDayInLives.SingleAsync();
            Assert.True(OccupationDayTranslations.IsComplete(row.TranslationsJson, row.ContentHash));
            Assert.True(OccupationDayTranslations.TryGet(row.TranslationsJson, "ar", row.ContentHash, out var arabic));
            Assert.Contains("ar", arabic.Morning, StringComparison.Ordinal);
        }

        await using (var db = NewDb(name))
        {
            var writer = new ScriptWriter(CleanJson());
            var againTranslator = new EchoTranslator();
            var generator = Generator(db, writer, againTranslator);
            var only = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { id };
            var again = await generator.GenerateMissingAsync(1, null, only, CancellationToken.None);
            Assert.Equal(0, again.Generated);
            Assert.Equal(0, writer.Calls);
            Assert.Equal(0, againTranslator.Calls);
        }
    }

    [Fact]
    public void Candidate_page_shows_an_honest_empty_state_and_does_not_call_openai()
    {
        var root = RepoRoot.Find();
        var page = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/OccupationDayPage.razor"));
        var controller = File.ReadAllText(Path.Combine(root, "Jobsy.Api/Controllers/OccupationDayInLifeController.cs"));
        var reader = File.ReadAllText(Path.Combine(root, "Jobsy.Infrastructure/Services/OccupationDayInLifeReader.cs"));
        var writer = File.ReadAllText(Path.Combine(root, "Jobsy.Infrastructure/Services/OccupationDayInLifeOpenAiWriter.cs"));
        Assert.Contains("Day.Empty", page, StringComparison.Ordinal);
        Assert.Contains("Day.Invite", page, StringComparison.Ordinal);
        Assert.Contains("day-life__timeline", page, StringComparison.Ordinal);
        Assert.Contains("Day.FitTitle", page, StringComparison.Ordinal);
        Assert.Contains("Day.Tasks", page, StringComparison.Ordinal);
        Assert.Contains("Day.Skills", page, StringComparison.Ordinal);
        Assert.Contains("day-life__cloud", page, StringComparison.Ordinal);
        Assert.Contains("LobsyBubble", page, StringComparison.Ordinal);
        Assert.Contains("/images/brand/mascot-coach.webp", page, StringComparison.Ordinal);
        Assert.DoesNotContain("LobsyCoachAvatar", page, StringComparison.Ordinal);
        Assert.Contains("Culture.Language", page, StringComparison.Ordinal);
        var readerSource = reader;
        Assert.DoesNotContain("IOccupationDayInLifeTranslator", readerSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ITranslationService", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("OpenAiFeature", controller, StringComparison.Ordinal);
        var translator = File.ReadAllText(Path.Combine(root, "Jobsy.Infrastructure/Services/OccupationDayInLifeTranslator.cs"));
        Assert.Contains("OpenAiFeature.Translation", translator, StringComparison.Ordinal);
        Assert.DoesNotContain("OccupationDayInLifeTranslator", readerSource, StringComparison.Ordinal);
        Assert.Equal("Nog niet beschikbaar.", UiStrings.Get("Day.Empty", "nl"));
        Assert.Equal("Een dag als {0}", UiStrings.Get("Day.Title", "nl"));
        Assert.Equal("Kom, ik neem je mee door een gewone werkdag.", UiStrings.Get("Day.Invite", "nl"));
        Assert.Equal("Past dit bij jou?", UiStrings.Get("Day.FitTitle", "nl"));
        Assert.Equal("Wat je vaak doet", UiStrings.Get("Day.Tasks", "nl"));
        Assert.Equal("Wat dit werk vraagt", UiStrings.Get("Day.Skills", "nl"));
        Assert.DoesNotContain("chat/completions", page, StringComparison.Ordinal);
        Assert.DoesNotContain("OpenAI", page, StringComparison.Ordinal);
        Assert.DoesNotContain("IOccupationDayInLifeWriter", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("GenerateMissing", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", reader, StringComparison.Ordinal);
        Assert.DoesNotContain("chat/completions", reader, StringComparison.Ordinal);
        Assert.DoesNotContain("MistralOptions", writer, StringComparison.Ordinal);
        Assert.DoesNotContain("AiProviderChoice", writer, StringComparison.Ordinal);
        Assert.Contains("ForceOpenAiBaseUrl", writer, StringComparison.Ordinal);
        Assert.Contains("OccupationDayLink", File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Candidate/CareerCompassPanel.razor")), StringComparison.Ordinal);
        Assert.Contains("OccupationDayLink", File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Candidate/RoleFitCheckPanel.razor")), StringComparison.Ordinal);
        Assert.Contains("OccupationDayLink", File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Candidate/Passport/PassportFitTab.razor")), StringComparison.Ordinal);
    }

    private static OccupationDayFacts SampleFacts()
        => OccupationDayFacts.Create(
            "11111111-1111-1111-1111-111111111111",
            "http://data.europa.eu/esco/occupation/11111111-1111-1111-1111-111111111111",
            "orderpicker",
            "Een orderpicker verzamelt producten in een magazijn. Je loopt langs stellingen en legt goederen in een bak. Je controleert aantallen en zet de bak klaar voor de volgende stap.",
            ["orders verzamelen", "tellen"],
            ["Producten uit het magazijn halen", "Bakken klaarzetten"]);

    private static OccupationDayDraft CleanDraft()
        => new(
            "orderpicker",
            "Je begint met het klaarzetten van je bak en je lijst. Daarna loop je naar de eerste stelling.",
            "In het midden van de dag ga je door met verzamelen. Je telt de producten en legt ze in de bak.",
            "In de middag zet je de bakken klaar. Je controleert de lijst en vult aan wat nog ontbreekt.",
            "Aan het eind geef je de bakken door. Je vinkt de lijst af en zet je spullen terug.",
            ["Producten verzamelen", "Aantallen controleren", "Bakken klaarzetten"],
            "De volgorde van taken verschilt per werkgever.");

    private static OccupationDayDraft BlockDraft()
    {
        var blocks = new List<OccupationDayBlock>
        {
            new("start", "Start", "Je start met je lijst en je bak. Je kijkt welke producten eerst gaan."),
            new("morning", "Ochtend", "In de ochtend loop je langs de stellingen. Je haalt de producten uit het magazijn."),
            new("plan", "Plannen", "Je plant de rest van de ronde. Je telt wat nog op de lijst staat."),
            new("pause", "Pauze", "Je neemt pauze. Daarna pak je de lijst weer op en ga je verder."),
            new("afternoon", "Middag", "In de middag zet je de bakken klaar. Je vult aan wat nog ontbreekt."),
            new("close", "Afronden", "Aan het eind vink je de lijst af. Je zet je spullen terug.")
        };
        return OccupationDayBlocks.WithDerived(new OccupationDayDraft(
            "orderpicker",
            "Je begint met het klaarzetten van je bak en je lijst. Daarna loop je naar de eerste stelling.",
            "In het midden van de dag ga je door met verzamelen. Je telt de producten en legt ze in de bak.",
            "In de middag zet je de bakken klaar. Je controleert de lijst en vult aan wat nog ontbreekt.",
            "Aan het eind geef je de bakken door. Je vinkt de lijst af en zet je spullen terug.",
            ["Producten verzamelen", "Aantallen controleren", "Bakken klaarzetten"],
            "De volgorde van taken verschilt per werkgever.",
            blocks,
            ["orders verzamelen", "tellen"],
            ["Producten uit het magazijn halen"]));
    }

    private static string CleanJson()
    {
        var draft = CleanDraft();
        return "{"
            + "\"morning\":" + System.Text.Json.JsonSerializer.Serialize(draft.Morning) + ","
            + "\"midday\":" + System.Text.Json.JsonSerializer.Serialize(draft.Midday) + ","
            + "\"afternoon\":" + System.Text.Json.JsonSerializer.Serialize(draft.Afternoon) + ","
            + "\"closing\":" + System.Text.Json.JsonSerializer.Serialize(draft.Closing) + ","
            + "\"highlights\":" + System.Text.Json.JsonSerializer.Serialize(draft.Highlights) + ","
            + "\"varies\":" + System.Text.Json.JsonSerializer.Serialize(draft.VariesNote)
            + "}";
    }

    private static OccupationDayInLife Stored(string escoId, string morning)
    {
        var job = OccupationCatalog.Shared.Get(escoId)!;
        return new OccupationDayInLife
        {
            Id = Guid.NewGuid(),
            EscoId = job.Id,
            Uri = job.Uri,
            TitleNl = job.Nl,
            Morning = morning,
            Midday = "Daarna ga je door met de taken uit de lijst.",
            Afternoon = "In de middag ruim je je plek op en kijk je de lijst na.",
            Closing = "Aan het eind vink je de lijst af en zet je je spullen terug.",
            HighlightsJson = "[\"Eerste taak\",\"Plek opruimen\"]",
            VariesNote = "De volgorde verschilt per werkgever.",
            SourceModel = "gpt-4o-mini",
            GeneratedAtUtc = DateTime.UtcNow,
            ContentHash = "abc",
            Locale = "nl"
        };
    }

    private static JobsyDbContext NewDb(string? name = null)
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(name ?? Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }

    private static OccupationDayInLifeGenerator Generator(
        JobsyDbContext db,
        IOccupationDayInLifeWriter writer,
        IOccupationDayInLifeTranslator? translator = null)
        => new(
            db,
            writer,
            translator ?? new EchoTranslator(),
            Options.Create(new OccupationDayInLifeOptions { DelayMilliseconds = 0 }),
            NullLogger<OccupationDayInLifeGenerator>.Instance);

    private sealed class EchoTranslator : IOccupationDayInLifeTranslator
    {
        public int Calls { get; private set; }

        public Task<OccupationDayTranslateResult> TranslateAsync(
            OccupationDayDraft source,
            string targetLanguage,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            var tag = " " + targetLanguage;
            var draft = source with
            {
                TitleNl = source.TitleNl + tag,
                Morning = source.Morning + tag,
                Midday = source.Midday + tag,
                Afternoon = source.Afternoon + tag,
                Closing = source.Closing + tag,
                VariesNote = source.VariesNote + tag,
                Highlights = source.Highlights.Select(line => line + tag).ToList(),
                Blocks = source.Blocks is { Count: > 0 }
                    ? source.Blocks.Select(block => block with { Label = block.Label + tag, Text = block.Text + tag }).ToList()
                    : source.Blocks,
                Tasks = source.Tasks?.Select(line => line + tag).ToList(),
                Skills = source.Skills?.Select(line => line + tag).ToList()
            };
            return Task.FromResult(new OccupationDayTranslateResult(true, draft, null, "test-translator", false));
        }
    }

    private sealed class ScriptWriter(string json, string? error = null) : IOccupationDayInLifeWriter
    {
        public int Calls { get; private set; }

        public Task<OccupationDayWriteResult> CompleteAsync(
            string systemPrompt,
            string userPrompt,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(error is null
                ? new OccupationDayWriteResult(true, json, null, "gpt-4o-mini")
                : new OccupationDayWriteResult(false, null, error, "gpt-4o-mini"));
        }
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            var payload = """{"choices":[{"message":{"content":"{\"morning\":\"ok\"}"}}]}""";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class StubCredentials(string? apiKey, string? baseUrl) : IIntegrationCredentialService
    {
        public Task<IntegrationCredentialView?> GetAsync(IntegrationKey key, CancellationToken cancellationToken = default)
            => Task.FromResult<IntegrationCredentialView?>(null);

        public Task<IReadOnlyList<IntegrationCredentialView>> GetConfigurableAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<IntegrationCredentialView>>([]);

        public Task<IntegrationCredentialView> UpsertAsync(
            IntegrationKey key,
            IntegrationCredentialUpdate update,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task SavePingResultAsync(IntegrationKey key, bool ok, string message, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<string?> GetRawApiKeyAsync(IntegrationKey key, CancellationToken cancellationToken = default)
            => Task.FromResult(apiKey);

        public Task<string?> GetModelAsync(IntegrationKey key, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);

        public Task<string?> GetBaseUrlAsync(IntegrationKey key, CancellationToken cancellationToken = default)
            => Task.FromResult(baseUrl);

        public Task<IntegrationCredentialSecrets?> GetSecretsAsync(IntegrationKey key, CancellationToken cancellationToken = default)
            => Task.FromResult<IntegrationCredentialSecrets?>(null);
    }
}
