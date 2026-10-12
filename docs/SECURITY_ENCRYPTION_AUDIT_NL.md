# Encryptie- & beveiligingsaudit Lobsy (applicatie)

Datum: 2026-10-12 · Branch: `cursor/field-encryption-security-audit-9a88` · Doel: acceptatie-review (niet mergen via deze PR).

## Samenvatting

De codebase had al sterke baselines (PBKDF2-wachtwoorden, HMAC OTP, Data Protection voor IBAN/integratiesecrets, CSP/HSTS, cookie-hardening, progressive disclosure). Deze wijziging voegt **AES-256-GCM veldencryptie** toe voor de gevoeligste PII-kolommen, **blind indexes** voor e-maillogin, **SSL-afdwinging** op Postgres-connection strings, **versleutelde Data Protection XML** in Postgres, en een **idempotente backfill** bij startup.

---

## Bevindingen (risico)

| Onderwerp | Risico | Status |
|-----------|--------|--------|
| PII in Postgres leesbaar bij DB-lek (e-mail, telefoon, adres in JSON, CV-bytes, sollicitatie-snapshots) | **Hoog** | **Gedeeltelijk opgelost** – veldencryptie + CV-bytes; naam/adres in `FullName`/vacatures/snapshots nog plaintext (bewust smaller scope) |
| E-mail login vereist equality lookup; random-nonce AES kan niet query'en | **Hoog** | **Opgelost** – `EmailLookupHash` (HMAC via bestaande OTP-pepper) |
| Postgres TLS niet afgedwongen bij handmatige `Host=…` connection strings | **Midden** | **Opgelost** – `NormalizePostgresConnectionString` zet `SSL Mode=Require` buiten localhost |
| `__DataProtectionKeys`.Xml plaintext in DB | **Midden** | **Opgelost** – zelfde AES-GCM als velden (legacy plaintext XML blijft leesbaar tot herschrijven) |
| IBAN kolommen | **Hoog** | **Al aanwezig** – Data Protection (`dp1:`) via `IbanProtector`; niet dubbel versleuteld |
| OTP / wachtwoord hashing | **Laag** | **Al OK** – PBKDF2 600k, HMAC OTP + pepper verplicht buiten Development |
| BSN in datamodel | **Laag** | **Niet aangetroffen** in entities |
| Render Postgres disk/backups encryptie | **Onbekend** | **Actie eigenaar** – zie actielijst |
| Rate limiting / anti-scraping | — | **Niet herhaald** (parallel audit) |
| Logging PII | **Laag** | **Al grotendeels OK** (`EmailServiceStub.RedactEmail`); geen extra plaintext e-mails toegevoegd |
| HTTPS / HSTS / security headers | **Laag** | **Al OK** (`JobsyHsts`, CSP nonce, cookies Secure) |
| CV/paspoort PDF opslag | **Midden** | **Gedeeltelijk** – geüploade CV-bytes versleuteld; Lobsy-CV PDF wordt on-the-fly gegenereerd (geen persistent bestand) |

---

## Wat is gewijzigd (technisch)

- `JobsyFieldEncryption` – AES-GCM (`lobsy1:{keyId}:…`), key rotation via `ActiveKeyId` + `Keys` map.
- EF value converters op o.a. `User` (e-mail, telefoon, DOB, preferences JSON, …), `Application` snapshots, CV blobs, diepte-analyse JSON, talent-contactberichten, partneradressen.
- `FieldEncryptionBackfillHostedService` – vult lookup-hashes en encrypt bestaande plaintext (idempotent).
- Login/queries – `WhereEmailEquals` / `WhereCredentialEmailEquals` i.p.v. `Email ==`.
- Data Protection Postgres repository – XML encrypt/decrypt via `FieldEncryptionAtRest`.

---

## Env vars voor Dennis (Render)

Stel **per omgeving** (acceptatie én productie) minimaal in:

| Variabele | Beschrijving |
|-----------|--------------|
| `JobsyFieldEncryption__ActiveKeyId` | Bijv. `v1` (label voor actieve sleutel) |
| `JobsyFieldEncryption__Keys__v1` | Base64 van **32 willekeurige bytes** (AES-256). Genereer: `openssl rand -base64 32` |
| `VerificationCodes__Pepper` | **Al verplicht** buiten Development – blijft nodig voor OTP **en** e-mail blind index |

**Acceptatie (eerste deploy):**

1. Zet bovenstaande op **API** én **Web** service (zelfde waarden per omgeving).
2. Deploy; migratie draait; backfill encrypt data bij opstart (kan enkele minuten duren op grote DB).
3. **Bewaar sleutels offline** (wachtwoordmanager / Render secret backup). Zonder sleutel is versleutelde data **niet terug te lezen**.

**Productie:**

- Zelfde structuur, **andere** sleutelwaarden dan acceptatie.
- App **start niet** zonder `JobsyFieldEncryption` buiten Development (fail-closed).

**Rotatie (toekomst):**

- Voeg `JobsyFieldEncryption__Keys__v2` toe, zet `ActiveKeyId` op `v2`; oude rijen blijven decryptbaar zolang `v1` in `Keys` staat. Backfill herschrijft bij save.

**Niet in repo committen:** geen sleutels in git; alleen placeholders in `appsettings.Development.json` (lokaal).

---

## Actie-items (infra / eigenaar – niet geraden)

1. **Render Postgres**: bevestig bij Render of disk-encryptie at-rest en backup-encryptie actief zijn voor acc/prod databases (documentatie in Render dashboard / support).
2. **Secrets**: voeg `JobsyFieldEncryption__*` toe aan API + Web op acceptatie vóór merge/deploy; daarna prod bij live-gang.
3. **Key backup**: procedure voor backup van `JobsyFieldEncryption` keys **samen met** DB-backups (data + DP keys + field keys).
4. **Cloudflare / Render**: origin secret, JWT PEMs – blijven verplicht per bestaande `SECURITY.md` (niet onderdeel van deze diff).
5. **Object storage**: als CV’s ooit naar S3/Render Disk gaan i.p.v. bytea – apart bucket-encryptie + IAM (nu: Postgres bytea).

---

## Gemaakte keuzes

- **Blind index = bestaande `EmailAddressHasher`** (HMAC met `VerificationCodes:Pepper`) – geen tweede pepper; wel scheiding encryptiepepper vs lookup via aparte AES-keys.
- **DOB kolom → `text`** in Postgres om encrypted payloads op te slaan; leeftijd/matching blijft via `CandidateAgeYears` / ontcodeerde waarden in app.
- **IBAN niet dubbel** – bestaande Data Protection-converters behouden.
- **Unieke e-mailindex → `EmailLookupHash`** met gefilterde unique index (`IS NOT NULL AND <> ''`) zodat migratie + backfill geen duplicate lege hashes blokkeren.
- **Development**: veldencryptie optioneel (passthrough) als keys ontbreken; acceptatie/prod fail-closed.
- **Revert**: migratie `Down` herstelt kolomtypes/indexen; ciphertext blijft in DB tot handmatige decrypt met key (bewust).

---

## Testen

- `dotnet build Jobsy.sln`
- `dotnet test Jobsy.Tests/Jobsy.Tests.csproj`
- E2E: `CoreFunctionalFlowE2ETests` (kandidaat + werkgever flows) – lokaal met stack uit `TESTING.md`
