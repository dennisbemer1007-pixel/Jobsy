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
    private async Task DownloadNamedFileAsync(string url, IJSRuntime js, string fallbackName, CancellationToken ct)
    {
        var response = await _http.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Downloaden mislukt.");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                       ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                       ?? fallbackName;
        var media = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var base64 = Convert.ToBase64String(bytes);
        await SendBrowserDownloadAsync(js, fileName, base64, media);
    }

    private static async Task SendBrowserDownloadAsync(IJSRuntime js, string fileName, string base64, string media)
    {
        await js.InvokeVoidAsync("jobsyExtras.ensure");
        await js.InvokeVoidAsync("jobsyDownload.bytes", fileName, base64, media);
    }

    private static string? ExtractMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("message", out var msg))
            {
                return msg.GetString();
            }
        }
        catch
        {
            // fall through
        }

        return body.Length > 400 ? body[..400] : body;
    }

    private async Task<MeGetResult<T>> GetMeJsonAsync<T>(string url, CancellationToken ct)
    {
        try
        {
            var value = await _http.GetFromJsonAsync<T>(url, ct);
            return MeGetResult<T>.Ok(value);
        }
        catch (HttpRequestException ex) when (
            ex.StatusCode is HttpStatusCode.Unauthorized
                or HttpStatusCode.NotFound
                or HttpStatusCode.Forbidden)
        {
            return MeGetResult<T>.Ok(default);
        }
        catch (HttpRequestException ex) when (IsTemporarilyUnavailable(ex.StatusCode))
        {
            return MeGetResult<T>.Unavailable();
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return MeGetResult<T>.Unavailable();
        }
        catch (HttpRequestException)
        {
            return MeGetResult<T>.Unavailable();
        }
    }

    private static bool IsTemporarilyUnavailable(HttpStatusCode? status)
        => status is HttpStatusCode.TooManyRequests
            or HttpStatusCode.RequestTimeout
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout
            || (status is not null && (int)status >= 500);

    public ValueTask DisposeAsync()
    {
        _http.Dispose();
        return ValueTask.CompletedTask;
    }

    private static string? TryExtractMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("message", out var message) &&
                message.ValueKind == JsonValueKind.String)
            {
                return message.GetString();
            }
        }
        catch (JsonException)
        {
            // fall through
        }

        return null;
    }

    private static T? TryDeserialize<T>(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(body, CaseInsensitiveJson);
        }
        catch (JsonException)
        {
            return default;
        }
    }
}
