using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Jobsy.Core.Security;

var apiBase = (Environment.GetEnvironmentVariable("JOBSY_API") ?? "https://lobsy-acc-api.onrender.com").TrimEnd('/');
var adminId = Guid.Parse(Environment.GetEnvironmentVariable("JOBSY_ADMIN_USER_ID") ?? "ffffffff-1111-1111-1111-111111111111");
var sv = int.TryParse(Environment.GetEnvironmentVariable("JOBSY_ADMIN_SESSION_VERSION"), out var parsed) ? parsed : 0;
var origin = Environment.GetEnvironmentVariable("CLOUDFLARE_ORIGIN_SECRET")
    ?? Environment.GetEnvironmentVariable("Cloudflare__OriginSecret");
var privatePem = Environment.GetEnvironmentVariable("JobsyAuth__Jwt__PrivateKeyPem");
if (string.IsNullOrWhiteSpace(privatePem))
{
    privatePem = JobsyAccessToken.DevelopmentPrivateKeyPem;
    Console.Error.WriteLine("Using DevelopmentPrivateKeyPem (no JobsyAuth__Jwt__PrivateKeyPem in env).");
}

var token = JobsyAccessToken.Create(adminId, sv, JobsyAccessToken.NormalizePem(privatePem), lifetime: TimeSpan.FromMinutes(45));
using var http = new HttpClient { BaseAddress = new Uri(apiBase + "/"), Timeout = TimeSpan.FromMinutes(20) };
http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
if (!string.IsNullOrWhiteSpace(origin))
{
    http.DefaultRequestHeaders.TryAddWithoutValidation("X-Jobsy-Origin-Secret", origin.Trim());
}

var manifestPath = args.Length > 0
    ? args[0]
    : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs/mockups/leerling/werelden/data/trial-20-manifest.json");
manifestPath = Path.GetFullPath(manifestPath);
var manifest = JsonSerializer.Deserialize<Manifest>(await File.ReadAllTextAsync(manifestPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
var ids = manifest?.Occupations?.Select(o => o.EscoId).ToList() ?? [];
if (ids.Count == 0)
{
    Console.Error.WriteLine("No ids in manifest.");
    return 1;
}

async Task<HttpResponseMessage> PostRegenAsync(IReadOnlyList<string> batch)
{
    var body = JsonSerializer.Serialize(new { limit = 15, ids = string.Join("\n", batch) });
    var content = new StringContent(body, Encoding.UTF8, "application/json");
    return await http.PostAsync("api/admin/occupation-day-in-life/regenerate", content);
}

Console.WriteLine("GET status…");
var statusRes = await http.GetAsync("api/admin/occupation-day-in-life/status");
Console.WriteLine($"status HTTP {(int)statusRes.StatusCode} {await statusRes.Content.ReadAsStringAsync()}");

if (!statusRes.IsSuccessStatusCode)
{
    return 2;
}

var log = new List<object>();
for (var i = 0; i < ids.Count; i += 15)
{
    var batch = ids.Skip(i).Take(15).ToList();
    Console.WriteLine($"POST regenerate batch {i / 15 + 1} ({batch.Count} ids)…");
    var res = await PostRegenAsync(batch);
    var text = await res.Content.ReadAsStringAsync();
    Console.WriteLine($"regenerate HTTP {(int)res.StatusCode} {(text.Length > 500 ? text[..500] : text)}");
    log.Add(new { batch = i / 15 + 1, status = (int)res.StatusCode, body = text });
    if (!res.IsSuccessStatusCode)
    {
        await File.WriteAllTextAsync("trial-20-api-regen-log.json", JsonSerializer.Serialize(log, new JsonSerializerOptions { WriteIndented = true }));
        return 3;
    }
}

Console.WriteLine("GET export…");
var exportRes = await http.GetAsync("api/admin/occupation-day-in-life/export");
if (!exportRes.IsSuccessStatusCode)
{
    Console.Error.WriteLine($"export failed {(int)exportRes.StatusCode}");
    return 4;
}

var outPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(manifestPath)!, "occupation-day-in-life.nl.json"));
await File.WriteAllBytesAsync(outPath, await exportRes.Content.ReadAsByteArrayAsync());
Console.WriteLine($"Saved {outPath}");
await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(manifestPath)!, "trial-20-api-regen-log.json"), JsonSerializer.Serialize(log, new JsonSerializerOptions { WriteIndented = true }));
return 0;

file sealed class Manifest
{
    public List<ManifestOcc> Occupations { get; set; } = [];
}

file sealed class ManifestOcc
{
    public string EscoId { get; set; } = "";
}
