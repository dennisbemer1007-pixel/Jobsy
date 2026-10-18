# Feature flag inventory

| Naam | Gelezen (API / Web / beide) | Standaardbron | Admin schakelaar |
|------|-----------------------------|---------------|------------------|
| EmployersEnabled | beide | DB / dev test override | ja |
| CandidatePassportEnabled | beide | DB (default aan) | ja |
| SensitiveExperienceEnabled | beide | env `Jobsy:*` → admin override | ja (fix: PATCH + audit) |
| TrainingPartnersEnabled | beide | env `Jobsy:*` → admin override | ja (fix: PATCH + audit) |
| KandidatenkaartEnabled | beide | DB | ja |
| ReferenceVerificationEnabled | beide | DB + override | ja (nieuw) |
| PupilPassportV2Enabled | beide | env → admin override | ja (nieuw in catalogus) |
| StudentPassportEnabled | beide | env → admin override | ja (nieuw in catalogus) |
| TestJourneyIllustrationEnabled | Web (via API flags) | env → admin override | ja (nieuw) |
| OnboardingFinaleV2Enabled | Web (via API flags) | env → admin override | ja (nieuw) |
| OccupationDayInLifeEnabled | API + Web | env → admin override | ja (nieuw) |
| TestsV2Enabled | beide | DB / JobsyFeatures | ja |
| ProactiveMatchingEnabled | beide | DB | ja |
| EmployerPhase2Enabled | beide | DB | ja |
| SchoolsEnabled | beide | DB | ja |
| AmbassadorsEnabled | beide | DB | ja |
| VacancyContentModerationEnabled | API | DB / options | ja |
| FreeCandidateTestsEnabled | API | DB | ja |
| MatchCalibrationV1Enabled | API | DB | ja |
| ResearchConsentEnabled | API | DB | nee (entity only; registry) |
| TestEditMode / SamplePdf | API options | appsettings | nee (JobsyFeatures, geen UI) |
| NaDeMatch / TestProgressInsight / DreamJobIllustration | — | — | niet in codebase (oktober 2026) |
| Bedrijvenkaart | beide | `EmployersEnabled` | via Employers |

Bronprioriteit na deze PR: **admin DB override (AdminFlagOverridesJson)** → **env/appsettings** → **default**.

Acceptatie: `PlatformFeatureAcceptatieBootstrapHostedService` zet SensitiveExperience + TrainingPartners **aan** via DB override (zonder Render-sync).
