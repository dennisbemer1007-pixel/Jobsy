# Feature flags

Platform feature gates (when `RequiresFeatureAttribute` / `PlatformFeature` exist — Dependencies A).

## Werkgevers actief (`PlatformFeature.Employers`)

- **Off:** hide all `/werkgever` pages and employer nav; employer APIs answer as paused.
- **On:** `/werkgever/…` shell and employer mutate endpoints available to BM/RM/VM (per rights matrix).

Fallback while the attribute type is absent: pages build without `[RequiresFeature]`; `WerkgeverFeatureGateTests` passes vacuously and enforces the attribute once it lands.

## Kandidaatinzichten (`PlatformFeature.CandidateInsights` / settings `CandidateInsightsEnabled`)

- **Off (D18):** Kandidaatinzichten nav item hidden; page + API answer `feature_disabled` / 404 equivalent. Existing unlocks keep their expiry; no refunds.
- **On:** free KPI block always; premium unlock spends tokens (`TokenSpendReason.InsightsUnlock`).

Admin settings (price, duration, per-branch scope, on/off) live under admin pricing / functies when the admin redesign catalog is present; otherwise `/admin/settings`.

## Related

- Rights: [`security/roles-matrix.md`](security/roles-matrix.md)
- Routes: [`ROUTES.md`](ROUTES.md)
