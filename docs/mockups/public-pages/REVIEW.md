# Review: publieke en juridische pagina's (stack B): fase 1

*Bron: `origin/acceptatie` @ `a611db40` (read-only worktree). Live check lobsy.nl (= `origin/main` @ `9a2c0d49`, 214 commits achter acc) op 30-09-2026 ±09:30 CEST.*
*Status: fase 1 (review + mockups + vragen). Nog geen spec: wacht op "akkoord".*
*Let op: dit is een technische/inhoudelijke review, geen juridisch advies. Laat de definitieve teksten (AV, privacy, bedenktijd, DSA) door een jurist checken.*

## 0. Hotfix-kandidaten (los van de stack)

| # | Wat | Ernst | Kern van de fix |
|---|---|---|---|
| H1 | **Placeholders live op lobsy.nl/privacy**: `[BEDRIJFSNAAM]`, `[KVK-NUMMER]`, `[ADRES]`, `[CONTACT E-MAIL PRIVACY]` uit `Jobsy.Core/Privacy/PlatformLegalIdentity.cs:9-12`. Kapotte mailto (4×), ook in de "Wie ik ben"/AI-secties en in de default-tekst van Wie zijn wij. | **Hoog** (AVG art. 13: identiteit + contact verplicht) | Waarden uit config `Legal:Name/Address/KvkNumber/VatNumber/PrivacyEmail` (één bron, ook voor e-mail-footer `MailOptions`). Lege waarde → regel verbergen, nooit placeholder tonen. Test `CandidateConsentRulesTests.cs:54-57` aanpassen (checkt nu juist op de placeholders). |
| H2 | **`/{kvk}` toont niet-gecontroleerde bedrijven**: `PublicCompaniesController.QueryPublicRows` (L138) filtert alleen op `KvkNumber`. Pending (KvK-API down bij aanmelding; ATS-auto-aangemaakt in `AtsVacancyModerationService` ~L364) en Failed blijven publiek, ook zonder vacatures. Een nep-aanmelding onder andermans KvK-nummer krijgt dan een openbare pagina (imitatie). API geeft ook alle vestigingsadressen (bij zzp'ers vaak thuisadres), exacte lat/lng en interne company-GUIDs. | **Hoog** (privacy + misbruik) | Alleen `KvkVerificationStatus == Verified` en ≥1 openbare vacature, anders 404. DTO zonder GUIDs/coördinaten op adresniveau; per vestiging alleen plaats (straat alleen bij vestiging met openbare vacature). Sitemap (`SiteController`) uit dezelfde gefilterde query. |
| H3 | `/partner/{code}` indexeerbaar (`PageSeoCatalog` prefix `/partner/` = Public; live `index,follow`) → dubbele pagina's met salescodes in Google. | Middel | `noindex` + canonical naar `/partner`. |
| H4 | Mail-deelknop: `Uri.EscapeDataString` over een tekst die al `%0A` bevat → letterlijk "%0A" in de mail. Zit op 3 plekken: `Partner/PartnerSales.razor:189`, `Employer/Tokens.razor:649`, `SalesManager/SalesToolkit.razor:152`. | Laag (snel) | `\n` gebruiken, één keer escapen. |
| H5 | Productie: `/hoe-werkt-lobsy` heeft op main nog `[Authorize(Roles=…)]` → anoniem 302 naar login. Op acc al `[AllowAnonymous]`. | Middel | Komt mee met de volgende release acc→main (geen aparte fix nodig, wel checken). |

## 1. Bevindingen per pagina

### /hoe-werkt-lobsy (`Pages/HowLobsyWorks.razor`, `Help/HowLobsyRoleGuides.cs`)
- `InteractiveServer` met `prerender: false`: crawlers en trage telefoons zien alleen "Laden…", terwijl `PageSeoCatalog` hem indexeerbaar noemt. Hij staat ook niet in `StaticIndexablePaths` (sitemap).
- Ingelogde kandidaat wordt naar `/home` gestuurd (onverwacht bij een link uit de footer).
- `UiStringsHowLobsyRoles` kopieert NL naar pl/ro/ar (bewust), dus ar toont Nederlands in RTL.
- Gast-stap linkt naar `/` en `/register`. Dat moet passen bij de landing-flow (test eerst, dan account).
- Voorstel: statische SSR, 4 stappen + "Dit beloven we je" + FAQ; tabs Voor jou / Voor werkgevers / Voor scholen; 5 talen.

### /wie-zijn-wij (`AboutPageSettingsService`, `Admin/AboutPageAdmin.razor`)
- Body-HTML uit DB (admin-bewerkbaar, `HtmlSanitize`), alleen NL. h1 negeert de admin-titel. Foutmelding hard-coded NL.
- Prerender + interactief haalt opnieuw op → laad-flikkering.
- Default-tekst: mailto met placeholder (H1), "Via chat weet je sneller" (er is geen publieke chat), "stoffige uitzendbureau-vibes" (toon).
- Live gebruikt de DB-rij privacy@lobsy.nl; voor algemene vragen past support@ beter.
- Voorstel: vaste pagina in 5 talen (verhaal, missie, 3 waarden, contactblok met legal identity uit config), statische SSR.

### /privacy (`Pages/Legal/Privacy.razor`)
- H1 placeholders. Datum hard-coded "26 september 2026". Alleen NL (`Legal.DocNote`).
- **Verwerkers**:
  - Er staan nu: Cloudflare, Render ("EU/VS", vaag; `render.yaml`: alles in **Frankfurt**, Render is wel een VS-bedrijf), Resend, Sentry, Cursor, OpenAI, Google/Microsoft-login, Mollie, KVK, OSRM/Transitous, OpenFreeMap, YouTube/Vimeo.
  - **Ontbreekt**:
    - **Pingen** (brief-verificatie, komt met werkgever-aanmelding 06; Zwitserland = adequaatheidsbesluit)
    - **valhalla1.openstreetmap.de / tile.openstreetmap.org** (server-side statische kaart in pdf)
    - **web-push-diensten** (Google FCM / Apple / Mozilla; tabel `WebPushSubscriptions` bestaat)
- **Bewaartermijnen missen**: PersonalDataAccessLog 730 d, notificaties 365 d, feedback-screenshots 90 d, kandidaat-actietokens 30 d, onbevestigde registratie 10 min. Voorstel: tabel direct uit `PrivacyConstants`, zodat code en tekst niet uit elkaar lopen.
- Zegt dat API-credentials één keer gemaild kunnen worden. Dat klopt niet meer na de e-mail-hotfix.
- Geen `#cookies`-anker (landing-spec linkt `/privacy#cookies`).
- **Leeftijd**: tekst "vanaf 13", ouder-toestemming onder 16 (`ParentalConsentAge` 16), talentpool 18+. Dit moet consistent zijn met de gebruiksvoorwaarden.
- Voorstel: inhoudsopgave links, "In het kort"-blok per sectie in 5 talen, NL volledige tekst officieel, pdf-download, versie-datum uit één constante.

### /privacy/data (`Pages/Legal/PrivacyData.razor`)
- `[Authorize]`, prerender false, hard-coded NL, `_message = ex.Message`.
- "Exporteer JSON" zet de hele export in een `<pre>` op het scherm (meekijkers, screenshots) terwijl de tekst zegt "Bewaar dit bestand veilig". Voorstel: echte bestand-download (`lobsy-mijn-gegevens-JJJJ-MM-DD.json`), niet tonen.
- Support-inzage-regels in `yyyy-MM-dd HH:mm (UTC)`. Voorstel: lokale tijd (Europe/Amsterdam), B1-zin "Support bekeek je gegevens op 12 sep om 14:05".
- Account verwijderen via UnsubscribeDialog → `/account/logout` via GET (klein: logout via POST).

### /algemene-voorwaarden (`Pages/Legal/AlgemeneVoorwaarden.razor`)
- Geen naam/adres/KvK/btw-id van Lobsy.
- "exclusief of inclusief btw zoals in checkout" is vaag. Voorstel: B2B-prijzen excl. btw, dat altijd zo tonen.
- Typo's: "bulkapakket", "early-adapter" (→ early adopter).
- Aansprakelijkheid "12 maanden betaald of €250, het laagste" kan €0 zijn bij gratis gebruik. Dat is vreemd en mogelijk niet houdbaar. Voorstel: "het hoogste van het bedrag van de laatste 12 maanden en €250", laten toetsen.
- Geen DSA-melding (notice & action) en geen uitleg bij verwijderen van een vacature (statement of reasons).
- Datum met de hand "2 augustus 2026". Voorstel: versie-constante + wijzigingslog.

### /gebruiksvoorwaarden (`Pages/Legal/Gebruiksvoorwaarden.razor`)
- Geen identiteit, geen minimumleeftijd/minderjarigen-sectie, geen meldmechanisme.
- **Betaalde kandidaat-extra (diepte-analyse €2,99)**: geen consumenten-informatie en geen afstand van herroepingsrecht. `git grep herroep` vindt niets. Bij digitale inhoud moet de consument vooraf uitdrukkelijk instemmen en bevestigen dat de bedenktijd vervalt; anders geldt 14 dagen. Voorstel: vinkje in checkout + sectie "Betaalde extra's en bedenktijd".
- Voorstel: één pagina-stijl met schakelaar bovenaan "Voor werkgevers / Voor kandidaten" (twee URL's blijven bestaan).

### /partner (`Pages/Partner/PartnerSales.razor`, `/partner/{TrackingCode?}`)
- H3 (indexeerbaar met code) en H4 (%0A).
- Rijen "0 tokens ≈ € 0,00" (live: Vrijwilligerswerk, Flex-inzet). Voorstel: "Gratis". Nergens staat incl./excl. btw.
- Jargon: "landelijke spill", "Funda-model", "Pulse". Hard-coded share-tekst "Westland & Den Haag". Fouten tonen `ex.Message`.
- Moet achter de werkgevers-actief-schakelaar (landing-spec): uit → 302 naar `/`.

### /{kvk} (`Pages/CompanyPublicPage.razor`, API `PublicCompaniesController`)
- H2 (verificatie, adressen, coördinaten, GUIDs). Pagina's met 0 vacatures zijn indexeerbaar.
- Sitemap-crawl-index bouwt bedrijfspaden uit alle actieve records, niet uit de publiek zichtbare set (`SiteController`).
- JSON-LD gebruikt `Navigation.BaseUri` (op acc/preview wijst dat naar het verkeerde domein). Voorstel: canonical base uit config.
- Eigen kaart-kop in plaats van de publieke layout. Onbekend nummer = 404 binnen de kaartpagina.
- Voorstel: publieke kop/footer, logo + naam + plaats + "KvK gecontroleerd", vestiging-tabs, vacaturekaarten, kaart laadt na de lijst, "Klopt er iets niet? Meld het" (DSA).

## 2. Voorstel gedeelde basis

- **Eén `Legal:*`-config** (`Name`, `TradeName`, `Address`, `KvkNumber`, `VatNumber`, `PrivacyEmail`, `SupportEmail`), gebruikt door privacy, voorwaarden, wie-zijn-wij, footer, e-mail-footer (emails-stack `MailOptions`) en ErrorLayout. Lege waarde → regel weg.
- **Juridische teksten**: NL volledig en officieel; per sectie een "In het kort"-blok in nl/en/pl/ro/ar (RTL). Versie + datum uit één constante per document; wijzigingen onderaan.
- Alle pagina's in PublicLayout (landing 01, `PublicRoutes`), statische SSR waar het kan, geen `ex.Message`.
- **Verwerkers-tabel** als data (`LegalProcessors.cs`), zodat die ook in de pdf en de admin terug te zien is.

## 3. Afhankelijkheden
- `docs/landing`: PublicLayout/`PublicRoutes`, werkgevers-actief-schakelaar, `/privacy#cookies`.
- `docs/emails`: `MailOptions` legal-footer, dezelfde config gebruiken.
- `docs/werkgever-aanmelding`: Pingen (verwerker), KvK-verificatiestatus (H2).
- Stack A (foutpagina's): 404 voor `/{kvk}` en onbekende pagina's.

## 4. Mockups (`docs/mockups/public-pages/`, HTML in `html/`)

| PNG | Wat |
|---|---|
| `pb-d01-hoe-werkt-lobsy.png` / `pb-m01-…` | 4 stappen, beloftes, FAQ, rol-tabs |
| `pb-d02-wie-zijn-wij.png` / `pb-m02-…` | Verhaal, waarden, contactblok uit config |
| `pb-d03-privacy.png` / `pb-m03-privacy.png` | Inhoud links, "In het kort", identiteit, verwerkers (nieuw: Pingen, OSM, push), bewaartermijnen |
| `pb-m08-privacy-arabisch-rtl.png` | Privacy in het Arabisch (RTL) |
| `pb-d04-mijn-gegevens.png` / `pb-m04-…` | Download als bestand, inzage-log in lokale tijd, verwijderen |
| `pb-d05-algemene-voorwaarden.png` / `pb-m05-gebruiksvoorwaarden.png` | Schakelaar werkgever/kandidaat, nieuwe secties (Wie is Lobsy, Iets melden, Bedenktijd) |
| `pb-d06-partner.png` / `pb-m06-partner.png` | B1, prijzen excl. btw, "Gratis", delen |
| `pb-d07-bedrijfspagina.png` / `pb-m07-…` | `/{kvk}`: alleen gecontroleerd, plaats, vacatures, melden |

Grijze "Notities"-blokken staan alleen in de mockup. Opnieuw bouwen: `cd mockup/public && python3 build.py [filter]`.

## 5. Vragen (B1) met voorstel

1. **Bedrijfsgegevens**: naam, adres, KvK en btw-nummer op één plek (instelling), ook voor de mail-footer? *Voorstel: ja. Jij geeft de gegevens; tot die tijd laten we de regel weg.*
2. **Hotfix nu**: H1 (placeholders), H2 (/{kvk}), H3 (partner noindex) en H4 (%0A) als losse hotfix, vóór deze stack? *Voorstel: ja.*
3. **Talen**: Nederlandse tekst is officieel, plus een blok "In het kort" in 5 talen? *Voorstel: ja.*
4. **Bedrijfspagina /{kvk}**: alleen voor bedrijven met een gecontroleerd KvK-nummer en minstens één vacature, en alleen de plaats tonen? *Voorstel: ja, anders 404.*
5. **Partnerprijzen**: altijd zonder btw tonen, en "Gratis" in plaats van € 0,00? *Voorstel: ja.*
6. **Aansprakelijkheid**: maximaal wat de klant in 12 maanden betaalde, maar minstens € 250? *Voorstel: ja, een jurist kijkt mee.*
7. **Bedenktijd €2,99**: vinkje "Ik wil de analyse nu, mijn bedenktijd vervalt"? *Voorstel: ja, zonder vinkje geen start.*
8. **Meldknop**: "Meld deze vacature/dit bedrijf" (DSA) en meldingen in een admin-lijst? *Voorstel: ja.*
9. **Contact op Wie zijn wij**: support@lobsy.nl (privacy@ alleen voor privacyvragen)? *Voorstel: ja.*
10. **Wie zijn wij**: vaste tekst in 5 talen in plaats van tekst in de admin? *Voorstel: vaste tekst; admin-editor weg.*
11. **Leeftijd**: vanaf 13 jaar, onder 16 met ouders, talentpool vanaf 18, in beide teksten gelijk? *Voorstel: ja.*
12. **Mijn gegevens**: export als bestand downloaden, niet op het scherm? *Voorstel: ja.*
