# E-mail deliverability checklist (Dennis)

Short ops checklist — not for code. Keeps transactional mail out of spam and replies working.

1. **Active sender.** `Mail__Provider=Lettermint` with `Lettermint__ApiKey` (https://lettermint.co, mail stays in the EU). Without the key, mail stays on Resend. Add the domain `mail.lobsy.nl` at that sender and create exactly the DNS records it shows: SPF, DKIM, and the bounce/return-path records. Wait until the domain is verified.
2. **DMARC** on `lobsy.nl` (applies to the subdomain): start `v=DMARC1; p=none; rua=mailto:dmarc@lobsy.nl; adkim=r; aspf=r`, then move to `p=quarantine` after 2–4 clean weeks.
3. **Tracking off** at the sender (no open/click tracking, D6). The API request also sends `track_opens` and `track_clicks` as false for Lettermint.
4. **`support@lobsy.nl`** must be a real, read mailbox (Reply-To for every mail). Decide who reads it (Dennis, as the owner).
5. `hallo@mail.lobsy.nl` needs no inbox (replies go to Reply-To). Optionally forward it to support.
6. Set in Render for acceptatie and production (no secret values in git):
   - `Mail__Provider` = `Lettermint` on Acceptatie, `Resend` on production until the domain is verified
   - `Lettermint__ApiKey` on the API and the web service (Dashboard, sync off)
   - `Mail__ResendApiKey` kept so Acceptatie can fall back
   - `Mail__FromAddress` = `Lobsy <hallo@mail.lobsy.nl>`
   - `Mail__ReplyTo` = `support@lobsy.nl`
   - `Mail__SupportAddress` = `support@lobsy.nl`
   - `Mail__AllowedRecipientPattern` = `^test-[^@]+@lobsy\.nl$` on Acceptatie only. Leave empty on production.
   - `Mail__AllowedRecipientAddresses__0` = the admin address, Acceptatie only, if test mail to that inbox should pass
   - `Mail__LegalName`, `Mail__LegalAddress`, `Mail__KvkNumber` (address and KvK still need to be provided)
7. Google Postmaster Tools for `mail.lobsy.nl` (optional).

## Dennis to do

- Legal registered address + KvK number for the mail footer
- Create the Lettermint account and verify `mail.lobsy.nl` (SPF, DKIM, DMARC)
- DNS for the active sender
- Confirm `support@lobsy.nl` is a monitored inbox
- Turn open/click tracking **off**
- Set the env vars above on Render Acceptatie (API and web)
