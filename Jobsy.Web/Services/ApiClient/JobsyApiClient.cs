using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
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

    private readonly HttpClient _http;
    private readonly MeGetCache? _meCache;

    public JobsyApiClient(HttpClient http, MeGetCache? meCache = null)
    {
        _http = http;
        _meCache = meCache;
    }
}
