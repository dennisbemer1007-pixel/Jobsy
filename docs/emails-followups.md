# E-mails follow-ups

Dependency cases noted while implementing emails stack:

- **H. Intermediair — ABSENT (re-checked at 05):** candidate mails keep today's `CompanyName`. When intermediair 03 lands (`IntermediaryPublicIdentity`), switch employer display names in candidate mails and support hidden-mode bureau naming (`Email.EmployerContacting.ViaIntermediary` / `Email.ApplicationHired.ViaIntermediary` keys are ready).
- **A/C — PRESENT (re-checked at 05):** `EmailLinks.Map` = `/banenkaart` (`PublicRoutes.Banenkaart` / `KbRoutes.Map`).
