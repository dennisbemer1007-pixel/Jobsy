# Candidate gaps + form UX (fixes2)

## Gemaakte keuzes

- **Paspoort aan, nieuwe kandidaat:** eerst step-by-step wizard op `/candidate/start`, daarna ontdekkingsreis; paspoort als home pas na `CompletedAtUtc` (discovery afgerond).
- **Datums:** overal native picker via `JobsyDateField` (`type=date` / `type=month`), gedeelde min/max in `JobsyDateInputRules`.
- **Formulieren:** gedeelde tokens in `form-fields.css` (labels boven veld, donkere waarden, meer verticale ruimte, duidelijke geselecteerde opties en samenvattingen).

## Geconverteerde datumvelden

| Veld | Locatie |
|------|---------|
| Geboortedatum | Onboarding personal step, profiel, vacaturedetail DOB |
| Beschikbaar vanaf | Onboarding + profiel availability (`AvailabilityStartSection`) |
| Werkervaring van/tot | `MonthYearInput` → maand-picker (yyyy-MM) |
| Diploma-evaluatiedatum | Profiel education |
| Admin logfilters | LoggingAdmin, PersonalDataAccessLogAdmin |
| WAADI geldig tot | WerkgeverVerificatieAdmin |

## Form UX — screenshots (`/cursor/stores/self/fixes2/`)

| Bestand | Inhoud |
|---------|--------|
| `01-after-onboarding-personal-filled-{mobile,desktop}.png` | Ontdekkingsreis stap 1, ingevulde velden + geboortedatum |
| `02-before-onboarding-availability-immediate-mobile.png` | Vóór tokens (referentie, mobile) |
| `02-after-onboarding-availability-immediate-{mobile,desktop}.png` | Beschikbaarheid, optie “Per direct” geselecteerd |
| `03-after-onboarding-availability-date-{mobile,desktop}.png` | “Vanaf een datum” + native datumveld ingevuld |
| `04-after-profile-availability-date-{mobile,desktop}.png` | Profiel → Beschikbaarheid, datum ingevuld |
