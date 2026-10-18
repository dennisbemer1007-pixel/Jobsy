# Feature flag verification (admin flags r1)

Generated as part of PR: admin feature-flag centralization.

| Flag | Aan gedrag | Uit gedrag | Geautomatiseerd | Opmerking |
|------|------------|------------|-----------------|-----------|
| EmployersEnabled | Werkgever-routes/API zichtbaar | 404 / binnenkort | FeatureFlagFoundationTests, FeatureFlagBehaviorTests | Master switch |
| SensitiveExperienceEnabled | Privacy UI werkervaring | Geen sensitive controls | SensitiveExperiencePlaywrightTests, FeatureFlagBehaviorTests | Acc bootstrap + admin override |
| TrainingPartnersEnabled | Carrière/partner feeds | Geen partner UI | TrainingPartnerTests, FeatureFlagBehaviorTests | Acc bootstrap + admin override |
| KandidatenkaartEnabled | `/werkgever/kandidatenkaart` | Route gate 404 | Kandidatenkaart evidence tests | Admin + public endpoint |
| ReferenceVerificationEnabled | Referentiekaart onboarding | Geen referentiekaart | OnboardingFinaleCardsTests, FeatureFlagBehaviorTests | Nieuw in snapshot |
| CandidatePassportEnabled | Paspoort-nav | Profiel-nav | FeatureRoutes tests | Default aan |
| TestsV2Enabled | Quick-Scan v2 | v1 catalog | TestsV2PlaywrightTests | |
| OnboardingFinaleV2Enabled | Finale v2 layout | Klassieke finale | OnboardingFinaleV2BunitTests | Via API flags |
| ResearchConsentEnabled | Onderzoeksopt-in UI | Geen opt-in flow | Research admin page | Admin + platform DB |
| TestEditModeEnabled | Antwoorden wijzigen | Geen edit-knop | TestDetail + feature-flags | Admin override |
| SamplePdfEnabled | Voorbeeld-PDF knop | Geen sample | TestDetail + feature-flags | Admin override |
| TestJourneyIllustrationEnabled | Illustratie stap 7 | Geen illustratie CSS | TestJourneyConsentStepBunitTests | Via API flags |
| PupilPassportV2Enabled / StudentPassportEnabled | School PDF varianten | Standaard uit | Scholen API tests | Config + admin override |

## Flags zonder direct producteffect (documentatie)

- `PhoneVerificationEnabled` — geen SMS-provider; UI blijft verborgen.
- `MatchCalibrationV1Enabled` — matchscore-intern; geen route gate.
- `ResearchConsentEnabled` — onderzoeksopt-in; apart admin-onderzoek scherm.

## Kapotte / dode flags

Geen flags gevonden die in admin staan maar nergens gelezen worden. Web-only config toggles (`TestJourneyIllustration`, `OnboardingFinaleV2`) lezen nu via `IFeatureFlags` / public endpoint.
