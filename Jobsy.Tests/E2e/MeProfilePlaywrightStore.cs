using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Jobsy.Tests.E2e;

/// <summary>In-memory /api/me/profile for Playwright BFF save + reload tests.</summary>
internal static class MeProfilePlaywrightStore
{
    private static readonly object Gate = new();
    private static JsonObject _profile = CreateDefaultProfile();

    public static void ResetToDefault()
    {
        lock (Gate)
        {
            _profile = CreateDefaultProfile();
        }
    }

    public static bool TryHandle(HttpRequestMessage request, out HttpResponseMessage response)
    {
        response = null!;
        var path = request.RequestUri?.AbsolutePath ?? "";
        if (!path.Contains("/me/profile", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        lock (Gate)
        {
            if (request.Method == HttpMethod.Put)
            {
                var body = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
                if (!string.IsNullOrWhiteSpace(body))
                {
                    try
                    {
                        var put = JsonNode.Parse(body)?.AsObject();
                        if (put?["preferences"] is JsonObject prefs)
                        {
                            var currentPrefs = _profile["preferences"] as JsonObject ?? new JsonObject();
                            if (prefs["aboutMe"] is not null)
                            {
                                currentPrefs["aboutMe"] = prefs["aboutMe"]?.DeepClone();
                            }

                            if (prefs["defaultMotivation"] is not null)
                            {
                                currentPrefs["defaultMotivation"] = prefs["defaultMotivation"]?.DeepClone();
                            }

                            _profile["preferences"] = currentPrefs;
                        }

                        foreach (var key in new[] { "openForWork", "firstName", "lastName", "dateOfBirth" })
                        {
                            if (put![key] is not null)
                            {
                                _profile[key] = put[key]?.DeepClone();
                            }
                        }
                    }
                    catch
                    {
                        // Keep last good state; still return success like a tolerant API.
                    }
                }

                response = JsonResponse(_profile.ToJsonString());
                return true;
            }

            if (request.Method == HttpMethod.Get)
            {
                response = JsonResponse(_profile.ToJsonString());
                return true;
            }
        }

        return false;
    }

    private static HttpResponseMessage JsonResponse(string json)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private static JsonObject CreateDefaultProfile()
        => JsonNode.Parse(
            """
            {
              "fullName": "Sanne de Vries",
              "firstName": "Sanne",
              "lastName": "de Vries",
              "role": "Candidate",
              "openForWork": true,
              "preferences": {
                "homeAddress": "1506 AB Zaandam",
                "workRegion": null,
                "aboutMe": "Ik werk graag in teamverband en houd van duidelijke afspraken."
              }
            }
            """)!.AsObject();
}
