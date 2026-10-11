# Break-glass systeembeheerder (env-only)

**NL (B1)**  
Dit is een **nood-account** voor Lobsy-beheer. Je zet het alleen aan via **omgevingsvariabelen** op Render (Acceptatie of Productie). Er staan **geen wachtwoorden in de code** of in git.

| Variabele | Verplicht | Standaard | Uitleg |
|-----------|-----------|-----------|--------|
| `SystemAdmin__Enabled` | nee | `false` | `true`: account aanmaken of bijwerken bij opstarten. `false`: account **blokkeren** (niet verwijderen). |
| `SystemAdmin__Email` | ja als enabled | — | E-mail van het break-glass account. |
| `SystemAdmin__Password` | ja als enabled | — | Minimaal **16 tekens**. Wordt gehasht opgeslagen; wijziging in Render roteert het wachtwoord. **Nooit** in logs. |
| `SystemAdmin__RequireTwoFactor` | nee | `false` | `true`: dit account **moet** 2FA gebruiken (TOTP bij eerste login). Los van andere 2FA-regels. |
| `SystemAdmin__NotifyEmail` | nee | — | Extra adres voor login-meldingen (naast `SystemAdmin__Email`). |

**Gedrag**

- Bij opstarten: aanmaken of bijwerken (rol Admin, lokaal wachtwoord).
- Elke **succesvolle login**: auditlog (`auth.system-admin.login`) + e-mail naar de notify-adressen.
- Het account staat **niet** in de admin-gebruikerslijst en kan niet geblokkeerd of 2FA-gerest worden door andere admins.
- Ongeldige config (bijv. wachtwoord te kort) → API start **niet** (fail closed, duidelijke logregel).

**English**  
This is an **emergency administrator** provisioned only via **environment variables** on Render (Acceptatie or Production). **No credentials in source control.**

| Variable | Required | Default | Meaning |
|----------|----------|---------|---------|
| `SystemAdmin__Enabled` | no | `false` | `true`: create/update on startup. `false`: **lock** the account (not deleted). |
| `SystemAdmin__Email` | when enabled | — | Login e-mail. |
| `SystemAdmin__Password` | when enabled | — | At least **16 characters**. Hashed at rest; changing the env value rotates the password. **Never logged.** |
| `SystemAdmin__RequireTwoFactor` | no | `false` | `true`: this account **must** use TOTP 2FA (enrol on first login). Separate from other roles’ MFA rules. |
| `SystemAdmin__NotifyEmail` | no | — | Optional extra inbox for login alerts (in addition to `SystemAdmin__Email`). |

See also [`deploy-render.md`](deploy-render.md) for where to set env vars in the dashboard.
