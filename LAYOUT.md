Hier is het ontwerpbestand **`LAYOUT.md`** voor de gebruikersinterface. Dit legt de "Funda-formule" vast: de legendarische, supergebruiksvriendelijke 50/50 split-screen layout met een vloeiende switch, zodat Cursor precies weet hoe de frontend ontworpen moet worden.

---

### 9. `LAYOUT.md`

```markdown
# UI/UX Layout Specificatie: Jobsy (De "Funda-Formule")

## 1. Visuele Filosofie
Geen eindeloze lijsten of onoverzichtelijke swipemenu's. Jobsy gebruikt de beproefde **Funda-layout**: maximale controle voor de gebruiker door een gelijktijdige, synchrone weergave van een lijst met vacatures en een interactieve kaart.

## 2. De Schermindeling (Desktop & Tablet)
Het scherm is opgedeeld in een **vaste 50/50 of 40/60 split-screen**:
- **Linkerkant (De Lijst & Filters):** 
  - Bovenaan een strakke filterbalk (Reistijd in minuten, Vervoersmiddel: Fiets/Auto/OV, en Straal in kilometers).
  - Daaronder een oneindige scrolllijst met vacature-kaarten. Elk kaartje toont:
    - Functietitel en Bedrijfsnaam.
    - Uurloon en reistijd-indicator (bijv. *"12 min met de e-bike"*).
    - Subtiele badges voor vereist vervoer.
- **Rechterkant (De Interactieve Kaart):**
  - Een vaststaande, full-height OpenStreetMap (via Leaflet.js).
  - Visuele markers voor elke vacature in de lijst.
  - **De Synchronisatie:** Als je met je muis over een vacature in de lijst beweegt, licht de bijbehorende marker op de kaart op (en vice versa). Als je op een marker klikt, opent direct de detailkaart van die vacature.

## 3. Mobiele Ervarenheid (Responsive / MAUI)
Op een kleiner mobiel scherm werkt de Funda-formule net even anders dankzij een **sublieme switch**:
- **De Toggle-knop:** Onderaan in het midden van het scherm zweeft een prominente knop met twee toestanden:
  - `[ Kaart tonen ]` of `[ Lijst tonen ]`.
- **De Transitie:** Met één tap schakelt het scherm direct om van de volledige lijst naar een interactieve schermvullende kaart (met losse vacature-popups onderin), zonder dat de gebruiker de context verliest.

## 4. Kleurgebruik & Rust (Clean & Professioneel)
- **Rustige achtergronden:** Wit en lichtgrijs voor een professionele, overzichtelijke uitstraling (geen visuele ruis).
- **Accentkleur (Call-to-Action):** Een krachtige, frisse accentkleur (bijvoorbeeld energiek groen of diepblauw) voor knoppen zoals "Direct Solliciteren" of actieve filters.
- **Typografie:** Schoon, schreefloos lettertype (Inter of Roboto) met duidelijke hiërarchie in koppen en uurlonen.

```