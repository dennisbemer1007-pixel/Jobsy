# Feature flags — landing follow-up (werkgevers-actief)

Landing stack file 01 introduced `IEmployersSwitch` because `IFeatureFlags` /
`FeatureRoutes` are **absent** on `acceptatie`. Default DI registers
`AlwaysOnEmployersSwitch` (employers ON). OFF is tested via doubles and
Development-only `?_variant=zw` / `Landing:ForceVariant`.

When **werkgevers-actief** (mijn-paspoort file 01) lands, it must:

1. Register an `IEmployersSwitch` adapter over `IFeatureFlags` (`EmployersEnabled`).
2. Anonymous "/" OFF renders the landing **-zw** variant instead of redirecting to `/ontdek`; the home canonical stays "/".
3. ON-candidate home = `/banenkaart`.
4. Replace `EmployersGate` with `RequiresFeature`.

Also (when `Passport.Tab.Fit` exists): passport tab must use `.Zw` = "Past dit beroep?" when OFF.
