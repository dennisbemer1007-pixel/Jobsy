# E-mail deliverability checklist (Dennis)

Short ops checklist — not for code. Keeps transactional mail out of spam and replies working.

1. **Resend domain `mail.lobsy.nl`:** in Resend, add the domain, then create exactly the records Resend shows at the DNS host of lobsy.nl: the DKIM TXT record, plus the MX and SPF TXT records on its bounce subdomain (typically `send.mail.lobsy.nl`). Wait for "Verified".
2. **DMARC** on `lobsy.nl` (applies to the subdomain): start `v=DMARC1; p=none; rua=mailto:dmarc@lobsy.nl; adkim=r; aspf=r`, then move to `p=quarantine` after 2–4 clean weeks.
3. **Resend settings:** click and open tracking **off** for the domain (no open/click tracking, D6).
4. **`support@lobsy.nl`** must be a real, read mailbox (Reply-To for every mail). Decide who reads it (Dennis, as the owner).
5. `hallo@mail.lobsy.nl` needs no inbox (replies go to Reply-To). Optionally forward it to support.
6. Set in Render for acceptatie and production:
   - `Mail__FromAddress` = `Lobsy <hallo@mail.lobsy.nl>`
   - `Mail__ReplyTo` = `support@lobsy.nl`
   - `Mail__SupportAddress` = `support@lobsy.nl`
   - `Mail__LegalName`, `Mail__LegalAddress`, `Mail__KvkNumber` (address and KvK still need to be provided)
7. Google Postmaster Tools for `mail.lobsy.nl` (optional).

## Dennis to do

- Legal registered address + KvK number for the mail footer
- DNS / Resend domain verification for `mail.lobsy.nl`
- Confirm `support@lobsy.nl` is a monitored inbox
- Turn Resend open/click tracking **off**
