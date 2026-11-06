# Waarden-scan v2 — construct matrix (Schwartz workplace drivers)

**Bron van itemteksten:** `UiStringsAssessmentV2ValuesCareerBank.cs` (via `UiStringsAssessmentV2.MergeAll`).  
**Catalogus / keying:** `SchwartzValuesCatalogV2.cs`.  
**Scoring:** gemiddelde per driver; reverse items → `6 − ruwe score` vóór middeling.  
**v3 free tier (25):** ids `1–5, 9–13, 17–21, 25–29, 33–37` — zie `QUICKSCAN_V3_FREE_TIER_MATRIX.md`.

| Id | Key | Driver | Keying | NL stem (referentie) |
|----|-----|--------|--------|----------------------|
| 1 | AssessmentV2.Values.Q01 | Autonomie | F | Ik wil zelf kunnen beslissen wanneer ik welke taak doe. |
| 2 | AssessmentV2.Values.Q02 | Autonomie | F | Op het werk kies ik graag mijn eigen werkwijze. |
| 3 | AssessmentV2.Values.Q03 | Autonomie | R | Ik werk het best als iemand precies zegt wat ik moet doen. |
| 4 | AssessmentV2.Values.Q04 | Autonomie | F | Ik wil zelf bepalen met welke taak ik begin. |
| 5 | AssessmentV2.Values.Q05 | Autonomie | R | Ik voel me prettiger bij vaste routines dan bij veel vrijheid. |
| 6 | AssessmentV2.Values.Q06 | Autonomie | F | Ik bepaal graag zelf de volgorde van mijn taken. |
| 7 | AssessmentV2.Values.Q07 | Autonomie | R | Veel vrijheid zonder duidelijke doelen maakt me onrustig. |
| 8 | AssessmentV2.Values.Q08 | Autonomie | R | Ik laat liever de leiding bepalen hoe we het werk organiseren. |
| 9 | AssessmentV2.Values.Q09 | Verbinding | F | Ik voel me thuis in een team waar we elkaar ondersteunen. |
| 10 | AssessmentV2.Values.Q10 | Verbinding | F | Samen lachen of praten met collega's hoort bij werk voor mij. |
| 11 | AssessmentV2.Values.Q11 | Verbinding | R | Ik geef liever geen hulp als dat mijn planning verstoort. |
| 12 | AssessmentV2.Values.Q12 | Verbinding | F | Ik merk snel wanneer een collega behoefte heeft aan contact. |
| 13 | AssessmentV2.Values.Q13 | Verbinding | R | Informele gesprekjes op het werk vind ik vooral afleiding. |
| 14 | AssessmentV2.Values.Q14 | Verbinding | F | Ik stel collega's gerust als ze stress hebben op het werk. |
| 15 | AssessmentV2.Values.Q15 | Verbinding | R | Ik koppel liever alleen als het echt nodig is voor mijn eigen taak. |
| 16 | AssessmentV2.Values.Q16 | Verbinding | R | Ik werk liever zonder veel contact met anderen. |
| 17 | AssessmentV2.Values.Q17 | Prestatie | F | Ik ben trots als ik normen of targets overtreft. |
| 18 | AssessmentV2.Values.Q18 | Prestatie | F | Erkenning of promotie motiveert me extra. |
| 19 | AssessmentV2.Values.Q19 | Prestatie | R | Ik hoef niet de beste te zijn zolang het werk klopt. |
| 20 | AssessmentV2.Values.Q20 | Prestatie | F | Ik stel hoge eisen aan mezelf op het werk. |
| 21 | AssessmentV2.Values.Q21 | Prestatie | R | Of anderen mijn resultaten zien, maakt mij weinig uit. |
| 22 | AssessmentV2.Values.Q22 | Prestatie | F | Ik zoek bewust naar taken waarmee ik kan groeien in mijn vak. |
| 23 | AssessmentV2.Values.Q23 | Prestatie | R | Titels of rangorde op het werk zijn voor mij niet belangrijk. |
| 24 | AssessmentV2.Values.Q24 | Prestatie | R | Competitie tussen collega's interesseert me weinig. |
| 25 | AssessmentV2.Values.Q25 | Stabiliteit | F | Ik waardeer een vast rooster dat weinig verandert. |
| 26 | AssessmentV2.Values.Q26 | Stabiliteit | F | Ik voel me gerust als afspraken op het werk duidelijk zijn. |
| 27 | AssessmentV2.Values.Q27 | Stabiliteit | R | Ik kan planningwijzigingen op het werk goed verdragen. |
| 28 | AssessmentV2.Values.Q28 | Stabiliteit | F | Voorspelbare werkdagen geven mij rust. |
| 29 | AssessmentV2.Values.Q29 | Stabiliteit | R | Ik vind het prima als taken en prioriteiten vaak wisselen. |
| 30 | AssessmentV2.Values.Q30 | Stabiliteit | F | Langdurige zekerheid over mijn baan is voor mij belangrijk. |
| 31 | AssessmentV2.Values.Q31 | Stabiliteit | R | Last-minute wijzigingen in rooster of taken kan ik meestal goed plaatsen. |
| 32 | AssessmentV2.Values.Q32 | Stabiliteit | R | Losse afspraken en weinig vaste regels passen mij goed. |
| 33 | AssessmentV2.Values.Q33 | Impact | F | Ik kies liever een werkgever die maatschappelijk verantwoord onderneemt. |
| 34 | AssessmentV2.Values.Q34 | Impact | F | Lokale of sociale projecten op het werk doe ik graag mee. |
| 35 | AssessmentV2.Values.Q35 | Impact | R | Puur salaris of status telt voor mij zwaarder dan maatschappelijke meerwaarde. |
| 36 | AssessmentV2.Values.Q36 | Impact | F | Ik let op of mijn werk anderen echt helpt of beschermt. |
| 37 | AssessmentV2.Values.Q37 | Impact | R | Mijn werk hoeft voor mij geen verschil te maken buiten mijn eigen team. |
| 38 | AssessmentV2.Values.Q38 | Impact | F | Ik wil weten dat mijn werk bijdraagt aan een eerlijke samenleving. |
| 39 | AssessmentV2.Values.Q39 | Impact | R | Of mijn werk maatschappelijk iets oplevert, boeit mij weinig. |
| 40 | AssessmentV2.Values.Q40 | Impact | R | Ik zoek geen werk dat verder gaat dan mijn eigen taken. |

**Legenda:** F = forward (hoge agreement → hoge driver-score); R = reverse (hoge agreement → lage driver-score na `6 − x`).

Vertalingen: zelfde keys in EN/PL/RO/AR in de values/career bank. Guards: `AssessmentV2TranslationGuardTests`.
