using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Core.Enums;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Web.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace Jobsy.Web.Services;

public sealed partial class JobsyApiClient
{
    public async Task<string?> GetWebPushVapidPublicKeyAsync(CancellationToken ct = default)
    {
        var dto = await _http.GetFromJsonAsync<WebPushVapidPublicKeyWire>("api/push/vapid-public-key", ct);
        return dto?.PublicKey;
    }

    public async Task<bool> SubscribeWebPushAsync(string endpoint, string p256dh, string auth, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/push/subscribe",
            new { endpoint, keys = new { p256dh, auth } },
            ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<IReadOnlyList<DeviceSessionItem>> GetDeviceSessionsAsync(CancellationToken ct = default)
    {
        var rows = await _http.GetFromJsonAsync<List<DeviceSessionItem>>("api/auth/device-sessions", ct);
        return rows ?? [];
    }

    public async Task RevokeDeviceSessionAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/auth/device-sessions/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task RevokeAllDeviceSessionsAsync(CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync("api/auth/device-sessions", ct);
        response.EnsureSuccessStatusCode();
    }

    public sealed record DeviceSessionItem(
        Guid Id,
        string DeviceName,
        DateTime LastUsedAtUtc,
        DateTime CreatedAtUtc,
        DateTime ExpiresAtUtc,
        bool IsCurrent);

    private sealed class WebPushVapidPublicKeyWire
    {
        public string PublicKey { get; set; } = "";
    }
}
