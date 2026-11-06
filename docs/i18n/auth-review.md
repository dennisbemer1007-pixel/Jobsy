# Auth copy review (file 07)

Glossary (same word everywhere):

| Concept | nl | en |
|---|---|---|
| Sign in | Inloggen | Sign in |
| Choose new password | Nieuw wachtwoord kiezen | Choose a new password |
| App code | Code uit je app | Code from your app |
| Recovery code | Herstelcode | Recovery code |
| Trust device | Vertrouw dit apparaat | Trust this device |
| Pause | Even pauze | A short pause |

## Needs a native check before production (Dennis)

**pl** and **ar** drafts below need a native speaker pass before production.

## MFA keys (was EN-copied into pl/ro/ar)

| key | nl | draft (pl / ro / ar) | note |
|---|---|---|---|
| Mfa.* (69 keys) | see table below + `UiStringsMfa.cs` | B1 drafts | pl + ar: native check |

### MFA key drafts

| key | nl | pl draft | note |
|---|---|---|---|
| `Mfa.AdminHelp` | Nieuwe telefoon en geen herstelcodes? Mail support | Nowy telefon i brak kodów? Napisz do supportu | pl+ar native check |
| `Mfa.AdminReset` | 2FA resetten | Resetuj 2FA | pl+ar native check |
| `Mfa.AdminResetConfirm` | Jouw authenticatorcode | Twój kod authenticator | pl+ar native check |
| `Mfa.AdminResetLead` | Deze gebruiker moet bij de volgende inlog de authenticator o | Ta osoba musi ponownie ustawić authenticator przy następnym  | pl+ar native check |
| `Mfa.AdminResetLeadTrusted` | Hiermee zet je de extra beveiliging uit en vergeet Lobsy all | To wyłącza dodatkową ochronę i Lobsy zapomina wszystkie zauf | pl+ar native check |
| `Mfa.AdminResetReason` | Reden | Powód | pl+ar native check |
| `Mfa.AdminResetSuccess` | 2FA is gereset. De gebruiker moet opnieuw inschrijven. | 2FA zresetowane. Użytkownik musi się ponownie zapisać. | pl+ar native check |
| `Mfa.BackToLogin` | Naar inloggen | Do logowania | pl+ar native check |
| `Mfa.CodeLabel` | Code uit je app | Kod z aplikacji | pl+ar native check |
| `Mfa.CodesAlreadyShown` | Je herstelcodes zijn al getoond. Bewaar ze goed — we tonen z | Twoje kody odzyskiwania już były pokazane. Zachowaj je — nie | pl+ar native check |
| `Mfa.CodesCopied` | Gekopieerd | Skopiowano | pl+ar native check |
| `Mfa.Confirm` | Koppelen | Połącz | pl+ar native check |
| `Mfa.Continue` | Verder naar Lobsy | Dalej do Lobsy | pl+ar native check |
| `Mfa.ContinueCompany` | Verder naar je bedrijf | Dalej do firmy | pl+ar native check |
| `Mfa.CopyCodes` | Kopieer alles | Kopiuj wszystko | pl+ar native check |
| `Mfa.CopyKey` | Kopieer sleutel | Kopiuj klucz | pl+ar native check |
| `Mfa.DownloadHeaderDate` | Gemaakt op {0} | Utworzono {0} | pl+ar native check |
| `Mfa.DownloadHeaderEmail` | Lobsy herstelcodes voor {0} | Kody odzyskiwania Lobsy dla {0} | pl+ar native check |
| `Mfa.DownloadNote` | Elke code werkt één keer. | Każdy kod działa raz. | pl+ar native check |
| `Mfa.DownloadTxt` | Download (.txt) | Pobierz (.txt) | pl+ar native check |
| `Mfa.EnterCode` | Vul de 6 cijfers uit je app in | Wpisz 6 cyfr z aplikacji | pl+ar native check |
| `Mfa.EnterCodeStep` | Vul de code in | Wpisz kod | pl+ar native check |
| `Mfa.ErrorExpired` | Je inlogpoging is verlopen. Log opnieuw in. | Próba logowania wygasła. Zaloguj się ponownie. | pl+ar native check |
| `Mfa.ErrorGeneric` | Er ging iets mis. Probeer opnieuw in te loggen. | Coś poszło nie tak. Zaloguj się ponownie. | pl+ar native check |
| `Mfa.ErrorInvalid` | De code is onjuist. Probeer het opnieuw. | Kod jest nieprawidłowy. Spróbuj ponownie. | pl+ar native check |
| `Mfa.ErrorLocked` | Even pauze. Er is te vaak een verkeerde code ingevuld. Probe | Krótka przerwa. Zbyt często wpisano zły kod. Spróbuj ponowni | pl+ar native check |
| `Mfa.ErrorLockedFallback` | Probeer het over een kwartier opnieuw. | Spróbuj ponownie za około kwadrans. | pl+ar native check |
| `Mfa.ErrorTooMany` | Te veel pogingen. Wacht even en probeer opnieuw. | Zbyt wiele prób. Poczekaj chwilę i spróbuj ponownie. | pl+ar native check |
| `Mfa.ErrorTooManyUntil` | Te veel pogingen. Probeer het om {0} opnieuw. | Zbyt wiele prób. Spróbuj ponownie o {0}. | pl+ar native check |
| `Mfa.InstallApp` | Installeer een authenticator-app | Zainstaluj aplikację authenticator | pl+ar native check |
| `Mfa.Instructions` | Open Microsoft Authenticator of Google Authenticator, kies a | Otwórz Microsoft Authenticator lub Google Authenticator, dod | pl+ar native check |
| `Mfa.KeyCopied` | Sleutel gekopieerd | Klucz skopiowany | pl+ar native check |
| `Mfa.LinkApp` | Koppel de app | Połącz aplikację | pl+ar native check |
| `Mfa.Login` | Inloggen | Zaloguj się | pl+ar native check |
| `Mfa.LoginRecovery` | Inloggen met herstelcode | Zaloguj się kodem odzyskiwania | pl+ar native check |
| `Mfa.ManualKeySummary` | Lukt scannen niet? Vul deze sleutel in | Nie możesz skanować? Wpisz ten klucz | pl+ar native check |
| `Mfa.OnPhoneLink` | Op je telefoon? Open in authenticator-app | Na telefonie? Otwórz w aplikacji authenticator | pl+ar native check |
| `Mfa.OpenAuthenticator` | Open in authenticator-app | Otwórz w aplikacji authenticator | pl+ar native check |
| `Mfa.OtherAccount` | Ander account | Inne konto | pl+ar native check |
| `Mfa.Print` | Print | Drukuj | pl+ar native check |
| `Mfa.PromptLead` | Open je authenticator-app en typ de 6 cijfers. | Otwórz aplikację authenticator i wpisz 6 cyfr. | pl+ar native check |
| `Mfa.PromptTitle` | Vul je code in | Wpisz kod | pl+ar native check |
| `Mfa.QrAlt` | QR-code om Lobsy te koppelen | Kod QR do połączenia Lobsy | pl+ar native check |
| `Mfa.RecoveryAlmostOut` | Bijna op. Maak nieuwe herstelcodes of vraag een beheerder om | Prawie wyczerpane. Utwórz nowe kody odzyskiwania albo poproś | pl+ar native check |
| `Mfa.RecoveryHint` | Bijvoorbeeld 7KQ2-M9XA | Na przykład 7KQ2-M9XA | pl+ar native check |
| `Mfa.RecoveryLabel` | Herstelcode | Kod odzyskiwania | pl+ar native check |
| `Mfa.RecoveryLead` | Kwijt je telefoon? Met één van deze codes kom je toch binnen | Zgubiłeś telefon? Jednym z tych kodów i tak wejdziesz. Każdy | pl+ar native check |
| `Mfa.RecoverySummary` | Geen toegang tot je app? | Brak dostępu do aplikacji? | pl+ar native check |
| `Mfa.RecoveryTitle` | Bewaar je herstelcodes | Zachowaj kody odzyskiwania | pl+ar native check |
| `Mfa.RecoveryUsedLead` | Je bent ingelogd met een herstelcode. Je hebt er nog {0}. | Zalogowałeś się kodem odzyskiwania. Pozostało ci {0}. | pl+ar native check |
| `Mfa.RecoveryUsedTitle` | Herstelcode gebruikt | Użyto kodu odzyskiwania | pl+ar native check |
| `Mfa.RegenerateLead` | Je oude codes werken daarna niet meer. | Stare kody potem już nie działają. | pl+ar native check |
| `Mfa.RegenerateLink` | Maak nieuwe herstelcodes | Utwórz nowe kody odzyskiwania | pl+ar native check |
| `Mfa.RegenerateSubmit` | Maak nieuwe codes | Utwórz nowe kody | pl+ar native check |
| `Mfa.RegenerateTitle` | Nieuwe herstelcodes | Nowe kody odzyskiwania | pl+ar native check |
| `Mfa.SavedCheckbox` | Ik heb mijn codes veilig bewaard | Zachowałem kody w bezpiecznym miejscu | pl+ar native check |
| `Mfa.ScanQr` | Scan de QR-code met je app | Zeskanuj kod QR aplikacją | pl+ar native check |
| `Mfa.SetupLead` | Je hebt een app nodig die codes maakt, zoals Microsoft Authe | Potrzebujesz aplikacji, która tworzy kody, np. Microsoft Aut | pl+ar native check |
| `Mfa.SetupTitle` | Beveilig je account | Zabezpiecz swoje konto | pl+ar native check |
| `Mfa.ShowQrDetails` | Werkt dat niet? Toon de QR-code en sleutel | Nie działa? Pokaż kod QR i klucz | pl+ar native check |
| `Mfa.StatusEnrolled` | 2FA aan | 2FA włączone | pl+ar native check |
| `Mfa.StatusExternal` | Extern | Zewnętrzne | pl+ar native check |
| `Mfa.StatusNotEnrolled` | Geen 2FA | Bez 2FA | pl+ar native check |
| `Mfa.StepOf` | Stap {0} van {1} | Krok {0} z {1} | pl+ar native check |
| `Mfa.StepOfContext` | Stap 2 van 2 | Krok 2 z 2 | pl+ar native check |
| `Mfa.SuccessOn` | Je extra beveiliging staat aan | Twoja dodatkowa ochrona jest włączona | pl+ar native check |
| `Mfa.TrustDevice` | Vertrouw dit apparaat 30 dagen | Zaufaj temu urządzeniu przez 30 dni | pl+ar native check |
| `Mfa.TrustDeviceHint` | Dan vragen we de code hier niet elke keer. Alleen op je eige | Wtedy nie będziemy tu pytać o kod za każdym razem. Tylko na  | pl+ar native check |
| `Mfa.TrustedDevicesCount` | Vertrouwde apparaten: {0} | Zaufane urządzenia: {0} | pl+ar native check |
| Login.* / ForgotPassword.* / SetPassword.Reset* | see `UiStringsAuth.cs` | already 5-lang from 03–05 | pl + ar: native check |

## Copy changes this PR (B1 / consistency)

| key | old → new |
|---|---|
| *(none beyond MFA pl/ro/ar fill)* | UiStringsMfa helper now takes real pl/ro/ar |

## Contrast (token pairs on auth tint blocks)

| Surface | Tokens | Expected |
|---|---|---|
| Body text on card | `--ink` on `--surface` | AA |
| Danger alert | danger tint + ink | AA |
| Focus ring | pub focus token | AA |
| Soft tints (sun/sky/mint/peach) | soft mix + ink | AA for body |

## Screenshots

Deferred in this environment (no browser capture of ar desktop/mobile in CI agent). Playwright a11y path covers keyboard/dir where configured; ar visual screenshots remain a follow-up for Dennis.
