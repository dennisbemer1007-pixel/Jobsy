# Na de match r2 — capture vs mockup

Artifacts: `/opt/cursor/artifacts/nadematch-r2/` (desktop + mobiel per scherm, `compare/*-side-by-side-*.png`).

Routes use demo seed (`AfterMatchDemoSeeder`) with `kandidaat.denhaag@jobsy.local` / `ondernemer@jobsy.local`.

| Scherm | Bekende restafwijkingen t.o.v. mockup |
|--------|----------------------------------------|
| 01 | Hero-illustratie uit `gesprek.jpg`, geen custom kandidaat+werkgever-art; demo-kandidaat heet in account “Demo Kandidaat Den Haag” (sollicitatie toont wel Sanne de Vries). |
| 02 | Werkgever-shell is echte `WerkgeverLayout` (Lobsy wg-nav), niet pixel-identiek aan mockup-header met tokens/Linda-regel. |
| 03 | “Mijn agenda delen” is UI-toggle; koppeling volgt later — weekweergave + handmatig 2–3 slots werkt. |
| 04 | Zijpaneel “Goed om te weten” uit mockup nog niet als aparte kaart. |
| 05 | Agenda-knoppen (Outlook/Google/Apple) deels statisch; ICS-download via API werkt. |
| 06 | Mail/Outlook-voorbeelden zijn UI-previews (geen live SMTP); inhoud volgt thread-data. |
| 07 | Push-mockup is statisch; reminder-toggles zijn live gekoppeld. |
| 08 | “Afzeggen” roept nog geen apart cancel-endpoint aan (alleen verzetten-flow). |
| 09 | Geen aparte “Akkoord”-API op candidate-slot; werkgever kan counter via propose-panel. |
| 10–11 | Telefoon/IBAN-waarschuwing via preview-API; geen thread-list links/rechts zoals mockup 10 desktop. |
| 12 | Uitkomst-preview kaart vereenvoudigd; geen volledige kandidaat-sidebar uit mockup. |
| 13 | Similar jobs uit API indien aanwezig, anders fallback-regel; geen feedback-tags “Vriendelijk/Duidelijk/Snel”. |

Feature flag: `JobsyFeatures:NaDeMatchEnabled` / `PlatformFeature.AfterMatch`.
