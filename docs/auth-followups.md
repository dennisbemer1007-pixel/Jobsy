# Auth stack follow-ups

Dependency re-check at file 03 (login redesign):

| Dep | Case at 03 |
| A Public theme | **PRESENT** — PublicLayout + public-theme.css + PublicRoutes |
| B LobsyMascot | **PRESENT** (re-check) — `LobsyMascot.razor` exists; used with Pose=Waving Size=Small. Earlier README note of ABSENT was overturned on acceptatie tip. |
| C account-maken | **PRESENT** |
| D Employers | **PRESENT** — IEmployersSwitch + IFeatureFlags.EmployersEnabled |

No AuthPublicLayout / AuMascot fallbacks were added.

- Drop `PlatformFeatureSettings.ExposeRegistrationActivationLinks` column (auth 06 left it in DB).

Dependency re-check at file 06 (register-activate cleanup):
| Dep | Case |
| G WA wizard | **PRESENT** — `/register/bedrijf`, `/register/verifieren`, `/register/koppelen`; ActivateAsync kept for wizard code step; page/BuildActivationUrl/StubActivation/Activate HTTP removed; 301 `/register/activate`→`/register`.
