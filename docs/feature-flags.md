# Feature flags

Admin toggles live in the singleton `PlatformFeatureSettings` row and are read through `IFeatureFlags` (30s cache). Public bootstrap: `GET api/settings/feature-flags`.

| Flag | Default | Meaning |
|---|---|---|
| `EmployersEnabled` | **true** | ON = today’s product. OFF = self-discovery only; employer/vacancy surfaces are hidden and return 404 `feature_disabled`. Data is never deleted. |
| `CandidatePassportEnabled` | **false** | When ON: candidates see Mijn Paspoort (`/candidate/paspoort`) instead of Profiel; nav order per §N; `/home` redirects to the passport. |

Gate with `[RequiresFeature(PlatformFeature.Employers)]` (pages, controllers, actions) or `<FeatureVisible Feature="PlatformFeature.Employers">` (sections). Minimal APIs: `.RequireFeature(PlatformFeature.Employers)`.

## Gated Blazor pages (Employers)

Public: `/`, `/banen`, `/vacancies/{id}`, `/vestiging/{id}`, `/{kvk}`, `/westland`, `/lancering`, `/register`

Candidate: `/candidate/match`, `/candidate/vacancies`, `/candidate/liked`, `/candidate/shared`, `/candidate/applications`, `/candidate/actions/*`, `/candidate/talent-contacts`

Employer portals: `/employer/*`, `/branch/*`, `/regional/*`, `/intermediary`, `/intermediary/team`, `/tokens/checkout-*`

Acquisition: `/salesmanager/*`, `/ambassadeur/*`, `/werven/{code}`, `/ambassadeur/ref/{code}`, `/partner`, `/partner/{code}`

## Gated API controllers (Employers)

Whole controller: Vacancies, VacancyEngagement, ExternalVacancies, VacancyCsvImport, Applications, CandidateActions, TalentPool, CandidateInsights, CompanyCulture, Companies, CompanyUsers, CompanyApiKeys, PublicCompanies, SupplierOnboarding, Kvk, Registration, Tokens, TokenLogs, SalaryTables, Wages, EmployerFlyers, Dashboard, Metrics, SalesManagers, SalesCommercial, Ambassadeurs, PartnerAffiliate, Regions

Method-only (non-admin): VacancyCategories `GET`/`GET {id}`; RegionHosts `resolve`; AdminAts `scrape`/`health`

## Mixed responses (strip vacancy parts when OFF)

- CandidateKompas `TopMatches` → `[]`
- RoleFitCheck `DirectVacancies` → `[]`, `MapHref` → `null`
- CandidateMetrics summary/drilldown → empty (vacancy tiles)
- MockInterview vacancy mode → 404 `feature_disabled`
- AssistantChat vacancy-search tool disabled

## Paused jobs when OFF

AtsScrape, AtsVacancyHealth, VacancyDiscoveryIndex, VacancyEngagementReminder, DraftVacancyCleanup, CompanyReengagement, KvkVerificationRetry, UnconfirmedRegistrationCleanup, CultureFitRefineWorker, CandidateInsightsWorker; DashboardCacheRefresh skips employer/sales/ambassadeur kinds.

Keeps running: TalentContactRefund, TokenCheckoutReconcile, VatBufferTransfer, DataRetention, MinimumWageUpdate, AssessmentNormSnapshot, FeedbackAutomationPoll, IbanEncryptionMigration, DatabaseSeed.

No catch-up: when the flag returns ON, reminder/re-engagement jobs count from “now”.

## Emails (`RequiresEmployers`)

Suppressed in the central send path when OFF: Application*, EmployerReaction*, EmployerContacting, EmployerNewApplication, CandidateWithdrawn*, PushBom, PendingApproval, VacancyEngagementReminder, DraftVacancyCleanupWarning, CompanyReEngagement, Registration*, Takeover*, UserInvite, SalesManagerInvite, AmbassadeurInvite, CompanyApiKeyCredentials.

Still sent: MailTest, AccountUnsubscribeVerification, parental consent / MFA (non-catalog).

## Web push

New-vacancy / employer-reaction / PushBom / Match categories suppressed when OFF. Subscription endpoints stay.

## Nav (§N file 01)

`RoleNavCatalog.CandidateItems(flags)`:

| Passport | Employers | Items |
|---|---|---|
| OFF/ON | ON | Zoeken · Bewaard · Sollicitaties · Carrière · Profiel (today) |
| OFF/ON | OFF | Carrière · Profiel |

Employer-side catalogs empty when OFF (Admin unchanged). Passport-ON order lands in file 02.
