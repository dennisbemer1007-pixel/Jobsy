# 05 · Platforminstellingen

> Read `00-README.md` first (§0, §IA, D6, D7, D8, D10). Builds on 01–04.

| | |
|---|---|
| Branch | `cursor/admin-redesign-5`, created from `cursor/admin-redesign-4` |
| PR | ONE PR into `acceptatie`, title `feat(admin): grouped platform settings from one catalog; integrations and pricing moved out`. Body starts with `Stacked on #<PR of 04> (cursor/admin-redesign-4)` |
| Mockups | `ad-d4-platforminstellingen.png` (1440×1330) |

## 05.1 Today (verify)
- `SettingsAdmin.razor` (now `/admin/instellingen`, 559 lines) mixes: integration tiles (duplicate of `/admin/integrations`), mail-test link, CNAME link, "Platform features" checkboxes, token pricing packs, Flex & talent amounts, spend costs, PushBom pricing, early adapter rules; inline styles.
- `PlatformFeatureSettings` fields: `VacancyContentModerationEnabled`, `AuthenticatorEnabled` (**candidate application authenticator stub**, `ApplicationsController` ~L710; label since the 2FA fix: "Authenticator bij sollicitatie (stub)"), `ExposeRegistrationActivationLinks`, `PublicWebBaseUrl`, `InactiveCompanyDays`, `SessionInactivityTimeoutMinutes`, `MinimumSessionVersion` (not in the update DTO), `FreePublishUntil`, `SupportAccessNotifyAdmins`, `SupportAccessNotifySubject`, `UpdatedAtUtc`.
- Check whether `EmployersEnabled` / `CandidatePassportEnabled` exist on your branch: `git grep -n "EmployersEnabled\|CandidatePassportEnabled" -- Jobsy.Core/Entities/PlatformFeatureSettings.cs`.
- Login 2FA is **not** a setting: `MfaPolicy` + `MfaEnforcementMiddleware` (ADR 0005).

## 05.2 One catalog: `Jobsy.Web/Admin/PlatformSettingsCatalog.cs`
- `PlatformSettingDescriptor(Key, Group, TitleKey, DescriptionKey, Kind (Bool/Int/Date/Text/Policy), Read (snapshot → value), Write (update, value), ImpactKey?, ImpactLevel (None/Warn/Danger), ConfirmOnChange (bool), EnvironmentLock (None/AcceptatieOnly), ShowOnDashboard, Min/Max)`.
- Groups and entries (nl copy final):
  - **Platform-modus** ("Grote schakelaars die bepalen wie het platform kan gebruiken.")
    - `EmployersEnabled` **only if the field exists**: "Werkgevers actief" / "Werkgevers, vestigingen en intermediairs kunnen inloggen, vacatures plaatsen en matchen." Impact (warn): "**Impact bij uitzetten:** {n} werkgeversaccounts kunnen niet meer inloggen, {m} vacatures gaan offline en achtergrondtaken voor werkgevers pauzeren. Er wordt niets verwijderd; aanzetten herstelt alles." `{n}`/`{m}` from the existing metrics summary (`companies_employers` users / `active_vacancies`), not a new endpoint. `ConfirmOnChange`, `ShowOnDashboard`.
    - `CandidatePassportEnabled` **only if the field exists**: "Mijn Paspoort (nieuw kandidaatprofiel)" / "Kandidaten zien het nieuwe DNA-paspoort en de ontdekkingsreis in plaats van het oude profiel." `ShowOnDashboard`.
    - If neither exists: the group shows one muted line "Nog geen platformschakelaars. Ze verschijnen hier zodra ze live staan." and the entries stay as commented slots in the catalog.
  - **Vacatures** ("Controle en prijsacties rond vacatures.")
    - `VacancyContentModerationEnabled`: "AI-vacaturemoderatie" / "Nieuwe en gewijzigde vacatureteksten worden automatisch gecontroleerd op discriminatie en misleiding." Impact (warn) on off: "Vacatures worden zonder controle gepubliceerd." `ShowOnDashboard`.
    - `FreePublishUntil`: "Gratis publiceren" / "Werkgevers publiceren zonder tokens tot en met de gekozen datum. Uitlichten en PushBom blijven betaald." Date + "Actie stoppen" (clear).
  - **Beveiliging** ("Inloggen, sessies en toegang tot persoonsgegevens.")
    - Policy row (Kind `Policy`, no control, lock icon): "Tweestapsverificatie" / "Verplicht voor beheerders, vestigings-, regio- en enterprisemanagers en intermediairs bij inloggen met wachtwoord. Inloggen via Microsoft of Google gebruikt de 2FA van die dienst." Meta: "Vastgelegd in beleid (ADR 0005), niet uit te zetten." `ShowOnDashboard` (value "Verplicht").
    - `SessionInactivityTimeoutMinutes`: "Sessie-timeout bij inactiviteit" / "Gebruikers worden na deze periode zonder activiteit uitgelogd." Int with unit "min", min/max = what `PlatformFeatureService` validates today.
    - `SupportAccessNotifyAdmins`: "Beheerders melden bij support-toegang" / "Alle beheerders krijgen een e-mail zodra iemand tijdelijke toegang tot persoonsgegevens krijgt."
    - `SupportAccessNotifySubject`: "Gebruiker informeren bij support-toegang" / "De gebruiker ziet op zijn privacypagina dat support zijn gegevens heeft ingezien."
  - **Demo & test** ("Hulpmiddelen die nooit in Productie aan mogen staan.")
    - `ExposeRegistrationActivationLinks`: "Activatielinks tonen bij registratie" / "Voor demo's: toont de activatielink direct in plaats van via e-mail." `EnvironmentLock = AcceptatieOnly`, pill "Alleen in Acceptatie".
    - `AuthenticatorEnabled`: "Authenticator bij sollicitatie (stub)" / "Laat kandidaten bij solliciteren de authenticator-stub gebruiken. Staat los van de 2FA bij inloggen."
- `PublicWebBaseUrl` and `InactiveCompanyDays` are **Algemeen** entries (05.4). `MinimumSessionVersion` stays out of the UI (deferred; say so in the PR).
- 02's `PlatformModeSummary` is replaced by reading `ShowOnDashboard` entries.

## 05.3 Functies `/admin/instellingen` (`ad-d4`)
- `h1` "Functies", lead "Zet onderdelen van het platform aan of uit." (07 appends "Elke wijziging wordt met reden gelogd in het auditlog.").
- One card per group (`h2` + muted description), rows = `AdminToggleRow` (title 600, description muted, control right, meta line, optional `AdminImpactNote`). Changed-but-unsaved rows get the `--accent-soft` tint and meta "Gewijzigd, nog niet opgeslagen · was: {oud}".
- **Save bar** (sticky bottom, `--brand-deep`): "{n} wijziging(en): {titel} {oud} → {nieuw}" · Annuleren · **Opslaan**. One `PUT api/settings/platform-features` with only the changed fields (nullable = keep). `ConfirmOnChange` entries open a confirm dialog first with the impact text. 07 turns the button into "Opslaan en loggen" with a reason field.
- Meta line until 07: page-level "Laatst opgeslagen {UpdatedAtUtc, Europe/Amsterdam}". Per-row "Laatst gewijzigd door …" and the right-hand **Wijzigingen** panel are **07 slots** (not rendered before 07; the main column is then full width). The mockup's "Acceptatie · Productie (alleen lezen)" segment is **not built** (one environment can't read the other).
- **Environment lock (stricter server-side):** `SettingsController.UpdatePlatformFeatures` refuses `ExposeRegistrationActivationLinks = true` when `DeploymentEnvironment` (01.6, Core) resolves to Productie → 400 "Alleen in Acceptatie." The UI shows the switch disabled with the pill in Productie.
- Mobile: rows stack (control under text, ≥ 44 px); save bar full width.

## 05.4 Algemeen `/admin/instellingen/algemeen`
- Cards: **Lobsy bedrijfsgegevens** (the existing `CompanySettingsAdmin` body moved into `Sections/LobsyCompanySection.razor`, unchanged logic; lead "Gegevens van Lobsy zelf, voor facturen en e-mails.") · **Publieke URL** (`PublicWebBaseUrl`) · **Werkgevers opnieuw benaderen** (`InactiveCompanyDays`: "Na hoeveel dagen zonder activiteit krijgt een werkgever één herinneringsmail?"). Both catalog entries, same save bar.

## 05.5 Integraties & API `/admin/instellingen/integraties`
- Tabs from 01: **Integraties** (the existing `IntegrationsAdmin` tiles with `IntegrationSettingsTile` + health; this is now the **only** place for integration tiles) · **API-sleutels** (existing `ApiKeysAdmin`). Remove the integration tiles, the mail-test section and the CNAME section from the settings page (they live under Integraties, Content › E-mails and Organisaties › Regio's & domeinen).

## 05.6 Pricing moves out (D10; 06 merges)
- Move the pricing sections of `SettingsAdmin.razor` (token pricing packs, spend costs, Flex & talent amounts, PushBom, early adapter rules) **unchanged** into `Sections/PricingSettingsSection.razor`. Turn `/admin/financien/prijzen` (the moved `/admin/sales` page from 01) into a tab host `Pages/Admin/PricesPage.razor` with `AdminTabs`: Tokenprijzen · PushBom · Flex & talent · Early adapters · Sales & commissie (the existing sales body moved into `Sections/SalesCommercialSection.razor`). No price value or logic changes. The legacy `/admin/sales` redirect now targets `…/prijzen?tab=sales`.
- `SettingsAdmin.razor` ends up with only the catalog-driven Functies page; delete its inline styles.

## Tests
- `PlatformSettingsCatalog`: every entry's `Read`/`Write` round-trips through `PlatformFeatureUpdate`; keys unique; groups in order; absent flags produce no entries (reflection on `PlatformFeatureSettings`); exactly one `Policy` entry for 2FA.
- API: activation links can't be enabled when the environment resolves to Productie; can in Acceptatie; other fields unaffected.
- bUnit: changing two rows shows "2 wijzigingen" and sends one PUT with only those fields; `ConfirmOnChange` opens the impact dialog; Annuleren restores; locked row disabled in Productie; 2FA row has no control.
- Pricing page renders the moved sections; existing pricing tests (`LobsyCommercialSettingsTests`, `Sprint6AdminSuiteTests`) green with updated URLs.
- Playwright: settings page at 1440 (full height) and 390; save bar appears after a change.

## Success criteria
- Functies shows grouped rows with description, impact and meta; one save bar; no pricing, integrations or links left on it.
- 2FA shown as policy; the stub flag correctly named under Demo & test.
- Werkgevers actief / Mijn Paspoort appear only if the fields exist.
- Integration tiles in one place; pricing on Financiën › Prijzen & pakketten with unchanged values.
- Build + tests green; PR body complete.

## Done → next
Push, open the PR, note its number. Continue with **`06-financien.md`**.
