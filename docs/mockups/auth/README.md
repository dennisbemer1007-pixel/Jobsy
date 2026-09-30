# Lobsy auth mockups (au-*), phase 1 — 30-09-2026
All screens carry the "Voorbeelddata" pill. Warm public theme (docs/landing: cream stage, organic blobs, pill buttons, radius 36, --shadow-soft), tokens 1:1 from app.css :root. Small waving LobsyMascot peeks over the card.
Build: `python3 build.py` (HTML in html/) then render.js (needs playwright-core: `PWC=$(node -p "require.resolve('playwright-core')") node render.js`; desktop 1440x900 @1x, mobile 390x844 @2x, full page).

| file | screen |
|---|---|
| au-d01-login / au-m01-login | Login: Microsoft + Google + e-mail/password, "Wachtwoord vergeten?", remember unchecked, Account maken + Bedrijf registreren (KvK) |
| au-d02-login-fout / au-m02-login-fout | Wrong password: role=alert summary, e-mail kept, password field marked |
| au-d03-login-pauze / au-m03-login-pauze | Lockout "Even pauze": time to retry, reset-password CTA, Microsoft/Google still work |
| au-d04-2fa-code / au-m04-2fa-code | 2FA prompt: masked e-mail + "Ander account", single one-time-code field, trust device 30 days (question), recovery disclosure |
| au-d05-2fa-instellen / au-m05-2fa-instellen | 2FA setup: 3 numbered steps; desktop QR + manual key + copied toast; mobile "Open in authenticator-app" (otpauth) first, QR collapsed |
| au-d06-herstelcodes / au-m06-herstelcodes | Recovery codes: success banner, 10 grouped codes, download/copy/print, checkbox, continue (register wizard step 4 variant) |
| au-d07-wachtwoord-vergeten | Proposed forgot password: form + neutral "Kijk in je mail" state side by side |
| au-m07-wachtwoord-vergeten / au-m08-wachtwoord-mail-verstuurd | Same, mobile, two screens |
| au-d09-nieuw-wachtwoord / au-m09-nieuw-wachtwoord | Set new password on /account/wachtwoord-instellen (docs/emails 01 page, reset variant) |
| au-d10-login-ar-rtl / au-m10-login-ar-rtl | Login in Arabic, RTL mirrored, e-mail/code fields stay LTR |
