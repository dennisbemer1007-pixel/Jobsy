# Gelijkgetrokken zone- en stapnamen (kreeft-mockup README #878)

Referentie: `docs/mockups/kandidaat/kreeft/README.md` @ `53863b26`.

| Onderdeel | Was (NL, kort) | Nu (NL) |
|---|---|---|
| `Discovery.Zone.*` haltes/rail | Het strand, Aan de kust, Tussen de rotsen, In de diepte, Naar het licht | Strand, Kust, Rotsen, Diepte, Licht |
| `Discovery.Journey.*` (landing koppen) | (ontbrak) | Strand · Start, Kust · Over jou, Rotsen · Achtergrond en Droom, Diepte · Wie ben jij, Licht · Jouw Kompas |
| `Discovery.Overview.Phase.*Lead` | langere fase-uitleg | één zin per zone (zelfde als About-zones) |
| `Landing.Journey.*` body | andere marketingzinnen | dezelfde vijf zinnen |
| `LandingJourneyFive` h3 | zone-poëtische namen | `Discovery.Journey.*` |
| `OnboardingRustigSidebarNav` | hardcoded NL | `Onboarding.Phase.*` / `Onboarding.Rustig.Phase.*` via `CultureState` |
| `Onboarding.Phase.WieBenJij` (EN) | Who are you | Who you are |

Mails/statusmeldingen: geen zone-poëtische namen in code gevonden; ongewijzigd.

Reiskaart (`UiStringsKreeftReiskaart.cs`) stond al op mockup-formaat (Strand · Start, enz.).
