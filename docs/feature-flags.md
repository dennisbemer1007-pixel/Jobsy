# Feature flags

Admin toggles live in the singleton `PlatformFeatureSettings` row and are read through `IFeatureFlags` (API cache 30s, cleared on save). The web app keeps its own copy for 10 seconds. Public bootstrap: `GET api/settings/feature-flags`.

| Flag | Default | Meaning |
|---|---|---|
| `EmployersEnabled` | **false** | OFF by default until phase 2 (decision 20, 03-10-2026). The phase-2 employer view is redesigned later and is out of scope: no match percentages or personality scores. ON = today’s employer product, behind an admin confirm dialog. OFF = self-discovery only; employer/vacancy surfaces are hidden, anonymous visitors see `/werkgevers/binnenkort`, and APIs return 404 `feature_disabled`. Data is never deleted. |
| `CandidatePassportEnabled` | **true** | When ON (default): candidates see Mijn Paspoort (`/candidate/paspoort`) instead of Profiel; nav order Discovery · Passport · Zoeken · Sollicitaties · Carrière; Bewaard is a tab under Sollicitaties; default landing via `FeatureRoutes.HomeFor` (not ready → ontdekkingsreis, ready → passport). Admin can still turn it OFF. |
| `PassportPartnersEnabled` | **false** | Partner portal, partner codes and consent. Off hides those surfaces. Nothing is deleted. Does not turn on PDF v2. |
| `PassportPdfV2Enabled` | **false** | Shareable work preferences on the passport Data tab, and later the PDF v2 download. Off leaves the Data tab as it is today. |
| `PhoneVerificationEnabled` | **false** | SMS phone confirmation. Not a route gate. Stays off until an SMS provider exists. The stub logs the code only in Development. |
| `CompactTestPdfEnabled` | **false** | Compact personal deep-test PDFs (about 4 A4 pages with charts). Off keeps today's longer layout. Does not change scores, payment, or the partner passport PDF. |
| `HonestAdviceEnabled` | **false** | Stored "Eerlijk advies" under the job outlook on carrière and functiefit. Off hides the block. The text comes from `honest_advice.nl.json`. The candidate flow never calls a model for it. |
| `FutureJobsForYouEnabled` | **false** | "Werk met toekomst dat bij jou past" on Functiefit and Carrière. Off hides the block. On shows up to 10 occupations that fit the candidate's own test and that ROA expects employers to need strongly (Netherlands only). No invented figures. Does not turn employers on. While employers are off, the Functiefit tab stays reachable so the block and the self-check can be used. |

Gate with `[RequiresFeature(PlatformFeature.Employers)]` (pages, controllers, actions) or `<FeatureVisible Feature="PlatformFeature.Employers">` (sections). Minimal APIs: `.RequireFeature(PlatformFeature.Employers)`.

## Gated Blazor pages (Employers)

Public: `/`, `/banen`, `/vacancies/{id}`, `/vestiging/{id}`, `/{kvk}`, `/westland`, `/lancering`, `/register`

Candidate: `/candidate/match`, `/candidate/vacancies`, `/candidate/liked`, `/candidate/shared`, `/candidate/applications`, `/candidate/actions/*`, `/candidate/talent-contacts`

Employer portals: `/employer/*`, `/branch/*`, `/regional/*`, `/intermediary`, `/intermediary/team`, `/tokens/checkout-*`

Acquisition: `/sales`, `/sales/*`, `/salesmanager/*`, `/ambassadeur/*`, `/werven/{code}`, `/ambassadeur/ref/{code}`, `/partner`, `/partner/{code}`

Company registration: `/register/bedrijf`, `/register/koppelen`, `/register/toegang`, `/register/verifieren`, `/register/verifieren/brief`, `/home/metrics/{Key}`

Anonymous fallback while OFF: `/werkgevers/binnenkort` (noindex). Signed-in employer-side users still land on `/access-denied?reason=employers-off`.

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

## Nav (§N)

`RoleNavCatalog.CandidateItems(flags)`:

| Passport | Employers | Items (left → right) | Count |
|---|---|---|---|
| ON (default) | ON | De ontdekkingsreis · Mijn Paspoort · Zoeken · Sollicitaties · Carrière (Bewaard is a tab inside Sollicitaties) | 5 |
| ON | OFF | De ontdekkingsreis · Mijn Paspoort · Carrière | 3 |
| OFF | ON | Zoeken · Bewaard · Sollicitaties · Carrière · Profiel (legacy D1) | 5 |
| OFF | OFF | Carrière · Profiel | 2 |

Employer-side catalogs empty when OFF (Admin unchanged). Match is a map button, never a nav item.
