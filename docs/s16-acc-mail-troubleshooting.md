# S16 — aanmeldcode-mail op Acceptatie

Als OTP / aanmeldcode naar `lobsy-test+s16@mail.grokbot.com` (of andere `@mail.grokbot.com`-adressen) niet aankomt, controleer **Render → jobsy-api (Acceptatie)**:

| Key | Verwacht |
|-----|----------|
| `Mail__Provider` | `Lettermint` (of expliciet geconfigureerde provider) |
| `Lettermint__ApiKey` | Geldige Lettermint API-sleutel (niet leeg) |
| `Mail__FromAddress` | Geverifieerd afzenderdomein (bijv. `noreply@…`) |
| `Mail__AllowedRecipientPattern` | `^(test-[^@]+@lobsy\.nl\|.+@mail\.grokbot\.com)$` (staat in `render.yaml`) |

In **PlatformLogs** (API): zoek naar `Mail skipped by allowlist` of `Mail: niet ingesteld`. Allowlist blokkeert nooit `@mail.grokbot.com` als het patroon synced is.

**Account vs. inbox:** login zoekt een user op **exact** genormaliseerd e-mailadres (`+s16` is een andere user dan `lobsy-test@mail.grokbot.com`). Mail kan wél verstuurd worden terwijl het account leeg blijft tot de user de code invult.

Agent kan Render-secrets niet zelf zetten; vraag Dennis om bovenstaande keys op **jobsy-api Acceptatie** te verifiëren.
