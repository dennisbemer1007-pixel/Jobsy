# Paspoort compact layout mockups

Step 1: layout directions for a shorter desktop fold on `/candidate/paspoort`.

## r2 (current): real Blazor UI

Mockups render the **actual** paspoort page with stubbed API data (fake user **Alex Demo**, NL UI). Layout variants via query string when `Jobsy:PassportCompactMockups:Enabled=true` (screenshot test host only).

| Query | Direction |
| --- | --- |
| `compactLayout=a` | Slim ~84px greeting band |
| `compactLayout=b` | Greeting in passport card, no top hero |
| `compactLayout=c` | Wide canvas (PO choice, **r3**): greeting in card, soft margin art, anchored nudge |

Regenerate PNGs and coverage table:

```bash
dotnet test Jobsy.Tests/Jobsy.Tests.csproj --filter FullyQualifiedName~PassportCompactMockupR2ScreenshotTests
```

Outputs:

- `/opt/cursor/artifacts/paspoort-compact-mockups/variant-{a,b,c}-{viewport}-r2.png`
- `/opt/cursor/artifacts/paspoort-compact-mockups/variant-c-{viewport}-r3.png`
- `above-the-fold-coverage-r2.md` / `above-the-fold-coverage-r3.md`

## r1 (deprecated)

Static HTML under this folder (`variant-a-slim-bar.html`, etc.) — mixed EN copy and simplified chrome. Kept for history only; do **not** send to PO.
