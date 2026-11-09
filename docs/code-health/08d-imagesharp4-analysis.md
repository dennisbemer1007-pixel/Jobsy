# 08d — SixLabors.ImageSharp 3 → 4 (analysis only)

**Status:** Decision 1 — licence unconfirmed. **No package bump and no code change in this PR.**

## Current usage (repo @ code-health-08c)

| Location | Role |
|---|---|
| `Jobsy.Infrastructure` — `PlatformCompanySettingsService` | Company logo resize / encode |
| `Jobsy.Infrastructure` — `OsmTileMapImageService` | Compose OSM tiles into a PNG |
| `tools/generate-icons` | Png icon generation (`ImageSharp` + `ImageSharp.Drawing`) |
| CPM | `SixLabors.ImageSharp` **3.1.12**, `SixLabors.ImageSharp.Drawing` **2.1.7** |

## Licence (ImageSharp 4 / Split License)

Source: [Six Labors Split License](https://github.com/SixLabors/ImageSharp/blob/main/LICENSE) (Version 1.0, June 2022), also summarized on [sixlabors.com/pricing](https://sixlabors.com/pricing/).

Quoted criteria for Apache 2.0 (free) use of a **Direct Package Dependency**:

> Works in Source or Object form are licensed to You under the Apache License, Version 2.0 if:
>
> - You are consuming the Work in for use in software licensed under an Open Source or Source Available license.
> - You are consuming the Work as a Transitive Package Dependency.
> - You are consuming the Work as a Direct Package Dependency in the capacity of a For-profit company/individual with **less than 1M USD annual gross revenue**.
> - You are consuming the Work as a Direct Package Dependency in the capacity of a Non-profit organization or Registered Charity.

Otherwise a **Six Labors Commercial Use License** is required.

### What that means for Lobsy

Lobsy uses ImageSharp as a **direct** dependency (Infrastructure + generate-icons). Whether Apache 2.0 applies depends on annual gross revenue and legal form. **Dennis must confirm** before any 4.x bump:

1. Is Lobsy under the &lt; $1M USD revenue threshold (and expected to stay under it through the next release window)?
2. If not, purchase/confirm a commercial licence covering ImageSharp (+ Drawing).

Until that confirmation, stay on 3.1.x.

## API impact (expected for 4.x)

- Processing / mutator pipeline and encoder options have evolved; call sites above are small (`Mutate`, `Resize`, `PngEncoder`, pixel formats).
- `ImageSharp.Drawing` must move to a matching major (Drawing 3.x with ImageSharp 4).
- Binary output of PNGs may change slightly (encoder defaults) — visual/byte comparison needed for logos, map tiles, and generated icons.

## Effort (when licence OK)

| Work | Notes |
|---|---|
| Package bump + Drawing major align | Small CPM/csproj change |
| Compile fixes | Likely few; focused on Infrastructure + generate-icons |
| Regression | Existing image upload/resize tests, OSM map image, `dotnet run --project tools/generate-icons` with visual/byte compare |
| Estimate | Low–medium (one focused PR after licence green light) |

## Out of scope here

No `Directory.Packages.props` change, no code edits, no merge recommendation until Dennis confirms the licence path.
