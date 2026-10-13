# Diploma evaluation strings — native review

Keys live in `Jobsy.Web/Localization/UiStringsDiplomaEvaluation.cs` (`DiplomaEval.*`).

Dutch is the source. English, Polish, Romanian and Arabic are machine-translated and still need a native pass, same as the other pl/ro/ar review notes in this folder.

Please check in particular:

- `DiplomaEval.Attribution.Official` — Dutch must stay **volgens waardering van Nuffic/SBB**.
- The pick-list labels (`DiplomaEval.Level.*`) keep the official Dutch names (mbo, havo, vwo, hbo, wo). Translations only add a short gloss.
- `DiplomaEval.LevelTextHint` must keep saying that Lobsy does not fill in or calculate the level.
- `DiplomaEval.DocumentHint` must keep saying that partners and employers do not receive the file.
- Links stay `https://www.idw.nl/` and `https://www.s-bb.nl/` (not translated).
