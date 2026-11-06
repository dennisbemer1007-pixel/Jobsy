# Landing follow-up (werkgever-aanmelding)

Dependency **A** (PublicLayout / LobsyMascot / `.pub-theme`) was **ABSENT** on `origin/acceptatie` when file 05 landed.

When landing 01/02 land:

1. Replace `WaPublicLayout` with `PublicLayout`
2. Replace `WaMascot` with `LobsyMascot`
3. Replace `.wa-theme` with `.pub-theme` and drop the duplicated warm tints in `werkgever-aanmelding.css`
4. Use `PublicRoutes.CompanyRegister` for `/register` links

Do **not** copy landing files or invent parallel `PublicLayout`/`LobsyMascot` names beyond these temporary fallbacks.
