# i18n notes (cleanup prompt 14)

- `untranslated-baseline.txt` — per-language count of UiStrings values still identical to Dutch (after allow-list). `LocalizationParityReportTests` fails only if a count **increases**.
- `candidate-unused-keys.md` — review of keys without a literal `"Key"` reference; marks are heuristic. **Do not delete keys** without a second pass.
- Optional export for translators:

```bash
dotnet run --project tools/i18n-export -- docs/i18n/untranslated-for-translators.csv
```

Brand: user-visible copy uses **Lobsy**. The code namespace / cookies / storage keys remain `Jobsy` (ADR in prompt 15).
