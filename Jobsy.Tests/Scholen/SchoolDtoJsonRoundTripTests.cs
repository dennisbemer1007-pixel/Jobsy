using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Web.Services;

namespace Jobsy.Tests.Scholen;

/// <summary>
/// The API writes enums as strings. School, teacher and pupil pages deserialize through
/// <see cref="JobsyApiClient.ApiJson"/>; the framework default options throw on those strings.
/// </summary>
public class SchoolDtoJsonRoundTripTests
{
    private static readonly JsonSerializerOptions ApiWire = CreateApiWire();

    [Fact]
    public async Task Client_reads_string_enums_for_school_teacher_and_pupil_dtos()
    {
        var classId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var schoolId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var dashboard = new SchoolDashboardDto(
            "Testschool", "Den Haag", "2026-2027", 2, 10, 0, 0, 1, 0,
            [
                new SchoolDashboardClassRowDto(
                    classId, "1A", "Test Leraar", 10, 0, 0, 0, TestWindowState.Open, new DateOnly(2026, 10, 31))
            ],
            [],
            null);

        var classes = new List<SchoolPortalClassListItemDto>
        {
            new(
                classId, "1A", SchoolLevel.Havo, 1, 2026, "2026-2027", ["Test Leraar"],
                10, 0, true, TestWindowState.Closed, null, PupilQuestionSet.Vo)
        };

        var detail = new SchoolPortalClassDetailDto(
            classId, "7A", SchoolLevel.Groep78, 7, 2026, "2026-2027", 28, [],
            false, null, null, null, TestWindowState.NotOpen, null, true,
            [
                new SchoolPortalCodeRowDto(
                    Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    1, "ABC-123", PupilCodeStatus.InProgress, 4, 60, null)
            ],
            PupilQuestionSet.Groep78,
            true);

        var teacherClasses = new List<TeacherAssignedClassDto>
        {
            new(classId, "1A", SchoolLevel.Vwo, 1, 2026, "2026-2027", PupilQuestionSet.Vo)
        };

        var overview = new TeacherClassOverviewDto(
            classId, "1A", SchoolLevel.Havo, 1, "2026-2027", 10, 0, 0, 10, 0, null,
            TestWindowState.Open, new DateOnly(2026, 11, 1), true, null, [],
            new TeacherGroupInsightsDto(false, 0, [], [], [], [], [], [], PupilQuestionSet.Vo),
            PupilQuestionSet.Vo);

        var results = new SchoolPortalResultsDto(
            classId, "1A", false, new ClassResultsAggregate(10, 0, 0, false, [], [], []), null, PupilQuestionSet.Vo);

        var privacy = new SchoolPrivacyDto(
            new DateOnly(2026, 3, 10), "test-seed", new DateOnly(2026, 8, 1), "2025-2026", [],
            [new SchoolOuderbriefDto(PupilQuestionSet.Groep78, "brief")]);

        var pupilClasses = new List<PupilClassOptionDto>
        {
            new(classId, "1A · havo 1", "1A", SchoolLevel.Havo, 1, false, PupilQuestionSet.Vo)
        };

        var pupilSchools = new List<PupilSchoolOptionDto>
        {
            new(schoolId, "Testschool", "Den Haag", false)
        };

        var progress = new PupilProgressStateDto(
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            "1A", "ABC-123", PupilCodeStatus.NotStarted, 0, 100, 0, 0, false,
            null, null, new Dictionary<string, int>(), true, false, false, false, [], [], null, null,
            PupilQuestionSet.Vo);

        foreach (var sample in new object[] { dashboard, classes, detail, teacherClasses, overview, results, privacy, pupilClasses, progress })
        {
            AssertEnumsAreStrings(JsonSerializer.Serialize(sample, ApiWire));
        }

        var dash = await Client(dashboard).GetSchoolDashboardAsync();
        Assert.NotNull(dash);
        Assert.Equal(TestWindowState.Open, dash!.Classes[0].TestWindow);

        var list = await Client(classes).GetSchoolClassesAsync();
        Assert.Equal(SchoolLevel.Havo, list[0].Level);
        Assert.Equal(TestWindowState.Closed, list[0].TestWindow);
        Assert.Equal(PupilQuestionSet.Vo, list[0].QuestionSet);

        var classDetail = await Client(detail).GetSchoolClassAsync(classId);
        Assert.NotNull(classDetail);
        Assert.Equal(SchoolLevel.Groep78, classDetail!.Level);
        Assert.Equal(PupilQuestionSet.Groep78, classDetail.QuestionSet);
        Assert.Equal(PupilCodeStatus.InProgress, classDetail.Codes[0].Status);

        var assigned = await Client(teacherClasses).GetTeacherClassesAsync();
        Assert.Equal(SchoolLevel.Vwo, assigned[0].Level);
        Assert.Equal(PupilQuestionSet.Vo, assigned[0].QuestionSet);

        var teacherOverview = await Client(overview).GetTeacherOverviewAsync(classId);
        Assert.NotNull(teacherOverview);
        Assert.Equal(TestWindowState.Open, teacherOverview!.TestWindow);
        Assert.Equal(PupilQuestionSet.Vo, teacherOverview.GroupInsights.QuestionSet);

        var classResults = await Client(results).GetSchoolClassResultsAsync(classId);
        Assert.NotNull(classResults);
        Assert.Equal(PupilQuestionSet.Vo, classResults!.QuestionSet);

        var schoolPrivacy = await Client(privacy).GetSchoolPrivacyAsync();
        Assert.NotNull(schoolPrivacy);
        Assert.Equal(PupilQuestionSet.Groep78, schoolPrivacy!.Ouderbrieven[0].QuestionSet);

        var schools = await Client(pupilSchools).GetPupilSchoolsAsync();
        Assert.Equal("Testschool", schools[0].SchoolName);

        var dropdown = await Client(pupilClasses).GetPupilClassesAsync(schoolId);
        Assert.NotNull(dropdown);
        Assert.Equal(SchoolLevel.Havo, dropdown![0].Level);
        Assert.Equal(PupilQuestionSet.Vo, dropdown[0].QuestionSet);

        var pupilProgress = await Client(progress).GetPupilProgressAsync();
        Assert.NotNull(pupilProgress);
        Assert.Equal(PupilCodeStatus.NotStarted, pupilProgress!.Status);
        Assert.Equal(PupilQuestionSet.Vo, pupilProgress.QuestionSet);

        var bare = JsonSerializer.Deserialize<SchoolDashboardClassRowDto>(
            """{"id":"11111111-1111-1111-1111-111111111111","className":"1A","codeCount":1,"startedCount":0,"completedCount":0,"completionPercent":0,"testWindow":"Open"}""",
            JobsyApiClient.ApiJson);
        Assert.NotNull(bare);
        Assert.Equal(TestWindowState.Open, bare!.TestWindow);
    }

    [Fact]
    public void Pupil_login_response_roundtrips_string_status_through_web_options()
    {
        var dto = new PupilLoginResponse(
            "/leerling/start",
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "7A",
            "ABC-123",
            PupilCodeStatus.NotStarted,
            0,
            60,
            1);
        var json = JsonSerializer.Serialize(dto, ApiWire);
        Assert.Contains("NotStarted", json, StringComparison.Ordinal);
        var back = JsonSerializer.Deserialize<PupilLoginResponse>(json, JobsyApiClient.ApiJson);
        Assert.NotNull(back);
        Assert.Equal(PupilCodeStatus.NotStarted, back!.Status);
        Assert.Equal("/leerling/start", back.RedirectPath);
    }

    private static void AssertEnumsAreStrings(string json)
    {
        using var doc = JsonDocument.Parse(json);
        Walk(doc.RootElement);

        static void Walk(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var prop in element.EnumerateObject())
                    {
                        if (prop.Name is "testWindow" or "level" or "questionSet" or "status")
                        {
                            Assert.Equal(JsonValueKind.String, prop.Value.ValueKind);
                        }

                        Walk(prop.Value);
                    }

                    break;
                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                    {
                        Walk(item);
                    }

                    break;
            }
        }
    }

    private static JobsyApiClient Client<T>(T payload)
    {
        var json = JsonSerializer.Serialize(payload, ApiWire);
        var http = new HttpClient(new FixedJsonHandler(json))
        {
            BaseAddress = new Uri("http://api.test/")
        };
        return new JobsyApiClient(http);
    }

    private static JsonSerializerOptions CreateApiWire()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        return options;
    }

    private sealed class FixedJsonHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
