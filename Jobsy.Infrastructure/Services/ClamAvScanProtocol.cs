using System.Text;
using System.Text.Json;
using Jobsy.Core.Interfaces;

namespace Jobsy.Infrastructure.Services;

/// <summary>Parses clamd and ClamAV-REST replies. Never returns file bytes.</summary>
internal static class ClamAvScanProtocol
{
    public static UploadScanResult ParseBody(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return UploadScanResult.Unavailable;
        }

        var trimmed = body.Trim().Trim('\0', '\r', '\n');
        if (trimmed.Length == 0)
        {
            return UploadScanResult.Unavailable;
        }

        if (trimmed[0] is '{' or '[')
        {
            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                return ParseJson(doc.RootElement);
            }
            catch (JsonException)
            {
                return UploadScanResult.Unavailable;
            }
        }

        return ParsePlain(trimmed);
    }

    public static string? SanitizeThreat(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var sb = new StringBuilder(Math.Min(raw.Length, 80));
        foreach (var c in raw.Trim())
        {
            if (sb.Length >= 80)
            {
                break;
            }

            if (char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-')
            {
                sb.Append(c);
            }
        }

        return sb.Length == 0 ? null : sb.ToString();
    }

    public static bool TryParseEndpoint(string? raw, out UploadScanEndpoint endpoint)
    {
        endpoint = default;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var text = raw.Trim();
        if (text.StartsWith("tcp://", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("clamav://", StringComparison.OrdinalIgnoreCase))
        {
            var schemeEnd = text.IndexOf("://", StringComparison.Ordinal);
            var rest = text[(schemeEnd + 3)..];
            if (!TryHostPort(rest, 3310, out var host, out var port))
            {
                return false;
            }

            endpoint = new UploadScanEndpoint(UploadScanTransport.Tcp, host, port, null);
            return true;
        }

        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        if (string.IsNullOrEmpty(uri.Host) || uri.Port is < 1 or > 65535)
        {
            return false;
        }

        endpoint = new UploadScanEndpoint(UploadScanTransport.Http, uri.Host, uri.Port, uri);
        return true;
    }

    public static string ResolveFieldName(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "file";
        }

        var trimmed = raw.Trim();
        if (trimmed.Length is < 1 or > 32)
        {
            return "file";
        }

        foreach (var c in trimmed)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c is not '_' and not '-')
            {
                return "file";
            }
        }

        return trimmed;
    }

    private static UploadScanResult ParseJson(JsonElement root)
    {
        var infected = false;
        var sawClean = false;
        string? threat = null;
        Walk(root, ref infected, ref sawClean, ref threat);
        if (infected)
        {
            return new UploadScanResult(UploadMalwareVerdict.Infected, threat ?? "FOUND");
        }

        if (sawClean)
        {
            return UploadScanResult.Clean;
        }

        return UploadScanResult.Unavailable;
    }

    private static void Walk(JsonElement element, ref bool infected, ref bool sawClean, ref string? threat)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    if (NameIs(prop.Name, "is_infected") || NameIs(prop.Name, "isInfected") || NameIs(prop.Name, "infected"))
                    {
                        if (prop.Value.ValueKind == JsonValueKind.True)
                        {
                            infected = true;
                        }
                        else if (prop.Value.ValueKind == JsonValueKind.False)
                        {
                            sawClean = true;
                        }
                    }
                    else if (NameIs(prop.Name, "viruses") || NameIs(prop.Name, "threats") || NameIs(prop.Name, "signatures"))
                    {
                        ReadThreatArray(prop.Value, ref infected, ref threat);
                    }
                    else if ((NameIs(prop.Name, "status") || NameIs(prop.Name, "result"))
                             && prop.Value.ValueKind == JsonValueKind.String)
                    {
                        ApplyStatus(prop.Value.GetString(), ref infected, ref sawClean, ref threat);
                    }
                    else
                    {
                        Walk(prop.Value, ref infected, ref sawClean, ref threat);
                    }
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    Walk(item, ref infected, ref sawClean, ref threat);
                }

                break;
        }
    }

    private static void ReadThreatArray(JsonElement value, ref bool infected, ref string? threat)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var name = SanitizeThreat(item.GetString());
            if (name is null)
            {
                continue;
            }

            infected = true;
            threat ??= name;
        }
    }

    private static void ApplyStatus(string? text, ref bool infected, ref bool sawClean, ref string? threat)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (text.Contains("FOUND", StringComparison.OrdinalIgnoreCase))
        {
            infected = true;
            threat ??= SanitizeThreat(text) ?? "FOUND";
            return;
        }

        if (text.Equals("OK", StringComparison.OrdinalIgnoreCase)
            || text.Equals("CLEAN", StringComparison.OrdinalIgnoreCase))
        {
            sawClean = true;
        }
    }

    private static UploadScanResult ParsePlain(string text)
    {
        if (text.Contains("Everything ok : false", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Everything ok: false", StringComparison.OrdinalIgnoreCase))
        {
            return new UploadScanResult(UploadMalwareVerdict.Infected, "FOUND");
        }

        if (text.Contains("Everything ok : true", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Everything ok: true", StringComparison.OrdinalIgnoreCase))
        {
            return UploadScanResult.Clean;
        }

        var foundAt = text.IndexOf("FOUND", StringComparison.OrdinalIgnoreCase);
        if (foundAt >= 0)
        {
            var head = text[..foundAt].Trim().TrimEnd(':').Trim();
            var line = head.Split('\n', '\r').LastOrDefault() ?? head;
            if (line.StartsWith("stream:", StringComparison.OrdinalIgnoreCase))
            {
                line = line["stream:".Length..].Trim();
            }

            return new UploadScanResult(UploadMalwareVerdict.Infected, SanitizeThreat(line) ?? "FOUND");
        }

        if (text.Contains("ERROR", StringComparison.OrdinalIgnoreCase))
        {
            return UploadScanResult.Unavailable;
        }

        var flat = text.Trim();
        if (flat.Equals("OK", StringComparison.OrdinalIgnoreCase)
            || flat.Equals("stream: OK", StringComparison.OrdinalIgnoreCase)
            || flat.EndsWith(": OK", StringComparison.OrdinalIgnoreCase))
        {
            return UploadScanResult.Clean;
        }

        return UploadScanResult.Unavailable;
    }

    private static bool NameIs(string name, string expected)
        => name.Equals(expected, StringComparison.OrdinalIgnoreCase);

    private static bool TryHostPort(string rest, int defaultPort, out string host, out int port)
    {
        host = "";
        port = defaultPort;
        if (string.IsNullOrWhiteSpace(rest))
        {
            return false;
        }

        var slash = rest.IndexOf('/');
        if (slash >= 0)
        {
            rest = rest[..slash];
        }

        if (rest.StartsWith('['))
        {
            var end = rest.IndexOf(']');
            if (end <= 1)
            {
                return false;
            }

            host = rest[1..end];
            if (end + 1 < rest.Length)
            {
                if (rest[end + 1] != ':')
                {
                    return false;
                }

                if (!int.TryParse(rest[(end + 2)..], out port))
                {
                    return false;
                }
            }
        }
        else
        {
            var colon = rest.LastIndexOf(':');
            if (colon > 0 && colon < rest.Length - 1 && rest.IndexOf(':') == colon)
            {
                host = rest[..colon];
                if (!int.TryParse(rest[(colon + 1)..], out port))
                {
                    return false;
                }
            }
            else
            {
                host = rest;
            }
        }

        return !string.IsNullOrWhiteSpace(host) && port is >= 1 and <= 65535;
    }
}

internal enum UploadScanTransport
{
    Tcp,
    Http
}

internal readonly record struct UploadScanEndpoint(
    UploadScanTransport Transport,
    string Host,
    int Port,
    Uri? HttpUri);
