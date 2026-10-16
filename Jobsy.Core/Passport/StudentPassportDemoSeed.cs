namespace Jobsy.Core.Passport;

public static class StudentPassportDemoSeed
{
    public static StudentPassportPdfModel Yara(bool sparse = false)
    {
        if (sparse)
        {
            return new StudentPassportPdfModel(
                FullName: "",
                Initials: "",
                StudyLine: "Hbo Verpleegkunde",
                YearLine: "Jaar 3",
                Tagline: null,
                Region: "Utrecht",
                DnaCards: DefaultDna(sparse: true),
                Strengths: ["Samenwerken", "Zorgvuldig werken"],
                Vacancies: [],
                Experience: [],
                LearningGoals: [],
                DirectionSteps: DefaultDirection(sparse: true),
                OwnWords: null,
                Motivation: null,
                HowIWork: [],
                Certificates: [],
                FooterNote: "Demo · sparse variant · geen vacaturelinks",
                Sparse: true);
        }

        return new StudentPassportPdfModel(
            FullName: "Yara de Vries",
            Initials: "YV",
            StudyLine: "Hbo Verpleegkunde",
            YearLine: "Jaar 3 · stageperiode",
            Tagline: "Zorgzaam, rustig en nieuwsgierig in de praktijk",
            Region: "Utrecht · 25 km",
            DnaCards: DefaultDna(sparse: false),
            Strengths:
            [
                "Samenwerken in een team",
                "Zorgvuldig werken onder tijdsdruk",
                "Rustig blijven als het druk is"
            ],
            Vacancies:
            [
                new("Stage verpleegafdeling", "Ziekenhuis (fictief)", ["16 uur", "dagen"], [
                    "Past bij zorgzaam en zorgvuldig",
                    "Meelopen met verpleegkundigen",
                    "Geen sollicitatie — demo-kaart"
                ]),
                new("Bijbaan zorginstelling", "Thuiszorg (fictief)", ["avond", "16+"], [
                    "Rustige uren na school",
                    "Helpen bij basiszorg",
                    "Types werk, geen vacature"
                ]),
                new("Vrijwilligerswerk", "Buurtcentrum", ["za"], [
                    "Contact met mensen",
                    "Korte shifts",
                    "Om te ontdekken"
                ])
            ],
            Experience:
            [
                new("Stage SEH (snuffel)", "2025 · 1 week", "Observatie · begeleid"),
                new("BHV-helper school", "2024–2025", "Eerste hulp team")
            ],
            LearningGoals:
            [
                "Meer ervaring met acute zorg",
                "Medicatie-toediening onder begeleiding oefenen",
                "Communicatie met patiënten en familie"
            ],
            DirectionSteps: DefaultDirection(sparse: false),
            OwnWords: "Ik wil mensen echt helpen als ze bang of onzeker zijn — rustig blijven en uitleggen wat er gebeurt.",
            Motivation: "Verpleegkundige in het ziekenhuis · daarna specialisatie",
            HowIWork:
            [
                "Werkt graag in een vast team",
                "Vraagt door als iets niet duidelijk is",
                "Plant vooruit voor toetsen en stage"
            ],
            Certificates: ["BHV", "EHBO"],
            FooterNote: "Demo Yara · hbo Verpleegkunde jaar 3 · fictieve vacatures",
            Sparse: false);
    }

    private static IReadOnlyList<StudentPassportDnaCard> DefaultDna(bool sparse)
    {
        if (sparse)
        {
            return
            [
                new("heart", "Zo ben ik", "Behulpzaam", null),
                new("dna", "Dit doe ik graag", "Helpen", null),
                new("flag", "Belangrijk", "Eerlijkheid", null),
                new("shield", "Thuis in", "Rustig team", null)
            ];
        }

        return
        [
            new("heart", "Zo ben ik", "Zorgzaam en betrouwbaar", "checkt afspraken na"),
            new("dna", "Dit doe ik graag", "Helpen en uitleggen", "S-I profiel"),
            new("flag", "Belangrijk", "Veiligheid en respect", "voor patiënt en team"),
            new("shield", "Thuis in", "Drukke afdeling met vaste routine", "stage SEH")
        ];
    }

    private static IReadOnlyList<StudentPassportDirectionStep> DefaultDirection(bool sparse)
    {
        if (sparse)
        {
            return
            [
                new(1, "Nu", "Hbo jaar 3", false),
                new(2, "Stage", "Verpleegafdeling", false),
                new(3, "Afronden", "Propedeuse + stage", false)
            ];
        }

        return
        [
            new(1, "Nu: hbo jaar 3", "Verpleegkunde · stageperiode", false),
            new(2, "Stage afgerond", "Minimaal 800 uur praktijk", false),
            new(3, "Afstuderen", "2027 · verpleegkundige", false),
            new(4, "Verpleegkundige", "Ziekenhuis of thuiszorg", true)
        ];
    }
}
