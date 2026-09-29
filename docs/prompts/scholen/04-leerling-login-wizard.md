# 04. Leerling: login with school/klas/code + wizard shell (4 worlds, shell plates, save progress)

Read `00-README.md` first. Branch `cursor/scholen-4` from `cursor/scholen-3` (or `-3b`).

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/scholen-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - No pupil names anywhere, no AI / partner links / vacancies in anything pupil-related, and server-side authorization per §R.

| | |
|---|---|
| Branch | `cursor/scholen-4` |
| PR title | `feat(scholen): leerling login (school/klas/code, rate limit, session cookie) + wizard shell with save progress` |
| PR body starts with | `Stacked on #<PR 03> (cursor/scholen-3)` |
| Mockups | `sc-p1-leerling-inloggen.png`, `sc-p2-leerling-vraag-info.png` (shell only), `sc-p7-chromebook-vraag.png` |
| Split seam | **04a** = pupil auth scheme + login + rate limits + session rules (04.2–04.4). **04b** = `LeerlingLayout`, scene/lobster, wizard shell + progress API (04.5–04.7) |

## Goal
A pupil on a school Chromebook or phone goes to `/leerling`, picks the school and class from dropdowns, types the code from the card and starts. The session is short-lived and never persistent. Brute-forcing codes is not practical, and one code can't be used on two devices at once. The wizard shell (worlds, lobster, shell plates, progress bar, pause) works end-to-end with a placeholder item set; 05 plugs in the real 60 questions.

## 04.1 Today (verify first)
- Auth: `Jobsy.Web/Auth/AuthServiceCollectionExtensions.cs` (cookie "Jobsy.Auth", sliding, persistent). MFA SSR form pages are the pattern for an SSR form post that signs in.
- Rate limiting: `Jobsy.Web/Security/LoginProtectionMiddleware.cs` + `LoginProtectionRateLimiter`; `Jobsy.Api/Program.cs` `AddRateLimiter`.
- Ontdekkingsreis components (Dependencies E) or the mockup source `docs/mockups/scholen/base/ontdekkingsreis_build.py` + `pupil.py`.

## 04.2 Pupil auth scheme
- New cookie scheme **`Pupil`** (cookie name `Lobsy.Leerling`), completely separate from `Jobsy.Auth`:
  - `IsPersistent = false` (session cookie, no `Expires`/`Max-Age`)
  - `HttpOnly`, `Secure`, `SameSite=Strict`, path `/`
  - `ExpireTimeSpan = 20 min` sliding, absolute 90 min (a claim `iat` checked in `OnValidatePrincipal`)
- Claims: `pupil_code_id`, `class_id`, `school_id`, `session_version`, `iat`. **No** name, e-mail or role claim that any staff/candidate policy accepts. Policy `PupilSession` = scheme `Pupil` + claims present + `SessionVersion` matches DB + class window rules (04.4) + `School.IsActive`.
- `api/pupil/*` (except the anonymous login endpoints) and `/leerling/*` pages (except `/leerling`) require `PupilSession` **only**; a staff/candidate `Jobsy.Auth` cookie never grants pupil access, and a pupil cookie never grants anything else (tests both ways).
- If a staff/candidate is already signed in on the same browser, `/leerling` shows "Je bent ingelogd als {rol}. Log eerst uit om als leerling te starten." (no mixing).
- Antiforgery on the SSR login form. `Cache-Control: no-store` on all pupil pages and APIs.

## 04.3 Login `/leerling` (sc-p1)
- SSR form (works without an interactive circuit):
  - **School** select (`GET api/pupil/schools`: active + agreement + ≥ 1 class with an open window, or a closed current-year class with completed codes (read-only view, D8); name + city)
  - **Klas** select (`GET api/pupil/schools/{id}/classes`: same rule; label "2B · havo 2")
  - **Code** input (`autocomplete="off"`, `autocapitalize="characters"`, `spellcheck="false"`, `inputmode="text"`, `maxlength=7`, placeholder "K7Q-M2P")
  - primary **"Start je reis"**
  - Speech bubble from the lobster: "Hoi! Kies je school en je klas. Typ dan de code van je kaartje."
  - Footer line: "Je naam hoeft niet. Lobsy kent alleen je code."
- `POST api/pupil/login` (also used by the SSR form handler): normalize code → lookup hash within that class → checks → `SignInAsync("Pupil", …)` → `SessionVersion++` (so an older session on another device ends) → redirect `/leerling/start` (first time) or `/leerling/reis` (continue) or `/leerling/dit-ben-jij` (completed).
- Errors are **generic**: "Die code klopt niet bij deze klas. Kijk goed op je kaartje of vraag je leraar." There is no hint whether the school/class/code exists. Window closed and not completed: "Het testvenster van je klas is dicht. Je leraar zet het weer open."
- **Rate limits + lockout** (`PupilLoginProtection`, in-memory + DB-backed counters so it survives multiple instances if the existing limiter does; otherwise document the single-instance assumption):
  - per **(class, client partition)** (client partition = HMAC of IP + User-Agent family, never stored raw): 10 failures / 15 min → 15 min cooldown, message "Even pauze. Probeer het over een kwartier opnieuw of vraag je leraar."
  - per **class**: 50 failures / hour → class login paused for 30 min (`SchoolClass.LoginPausedUntilUtc`, from 01) + Te doen item for school and leraar ("Veel foute codes bij klas 2B. Inloggen staat 30 minuten op pauze.") + the leraar can lift the pause on `/leraar/klas/{id}/testvenster`
  - per **code** (successful logins only, since an unknown code can't be tracked): > 20 logins / hour on one code → 15 min lock (`PupilCode.LockedUntilUtc`, from 01)
  - global per IP partition fixed window 60 requests / min on `api/pupil/*` (ASP.NET rate limiter policy `pupil`)
- The code space (30^6 ≈ 729 million per class) + the class-scoped limits make guessing impractical. Put the calculation in the ADR from 01.

## 04.4 Session rules
- Answer endpoints require the window **Open**. When it closes mid-session, the next save returns 409 `window_closed` → friendly screen "Je antwoorden zijn bewaard. Je leraar zet de test weer open."
- A completed pupil keeps read-only access to the result + PDF while the class exists (until retention).
- "Nieuwe code" (02/03) bumps `SessionVersion` → the old session ends on its next request.
- **Pauze** (header button) → `POST /leerling/stop` → sign-out + "Goed gedaan! Je antwoorden zijn bewaard. Log de volgende keer weer in met dezelfde code." Closing the browser ends the session cookie too.

## 04.5 `LeerlingLayout` + scene
- No BottomNav, no candidate chrome, no cookie-banner changes. Header: Lobsy logo · class/code chip ("2B · K7Q-M2P") · **Pauze**. `lang="nl"`.
- Ocean scene + lobster with 10 shell plates (Dependencies E), as in the mockups: every 6 answered questions Lobsy **loses one old plate** (10 × 6 = 60). At the end the new gold shell shines ("Kijk, mijn nieuwe schaal glimt!"). Reduced motion: no swim/bubble/fall animation, just the static plate state.
- Mobile 390: question card full-width, lobster small at the top. Chromebook 1366 (sc-p7): left rail with the 4 worlds + progress, center card, lobster on the right.

## 04.6 Wizard shell `/leerling/start`, `/leerling/reis`
- `/leerling/start` (one screen): "Zo werkt het": 4 worlds, ~25 minuten, "Er zijn geen foute antwoorden", "Je kunt stoppen en later verder". Primary "Beginnen".
- Worlds (fixed order, from `PupilWorldCatalog`, labels as in the mockups):
  1. **Het koraalrif**, "Hoe ben jij?" (Competentietest / Big Five), 15 items
  2. **De schatgrot**, "Wat doe je graag?" (Beroepentest / RIASEC), 15 items
  3. **Pauze-eiland**, "Wat vind jij leuk?" (hobby's / niet leuk chips, screen from 05; not a test world, no plate)
  4. **De vuurtoren**, "Wat vind je belangrijk?" (Waarden op werk / Schwartz), 15 items
  5. **De lagune**, "Waar voel je je thuis?" (Cultuur & persoonlijkheid), 15 items

  The 4 test worlds are shown as "Wereld 1–4". The Pauze-eiland sits after 30 items. Each world has an intro bubble and a "Wereld klaar!" moment with a puzzle piece (sc-p2 puzzle strip).
- Question screen (sc-p2):
  - "Vraag 23 van 60" + world name
  - the question text
  - an **(i)** button opening the "Stel je voor…" bubble (05)
  - **5 answer buttons** (stacked on mobile, a row on ≥ 900) with the labels from the mockup and `vragen-voorbeelden.md`: **Nee · Niet echt · Soms · Best wel · Ja!** (values 1–5, same Likert as the adult tests), plus small growing-dot icons (no emoji)
  - "Vorige" (text button) to change the previous answer
  - Keyboard: 1–5 keys + arrows; focus-visible rings
- **Save after every answer:** `PUT api/pupil/progress/answers/{itemId}` `{value}` (idempotent) → updates `PupilProgress.AnswersJson`, `CurrentIndex`, `UpdatedAtUtc`, sets `PupilCode.Status = InProgress`, `LastSeenAtUtc`. Optimistic UI with retry; if offline, show "Niet bewaard — probeer opnieuw" and don't advance.
- **Continue later:** login lands on the first unanswered item; the world/plate state is restored.
- The item set comes from `IPupilQuestionBank` (interface here, placeholder 4×3 test items in a test-only implementation; the real one in 05). Completing all items calls `IPupilResultBuilder` (stub here → 05/06).

## 04.7 Guards
- Extend `ScholenNoExternalProcessingTests` to the pupil controllers/pages.
- `PupilPagesNoCandidateChromeTests` (bUnit): `LeerlingLayout` pages render no BottomNav, `TrainingOffersBlock`, vacancy, map or partner-link components.
- Cookie test: the `Set-Cookie` for `Lobsy.Leerling` has no `Expires`/`Max-Age`, has `HttpOnly; Secure; SameSite=Strict`.

## Tests
- Login happy path (first time / continue / completed) and every error path, with generic messages.
- Rate limits: 10 failures → cooldown; 50/class/hour → pause + todo + teacher unpause; per-code success lock; 60/min policy.
- Session: second device login ends the first (`SessionVersion`); idle 20 min and absolute 90 min (clock abstraction); "Nieuwe code" ends the session; window closed → 409 + friendly screen; school deactivated → signed out.
- Isolation: pupil cookie can't call `api/teacher|school|admin/*` or candidate APIs; staff cookie can't call `api/pupil/*` (403/401).
- Progress persistence + resume at the first unanswered item. bUnit for the question screen (keyboard, 5 buttons, (i) bubble toggles). Playwright mobile 390 + 1366 if runnable.

## Success criteria
- A pupil logs in with school/klas/code, answers placeholder items, pauses, logs in again and continues at the right item.
- The session cookie is non-persistent and short-lived; brute-force limits work; pupil and staff sessions are fully separate.
- The shell matches sc-p1/p2/p7 (minus the question content from 05).

Done → next: `05-vragenbank.md`.
