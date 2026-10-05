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
        Assert.Equal(0, result.Generated);
        Assert.Equal(0, writer.Calls);
        Assert.Equal("Deze tekst blijft staan.", (await db.OccupationDayInLives.SingleAsync()).Morning);
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
    public void Candidate_page_shows_an_honest_empty_state_and_does_not_call_openai()
    {
        var root = RepoRoot.Find();
        var page = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/OccupationDayPage.razor"));
        var controller = File.ReadAllText(Path.Combine(root, "Jobsy.Api/Controllers/OccupationDayInLifeController.cs"));
        var reader = File.ReadAllText(Path.Combine(root, "Jobsy.Infrastructure/Services/OccupationDayInLifeReader.cs"));
        var writer = File.ReadAllText(Path.Combine(root, "Jobsy.Infrastructure/Services/OccupationDayInLifeOpenAiWriter.cs"));
        Assert.Contains("Day.Empty", page, StringComparison.Ordinal);
        Assert.Equal("Nog niet beschikbaar.", UiStrings.Get("Day.Empty", "nl"));
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
            "Rond de middag ga je door met verzamelen. Je telt de producten en legt ze in de bak.",
            "Aan het eind zet je de bakken klaar. Je ruimt je plek op en sluit de lijst af.",
            ["Producten verzamelen", "Aantallen controleren", "Bakken klaarzetten"],
            "De volgorde van taken verschilt per werkgever.");

    private static string CleanJson()
    {
        var draft = CleanDraft();
        return "{"
            + "\"morning\":" + System.Text.Json.JsonSerializer.Serialize(draft.Morning) + ","
            + "\"midday\":" + System.Text.Json.JsonSerializer.Serialize(draft.Midday) + ","
            + "\"afternoon\":" + System.Text.Json.JsonSerializer.Serialize(draft.Afternoon) + ","
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
            Afternoon = "Aan het eind ruim je je plek op en sluit je af.",
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

    private static OccupationDayInLifeGenerator Generator(JobsyDbContext db, IOccupationDayInLifeWriter writer)
        => new(
            db,
            writer,
            Options.Create(new OccupationDayInLifeOptions { DelayMilliseconds = 0 }),
            NullLogger<OccupationDayInLifeGenerator>.Instance);

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
