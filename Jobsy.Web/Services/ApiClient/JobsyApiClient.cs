using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Enums;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Web.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace Jobsy.Web.Services;

public sealed partial class JobsyApiClient : IAsyncDisposable
{
    public const string KompasCacheKey = "me/kompas";
    public const string KompasDnaCacheKey = "me/kompas/dna";
    public const string OnboardingCacheKey = "me/onboarding";

    private static readonly JsonSerializerOptions CaseInsensitiveJson = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions EnumJson = CreateEnumJson();

    /// <summary>
    /// One serializer for every API JSON call on this client. The API writes enums as
    /// strings (<c>JsonStringEnumConverter</c>); reading them with the framework default
    /// options throws and takes the school, teacher and pupil portals down.
    /// </summary>
    internal static JsonSerializerOptions ApiJson => EnumJson;

    private static JsonSerializerOptions CreateEnumJson()
    {
        var options = new JsonSerializerOptions(CaseInsensitiveJson);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private Task<T?> GetApiJsonAsync<T>(string requestUri, CancellationToken ct)
        => _http.GetFromJsonAsync<T>(requestUri, ApiJson, ct);

    private Task<HttpResponseMessage> PostApiJsonAsync<TValue>(string requestUri, TValue value, CancellationToken ct)
        => _http.PostAsJsonAsync(requestUri, value, ApiJson, ct);

    private Task<HttpResponseMessage> PutApiJsonAsync<TValue>(string requestUri, TValue value, CancellationToken ct)
        => _http.PutAsJsonAsync(requestUri, value, ApiJson, ct);

    private static Task<T?> ReadApiJsonAsync<T>(HttpContent content, CancellationToken ct)
        => content.ReadFromJsonAsync<T>(ApiJson, ct);

    private readonly HttpClient _http;
    private readonly MeGetCache? _meCache;

    public JobsyApiClient(HttpClient http, MeGetCache? meCache = null)
    {
        _http = http;
        _meCache = meCache;
    }
}
