namespace Jobsy.Web.Services;

/// <summary>Static copy for employer API-key docs (curl + sample response).</summary>
public static class ExternalApiDocsCopy
{
    public static string ExampleResponseJson { get; } =
        """
        {
          "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "companyId": "…",
          "title": "Magazijnmedewerker",
          "status": "Draft",
          "createdVia": "Api",
          "startDate": "2026-08-01",
          "endDate": "2026-12-31"
        }
        """;

    public static string ExampleRequestCurl(string vacanciesEndpoint) =>
        "curl -X POST \"" + vacanciesEndpoint + "\" \\\n" +
        "  -H \"X-API-Key: lobsy_…\" \\\n" +
        "  -H \"Content-Type: application/json\" \\\n" +
        "  -d '{ \"companyId\": \"…\", \"title\": \"…\", \"description\": \"…\", \"hourlyWage\": 14.50, \"startDate\": \"2026-08-01\", \"endDate\": \"2026-12-31\", \"requiredTransport\": \"PublicTransport\", \"workTypes\": [\"Logistiek\"], \"salaryTableId\": \"…\" }'";
}
