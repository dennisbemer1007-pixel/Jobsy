# UWV werkmap mockups (preview only)

Interactive preview routes live under `/preview/uwv-mockups/{screen}?lang=nl|en`.

| Screen | Route | PNG files |
|--------|-------|-----------|
| Carrière (Kansberoepen, vergelijkbare beroepen, Omscholen) | `carriere` | `01-carriere-kansberoepen-*` |
| Banenkaart laag Kansberoepen | `banenkaart` | `02-banenkaart-kanslaag-*` |
| Workshops en opleidingen | `workshops` | `03-workshops-omscholen-*` |
| Ondernemerstest (intro / vraag / resultaat) | `ondernemer-*` | `04-ondernemer-*` |
| Drijfveren: Wat drijft jou | `drijfveren` | `05-drijfveren-wat-drijft-jou-*` |
| Neem mee naar adviseur | `adviseur` | `06-adviseur-share-*` |

Regenerate PNGs (Web on `:5201`):

```bash
JOBSY_UWV_MOCKUP_SCREENSHOTS=1 JOBSY_E2E_BASE_URL=http://127.0.0.1:5201 \
  dotnet test Jobsy.Tests/Jobsy.Tests.csproj --filter FullyQualifiedName~UwvMockupScreenshotPlaywrightTests
```

Then rebuild `00-contact-sheet.png` with Pillow if needed (see agent capture script).
