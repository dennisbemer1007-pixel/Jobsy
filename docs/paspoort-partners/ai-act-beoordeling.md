# EU AI Act-beoordeling Lobsy: partnerpaspoort, fase 2 (vacatures/matching) en zelfontdekkingstools

**CONCEPT – ter toetsing door jurist** · 3 oktober 2026 · opgesteld als aanvulling op de concept-DPIA (`Lobsy_DPIA_concept.md`, §18.4)

| | |
|---|---|
| Codebasis | branch `acceptatie`, commit `7aacb6c8` (3 okt 2026, 06:34). Er is niets aan de repository veranderd. |
| Wettekst | Verordening (EU) 2024/1689 (AI-verordening), Nederlandse tekst op EUR-Lex, zoals gewijzigd door **Verordening (EU) 2026/1744** ("Digital Omnibus on AI"), PB L van 24 juli 2026. Beide teksten zijn zelf op EUR-Lex gelezen. |
| Status richtsnoeren | De Commissie-richtsnoeren over de classificatie onder art. 6 zijn een **ontwerp** van 19 mei 2026. Ze zijn niet bindend en nog niet definitief. Waar dit stuk zich op die richtsnoeren baseert, staat dat erbij. |
| Markeringen | [AANNAME: …] = niet geverifieerd, wel aannemelijk. [OPEN: …] = moet nog worden uitgezocht of besloten. |

> **Dit document is geen juridisch advies.** Het is een gestructureerde eerste analyse op basis van de wettekst, de (ontwerp)richtsnoeren en de code. De conclusies moeten worden getoetst door een jurist met kennis van de AI-verordening. Zeker de grensgevallen, die hieronder als zodanig zijn gemarkeerd.

---

## 0. Samenvatting van de conclusies

| Use case | Valt onder bijlage III, punt 4 (werkgelegenheid)? | Hoog risico? | Kernvoorwaarde om buiten hoog risico te blijven |
|---|---|---|---|
| **1. Partnerpaspoort** (uitzendbureaus / HR van werkgevers) | **Ja, als het paspoort AI-output bevat** die kandidaten beoordeelt: het AI-verhaal "Wie ben ik", RoleFit-banden ("Past goed"), matches of AI-cultuurfit. Bevat het alleen feiten die de kandidaat zelf heeft aangeleverd of bevestigd, dan valt het **waarschijnlijk buiten 4(a)** of hooguit onder de voorbereidende taak van art. 6, lid 3, onder d). | Met AI-beoordelingen erin: **ja**. Profilering sluit de uitzonderingen van art. 6, lid 3, uit. Met alleen feiten: waarschijnlijk niet. | Een aparte **"partnerweergave"** zonder AI-gegenereerde beoordelingen, scores, banden of ranking. Contractueel verbod op scoren of rangschikken door de partner. Delen alleen op initiatief van de kandidaat. |
| **2. Fase 2: vacatures en matching** | **Ja, kern van 4(a)** zodra een AI-systeem kandidaten voor werkgevers zoekt, filtert, rangschikt of beoordeelt, of vacatures gericht aanbiedt op basis van profilering. **Buiten 4(a)** als het een zoek- en aanbevelingshulp is die de kandidaat zelf start en beheert, met uitkomsten die alleen de kandidaat ziet (voorbeeld uit de ontwerprichtsnoeren). | Werkgeversgerichte AI-matching of -ranking: **ja**, zonder uitzondering (profilering). Kandidaatgedreven ontwerp: **nee**. | Kandidaatgedreven zoeken. **Geen ranking of scores van kandidaten voor werkgevers.** Werkgevers zien alleen sollicitaties die kandidaten zelf hebben verstuurd, met uitlegbare harde filters op objectieve functie-eisen. Geen AI in werkgeversgerichte output. **Let op:** de huidige code bevat al werkgeversgerichte match-% en een talentpool met persoonlijkheidsscores (zie §5.2). |
| **3. Zelfontdekkingstests en AI-tools voor kandidaten** (ook scholen, groep 7/8) | **Nee**, zolang de output alleen naar de kandidaat of leerling gaat (zelfinzicht, cv-hulp, oefengesprek). De school-leerlingtest is regelgebaseerd zonder AI en valt **buiten de definitie van AI-systeem**. | Nee. Wel **art. 50** (transparantie bij chatbots en markering van gegenereerde tekst) en **art. 4** (AI-geletterdheid). | Output niet doorzetten naar werkgevers of scholen als beoordeling. Geen emotieherkenning op basis van stem of gezicht (verboden in onderwijs en werk, art. 5, lid 1, onder f). Geen manipulatieve upsell naar minderjarigen (art. 5, lid 1, onder b). |

**Belangrijkste data** (geverifieerd in het Publicatieblad):

- art. 5 (verboden praktijken) en art. 4 (AI-geletterdheid) gelden sinds **2 februari 2025**;
- art. 50 (transparantie) geldt vanaf **2 augustus 2026**. Voor generatieve systemen die vóór die datum in de handel waren, moet de markering van art. 50, lid 2, uiterlijk op **2 december 2026** op orde zijn;
- de verplichtingen voor hoogrisicosystemen uit bijlage III (hoofdstuk III, afdelingen 1–3) gelden door de Digital Omnibus pas vanaf **2 december 2027**, in plaats van 2 augustus 2026.

**Opvallend punt:** fase 2 wordt als "later" omschreven, maar de code op `acceptatie` bevat met de featureflag `EmployersEnabled` (standaard **aan**) al werkgeversgerichte functies die bij inzet van AI in de hoogrisicozone vallen. Denk aan het matchpercentage met breakdown bij sollicitaties, de talentpool met competentie- en RIASEC-scores, en het optioneel meesturen van het AI-verhaal "Wie ben ik" met scorebalken. Deze functies zijn nu grotendeels **regelgebaseerd**, en dat beperkt de blootstelling onder de AI-verordening (zie §2). Maar het AI-verhaal "Wie ben ik" dat met sollicitaties naar werkgevers gaat, is wel LLM-output. [OPEN: welke flags staan in productie aan?]

---

## 1. Kader en toepasselijke data

### 1.1 Relevante bepalingen (geverifieerd in de NL-tekst op EUR-Lex)

- **Art. 3, punt 1, AI-systeem:** een machinegebaseerd systeem dat met een variërende mate van autonomie werkt en dat "uit de ontvangen input afleidt hoe output te genereren", zoals voorspellingen, inhoud, aanbevelingen of besluiten. **Overweging 12** sluit systemen uit "die gebaseerd zijn op regels die uitsluitend door natuurlijke personen zijn vastgesteld om automatisch handelingen uit te voeren".
- **Art. 3, punt 3, aanbieder:** wie een AI-systeem ontwikkelt (of laat ontwikkelen) en het onder eigen naam of merk in de handel brengt of in gebruik stelt, betaald of gratis.
- **Art. 3, punt 4, gebruiksverantwoordelijke (deployer):** wie een AI-systeem onder eigen verantwoordelijkheid gebruikt. Persoonlijk, niet-beroepsmatig gebruik valt hier niet onder.
- **Art. 3, punt 52, profilering:** zoals gedefinieerd in art. 4, punt 4, AVG.
- **Art. 6, lid 2, met bijlage III, punt 4, onder a):** hoog risico zijn AI-systemen die bedoeld zijn voor "het werven of selecteren van natuurlijke personen, met name voor het plaatsen van gerichte vacatures, het analyseren en filteren van sollicitaties, en het beoordelen van kandidaten". Punt 4, onder b), gaat over besluiten binnen arbeidsrelaties.
- **Art. 6, lid 3:** een bijlage III-systeem is niet hoog risico als het geen significant risico inhoudt, onder meer doordat het de uitkomst van de besluitvorming niet wezenlijk beïnvloedt. Dat is het geval als het systeem:
    - a) een beperkte procedurele taak uitvoert;
    - b) het resultaat van een eerder voltooide menselijke activiteit verbetert;
    - c) besluitvormingspatronen of afwijkingen detecteert, zonder de menselijke beoordeling te vervangen of te beïnvloeden zonder behoorlijke menselijke toetsing;
    - d) een voorbereidende taak uitvoert voor een beoordeling die relevant is voor bijlage III.
    - **Slotalinea:** "Niettegenstaande … wordt een in bijlage III bedoeld AI-systeem altijd als een AI-systeem met een hoog risico beschouwd indien het AI-systeem profilering van natuurlijke personen uitvoert."
- **Art. 6, lid 4:** wie zich op lid 3 beroept, **documenteert de beoordeling** vóór het in de handel brengen en **registreert** het systeem in de EU-databank (art. 49, lid 2).
- **Overweging 53** geeft voorbeelden:
    - (a) een systeem dat ongestructureerde data omzet in gestructureerde data;
    - (b) het verbeteren van de taal van een eerder opgesteld document;
    - (d) indexeren, zoeken en vertalen.
- **Art. 25, lid 1:** een gebruiksverantwoordelijke of andere partij wordt aanbieder van een hoogrisicosysteem als zij:
    - (a) er haar eigen naam of merk op zet;
    - (b) het systeem substantieel wijzigt; of
    - (c) het beoogde doel zo verandert dat het systeem hoog risico wordt.
- **Art. 26:** verplichtingen van de gebruiksverantwoordelijke van een hoogrisicosysteem:
    - gebruik volgens de instructies;
    - menselijk toezicht;
    - relevante inputdata;
    - monitoring;
    - logs ten minste 6 maanden bewaren;
    - lid 7: werknemers informeren;
    - lid 8: registratie door overheidsinstanties;
    - lid 11: betrokkenen informeren.
- **Art. 27:** grondrechteneffectbeoordeling (FRIA) door publiekrechtelijke organen en door particuliere entiteiten die openbare diensten verlenen.
- **Art. 50:** transparantie.
    - Lid 1: de aanbieder ontwerpt het systeem zo dat mensen weten dat zij met AI communiceren, tenzij dat evident is.
    - Lid 2: synthetische audio, beeld, video en **tekst** worden machineleesbaar gemarkeerd.
    - Lid 3: informatieplicht bij emotieherkenning of biometrische categorisering.
    - Lid 5: de informatie wordt uiterlijk bij de eerste interactie gegeven, op toegankelijke wijze.
- **Art. 86:** recht op uitleg bij besluiten op basis van output van een hoogrisicosysteem uit bijlage III.
- **Art. 5, lid 1:** verboden praktijken, onder meer:
    - (a) manipulatie;
    - (b) uitbuiting van kwetsbaarheden door **leeftijd** of sociale of economische situatie;
    - (c) sociale scoring;
    - (f) **emotieherkenning op de werkplek en in het onderwijs** (behalve om medische of veiligheidsredenen);
    - (g) biometrische categorisering.
    - Een "systeem voor het herkennen van emoties" is volgens de definitie gebaseerd op **biometrische gegevens**.

### 1.2 Data van toepassing (art. 113, zoals gewijzigd door Verordening (EU) 2026/1744)

| Onderdeel | Van toepassing vanaf | Bron / opmerking |
|---|---|---|
| Hoofdstuk I (incl. art. 4 AI-geletterdheid) en hoofdstuk II (art. 5 verboden praktijken) | 2 februari 2025 | Art. 113, onder a). |
| Nieuwe verboden in art. 5, lid 1, onder b bis) (niet-consensuele intieme deepfakes) en onder b ter) (CSAM) | 2 december 2026 | Omnibus. Niet relevant voor Lobsy-functionaliteit. |
| Art. 4, nieuwe tekst: "maatregelen nemen om de ontwikkeling van AI-geletterdheid … te ondersteunen" | Inwerkingtreding Omnibus: 27 juli 2026 (derde dag na publicatie op 24 juli 2026) | De Omnibus vervangt art. 4. Er geldt geen verplichting meer om een bepaald niveau te "waarborgen". |
| GPAI-modellen (hoofdstuk V), aangemelde instanties, governance, sancties (gedeeltelijk) | 2 augustus 2025 | Art. 113, onder b). Relevant voor **OpenAI** als aanbieder van het GPAI-model, niet voor Lobsy zelf. |
| Algemene toepassingsdatum, waaronder **art. 50** (transparantie) | 2 augustus 2026 | Art. 113, tweede alinea. Ik heb geen uitstel van art. 50, lid 1, aangetroffen. |
| Art. 50, lid 2 (markering), voor generatieve systemen die vóór 2 augustus 2026 in de handel waren | 2 december 2026 | Nieuw art. 111, lid 4 (Omnibus). [AANNAME: de LLM-functies van Lobsy waren vóór 2 augustus 2026 in gebruik genomen] |
| **Hoofdstuk III, afdelingen 1–3** (classificatie, eisen, verplichtingen aanbieder en gebruiksverantwoordelijke, FRIA) voor **bijlage III-systemen** | **2 december 2027** | Art. 113, derde alinea, onder c), zoals gewijzigd (oorspronkelijk 2 augustus 2026). |
| Idem voor producten uit bijlage I | 2 augustus 2028 | Idem. |
| Hoofdstuk III, afdeling 5 (conformiteit, registratie art. 49) | Wordt in de gewijzigde onder c) niet genoemd. Lijkt dus onder de algemene datum te vallen, maar is in de praktijk gekoppeld aan de classificatie van art. 6 (afdeling 1). | [OPEN: jurist. Vanaf wanneer gelden de documentatie (art. 6, lid 4) en de registratie (art. 49, lid 2) voor wie zich op art. 6, lid 3, beroept? Mijn lezing: feitelijk vanaf 2 december 2027.] |
| Ontwerprichtsnoeren classificatie (art. 6, lid 5) | Ontwerp 19 mei 2026, consultatie gesloten op 23 juli 2026 | [OPEN: definitieve vaststelling. Volgens een secundaire bron "eind 2026", niet geverifieerd] |

**Overige Omnibus-wijzigingen die relevant zijn voor Lobsy** (geverifieerd in de tekst van Verordening (EU) 2026/1744):

- **Art. 6, lid 3, en bijlage III zijn niet inhoudelijk gewijzigd.** De Omnibus voegt bij art. 6 alleen leden over veiligheidscomponenten toe.
- Volgens de overwegingen wordt de registratie van systemen die zich op art. 6, lid 3, beroepen, vereenvoudigd (een beperkter bijlage VIII). De documentatieplicht blijft bestaan.
- Vereenvoudigde technische documentatie en een vereenvoudigd kwaliteitsbeheersysteem voor kmo's en kleine midcaps (art. 11/17).
- Nieuw **art. 4 bis**: verwerking van bijzondere categorieën persoonsgegevens voor biasdetectie, onder voorwaarden.
- **Art. 27**: de FRIA kan verwijzen naar de DPIA. Het AI-bureau levert een sjabloon.

---

## 2. Methode: welke Lobsy-onderdelen zijn een "AI-systeem"?

De AI-verordening is alleen van toepassing op **AI-systemen**. Volgens overweging 12 en de Commissie-richtsnoeren over de definitie van AI-systeem (C(2025) 924, 6 februari 2025, randnrs. 46 en 48) vallen **"basic data processing"** en **systemen op basis van klassieke heuristiek** met vaste, door mensen geprogrammeerde regels er doorgaans buiten. Een voorbeeld is databasefiltering op criteria. Ik heb de randnummers gelezen via het zoekresultaat van de officiële PDF op de AI Act Service Desk; het volledige document is niet integraal gelezen. Tegelijk noemen dezelfde richtsnoeren en overweging 12 ook "logica- en kennisgebaseerde benaderingen" als AI-technieken. Een uitgebreid regelsysteem kan dus een grensgeval zijn. [OPEN: jurist. Valt de regelgebaseerde matchcalculator van Lobsy, met gewogen regels en drempels, buiten de definitie?]

| Onderdeel (code) | Techniek | AI-systeem? |
|---|---|---|
| Tests: Big Five / persoonlijkheid (incl. EmotionalStability), cultuur, Schwartz-waarden, RIASEC, diepteanalyse | Vragenlijsten met vaste scoringsregels | **Waarschijnlijk niet** (vaste regels door mensen). |
| `ProfileVacancyMatchCalculator` / `MatchScoreCalculator` (reistijd 40 / uren 30 / dagdelen 30; competenties, cultuur, waarden, RIASEC; drempels 50/60/70) | Vaste gewogen regels | **Waarschijnlijk niet**, maar zie het grensgeval hierboven. |
| `CultureFitBuilder` (lokaal) | Vaste regels | Waarschijnlijk niet. |
| `CultureFitAiService` (LLM-verfijning, begrensd op ±12 punten rond de lokale score, plus een "waarom"-tekst) | LLM (OpenAI gpt-4o-mini) | **Ja**. |
| Wie ben ik (AI-verhaal), CareerCompass, CompetenceDeepReport, RoleFitCheck ("Past deze baan?"), CareerPathPlan | LLM | **Ja**. |
| AssistantChat, MockInterview | LLM-chatbot | **Ja**, en daarmee art. 50, lid 1. |
| CvExtraction (cv omzetten naar gestructureerde velden) | LLM | **Ja**. Wel een typische "beperkte procedurele taak" (overweging 53). |
| Vertaling, moderatie van vacatureteksten | LLM | Ja, maar niet gericht op het beoordelen van personen. |
| `TalentPoolService.SearchAsync` (filters op tags, reistijd, vervoer, beschikbaarheid, rijbewijs) | Databasefiltering | **Waarschijnlijk niet**. |
| `YouthLaborRules` (automatische blokkade op leeftijd) | Wettelijke regels | Niet (maar AVG art. 22: zie de DPIA). |
| Scholenmodule leerlingtest (namespaces Scholen; ADR 0006 D9: geen AI/HTTP) | Vaste regels | **Niet**. |
| ElevenLabs (stem) | Gepland, **niet in de code** | n.v.t. Zie §6.4. |

**Gevolg:** de hoogrisicovraag voor Lobsy draait om twee dingen. Ten eerste: komt **LLM-output** (of later embeddings of ML) terecht in wat werkgevers, partners, gemeenten of scholen te zien krijgen? Ten tweede: blijft de regelgebaseerde matching buiten de definitie? Elke toekomstige AI-component in de werkgeversgerichte keten (zoals de geplande embeddings in `docs/vergelijkbare-vacatures`, of AI-cultuurfit in het match-%) maakt van die keten een AI-systeem onder bijlage III, punt 4, onder a).

**Los van de AI-verordening** blijven de AVG-vragen bestaan. Het gaat dan om profilering, art. 22 AVG (geautomatiseerde besluiten, zoals de jeugdarbeidsblokkade) en transparantie, ook voor regelgebaseerde systemen. Zie de DPIA, §18.1.

---

## 3. Rollen: aanbieder of gebruiksverantwoordelijke?

| Partij | Rol onder de AI-verordening | Toelichting |
|---|---|---|
| **Lobsy** | **Aanbieder** (art. 3, punt 3) van de eigen AI-systemen. Waar Lobsy de systemen zelf exploiteert (bijv. moderatie), ook gebruiksverantwoordelijke. | Lobsy bouwt de LLM-functies op een GPAI-model en stelt ze onder het eigen merk in gebruik. Dat het gratis is, maakt niet uit. |
| **OpenAI** | Aanbieder van het **GPAI-model** (hoofdstuk V) | Verplichtingen voor GPAI-modellen sinds 2 augustus 2025. Die liggen bij OpenAI, niet bij Lobsy. |
| **Kandidaten / leerlingen** | Geen rol (betrokkene) | Persoonlijk, niet-beroepsmatig gebruik (art. 3, punt 4). |
| **Partners** (uitzendbureaus, HR van werkgevers) | **Gebruiksverantwoordelijke**, maar alleen als zij een Lobsy-AI-systeem (of AI-output) beroepsmatig gebruiken. | Krijgen zij alleen feitelijke gegevens zonder AI-output, dan gebruiken ze geen AI-systeem van Lobsy. Zet een partner er zijn eigen merk op (white-label) of zet hij de output in voor een hoogrisicodoel (bijv. scoren of rangschikken), dan kan hij volgens **art. 25, lid 1, onder a) of c), zelf aanbieder** worden. |
| **Werkgevers** (fase 2) | Gebruiksverantwoordelijke als zij Lobsy-AI-output voor werving of selectie gebruiken | Bij een hoogrisicosysteem gelden de verplichtingen van art. 26 (vanaf 2 december 2027). |
| **Pay4People** (fase 2) | [OPEN: rol en contractvorm onbekend. Pay4People staat niet in de code.] Als Pay4People Lobsy-output gebruikt om kandidaten te selecteren of te plaatsen, dan is het gebruiksverantwoordelijke. Bij plaatsing alleen na een selectie door de werkgever: waarschijnlijk geen AI-rol. | Het commerciële model (ca. €2/uur of 1 token voor direct in dienst) verandert de classificatie niet. |
| **Gemeente** | Gebruiksverantwoordelijke en **overheidsinstantie** | Bij een hoogrisicosysteem gelden registratie (art. 26, lid 8, en art. 49, lid 3) en een **FRIA (art. 27)**. Zetten consulenten Lobsy-output in om kandidaten aan vacatures of trajecten toe te wijzen, dan geldt bijlage III, punt 4, onder a). In de ontwerprichtsnoeren staat precies dit voorbeeld bij arbeidsbemiddelingsorganisaties als hoog risico, zonder uitzondering van art. 6, lid 3. Mogelijk speelt ook punt 5, onder a) (beoordeling van het recht op openbare diensten of uitkeringen) [OPEN]. Gebruikt de gemeente het systeem buiten het beoogde doel, dan kan zij volgens art. 25, lid 1, onder c), aanbieder worden. |
| **Scholen** | Gebruiksverantwoordelijke **alleen als er een AI-systeem wordt gebruikt** | De huidige leerlingtest is regelgebaseerd, dus geen AI-systeem. Bij toekomstige AI: zie §6.3. |

---

## 4. Use case 1: Partnerpaspoort

### 4.1 Feiten

- **Beoogd:** een partner (uitzendbureau of HR van een werkgever) krijgt alleen het paspoort van kandidaten die **de eigen partnercode** van die partner hebben gebruikt, en alleen **met toestemming**. Lobsy geeft geen scores, ranking of automatische selectie. Het paspoort dient alleen als input voor een gesprek.
- **Code (7aacb6c8):** het delen met partners is **niet gebouwd**. De fase 1-spec van het paspoort (branch `origin/docs/mijn-paspoort`) zegt expliciet "geen deelknop". `PartnerAffiliateProfile` / `TrackingCode` (BM-/IM-) dienen voor bedrijfsverwijzingen en commissie, niet voor het delen van paspoorten.
- **Inhoud van het huidige paspoort** (`Passport.razor`):
    - tab **Dna**: testresultaten plus **AI-"Story"** (Wie ben ik, LLM);
    - tab **Tests**;
    - tab **Fit**: "Past deze baan?" (RoleFit, LLM). Banden via `RoleFitBandRules` (≥75 "Past goed", ≥50 "Past redelijk"), plus top-3-matches;
    - tab **Career**;
    - tab **Proof** (bewijs van cursussen en ervaring);
    - tab **Data**.

### 4.2 Analyse

1. **Doel: werving en selectie.** Een paspoort dat een uitzendbureau of HR-afdeling als gespreksinput gebruikt, wordt gebruikt bij "het werven of selecteren van natuurlijke personen". Volgens randnr. 245 van de ontwerprichtsnoeren omvat werving ook voorbereidende stappen.
2. **Is er een AI-systeem dat kandidaten beoordeelt?**
    - **Variant A: het paspoort zoals het nu is**, met AI-verhaal, RoleFit-banden, matches en eventueel AI-cultuurfit. Dan levert een AI-systeem **kwalitatieve oordelen** over de persoonlijkheid en geschiktheid van de kandidaat aan een werver. Randnr. 254 van de ontwerprichtsnoeren noemt "beoordelen van kandidaten" breed: scoren, rangschikken én kwalitatieve oordelen. Volgens randnr. 253 zijn geschiktheidsscores, compatibiliteitsbeoordelingen en competentieprofielen die de selectie sturen hoog risico. Dit is bovendien **profilering** (evaluatie van persoonlijke aspecten, art. 4, punt 4, AVG). Daarmee is een beroep op art. 6, lid 3, uitgesloten. **Conclusie: hoog risico.** Dat Lobsy zelf geen ranking geeft, maakt dit niet anders: het AI-oordeel zelf is de beoordeling.
    - **Variant B: een partnerweergave met alleen feiten**, dus gegevens die de kandidaat zelf heeft ingevoerd of bevestigd (werkervaring, opleiding, certificaten, beschikbaarheid, bewijsstukken, een zelfgeschreven tekst). Randnr. 254 noemt "het tonen van feitelijke informatie zonder evaluatief gewicht (zoals een ongewijzigde werkgeschiedenis)" als mogelijk buiten scope. **Conclusie: waarschijnlijk buiten 4(a).** Is CvExtraction (LLM) gebruikt om velden te vullen, dan is dat een **beperkte procedurele taak** (overweging 53: ongestructureerde data omzetten in gestructureerde data). Randnr. 253 noemt ook "beschrijvende analyse" als voorbereidende taak (art. 6, lid 3, onder d)). Mits de kandidaat de velden controleert en er geen profilering plaatsvindt, valt dit binnen art. 6, lid 3. [AANNAME: voor zuivere extractie zonder beoordeling is een beroep op art. 6, lid 3, verdedigbaar. Lobsy moet dit dan wel documenteren (art. 6, lid 4) en registreren (art. 49, lid 2) zodra die bepalingen gelden]
    - **Variant C: feiten plus testuitkomsten** (scores van regelgebaseerde vragenlijsten). Geen AI-systeem, dus buiten de AI-verordening. Maar persoonlijkheidsscores (waaronder emotionele stabiliteit) bij een werver vragen om een zware AVG-afweging. Ook speelt de vraag naar de psychometrische validiteit. [OPEN: inhoudelijke keuze. Mijn advies is ze niet standaard te delen, hooguit per onderdeel en met keuze van de kandidaat]
3. **Wie het initiatief neemt, beslist de zaak niet.** Dat de kandidaat zelf de partnercode gebruikt en toestemming geeft, is goed voor de AVG. Toch verandert het **niets** aan de classificatie van een AI-systeem dat beoordelingen aan wervers levert. Het cv-voorbeeld uit de ontwerprichtsnoeren valt alleen buiten scope omdat de output **uitsluitend naar de kandidaat** gaat.

### 4.3 Rollen

- **Lobsy:** aanbieder (in variant A van een hoogrisicosysteem).
- **Partner:** gebruiksverantwoordelijke (in variant A met de verplichtingen van art. 26). Bij white-label of gebruik voor scoren of rangschikken mogelijk aanbieder (art. 25).
- **Gemeente als partner:** zie §3 (FRIA, registratie).

### 4.4 Ontwerpadvies om buiten hoog risico te blijven

1. **Een aparte partnerweergave** (niet het volledige paspoort) met:
    - alleen door de kandidaat ingevoerde of bevestigde feiten;
    - bewijsstukken (Proof);
    - beschikbaarheid;
    - een **zelfgeschreven** tekst van de kandidaat.
2. **Niet tonen aan partners:**
    - het AI-verhaal "Wie ben ik" en andere LLM-teksten over de persoon;
    - RoleFit-banden of percentages;
    - matches;
    - cultuurfit;
    - competentie- of persoonlijkheidsscores;
    - "sterk/zwak"-labels.
3. **AI-hulp voor de kandidaat is wel mogelijk.** De kandidaat mag zijn eigen tekst laten verbeteren door AI ("verbeter mijn tekst"; overweging 53, voorbeeld b). Daarbij gelden voorwaarden:
    - de kandidaat redigeert en publiceert de tekst zelf;
    - het systeem voegt geen eigen oordeel over de persoon toe.

    [AANNAME: dit valt onder art. 6, lid 3, onder b), of buiten scope zoals het cv-hulpvoorbeeld. Het blijft een grensgeval als de AI inhoudelijke persoonlijkheidsclaims toevoegt]

4. **Delen alleen op initiatief van de kandidaat:**
    - per partner;
    - te allen tijde intrekbaar;
    - inzage in wat er gedeeld is.
5. **Geen functies bij de partner** die kandidaten sorteren, filteren of vergelijken op paspoortinhoud. Een chronologische lijst van wie heeft gedeeld volstaat.
6. **Contract en gebruiksvoorwaarden** voor partners:
    - geen geautomatiseerde scoring of ranking;
    - geen invoer van paspoortdata in eigen AI-tools;
    - geen white-label.

    Verwijs daarbij naar art. 25 en art. 5, lid 1, onder f).

7. **Monitoring:** logging van inzage door partners en signalen van misbruik.
8. **Documenteer de positionering** in een korte interne notitie: het beoogde doel, waarom het niet onder 4(a) valt of waarom art. 6, lid 3, van toepassing is, en welke functies bewust ontbreken. Het beoogde doel en de bijbehorende documentatie bepalen de classificatie.

### 4.5 Als AI-beoordelingen toch gedeeld moeten worden

Dan is het partnerpaspoort een **hoogrisicosysteem** en geldt vanaf **2 december 2027** het volgende.

- **Lobsy als aanbieder:**
    - risicobeheer (art. 9);
    - data governance (art. 10);
    - technische documentatie (art. 11, vereenvoudigd voor kmo's);
    - logging (art. 12);
    - transparantie en gebruiksinstructies (art. 13);
    - menselijk toezicht (art. 14);
    - nauwkeurigheid en robuustheid (art. 15);
    - kwaliteitsbeheersysteem (art. 17);
    - conformiteitsbeoordeling (art. 43; voor bijlage III, punt 4, interne controle volgens bijlage VI [AANNAME: niet nader nagelezen]);
    - EU-conformiteitsverklaring en CE-markering (art. 47–48);
    - registratie (art. 49, lid 1);
    - monitoring na het in de handel brengen (art. 72).
- **Partners:** de verplichtingen van art. 26 (menselijk toezicht, logs, informeren van kandidaten). Overheidspartners bovendien de FRIA (art. 27) en registratie.
- **Kandidaten:** recht op uitleg (art. 86).

**Advies:** vermijd dit. De meerwaarde voor een gesprek staat niet in verhouding tot de lasten en het discriminatierisico (overweging 57).

---

## 5. Use case 2: Fase 2, vacatures en matching

### 5.1 Beoogd ontwerp (volgens de opdracht)

- Kandidaten geven zelf aan "beschikbaar voor werk" te zijn (opt-in).
- Ze krijgen een in-appmelding: "Lobsy kan nu passend werk vinden".
- Werkgevers plaatsen vacatures.
- Plaatsing loopt via Pay4People (ca. €2/uur) of als direct in dienst voor 1 token.

### 5.2 Wat er al in de code staat (`EmployersEnabled`, standaard **aan**)

Volgens `docs/feature-flags.md` hoort dit bij "het product van vandaag". [OPEN: waarde in productie]

- **Sollicitaties:**
    - de werkgever ziet `MatchPercent`, `MatchBreakdownJson`, `ViaSafetyNet`, leeftijd en `WorkPermitConfirmed` (`ApplicationsController`, RequireAdminOrEmployer);
    - het match-% komt uit de regelgebaseerde `ProfileVacancyMatchCalculator`, inclusief competenties, cultuur en persoonlijkheid (incl. emotionele stabiliteit), waarden en RIASEC.
- **AI-verhaal bij sollicitatie:** bij het solliciteren slaat `ApplicationsController` een momentopname op van het **AI-verhaal "Wie ben ik"** (`SnapshotWhoAmIJson`), met competentie- en cultuurscorebalken. Die gaat naar de werkgever (Lobsy-CV) **als de kandidaat "meesturen met cv" heeft aangezet**. `CandidateWhoAmIProfile.IncludeOnCv` staat standaard op false; het is dus opt-in. Dit is **LLM-output met een oordeel over de persoon, bij een werkgever in een selectieproces**. Zie §5.3 punt 3.
- **Talentpool** (`TalentPoolService`):
    - kandidaten met OpenForWork, toestemming en 18+;
    - werkgevers filteren op tags, reistijd, vervoer, beschikbaarheid en rijbewijs;
    - zij zien anonieme kaarten **met CompetencyScores (incl. Stressbestendigheid), RIASEC-scores, Holland-code en matchtags**;
    - contact ontgrendelen kost 1 token. Dit is "identificatie van potentiële kandidaten in databases" (randnr. 246).
- **AI-cultuurfit** (LLM, begrensd tot ±12 rond de lokale score) wordt alleen aan de **kandidaat** getoond op de vacaturepagina. Het lijkt niet in het match-% van de werkgever te zitten. [AANNAME: op basis van codelezing; verifiëren]
- **Kandidaatinzichten:** alleen aggregaten voor werkgevers (k≥10). Er worden geen individuele personen beoordeeld, dus valt dit waarschijnlijk buiten 4(a).
- **Pay4People:** niet in de code.

### 5.3 Analyse per onderdeel

| Onderdeel | 4(a)? | Art. 6, lid 3? | Inschatting |
|---|---|---|---|
| **Vacature-aanbevelingen aan de kandidaat**, door de kandidaat gestart, alleen zichtbaar voor de kandidaat en buiten controle van de werkgever | De ontwerprichtsnoeren noemen precies dit (de kandidaat helpen de beste functie te vinden; aanbevelingen alleen voor de kandidaat, door de kandidaat gestart en beheerd, buiten controle van de werkgever) als **buiten 4(a)** | n.v.t. | **Geen hoog risico**, ook als er AI wordt gebruikt. |
| **Melding "Lobsy kan nu passend werk vinden"** na opt-in | Grensgeval met "gerichte vacatures" (randnr. 250: gerichte vacatures op basis van profilering zijn altijd hoog risico). Ligt het initiatief bij de kandidaat en bepaalt de werkgever niet wie de vacature ziet, dan sluit dit aan bij het voorbeeld hierboven. Volgens randnr. 252 kan personalisatie op basis van objectieve, inherente functie-eisen of puur contextuele targeting buiten "gerichte vacatures" vallen. | Profilering sluit uit | **Buiten hoog risico mits:** (a) de melding en aanbevelingen alleen voortkomen uit voorkeuren die de kandidaat zelf heeft ingesteld en uit objectieve criteria; (b) werkgevers geen doelgroep kunnen kiezen of betalen voor bereik op basis van een profiel; (c) werkgevers niet te zien krijgen wie een aanbeveling kreeg. |
| **Werkgevers zoeken in de talentpool** | Ja, als het een AI-systeem is: identificatie van kandidaten in databases (randnr. 246) en sourcing met shortlists (voorbeeld van hoog risico in de richtsnoeren) | Profilering via competentie- en RIASEC-scores sluit uit | **Nu regelgebaseerd, dus waarschijnlijk geen AI-systeem.** Elke AI-ranking of embedding-zoekfunctie maakt het hoog risico. Ook zonder AI blijven persoonlijkheidsscores op anonieme kaarten een AVG- en discriminatierisico. |
| **Match-% / breakdown / "top"-lijsten bij sollicitaties voor werkgevers** | Ja, als het een AI-systeem is: "automatische matching- en rankingtool voor wervers" is een voorbeeld van hoog risico | Profilering sluit uit | **Nu regelgebaseerd** (zie het grensgeval in §2). Met een LLM of embeddings in deze keten: **hoog risico**. |
| **AI-verhaal "Wie ben ik" mee met de sollicitatie** (opt-in van de kandidaat) | De output komt bij de werkgever en bevat een kwalitatief oordeel over de persoonlijkheid (randnr. 254). Het is *niet* uitsluitend voor de kandidaat. | Profilering sluit uit | [OPEN: grensgeval / jurist]. Verdedigbaar als "cv-hulp" alleen als de kandidaat de tekst kan bewerken, er eigenaar van is en er geen scores bij staan. **Advies:** scorebalken weghalen, de kandidaat de tekst laten redigeren en duidelijk labelen als zelfpresentatie van de kandidaat. Of het AI-verhaal niet laten meesturen. |
| **Vacaturetekst genereren of vertalen, moderatie** | Een functieomschrijving genereren uit taken die mensen hebben opgesteld: beperkte procedurele taak | (a) | Geen hoog risico (documenteren). |
| **CV-extractie** voor het eigen profiel van de kandidaat | Binnen scope als de gegevens bij wervers terechtkomen ("CV-informatie ordenen in doorzoekbare database": vrijgesteld in de richtsnoeren) | (a)/(d) | Vrijstelling mogelijk. **Documenteren en registreren** (art. 6, lid 4, art. 49, lid 2). |
| **Automatische blokkade op leeftijd** (`YouthLaborRules`) | Geen AI | n.v.t. | AVG art. 22: zie de DPIA. |
| **Pay4People / 1 token** | Het verdienmodel bepaalt de classificatie niet | n.v.t. | [OPEN: rol Pay4People] |

### 5.4 Positioneringsadvies voor fase 2: "kandidaatgedreven bemiddeling"

1. **De kandidaat zoekt, niet de werkgever.**
    - Matching werkt richting de kandidaat: Lobsy rangschikt **vacatures voor de kandidaat**, nooit **kandidaten voor een werkgever**.
    - De kandidaat beslist of hij solliciteert.
    - Werkgevers krijgen alleen kandidaten te zien die **zelf hebben gesolliciteerd** of zelf contact hebben gezocht.
2. **Geen ranking of scores van kandidaten voor werkgevers.**
    - Sollicitaties verschijnen chronologisch of op een neutrale, uitlegbare volgorde.
    - Geen match-% of breakdown, geen "sterk/zwak"-labels, geen top-lijsten.
    - Geen persoonlijkheids-, competentie- of RIASEC-scores, geen AI-cultuurfit en geen AI-teksten over de persoon.
3. **Uitlegbare harde filters op objectieve functie-eisen**, ingesteld door de werkgever:
    - reistijd, uren, dagdelen, rijbewijs, verplichte certificaten, werkvergunning, wettelijke minimumleeftijd;
    - per kandidaat zichtbaar als ja/nee-checklist ("voldoet aan: rijbewijs B ✔, beschikbaar za ✔");
    - geen gewogen totaalscore;
    - geen filters op persoonskenmerken die tot discriminatie kunnen leiden. De code wijst leeftijdsfilters al expliciet af; dat is goed.
4. **De talentpool omvormen tot "kandidaat benadert werkgever"** of tot een anonieme kaart met alleen feiten en harde criteria:
    - geen competentie- of RIASEC-scores;
    - geen sortering op "fit";
    - alternatief: alleen een melding aan de kandidaat ("werkgever X zoekt iemand met rijbewijs in jouw regio; wil je reageren?").
5. **Meldingen en aanbevelingen** alleen op basis van door de kandidaat ingestelde voorkeuren en objectieve vacaturekenmerken:
    - werkgevers kunnen niet betalen voor gerichte verspreiding op basis van een profiel;
    - een uitlegregel bij elke aanbeveling ("omdat je zocht naar: horeca, ≤30 min, weekend").
6. **Geen AI in werkgeversgerichte output.**
    - LLM of embeddings alleen aan de kant van de kandidaat (zoeken, uitleg, cv-hulp).
    - Wordt de regelgebaseerde matching ooit vervangen door embeddings of ML, dan verandert de classificatie. Neem dit op als **vaste gate in het ontwikkelproces** (AI Act-check bij elke PR die werkgeversgerichte output raakt).
7. **Gemeente en consulenten:** geen "consulentenweergave" die kandidaten aan vacatures koppelt of rangschikt. Dat is het hoogrisicovoorbeeld van de arbeidsbemiddelingsorganisatie in de ontwerprichtsnoeren. Een gemeente kan wel kandidaten wijzen op de kandidaatgedreven tool.
8. **Documentatie:** het beoogde doel staat in de gebruiksvoorwaarden voor werkgevers en partners. Daarin staat ook een verbod op het exporteren van data naar eigen AI-ranking. Daarnaast een interne classificatienotitie per functie (zie §4.4 punt 8).

### 5.5 Als hoog risico niet te vermijden is

Bijvoorbeeld als werkgevers wél een AI-ranking of een "beste kandidaten"-voorstel krijgen.

- **Planning:** de verplichtingen gelden vanaf **2 december 2027**. Begin minstens 12 maanden van tevoren [AANNAME: ervaringsgetal, geen wettelijke termijn].
- **Lobsy als aanbieder:**
    - art. 9–15 (risicobeheer, data governance inclusief biastests, eventueel met gebruik van art. 4 bis; logging; instructies; menselijk toezicht; nauwkeurigheid);
    - art. 17 (kwaliteitsbeheersysteem; vereenvoudigd voor kmo's);
    - art. 43 en bijlage VI (interne controle);
    - art. 47–49 (verklaring, CE-markering, registratie);
    - art. 72–73 (monitoring na het in de handel brengen, melden van ernstige incidenten).
- **Werkgevers, Pay4People, gemeente als gebruiksverantwoordelijken:**
    - art. 26: menselijk toezicht door bekwame personen, logs ≥6 maanden, informeren van kandidaten (lid 11) en van werknemersvertegenwoordigers (lid 7);
    - gemeente bovendien registratie (art. 26, lid 8) en FRIA (art. 27, met verwijzing naar de DPIA mogelijk).
- **Kandidaten:** recht op uitleg (art. 86). Daarnaast AVG art. 22 en de uitlegplicht.
- **Ontwerp:**
    - uitlegbare factoren per kandidaat;
    - geen persoonlijkheidskenmerken als selectiefactor;
    - een menselijke beslisser met een overrule-mogelijkheid;
    - regelmatige biasaudits (art. 6, lid 3, onder c), voorbeeld "audit op geanonimiseerde data").

---

## 6. Use case 3: Zelfontdekkingstests en AI-tools voor kandidaten (incl. scholen, groep 7/8)

### 6.1 Bijlage III, punt 4 (werkgelegenheid)

Wanneer de output **alleen naar de kandidaat** gaat, valt het volgende **buiten 4(a)**:

- zelfinzicht (Wie ben ik, CareerCompass, CompetenceDeepReport, CareerPathPlan);
- RoleFitCheck ("Past deze baan?" als hulp voor de kandidaat);
- cv-hulp;
- het oefengesprek (MockInterview);
- de assistent (AssistantChat).

De ontwerprichtsnoeren noemen "kandidaten helpen hun cv aan te passen (output alleen voor de kandidaat)" en "kandidaten helpen de beste functie te vinden" uitdrukkelijk als buiten 4(a).

**Voorwaarde:** deze outputs gaan niet als beoordeling naar werkgevers, partners, gemeenten of scholen. Zie §4 en §5.3 voor het AI-verhaal "Wie ben ik".

### 6.2 Art. 6, lid 3, en profilering

Niet nodig zolang de functies buiten 4(a) blijven. Let wel: zelfinzicht op basis van persoonlijkheidstests is **profilering** in de zin van de AVG. Zodra de output in een werving- of selectiecontext terechtkomt, is een beroep op art. 6, lid 3, daarom uitgesloten.

### 6.3 Scholen (groep 7/8, minderjarigen): bijlage III, punt 3

- **Huidige code:** de leerlingtest is regelgebaseerd, zonder AI- of HTTP-aanroepen in de Scholen-namespaces (ADR 0006 D9). Er zijn pseudonieme codes en aggregaten met k≥5. Dat is **geen AI-systeem**: de AI-verordening is niet van toepassing op deze module. De AVG en de bescherming van kinderen blijven onverminderd gelden (zie DPIA §18.2).
- **Als er later AI aan wordt toegevoegd:**
    - **3(a) toegang, toelating, toewijzing:** buiten scope zolang het gaat om oriëntatie of inspiratie die de leerling zelf krijgt. De ontwerprichtsnoeren noemen een matchingplatform voor opleidingen dat leerlingen in het voortgezet onderwijs aanbevelingen geeft op basis van hun eigen voorkeuren als buiten 3(a).
    - **3(b) evaluatie van leerresultaten:** formatieve feedback valt buiten scope.
    - **3(c) passend onderwijsniveau:** **wel hoog risico** zodra de output het niveau of advies bepaalt of wezenlijk beïnvloedt (bijv. bij het schooladvies in groep 8). **Advies:** Lobsy-output nooit laten gebruiken voor het schooladvies of voor plaatsing. Leg dat vast in de schoolovereenkomst en de instructies.
    - **3(d) toezicht op verboden gedrag tijdens toetsen:** niet van toepassing.
- **Rol van de school:** gebruiksverantwoordelijke alleen bij gebruik van een AI-systeem. Art. 4 (AI-geletterdheid) geldt dan ook voor de leerkrachten die het gebruiken.

### 6.4 Art. 5: verboden praktijken

- **5(1)(f) emotieherkenning in onderwijs en werk.** Lobsy gebruikt **geen biometrische gegevens**. Tests zijn zelfrapportagevragenlijsten, en MockInterview en de chat verwerken getypte tekst. Volgens de ontwerprichtsnoeren (classificatie) is emotie afleiden uit getypte tekst geen biometrie. Dit verbod is daarom nu **niet van toepassing**. De schaal "emotionele stabiliteit" is een zelfgerapporteerde persoonlijkheidsdimensie, geen emotieherkenning.
    - **Risico bij ElevenLabs/stem (gepland):** emoties, stress of intenties afleiden uit stem of gezicht is biometrische emotieherkenning. In het onderwijs (scholen) en op de werkplek is dat **verboden**. Volgens de Commissie-richtsnoeren over verboden praktijken (2025) valt **ook de wervingsfase** onder "werkplek". Dat is gelezen in secundaire bronnen en zoekresultaten van de officiële PDF, niet integraal; volgens een secundaire bron betreft het randnr. 254.
    - Een uitzondering voor "persoonlijke training zonder effect op de arbeidsrelatie" wordt in secundaire bronnen genoemd. [OPEN: niet zelf geverifieerd. Niet op bouwen]
    - **Advies:** stem alleen gebruiken voor tekst-naar-spraak of spraak-naar-tekst, zonder enige afleiding van emotie, stress, zelfvertrouwen of "enthousiasme". Leg dat contractueel en technisch vast (geen ElevenLabs-functies voor emotie- of sentimentanalyse).
- **5(1)(b) uitbuiting van kwetsbaarheden door leeftijd of sociaal-economische situatie** (kinderen, arbeidsmigranten, mensen met een uitkering). Gamificatie en upsell (betaalde diepteanalyse via Mollie) zijn op zich geen AI-praktijk. Het verbod is relevant zodra een **AI-systeem** gedrag wezenlijk verstoort met aanzienlijke schade als (beoogd) gevolg.
    - **Advies:** geen AI-gestuurde verkoopprikkels of personalisatie van aanbiedingen richting minderjarigen of kwetsbare groepen;
    - geen betaalde functies binnen de scholenmodule [AANNAME: nu niet aanwezig in de scholenmodule].
- **5(1)(c) sociale scoring.** In de code heb ik geen beoordeling van kandidaten op basis van sociaal gedrag aangetroffen. De "engagement"-bonus in de vacature-discovery gaat over **werkgeversclaims** (`VacancyEngagementItem`), niet over gedrag van kandidaten. **Advies:** geen gedragsdata uit andere contexten gebruiken en geen AI-"betrouwbaarheidsscores" over personen maken.
- **5(1)(a) manipulatie en 5(1)(g) biometrische categorisering:** geen aanwijzingen in de code (geen foto- of stemanalyse).

### 6.5 Art. 50: transparantie (vanaf 2 augustus 2026)

- **50(1) chatbots** (AssistantChat, MockInterview, en eventuele stemassistent):
    - bij de eerste interactie duidelijk melden dat de gebruiker met AI praat;
    - begrijpelijk voor kinderen en laaggeletterden;
    - meertalig (arbeidsmigranten).
    - [OPEN: controleren of de huidige UI dit al doet; niet nagelopen]
- **50(2) markering van synthetische tekst** (Wie ben ik, rapporten, loopbaanplan, AI-cultuurfit "waarom"-tekst, RoleFit-uitleg):
    - Lobsy moet als aanbieder zorgen voor machineleesbare markering, bijv. metadata in API-respons en pdf. Die moet "voor zover technisch haalbaar" zijn.
    - Uitzondering bij "ondersteunende functie voor standaardbewerking" of als de input niet wezenlijk wordt gewijzigd. Die geldt wellicht voor vertaling en cv-extractie, **niet** voor de gegenereerde verhalen.
    - Termijn voor systemen die al in gebruik waren: **2 december 2026**.
    - Volg de gedragscode voor markering (art. 50, lid 7, door de Omnibus vervangen). [OPEN: inhoud van de gewijzigde art. 50, lid 7, en de gedragscode niet in detail gelezen]
- **Zichtbaar label**, aanbevolen maar niet verplicht volgens 50(2): "Deze tekst is met AI gemaakt" bij AI-teksten. Zeker als ze met een cv naar een werkgever gaan.
- **50(3):** niet van toepassing (geen emotieherkenning of biometrische categorisering).
- **50(4):** niet van toepassing (geen deepfakes; geen publicatie over aangelegenheden van algemeen belang).

### 6.6 Art. 4: AI-geletterdheid

- **Lobsy:** sinds de Omnibus geldt de plicht om maatregelen te nemen die de ontwikkeling van AI-geletterdheid **ondersteunen**, voor personeel en anderen die namens Lobsy AI-systemen bedienen. Denk aan ontwikkelaars, support, sales en admins die moderatie of kandidaatdata zien.
    - **Advies:** een korte interne training en een vastgelegd AI-beleid (welke AI, welke grenzen, wat nooit naar werkgevers gaat), met jaarlijkse update.
- **Partners, werkgevers, gemeente, scholen** hebben als gebruiksverantwoordelijke een eigen art. 4-plicht, alleen als zij AI-systemen gebruiken. Lobsy kan dit ondersteunen met een korte uitleg voor gebruikers ("wat doet de AI in Lobsy en wat niet").
- Kandidaten en leerlingen vallen niet onder art. 4. Voor hen gelden art. 50 en de AVG-transparantie.

---

## 7. Ontwerpchecklist "buiten hoog risico"

| # | Maatregel | Use case | Prioriteit |
|---|---|---|---|
| 1 | Partnerweergave van het paspoort met alleen bevestigde feiten en Proof; geen AI-verhaal, RoleFit, matches, scores | 1 | Vóór de lancering van het partnerpaspoort |
| 2 | Delen alleen per partner, op initiatief van de kandidaat, intrekbaar, met een log | 1 | Idem |
| 3 | Partnervoorwaarden: geen scoring of ranking, geen invoer in eigen AI, geen white-label, geen emotieherkenning | 1, 2 | Idem |
| 4 | Werkgevers zien geen match-% of breakdown en geen persoonlijkheids-, competentie- of RIASEC-scores bij sollicitaties of in de talentpool. Vervangen door een ja/nee-checklist op harde functie-eisen | 2 | **Nu**, als `EmployersEnabled` in productie aan staat (vooral een AVG-winst; AI Act-relevant zodra er AI in de keten komt) |
| 5 | Standaard chronologische volgorde, geen "top"-lijsten voor werkgevers | 2 | Nu / fase 2 |
| 6 | AI-verhaal "Wie ben ik" bij sollicitatie: scorebalken eruit, laten bewerken door de kandidaat, label "zelfpresentatie, met AI geschreven". Of niet meesturen | 2 | Nu (grensgeval; jurist) |
| 7 | Vacature-aanbevelingen en meldingen alleen richting de kandidaat, op basis van eigen voorkeuren en objectieve criteria, met een uitlegregel. Geen door werkgevers betaalde targeting | 2 | Fase 2-ontwerp |
| 8 | Geen consulentenweergave voor de gemeente die kandidaten koppelt of rangschikt | 2 | Vóór de gemeentepilot |
| 9 | Ontwikkelgate: elke PR die LLM, embeddings of ML aan werkgevers-, partner-, gemeente- of schoolgerichte output toevoegt, krijgt een AI Act-check | alle | Nu |
| 10 | Classificatienotitie per AI-functie (beoogd doel, waarom buiten 4(a) of waarom art. 6, lid 3). Bij art. 6, lid 3: documentatie (art. 6, lid 4) en registratie (art. 49, lid 2) vóór de toepassingsdatum | alle | Q1 2027 |
| 11 | Art. 50(1)-melding bij AssistantChat en MockInterview | 3 | **Nu** (geldt sinds 2 aug 2026) |
| 12 | Art. 50(2)-markering van AI-tekst (metadata en pdf) | 3 | Uiterlijk 2 dec 2026 |
| 13 | Stem (ElevenLabs): alleen TTS/STT, geen emotie- of stressanalyse; contractueel uitsluiten | 3 | Vóór de bouw |
| 14 | Scholen: AI uit de leerlingmodule houden. Contractueel geen gebruik voor schooladvies of plaatsing | 3 | Nu (schoolovereenkomst) |
| 15 | Geen AI-gestuurde upsell of personalisatie naar minderjarigen of kwetsbare groepen | 3 | Nu |
| 16 | Art. 4: intern AI-geletterdheidsbeleid en training; uitleg voor partners en scholen | alle | Nu |

---

## 8. Open punten

1. [OPEN] Productiewaarden van `EmployersEnabled` en `CandidatePassportEnabled`. Draaien de werkgeversfuncties uit §5.2 al live?
2. [OPEN] Exacte inhoud van het partnerpaspoort. Welke tabs of velden ziet een partner?
3. [OPEN] Rol en contractvorm van Pay4People. Selecteert Pay4People zelf kandidaten?
4. [OPEN] Komt AI-cultuurfit echt nergens bij werkgevers terecht? Codeverificatie van alle werkgeversendpoints.
5. [OPEN] Valt de regelgebaseerde matchcalculator buiten de definitie van AI-systeem (grensgeval "logica-gebaseerd")?
6. [OPEN] Het AI-verhaal "Wie ben ik" bij sollicitaties: cv-hulp van de kandidaat (buiten scope) of beoordeling van de kandidaat (4(a))?
7. [OPEN] Toepasselijkheid van art. 6, lid 4, en art. 49, lid 2, vóór 2 december 2027 (zie §1.2).
8. [OPEN] De definitieve Commissie-richtsnoeren over classificatie (de ontwerptekst kan nog wijzigen). Herbeoordeling na vaststelling.
9. [OPEN] De gemeente: bijlage III, punt 5, onder a), als Lobsy wordt ingezet bij re-integratie of uitkeringsvoorwaarden.
10. [OPEN] De gewijzigde art. 50, lid 7, en de gedragscode voor markering: nog niet in detail gelezen.
11. [OPEN] Wordt de art. 50(1)-melding al getoond in AssistantChat en MockInterview?

---

## 9. Bronnen

**Primair (zelf gelezen):**

- Verordening (EU) 2024/1689 (AI-verordening), NL, PB L, 12.7.2024: https://eur-lex.europa.eu/legal-content/NL/TXT/HTML/?uri=OJ:L_202401689
    - Gebruikt: art. 3, 4, 5, 6, 25, 26, 27, 49, 50, 86, 113, bijlage III en overwegingen 12, 53, 57.
- Verordening (EU) 2026/1744 (Digital Omnibus on AI), NL, PB L, 24.7.2026: https://eur-lex.europa.eu/legal-content/NL/TXT/HTML/?uri=CELEX:32026R1744
    - Gebruikt: wijzigingen van art. 4, 4 bis, 5, 6, 11, 17, 25, 27, 50, 111 en 113.
- Europese Commissie, ontwerprichtsnoeren over de classificatie van AI-systemen met een hoog risico (19 mei 2026, **ontwerp**):
    - overzichtspagina: https://digital-strategy.ec.europa.eu/en/library/draft-commission-guidelines-classification-high-risk-ai-systems
    - deel bijlage III: https://ec.europa.eu/newsroom/dae/redirection/document/128561 (vooral §3.4.2, randnrs. 245–254 en de voorbeelden over werkgelegenheid en onderwijs)
    - algemeen deel: https://ec.europa.eu/newsroom/dae/redirection/document/128559
- Code: branch `acceptatie`, commit `7aacb6c8`. Onder meer:
    - `docs/feature-flags.md`
    - `Jobsy.Api/Controllers/ApplicationsController.cs` (`SnapshotWhoAmIJson`, `MatchPercent`)
    - `Jobsy.Infrastructure/Services/WhoAmIService.cs` (`GetCvAttachmentAsync`, `IncludeOnCv`)
    - `TalentPoolService`, `ProfileVacancyMatchCalculator`, `CultureFitAiService`
    - `Jobsy.Web/Components/Pages/Candidate/Passport.razor`
    - ADR 0006 (scholen)

**Gedeeltelijk gelezen (via zoekresultaten en uittreksels van de officiële PDF, niet integraal):**

- Commissie-richtsnoeren over de definitie van een AI-systeem, C(2025) 924 (randnrs. 46 en 48): https://ai-act-service-desk.ec.europa.eu/sites/default/files/2025-08/commission_guidelines_on_the_definition_of_an_artificial_intelligence_system_established_by_regulation_eu_20241689_ai_actenglish_nf2skcqfrtjdfggjavcodopcwz4_112455.PDF
- Commissie-richtsnoeren over verboden AI-praktijken (2025), in het bijzonder art. 5, lid 1, onder f), en de werkplek inclusief werving: https://ai-act-service-desk.ec.europa.eu/sites/default/files/2025-08/guidelines_on_prohibited_artificial_intelligence_practices_established_by_regulation_eu_20241689_ai_act_english_ied3r5nwo50xggpcfmwckm3nuc_112367-1.PDF

**Secundair (alleen ter oriëntatie, niet als basis voor conclusies):** blogs van advocatenkantoren en organisaties over de richtsnoeren voor verboden praktijken (Freshfields, CDT, Wolters Kluwer), gebruikt voor het "werving = werkplek"-punt. Dat punt staat ook in het zoekresultaat van de officiële PDF.

*Opmerking over de werkwijze:* EUR-Lex blokkeert geautomatiseerde ophaalverzoeken. De teksten zijn daarom met een headless browser van de officiële EUR-Lex-URL's opgehaald en als tekst doorzocht. Citaten zijn uit de Nederlandse versie overgenomen. Waar dit document parafraseert, is dat bedoeld als samenvatting en niet als wettekst.
