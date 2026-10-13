# Runbook: maandelijkse sales uitbetaalronde

Stappen voor Lobsy admin (Europe/Amsterdam).

1. **Open de ronde** — Op de 1e werkdag vanaf 06:00 maakt de job automatisch een concept-ronde (`Draft`) en zet openstaande aanvragen op `InRun`. Of: maak handmatig een **Extra ronde** op `/admin/sales-managers?tab=uitbetalingen`.
2. **Check signalen** — Open de ronde. Let op vlaggen: nieuwe rekening (IBAN-hold), geen toestemming, saldo te laag, onvolledig profiel. Wijs individuele regels af met een reden (5–500 tekens) indien nodig.
3. **Goedkeuren** — **Ronde goedkeuren** (MFA-sessie). Lobsy maakt self-billing facturen (`SB-{jaar}-{seq}`) voor goedgekeurde regels. Uitgestelde regels gaan terug naar `Requested` voor de volgende ronde.
4. **SEPA downloaden** — Download het **SEPA-bestand** (`pain.001.001.03`) of de CSV “handmatig overmaken”. Upload het SEPA-bestand in de bank. Debiteur: `Sales:Payout:DebtorName` / `DebtorIban` / `DebtorBic`.
5. **Markeer als betaald** — Zodra de bank heeft overgemaakt: **Markeer als betaald**. Dit sluit de uitbetalingsaanvraag, boekt de `Payout`-ledgerregel en zet de ronde op `Paid`/`Closed`.

**Geparkeerde ambassadeurs:** tegoeden verschijnen onder de rondes; niet automatisch uitbetalen. Alleen correctie boeken (MFA).

**Admin redesign 06.4:** host `PayoutRunsSection` in tab **Rondes** op `/admin/financien/uitbetalingen` en houd mark-paid gekoppeld aan het sluiten van payout requests.
