# Lobsy e-mails: mockups (em-*)
Referenced by `docs/prompts/emails/` (branch `docs/emails`). These are a layout and copy reference only. **Where a mockup and the spec differ, the spec wins** (see the README of the spec, "Mockups").

- `em-dNN-*.png` = desktop: a 600 px mail column in a 680 px viewport (em-d01 is 1150 px wide with notes). `em-mNN-*.png` = mobile, a 375 px viewport. Both are rendered at 2x.
- `em-*08-donker` is rendered with `prefers-color-scheme: dark`. `em-*09-arabisch-rtl` is `lang="ar" dir="rtl"`.
- The inbox strip above each mail (sender, subject, preheader) and the "Voorbeelddata" pill are mockup-only. All names, companies, codes, addresses, the KvK number and percentages are sample data.
- `html/em-*.mail.html` is the mail itself without the inbox frame. It is email-grade HTML (ghost table, VML button, one `<style>` block for mobile and dark), useful as a reference for the renderer, **not** as code to paste.
- Sources: `build.py` (Playwright + Chrome), `em_layout.py` (tokens as hex, layout pieces), `em_mails.py` (copy), `em_current.py` (a port of today's `EmailLayout.Wrap`, for em-00). Images in `src/` are relative for the mockup; production uses absolute https URLs.

| File | What |
|---|---|
| em-d00 / em-m00-huidig-sollicitatie | Today's ApplicationConfirmation (before) |
| em-d01 / em-m01-basis-layout | New base layout, with numbered notes on desktop |
| em-d02 / em-m02-verificatiecode | 6-digit code mail (no button: the code is the action) |
| em-d03 / em-m03-sollicitatie-verstuurd | Candidate: application sent |
| em-d04 / em-m04-nieuwe-sollicitatie | Employer: new application |
| em-d05 / em-m05-reactie-geaccepteerd | Candidate: employer accepted (small mascot, good news) |
| em-d06 / em-m06-uitnodiging | Team invite with a set-password link (no temporary password) |
| em-d07 / em-m07-overnameverzoek | Takeover/access request to the current manager |
| em-d08 / em-m08-donker | em-05 in dark mode |
| em-d09 / em-m09-arabisch-rtl | em-03 in Arabic, right-to-left (draft copy, needs native review) |
