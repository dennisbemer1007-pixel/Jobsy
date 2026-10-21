using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Web.Models;
using Jobsy.Web.Services;

namespace Jobsy.Tests;

/// <summary>Fake API payloads for illustrated / stijl-A Playwright captures (Delft / Zaandam demo content).</summary>
internal static class IllustratedPagesApiStub
{
    public static readonly Guid SampleVacancyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid PhotoVacancyId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid SampleCompanyId = Guid.Parse("a1000000-0000-0000-0000-000000000001");
    public static readonly Guid SampleApplicationId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid SampleEmployerApplicationId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    /// <summary>When set, returned as mock-interview coach reply (screenshot tests).</summary>
    public static string? MockInterviewReplyOverride { get; set; }

    /// <summary>When true, /me/onboarding returns step 1 in progress (compact header preview captures).</summary>
    public static bool OnboardingWizardStep1Capture { get; set; }

    /// <summary>When true, /me/onboarding returns step 7 with test consent (mini-tests compact header captures).</summary>
    public static bool OnboardingWizardTestsStepCapture { get; set; }

    /// <summary>When true, kompas payloads use a minimal paspoort profile (many empty fields).</summary>
    public static bool PassportMinimalProfileCapture { get; set; }

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string BuildJson(string path, bool emptyAppsAndLikes = false, bool minimalPassportProfile = false)
    {
        if (path.Contains("/kompas/dna", StringComparison.OrdinalIgnoreCase))
        {
            var k = minimalPassportProfile || PassportMinimalProfileCapture
                ? BuildMinimalKompas()
                : PassportDeepOnboardingScreenshotTests.BuildKompas(completed: true);
            return Serialize(new CandidateDnaSummary
            {
                WhoAmI = k.WhoAmI,
                ProfileCompletenessPercent = k.ProfileCompletenessPercent,
                Competencies = new CandidateDnaCompetencySummary
                {
                    Status = "Completed",
                    AnsweredCount = 25,
                    QuestionCount = 25,
                    Scores = k.Competencies?.Scores,
                    DeepCompleted = true
                },
                CareerInterests = new CandidateDnaCareerSummary
                {
                    Status = "Completed",
                    AnsweredCount = 25,
                    QuestionCount = 25,
                    Scores = k.CareerInterests?.Scores,
                    DeepCompleted = true
                },
                Culture = new CandidateDnaCultureSummary
                {
                    Status = "Completed",
                    AnsweredCount = 18,
                    QuestionCount = 18,
                    DeepCompleted = true
                },
                Values = new CandidateDnaValuesSummary
                {
                    Status = "Completed",
                    AnsweredCount = 25,
                    QuestionCount = 25,
                    DeepCompleted = true
                }
            });
        }

        if (path.Contains("/kompas", StringComparison.OrdinalIgnoreCase))
        {
            if (minimalPassportProfile || PassportMinimalProfileCapture)
            {
                return Serialize(BuildMinimalKompas());
            }

            var kompas = PassportDeepOnboardingScreenshotTests.BuildKompas(completed: true);
            kompas.Profile!.FullName = "Sanne de Vries";
            kompas.Profile.FirstName = "Sanne";
            kompas.Profile.Role = "Candidate";
            if (kompas.WhoAmI is not null)
            {
                kompas.WhoAmI.Story =
                    "Sanne pakt taken concreet op en houdt het overzicht, ook als het druk wordt.";
            }

            return Serialize(kompas);
        }

        if (path.Contains("/site/status", StringComparison.OrdinalIgnoreCase))
        {
            return """{"maintenance":false,"expectedEndUtc":null}""";
        }

        if (path.Contains("/me/email-preferences", StringComparison.OrdinalIgnoreCase))
        {
            return """{"reminderEmailsEnabled":true,"optional":[{"key":"PushBom","enabled":true},{"key":"VacancyEngagementReminder","enabled":true}]}""";
        }

        if (path.Contains("/me/reminder-whatsapp", StringComparison.OrdinalIgnoreCase))
        {
            return """{"available":true,"optedIn":false,"phone":null}""";
        }

        if (path.Contains("/me/onboarding", StringComparison.OrdinalIgnoreCase)
            && !path.Contains("nearby", StringComparison.OrdinalIgnoreCase)
            && !path.Contains("discovery", StringComparison.OrdinalIgnoreCase))
        {
            if (OnboardingWizardStep1Capture)
            {
                return """
                    {
                      "currentStep": 1,
                      "startedAtUtc": "2026-01-15T08:00:00Z",
                      "completedAtUtc": null,
                      "isComplete": false,
                      "shouldShow": true,
                      "wizardVersion": 3,
                      "finishReached": false,
                      "steps": [{"step": 2, "completedAtUtc": null}]
                    }
                    """;
            }

            if (OnboardingWizardTestsStepCapture)
            {
                return """
                    {
                      "currentStep": 7,
                      "startedAtUtc": "2026-01-15T08:00:00Z",
                      "completedAtUtc": null,
                      "isComplete": false,
                      "shouldShow": true,
                      "wizardVersion": 3,
                      "finishReached": false,
                      "steps": []
                    }
                    """;
            }

            return """
                {
                  "currentStep": 10,
                  "startedAtUtc": "2026-01-15T08:00:00Z",
                  "completedAtUtc": "2026-10-01T12:00:00Z",
                  "isComplete": true,
                  "shouldShow": false,
                  "wizardVersion": 2,
                  "finishReached": true,
                  "steps": []
                }
                """;
        }

        if (path.Contains("/me/career-path/archived", StringComparison.OrdinalIgnoreCase))
        {
            return "[]";
        }

        if (path.Contains("/me/career-path/dream-options", StringComparison.OrdinalIgnoreCase))
        {
            return """
                {
                  "suggestions": [
                    { "catalogKey": "dierenarts", "title": "Dierenarts", "description": "Illustrated capture" }
                  ],
                  "results": []
                }
                """;
        }

        if (path.Contains("/me/career-path", StringComparison.OrdinalIgnoreCase))
        {
            return Serialize(SampleCareerPlan());
        }

        if (path.Contains("/me/career-interests", StringComparison.OrdinalIgnoreCase))
        {
            return """
                {
                  "status": "Completed",
                  "answeredCount": 25,
                  "questionCount": 25,
                  "completedAtUtc": "2026-09-01T10:00:00Z"
                }
                """;
        }

        if (path.Contains("/me/applications", StringComparison.OrdinalIgnoreCase))
        {
            return emptyAppsAndLikes ? "[]" : Serialize(CandidateApplications());
        }

        if (path.Contains("/applications", StringComparison.OrdinalIgnoreCase)
            && !path.Contains("/by-vacancy", StringComparison.OrdinalIgnoreCase))
        {
            return Serialize(EmployerApplications());
        }

        if (path.Contains("/likes", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/engagement/likes", StringComparison.OrdinalIgnoreCase))
        {
            return emptyAppsAndLikes ? "[]" : SampleLikeJson();
        }

        if (path.Contains("/engagement/shares", StringComparison.OrdinalIgnoreCase))
        {
            return "[]";
        }

        if (path.Contains("/companies/mine", StringComparison.OrdinalIgnoreCase))
        {
            return $$"""
                [
                  {
                    "id": "{{SampleCompanyId}}",
                    "name": "Care & Co",
                    "address": "Zaandam",
                    "kvkNumber": "12345678",
                    "verificationStatus": "Verified",
                    "tokenBalance": 42,
                    "activeVacancies": 2
                  }
                ]
                """;
        }

        if (path.Contains("/vacancies/manage", StringComparison.OrdinalIgnoreCase))
        {
            return $$"""
                [
                  {
                    "id": "{{SampleVacancyId}}",
                    "title": "Medewerker dierenpension",
                    "companyId": "{{SampleCompanyId}}",
                    "companyName": "De Kooger Dierenopvang",
                    "companyAddress": "Zaandam",
                    "status": "Active",
                    "hourlyWage": 13.9,
                    "wageVisible": true
                  }
                ]
                """;
        }

        if (path.Contains("/regions", StringComparison.OrdinalIgnoreCase))
        {
            return "[]";
        }

        if (path.Contains("/travel", StringComparison.OrdinalIgnoreCase))
        {
            return """{"travelMinutes":9,"distanceKm":2.4}""";
        }

        if (path.Contains("/vacancy-categories/field-catalog", StringComparison.OrdinalIgnoreCase))
        {
            return "[]";
        }

        if (path.Contains("/vacancy-categories", StringComparison.OrdinalIgnoreCase))
        {
            return Serialize(SampleVacancyCategories());
        }

        if (path.Contains("/exclusivity-settings", StringComparison.OrdinalIgnoreCase))
        {
            return Serialize(SampleExclusivitySettings());
        }

        if (path.Contains("/masterdata", StringComparison.OrdinalIgnoreCase))
        {
            return "[]";
        }

        if (path.Contains("/tokens/balance", StringComparison.OrdinalIgnoreCase))
        {
            return $$"""[{"companyId":"{{SampleCompanyId}}","balance":42}]""";
        }

        if (path.Contains("/salary-tables", StringComparison.OrdinalIgnoreCase))
        {
            return Serialize(SampleSalaryTables());
        }

        if (path.Contains("/profile-extras", StringComparison.OrdinalIgnoreCase))
        {
            return """{"workTypeLabels":[],"cultureSliders":{},"valueCardIds":[]}""";
        }

        if (path.Contains("/categories", StringComparison.OrdinalIgnoreCase))
        {
            return "[]";
        }

        if (path.Contains("/me/profile", StringComparison.OrdinalIgnoreCase))
        {
            if (emptyAppsAndLikes)
            {
                return """
                    {
                      "fullName": "Onboarding E2E",
                      "firstName": "Onboarding",
                      "lastName": "E2E",
                      "role": "Candidate",
                      "openForWork": true,
                      "preferences": {}
                    }
                    """;
            }

            var testConsentLine = OnboardingWizardTestsStepCapture
                ? "\n                  \"testAiConsentAt\": \"2026-10-01T12:00:00Z\","
                : string.Empty;
            return $$"""
                {
                  "fullName": "Sanne de Vries",
                  "firstName": "Sanne",
                  "lastName": "de Vries",{{testConsentLine}}
                  "role": "Candidate",
                  "openForWork": true,
                  "preferences": {
                    "homeAddress": "1506 AB Zaandam",
                    "workRegion": null,
                    "aboutMe": "Ik werk graag in teamverband en houd van duidelijke afspraken."
                  }
                }
                """;
        }

        if (path.Contains("/me/who-am-i", StringComparison.OrdinalIgnoreCase))
        {
            return """
                {
                  "story": "Sanne pakt taken concreet op en houdt het overzicht.",
                  "status": "Ready",
                  "cultureCompleted": true
                }
                """;
        }

        if (path.Contains("/me/values", StringComparison.OrdinalIgnoreCase))
        {
            return """{"status":"Completed","answeredCount":25,"questionCount":25}""";
        }

        if (path.Contains("vacanc", StringComparison.OrdinalIgnoreCase))
        {
            if (path.Contains(PhotoVacancyId.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return PhotoVacancyJson();
            }

            if (path.Contains(SampleVacancyId.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return SampleVacancyJson();
            }

            if (path.Contains("/similar", StringComparison.OrdinalIgnoreCase))
            {
                return "[]";
            }

            if (path.Contains("/culture-fit", StringComparison.OrdinalIgnoreCase))
            {
                return """{"status":"Ready","fitPercent":78,"fitBand":"Good"}""";
            }

            return IllustratedPagesVacancyStub.JsonForPath(path);
        }

        if (path.Contains("/employer/onboarding", StringComparison.OrdinalIgnoreCase))
        {
            return """{"completed":true,"dismissedSuggestions":[]}""";
        }

        if (path.Contains("/mock-interview", StringComparison.OrdinalIgnoreCase))
        {
            var reply = MockInterviewReplyOverride
                        ?? "Vraag: Vertel kort waarom je solliciteert als medewerker dierenpension bij De Kooger Dierenopvang. Wat spreekt je aan in de vacature?";
            return $$"""
                {
                  "reply": {{JsonSerializer.Serialize(reply, Json)}},
                  "usedAi": false,
                  "disclaimer": ""
                }
                """;
        }

        if (path.Contains("/me/reference-verification/verified-competences", StringComparison.OrdinalIgnoreCase))
        {
            return """
                [
                  {
                    "code": "plan",
                    "label": "Plannen en organiseren",
                    "recognitionLine": "Bevestigd door referent",
                    "completedAtUtc": "2026-09-01T10:00:00Z"
                  }
                ]
                """;
        }

        if (path.Contains("/me/uwv", StringComparison.OrdinalIgnoreCase))
        {
            if (path.Contains("adviseur-share", StringComparison.OrdinalIgnoreCase))
            {
                return """{"token":"uwv-share-demo-token","expiresAtUtc":"2026-12-31T00:00:00Z"}""";
            }

            return """
                {
                  "ondernemerOutcomeKey": "explore",
                  "drijfverenDriverKeys": ["security", "connection", "growth"]
                }
                """;
        }

        return "{}";
    }

    public static bool ShouldStubVacancyCatalog(string path)
    {
        if (!path.StartsWith("/api/vacancies", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (path.Contains(SampleVacancyId.ToString(), StringComparison.OrdinalIgnoreCase)
            || path.Contains(PhotoVacancyId.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return path.Contains("/discover", StringComparison.OrdinalIgnoreCase)
               || path.Contains("/pins", StringComparison.OrdinalIgnoreCase)
               || path.Contains("/map-view", StringComparison.OrdinalIgnoreCase)
               || path.Contains("/cards", StringComparison.OrdinalIgnoreCase);
    }

    private static CareerPathPlanApiModel SampleCareerPlan()
        => new()
        {
            DreamTitle = "Dierenarts",
            DreamFitBand = "Good",
            MatchPercent = 72,
            MatchSummary = "Je bouwt stappen richting dierenarts met praktijk en opleiding.",
            GoalReached = false,
            Steps =
            [
                new CareerPathStepApiModel
                {
                    Id = "s1",
                    Order = 1,
                    Title = "Dierenoppas in de buurt",
                    Status = "Completed",
                    StepFitBand = "Good",
                    CourseStatuses = [new CareerPathCourseApiModel { Name = "EHBO dieren", OnProfile = true }]
                },
                new CareerPathStepApiModel
                {
                    Id = "s2",
                    Order = 2,
                    Title = "Mbo 4 paraveterinair",
                    Status = "Active",
                    StepFitBand = "Fair",
                    SkillsGap = ["Stage in dierenkliniek"],
                    CourseStatuses =
                    [
                        new CareerPathCourseApiModel { Name = "Dierengedrag basis", OnProfile = true },
                        new CareerPathCourseApiModel { Name = "Anatomie intro", OnProfile = false }
                    ],
                    ActionKinds = ["Vacancies"],
                    ActionHref = "/banenkaart",
                    ActionLabel = "Zoek banen"
                },
                new CareerPathStepApiModel
                {
                    Id = "s3",
                    Order = 3,
                    Title = "Dierenarts studie",
                    Status = "Open",
                    StepFitBand = "",
                    SkillsGap = ["Wiskunde B", "Biologie VWO"]
                }
            ]
        };

    private static string SampleVacancyJson(bool withPhoto = false)
    {
        const string descriptionSections =
            "<h2>Wat ga je doen?</h2><p>Je helpt met voeren, schoonmaken en aandacht voor de dieren.</p>" +
            "<h2>Wat vragen we?</h2><p>Je bent zorgvuldig en vindt het fijn om met dieren te werken.</p>" +
            "<h2>Wat bieden we?</h2><p>Een warm team, vaste uren en begeleiding.</p>";
        var image = withPhoto ? "\"/images/candidate-illustrated/finale-hero.jpg\"" : "null";
        return $$"""
            {
              "id": "{{SampleVacancyId}}",
              "title": "Medewerker dierenpension",
              "companyName": "De Kooger Dierenopvang",
              "companyAddress": "Kooger Dorpsweg 12, Koog aan de Zaan",
              "status": "Active",
              "openForWorkRequired": false,
              "fitPercent": 81,
              "fitBand": "Strong",
              "fitGate": "Open",
              "fitDimensions": { "culture": 84, "values": null, "competencies": 76, "interests": 90 },
              "fitWhyKinds": ["culture", "interest", "competency"],
              "wageVisible": true,
              "hourlyWage": 13.9,
              "minHoursPerWeek": 16,
              "maxHoursPerWeek": 24,
              "flexibleTimes": false,
              "latitude": 52.44,
              "longitude": 4.83,
              "workTypes": ["PartTime"],
              "startDate": "2026-01-01",
              "endDate": "2026-12-31",
              "kind": "Job",
              "imageUrl": {{image}},
              "companyLogoUrl": null,
              "travelMinutes": 9,
              "matchGaps": ["Werken in het weekend"],
              "description": "{{descriptionSections}}"
            }
            """;
    }

    private static string PhotoVacancyJson() => SampleVacancyJson(withPhoto: true);

    private static List<ApplicationItem> CandidateApplications()
        =>
        [
            new ApplicationItem
            {
                Id = SampleApplicationId,
                VacancyId = SampleVacancyId,
                VacancyTitle = "Medewerker dierenpension",
                CompanyName = "De Kooger Dierenopvang",
                LocationLabel = "Zaandam",
                Status = "EmployerContacting",
                CreatedAt = new DateTime(2026, 10, 2, 10, 0, 0, DateTimeKind.Utc),
                PictureUrl = "/images/candidate-illustrated/finale-hero.jpg",
                PictureKind = "Illustrated",
                Timeline =
                [
                    new ApplicationTimelineStepItem
                    {
                        Key = "Sent", LabelKey = "Kb.Timeline.Sent", State = "Done",
                        OccurredAtUtc = new DateTime(2026, 10, 2, 10, 0, 0, DateTimeKind.Utc)
                    },
                    new ApplicationTimelineStepItem
                    {
                        Key = "Seen", LabelKey = "Kb.Timeline.Seen", State = "Done",
                        OccurredAtUtc = new DateTime(2026, 10, 4, 10, 0, 0, DateTimeKind.Utc)
                    },
                    new ApplicationTimelineStepItem
                    {
                        Key = "Interview", LabelKey = "Kb.Timeline.Interview", State = "Current",
                        OccurredAtUtc = new DateTime(2026, 10, 9, 10, 0, 0, DateTimeKind.Utc)
                    },
                    new ApplicationTimelineStepItem
                    {
                        Key = "Outcome", LabelKey = "Kb.Timeline.Outcome", State = "Pending"
                    }
                ],
                NextStepKey = "Kb.Next.Practice"
            },
            new ApplicationItem
            {
                Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                VacancyId = Guid.Parse("b1000001-0000-0000-0000-000000000001"),
                VacancyTitle = "Zorgmedewerker weekend",
                CompanyName = "Care & Co",
                LocationLabel = "Delft",
                Status = "Pending",
                CreatedAt = new DateTime(2026, 9, 20, 8, 0, 0, DateTimeKind.Utc),
                PictureUrl = "/images/vacancies/zorg.webp",
                PictureKind = "Photo",
                Timeline =
                [
                    new ApplicationTimelineStepItem
                    {
                        Key = "Sent", LabelKey = "Kb.Timeline.Sent", State = "Done",
                        OccurredAtUtc = new DateTime(2026, 9, 20, 8, 0, 0, DateTimeKind.Utc)
                    },
                    new ApplicationTimelineStepItem
                    {
                        Key = "Seen", LabelKey = "Kb.Timeline.Seen", State = "Current"
                    }
                ],
                NextStepKey = "Kb.Next.Wait"
            }
        ];

    private static List<EmployerApplicationItem> EmployerApplications()
        =>
        [
            new EmployerApplicationItem
            {
                Id = SampleEmployerApplicationId,
                VacancyId = SampleVacancyId,
                VacancyTitle = "Medewerker dierenpension",
                CompanyName = "De Kooger Dierenopvang",
                PreferredTransport = "Fiets",
                EstimatedTravelMinutes = 12,
                CreatedAt = new DateTime(2026, 10, 8, 9, 30, 0, DateTimeKind.Utc),
                Status = "Pending",
                CandidateCity = "Zaandam",
                DistanceKm = 3.2,
                PreferencesSummary = "16–24 uur · doordeweeks",
                CandidateName = "Sanne de Vries",
                Motivation = "Ik werk graag met dieren en zoek een vaste bijbaan.",
                CvPdfAvailable = true
            },
            new EmployerApplicationItem
            {
                Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                VacancyId = SampleVacancyId,
                VacancyTitle = "Medewerker dierenpension",
                CompanyName = "De Kooger Dierenopvang",
                PreferredTransport = "OV",
                EstimatedTravelMinutes = 22,
                CreatedAt = new DateTime(2026, 10, 5, 14, 0, 0, DateTimeKind.Utc),
                Status = "EmployerContacting",
                RespondedAt = new DateTime(2026, 10, 6, 11, 0, 0, DateTimeKind.Utc),
                CandidateCity = "Amsterdam",
                DistanceKm = 14.1,
                CandidateName = "Jamal Bakker",
                PiiRevealed = true,
                CandidateEmail = "jamal@example.test",
                CvPdfAvailable = false
            },
            new EmployerApplicationItem
            {
                Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                VacancyId = SampleVacancyId,
                VacancyTitle = "Medewerker dierenpension",
                CompanyName = "De Kooger Dierenopvang",
                PreferredTransport = "Fiets",
                EstimatedTravelMinutes = 8,
                CreatedAt = new DateTime(2026, 9, 28, 16, 45, 0, DateTimeKind.Utc),
                Status = "Interview",
                CandidateCity = "Koog aan de Zaan",
                CandidateName = "Lotte Jansen",
                PiiRevealed = true
            }
        ];

    private static List<VacancyCategoryItem> SampleVacancyCategories()
        =>
        [
            new VacancyCategoryItem
            {
                Id = Guid.Parse("c1000000-0000-0000-0000-000000000001"),
                Slug = "zorg",
                Name = "Zorg & welzijn",
                ColorHex = "#3366cc",
                PublishCostTokens = 1,
                SortOrder = 1
            }
        ];

    private static List<ExclusivitySettingItem> SampleExclusivitySettings()
        =>
        [
            new ExclusivitySettingItem
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-000000000001"),
                Name = "Standaard",
                IsOpenOption = true,
                SortOrder = 1
            }
        ];

    private static List<SalaryTableItem> SampleSalaryTables()
        =>
        [
            new SalaryTableItem
            {
                Id = Guid.Parse("s1000000-0000-0000-0000-000000000001"),
                Name = "CAO Retail 2026",
                CompanyId = SampleCompanyId,
                IsActive = true
            }
        ];

    private static string SampleLikeJson()
        => $$"""
            [{
              "vacancyId": "{{SampleVacancyId}}",
              "vacancyTitle": "Medewerker dierenpension",
              "companyName": "De Kooger Dierenopvang",
              "locationLabel": "Zaandam",
              "fitPercent": 81,
              "fitBand": "Strong",
              "createdAt": "2026-10-10T10:00:00Z",
              "savedStateKind": "Open",
              "imageUrl": null,
              "companyLogoUrl": null
            }]
            """;

    private static CandidateKompasState BuildMinimalKompas()
    {
        var kompas = PassportDeepOnboardingScreenshotTests.BuildKompas(completed: false);
        kompas.ProfileCompletenessPercent = 8;
        kompas.Profile!.FullName = "Sanne de Vries";
        kompas.Profile.FirstName = "Sanne";
        kompas.Profile.Role = "Candidate";
        kompas.Profile.Preferences = new CandidatePreferences();
        kompas.WhoAmI = null;
        return kompas;
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Json);
}
