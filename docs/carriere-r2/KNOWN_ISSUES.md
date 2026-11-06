# Carrière r2 — known issues (Acceptatie)

Do **not** ship quick CSS-only patches for these; fix belongs in the full Carrière r2 pass.

| Issue | Notes |
|-------|--------|
| Desktop hero (~1280px) | Page heading renders in a narrow column (often one word per line); first line can sit under the sticky app header. Hero art appears as a small strip on the far right instead of the intended showcase layout. |
| Droombaan input | Unstyled / not wired to the r2 segmented UX on `/carriere`. |

PR #1024 (`carriere.css` auto-height on `uwv-carriere-showcase-hero`) did not resolve the above on Acc.
