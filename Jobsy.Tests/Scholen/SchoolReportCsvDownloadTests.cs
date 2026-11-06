using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Jobsy.Core.Enums;
using Jobsy.Web.Services;
using Microsoft.JSInterop;

namespace Jobsy.Tests.Scholen;

/// <summary>
/// The admin report export must download via the API client. Opening
/// <c>/api/admin/schools/rapportage.csv</c> on the web origin is a 404.
/// </summary>
public class SchoolReportCsvDownloadTests
{
    [Fact]
    public async Task Download_fetches_the_api_csv_and_hands_it_to_the_browser()
    {
        var handler = new CsvHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") };
        var api = new JobsyApiClient(http);
        var js = new RecordingJs();

        var schoolId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        await api.DownloadAdminSchoolReportCsvAsync(
            js,
            schoolYearStart: 2025,
            schoolId: schoolId,
            level: SchoolLevel.VmboB,
            year: 3,
            questionSet: PupilQuestionSet.Groep78);

        Assert.NotNull(handler.PathAndQuery);
        Assert.Contains("/api/admin/schools/rapportage.csv", handler.PathAndQuery, StringComparison.Ordinal);
        Assert.Contains("schoolYearStart=2025", handler.PathAndQuery, StringComparison.Ordinal);
        Assert.Contains($"schoolId={schoolId:D}", handler.PathAndQuery, StringComparison.Ordinal);
        Assert.Contains("level=VmboB", handler.PathAndQuery, StringComparison.Ordinal);
        Assert.Contains("year=3", handler.PathAndQuery, StringComparison.Ordinal);
        Assert.Contains("questionSet=Groep78", handler.PathAndQuery, StringComparison.Ordinal);
        Assert.Equal("scholen-2025.csv", js.FileName);
        Assert.Equal(Convert.ToBase64String(Encoding.UTF8.GetBytes("klas,n\n")), js.Base64);
        Assert.Equal("text/csv", js.Media);
        Assert.Contains("jobsyExtras.ensure", js.Calls, StringComparer.Ordinal);
    }

    private sealed class CsvHandler : HttpMessageHandler
    {
        public string? PathAndQuery { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            PathAndQuery = request.RequestUri!.PathAndQuery;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(Encoding.UTF8.GetBytes("klas,n\n"))
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = "scholen-2025.csv"
            };
            return Task.FromResult(response);
        }
    }

    private sealed class RecordingJs : IJSRuntime
    {
        public List<string> Calls { get; } = [];
        public string? FileName { get; private set; }
        public string? Base64 { get; private set; }
        public string? Media { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Capture(identifier, args);
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            Capture(identifier, args);
            return ValueTask.FromResult(default(TValue)!);
        }

        private void Capture(string identifier, object?[]? args)
        {
            Calls.Add(identifier);
            if (identifier == "jobsyDownload.bytes" && args is { Length: >= 3 })
            {
                FileName = args[0]?.ToString();
                Base64 = args[1]?.ToString();
                Media = args[2]?.ToString();
            }
        }
    }
}
