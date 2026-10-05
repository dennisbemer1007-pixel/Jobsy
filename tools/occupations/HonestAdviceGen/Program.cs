using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Core.Careers;

// Offline batch. The candidate app reads the JSON and never calls a model.
//
// Env (no secrets in the repo):
//   Ai__Provider or AI_PROVIDER          OpenAI (default) or Mistral
//   OpenAI__ApiKey or OPENAI_API_KEY
//   OpenAI__Model                        default gpt-4o-mini
//   OpenAI__BaseUrl                      default https://api.openai.com/v1/
//   Mistral__ApiKey or MISTRAL_API_KEY
//   Mistral__Model                       default mistral-small-latest
//   Mistral__BaseUrl                     default https://api.eu.mistral.ai/v1/
//
//   dotnet run --project tools/occupations/HonestAdviceGen -- --stamp
//   dotnet run --project tools/occupations/HonestAdviceGen -- --ids <esco-id>[,...]
//   dotnet run --project tools/occupations/HonestAdviceGen -- --all --delay-ms 500
//   dotnet run --project tools/occupations/HonestAdviceGen -- --translate
//   --force regenerates even when the source hash is unchanged.

var options = RunOptions.Parse(args);
if (options.Help || (!options.Stamp && !options.Check && !options.DryRun && !options.Translate && !options.All && options.Ids.Count == 0 && options.Limit is null))
{
    Console.WriteLine("""
        Eerlijk advies, één keer per beroep. Schrijft Jobsy.Core/Data/Occupations/honest_advice.nl.json.

          --stamp          hash en peildatum bijwerken, geen AI
          --check          faal als tekst of hash niet bij de feiten past
          --dry-run        feiten tonen, niets schrijven en geen AI
          --ids a,b        alleen deze ESCO-id's
          --limit N        eerste N beroepen die nog advies nodig hebben
          --all            alle beroepen in de catalogus
          --translate      nl → en, pl, ro, ar. Een keer opslaan. Geen live vertaling.
          --force          opnieuw, ook als de hash gelijk is
          --delay-ms 500   pauze tussen AI-aanroepen

        Sleutel: OpenAI__ApiKey of OPENAI_API_KEY, of Mistral__ApiKey of MISTRAL_API_KEY.
        Provider: Ai__Provider = OpenAI (standaard) of Mistral.
        """);
    return options.Help ? 0 : 1;
}

var path = options.Path;
var file = Load(path);
if (options.Stamp)
{
    return Stamp(file, path);
}

if (options.Check)
{
    return Check(file);
}

if (options.Translate)
{
    return await TranslateFileAsync(file, path, options);
}

var targets = Targets(file, options);
if (options.DryRun)
{
    foreach (var facts in targets)
    {
        Console.WriteLine(facts.EscoId + " " + facts.Title);
        Console.WriteLine(facts.EnoughToAdvise ? HonestAdvicePrompt.User(facts) : HonestAdviceValidator.TooLittle);
        Console.WriteLine("---");
    }

    Console.WriteLine(targets.Count + " beroepen.");
    return 0;
}

var needingModel = targets.Where(facts => facts.EnoughToAdvise && NeedsWork(file, facts, options.Force)).ToList();
HttpClient? client = null;
var model = "";
if (needingModel.Count > 0)
{
    client = BuildClient(out model);
    if (client is null)
    {
        return 2;
    }
}

var wrote = 0;
foreach (var facts in targets)
{
    if (!options.Force && file.Entries.TryGetValue(facts.EscoId, out var existing)
        && string.Equals(existing.SourceHash, facts.SourceHash, StringComparison.Ordinal)
        && HonestAdviceValidator.RejectionReason(existing.Text, facts) is null)
    {
        continue;
    }

    string text;
    string usedModel;
    if (!facts.EnoughToAdvise)
    {
        text = HonestAdviceValidator.TooLittle;
        usedModel = "none";
    }
    else
    {
        var generated = await GenerateAsync(client!, model, facts, options.Delay);
        if (generated is null)
        {
            Console.Error.WriteLine("overgeslagen " + facts.EscoId + " " + facts.Title);
            continue;
        }

        text = generated;
        usedModel = model;
    }

    var previous = file.Entries.GetValueOrDefault(facts.EscoId);
    file.Entries[facts.EscoId] = new HonestAdviceFileEntry
    {
        Text = text,
        GeneratedAt = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
        Model = usedModel,
        Peildatum = facts.Peildatum,
        SourceHash = facts.SourceHash,
        Translations = previous is not null && string.Equals(previous.Text, text, StringComparison.Ordinal)
            ? previous.Translations
            : null
    };
    wrote++;
    if (wrote % 25 == 0)
    {
        Save(path, file);
    }
}

Save(path, file);
Console.WriteLine("geschreven " + wrote + " / bekeken " + targets.Count);
return 0;

static bool NeedsWork(HonestAdviceFile file, HonestAdviceFacts facts, bool force)
    => force
       || !file.Entries.TryGetValue(facts.EscoId, out var existing)
       || !string.Equals(existing.SourceHash, facts.SourceHash, StringComparison.Ordinal)
       || HonestAdviceValidator.RejectionReason(existing.Text, facts) is not null;

static int Stamp(HonestAdviceFile file, string path)
{
    var failed = 0;
    foreach (var pair in file.Entries.ToList())
    {
        var facts = HonestAdviceFacts.TryFor(pair.Key);
        if (facts is null)
        {
            Console.Error.WriteLine("onbekend beroep " + pair.Key);
            failed++;
            continue;
        }

        var reason = HonestAdviceValidator.RejectionReason(pair.Value.Text, facts);
        if (reason is not null)
        {
            Console.Error.WriteLine(pair.Key + " " + reason);
            failed++;
            continue;
        }

        pair.Value.SourceHash = facts.SourceHash;
        pair.Value.Peildatum = facts.Peildatum;
        failed += StampLocales(pair.Key, pair.Value, facts);
    }

    if (failed > 0)
    {
        return 1;
    }

    Save(path, file);
    Console.WriteLine("stempel " + file.Entries.Count);
    return 0;
}

static int Check(HonestAdviceFile file)
{
    var failed = 0;
    foreach (var pair in file.Entries)
    {
        var facts = HonestAdviceFacts.TryFor(pair.Key);
        if (facts is null)
        {
            Console.Error.WriteLine("onbekend beroep " + pair.Key);
            failed++;
            continue;
        }

        var reason = HonestAdviceValidator.RejectionReason(pair.Value.Text, facts);
        if (reason is not null)
        {
            Console.Error.WriteLine(pair.Key + " " + reason);
            failed++;
        }
        else if (!string.Equals(pair.Value.SourceHash, facts.SourceHash, StringComparison.Ordinal))
        {
            Console.Error.WriteLine(pair.Key + " hash wijkt af");
            failed++;
        }

        failed += CheckLocales(pair.Key, pair.Value, facts);
    }

    Console.WriteLine(failed == 0 ? "ok " + file.Entries.Count : "fouten " + failed);
    return failed == 0 ? 0 : 1;
}

static int StampLocales(string id, HonestAdviceFileEntry entry, HonestAdviceFacts facts)
{
    var failed = 0;
    if (entry.Translations is null)
    {
        return 0;
    }

    var dutchHash = HonestAdviceFacts.Hash(entry.Text);
    foreach (var pair in entry.Translations)
    {
        var reason = HonestAdviceValidator.RejectionReason(pair.Value.Text, facts, pair.Key);
        if (reason is not null)
        {
            Console.Error.WriteLine(id + " " + pair.Key + " " + reason);
            failed++;
            continue;
        }

        pair.Value.SourceHash = dutchHash;
    }

    return failed;
}

static int CheckLocales(string id, HonestAdviceFileEntry entry, HonestAdviceFacts facts)
{
    var failed = 0;
    if (entry.Translations is null)
    {
        return 0;
    }

    var dutchHash = HonestAdviceFacts.Hash(entry.Text);
    foreach (var pair in entry.Translations)
    {
        var reason = HonestAdviceValidator.RejectionReason(pair.Value.Text, facts, pair.Key);
        if (reason is not null)
        {
            Console.Error.WriteLine(id + " " + pair.Key + " " + reason);
            failed++;
        }
        else if (!string.Equals(pair.Value.SourceHash, dutchHash, StringComparison.Ordinal))
        {
            Console.Error.WriteLine(id + " " + pair.Key + " vertaling hoort niet bij de Nederlandse tekst");
            failed++;
        }
    }

    return failed;
}

static async Task<int> TranslateFileAsync(HonestAdviceFile file, string path, RunOptions options)
{
    var ids = options.Ids.Count > 0
        ? options.Ids.Where(file.Entries.ContainsKey)
        : file.Entries.Keys.OrderBy(id => id, StringComparer.Ordinal).ToList();
    var pending = new List<(string Id, HonestAdviceFacts Facts, string Language)>();
    foreach (var id in ids)
    {
        if (!file.Entries.TryGetValue(id, out var entry))
        {
            Console.Error.WriteLine("geen advies " + id);
            continue;
        }

        var facts = HonestAdviceFacts.TryFor(id);
        if (facts is null)
        {
            Console.Error.WriteLine("onbekend beroep " + id);
            continue;
        }

        foreach (var language in new[] { "en", "pl", "ro", "ar" })
        {
            if (!options.Force && LocaleFresh(entry, facts, language))
            {
                continue;
            }

            pending.Add((id, facts, language));
        }
    }

    if (pending.Count == 0)
    {
        Console.WriteLine("vertalingen al bij");
        return 0;
    }

    var needsModel = pending.Any(item => !string.Equals(file.Entries[item.Id].Text, HonestAdviceValidator.TooLittle, StringComparison.Ordinal));
    HttpClient? client = null;
    var model = "";
    if (needsModel)
    {
        client = BuildClient(out model);
        if (client is null)
        {
            return 2;
        }
    }

    var wrote = 0;
    foreach (var item in pending)
    {
        var entry = file.Entries[item.Id];
        entry.Translations ??= new Dictionary<string, HonestAdviceLocaleFile>(StringComparer.OrdinalIgnoreCase);
        string text;
        string usedModel;
        if (string.Equals(entry.Text, HonestAdviceValidator.TooLittle, StringComparison.Ordinal))
        {
            text = HonestAdviceValidator.TooLittleFor(item.Language);
            usedModel = "none";
        }
        else
        {
            var generated = await TranslateAsync(client!, model, entry.Text, item.Facts, item.Language, options.Delay);
            if (generated is null)
            {
                Console.Error.WriteLine("overgeslagen " + item.Id + " " + item.Language);
                continue;
            }

            text = generated;
            usedModel = model;
        }

        entry.Translations[item.Language] = new HonestAdviceLocaleFile
        {
            Text = text,
            GeneratedAt = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
            Model = usedModel,
            SourceHash = HonestAdviceFacts.Hash(entry.Text)
        };
        wrote++;
        if (wrote % 25 == 0)
        {
            Save(path, file);
        }
    }

    Save(path, file);
    Console.WriteLine("vertaald " + wrote + " / " + pending.Count);
    return wrote == pending.Count ? 0 : 1;
}

static bool LocaleFresh(HonestAdviceFileEntry entry, HonestAdviceFacts facts, string language)
{
    if (entry.Translations is null || !entry.Translations.TryGetValue(language, out var locale))
    {
        return false;
    }

    return string.Equals(locale.SourceHash, HonestAdviceFacts.Hash(entry.Text), StringComparison.Ordinal)
           && HonestAdviceValidator.RejectionReason(locale.Text, facts, language) is null;
}

static async Task<string?> TranslateAsync(
    HttpClient client,
    string model,
    string dutch,
    HonestAdviceFacts facts,
    string language,
    int delayMs)
{
    string? lastReason = null;
    for (var attempt = 1; attempt <= 3; attempt++)
    {
        if (attempt > 1 && delayMs > 0)
        {
            await Task.Delay(delayMs);
        }

        var user = dutch;
        if (lastReason is not null)
        {
            user += "\nThe previous translation was rejected. " + HonestAdviceValidator.ReasonSentence(lastReason);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        request.Content = JsonContent.Create(new
        {
            model,
            temperature = 0.2,
            messages = new object[]
            {
                new { role = "system", content = HonestAdvicePrompt.TranslateSystem(language) },
                new { role = "user", content = user }
            }
        });
        using var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            Console.Error.WriteLine("AI " + (int)response.StatusCode + " bij " + facts.EscoId + " " + language);
            return null;
        }

        var body = await response.Content.ReadAsStringAsync();
        var text = Clean(ReadContent(body));
        var reason = HonestAdviceValidator.RejectionReason(text, facts, language);
        if (reason is null)
        {
            if (delayMs > 0)
            {
                await Task.Delay(delayMs);
            }

            return text;
        }

        lastReason = reason;
        Console.Error.WriteLine("afgekeurd " + facts.EscoId + " " + language + " " + reason + " poging " + attempt);
    }

    return null;
}

static List<HonestAdviceFacts> Targets(HonestAdviceFile file, RunOptions options)
{
    IEnumerable<string> ids;
    if (options.Ids.Count > 0)
    {
        ids = options.Ids;
    }
    else if (options.All || options.Limit is not null || options.DryRun)
    {
        ids = OccupationCatalog.Shared.All.Select(item => item.Id).OrderBy(id => id, StringComparer.Ordinal);
    }
    else
    {
        ids = file.Entries.Keys;
    }

    var list = new List<HonestAdviceFacts>();
    foreach (var id in ids)
    {
        var facts = HonestAdviceFacts.TryFor(id);
        if (facts is null)
        {
            Console.Error.WriteLine("onbekend beroep " + id);
            continue;
        }

        if (options.Limit is int limit && !options.Force && !NeedsWork(file, facts, force: false))
        {
            continue;
        }

        list.Add(facts);
        if (options.Limit is int cap && list.Count >= cap && options.Ids.Count == 0)
        {
            break;
        }
    }

    return list;
}

static HttpClient? BuildClient(out string model)
{
    var provider = Env("Ai__Provider", "AI_PROVIDER") ?? "OpenAI";
    var mistral = provider.Equals("Mistral", StringComparison.OrdinalIgnoreCase);
    var key = mistral
        ? Env("Mistral__ApiKey", "MISTRAL_API_KEY")
        : Env("OpenAI__ApiKey", "OPENAI_API_KEY");
    if (string.IsNullOrWhiteSpace(key))
    {
        Console.Error.WriteLine(mistral
            ? "Geen sleutel. Zet Mistral__ApiKey of MISTRAL_API_KEY."
            : "Geen sleutel. Zet OpenAI__ApiKey of OPENAI_API_KEY. Of Ai__Provider=Mistral.");
        model = "";
        return null;
    }

    model = mistral
        ? Env("Mistral__Model", "MISTRAL_MODEL") ?? "mistral-small-latest"
        : Env("OpenAI__Model", "OPENAI_MODEL") ?? "gpt-4o-mini";
    var baseUrl = mistral
        ? Env("Mistral__BaseUrl", "MISTRAL_BASE_URL") ?? "https://api.eu.mistral.ai/v1/"
        : Env("OpenAI__BaseUrl", "OPENAI_BASE_URL") ?? "https://api.openai.com/v1/";
    if (!baseUrl.EndsWith('/'))
    {
        baseUrl += "/";
    }

    var client = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(60) };
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
    Console.WriteLine("provider " + (mistral ? "Mistral" : "OpenAI") + " model " + model);
    return client;
}

static async Task<string?> GenerateAsync(HttpClient client, string model, HonestAdviceFacts facts, int delayMs)
{
    string? lastReason = null;
    for (var attempt = 1; attempt <= 3; attempt++)
    {
        if (attempt > 1 && delayMs > 0)
        {
            await Task.Delay(delayMs);
        }

        var user = HonestAdvicePrompt.User(facts);
        if (lastReason is not null)
        {
            user += "\nDe vorige tekst is afgekeurd. " + HonestAdviceValidator.ReasonSentence(lastReason);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        request.Content = JsonContent.Create(new
        {
            model,
            temperature = 0.2,
            messages = new object[]
            {
                new { role = "system", content = HonestAdvicePrompt.System },
                new { role = "user", content = user }
            }
        });
        using var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            Console.Error.WriteLine("AI " + (int)response.StatusCode + " bij " + facts.EscoId);
            return null;
        }

        var body = await response.Content.ReadAsStringAsync();
        var text = Clean(ReadContent(body));
        var reason = HonestAdviceValidator.RejectionReason(text, facts);
        if (reason is null)
        {
            if (delayMs > 0)
            {
                await Task.Delay(delayMs);
            }

            return text;
        }

        lastReason = reason;
        Console.Error.WriteLine("afgekeurd " + facts.EscoId + " " + reason + " poging " + attempt);
    }

    return null;
}

static string? ReadContent(string body)
{
    using var doc = JsonDocument.Parse(body);
    if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
    {
        return null;
    }

    var message = choices[0].GetProperty("message");
    return message.TryGetProperty("content", out var content) ? content.GetString() : null;
}

static string Clean(string? text)
{
    var value = (text ?? "").Trim();
    if (value.StartsWith("```", StringComparison.Ordinal))
    {
        var lines = value.Split('\n').Where(line => !line.TrimStart().StartsWith("```", StringComparison.Ordinal));
        value = string.Join('\n', lines).Trim();
    }

    if (value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"'))
    {
        value = value[1..^1].Trim();
    }

    return value.Replace("\r\n", " ", StringComparison.Ordinal).Replace('\n', ' ').Trim();
}

static HonestAdviceFile Load(string path)
{
    if (!File.Exists(path))
    {
        return new HonestAdviceFile();
    }

    var json = File.ReadAllText(path);
    return JsonSerializer.Deserialize<HonestAdviceFile>(json, AdviceJson.Options) ?? new HonestAdviceFile();
}

static void Save(string path, HonestAdviceFile file)
{
    var ordered = file.Entries
        .OrderBy(pair => pair.Key, StringComparer.Ordinal)
        .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    var payload = new HonestAdviceFile { Entries = ordered };
    File.WriteAllText(path, JsonSerializer.Serialize(payload, AdviceJson.Options) + "\n");
}

static string? Env(params string[] keys)
{
    foreach (var key in keys)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }
    }

    return null;
}

internal static class AdviceJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}

internal sealed class RunOptions
{
    public bool Help { get; init; }
    public bool Stamp { get; init; }
    public bool Check { get; init; }
    public bool DryRun { get; init; }
    public bool All { get; init; }
    public bool Force { get; init; }
    public bool Translate { get; init; }
    public int Delay { get; init; } = 500;
    public int? Limit { get; init; }
    public List<string> Ids { get; init; } = [];
    public string Path { get; init; } = DefaultPath();

    public static RunOptions Parse(string[] args)
    {
        var help = false;
        var stamp = false;
        var check = false;
        var dry = false;
        var all = false;
        var force = false;
        var translate = false;
        var delay = 500;
        int? limit = null;
        var ids = new List<string>();
        var path = DefaultPath();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            string? Next() => i + 1 < args.Length ? args[++i] : null;
            switch (arg)
            {
                case "--help" or "-h":
                    help = true;
                    break;
                case "--stamp":
                    stamp = true;
                    break;
                case "--check":
                    check = true;
                    break;
                case "--dry-run":
                    dry = true;
                    break;
                case "--all":
                    all = true;
                    break;
                case "--force":
                    force = true;
                    break;
                case "--translate":
                    translate = true;
                    break;
                case "--delay-ms":
                    delay = int.TryParse(Next(), out var parsed) ? parsed : 500;
                    break;
                case "--limit":
                    limit = int.TryParse(Next(), out var count) ? count : null;
                    break;
                case "--ids":
                    ids.AddRange((Next() ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                    break;
                case "--path":
                    path = Next() ?? path;
                    break;
                default:
                    Console.Error.WriteLine("onbekend argument " + arg);
                    help = true;
                    break;
            }
        }

        return new RunOptions
        {
            Help = help,
            Stamp = stamp,
            Check = check,
            DryRun = dry,
            All = all,
            Force = force,
            Translate = translate,
            Delay = delay,
            Limit = limit,
            Ids = ids,
            Path = path
        };
    }

    private static string DefaultPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = System.IO.Path.Combine(dir.FullName, "Jobsy.Core", "Data", "Occupations", HonestAdviceService.FileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return System.IO.Path.Combine("Jobsy.Core", "Data", "Occupations", HonestAdviceService.FileName);
    }
}
