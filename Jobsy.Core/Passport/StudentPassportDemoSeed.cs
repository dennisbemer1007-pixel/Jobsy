namespace Jobsy.Core.Passport;

/// <summary>Rich HBO/student demo facts for DNA-paspoort v3 previews (Yara).</summary>
public static class StudentPassportDemoSeed
{
    public static readonly Guid YaraUserId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffff0002");

    private static readonly DateTime Generated = new(2026, 10, 14, 12, 0, 0, DateTimeKind.Utc);

    public static PassportPdfFacts YaraFacts(bool sparse = false)
        => sparse ? SparseFacts() : FullFacts();

    private static PassportPdfFacts SparseFacts()
        => new(
            Language: "nl",
            UserId: YaraUserId,
            FullName: "",
            OpenForWork: false,
            WorkRegion: "Utrecht",
            AvailableFrom: null,
            MinHours: null,
            MaxHours: null,
            FlexibleTimes: false,
            Availability: null,
            PreferredTransport: null,
            Licenses: null,
            MaxTravelMinutes: 25,
            HasOwnCar: null,
            IncludeContact: false,
            Email: null,
            Phone: null,
            WhatsApp: false,
            SpokenLanguages: null,
            DutchLevel: null,
            WorkPreferences: null,
            ShareEmployerPreferences: false,
            EmployerPreferences: null,
            Roles: ["Stage verpleegkunde"],
            ContractPreferences: null,
            Experience: null,
            ExperienceCountWithoutNames: 0,
            Certificates: null,
            Educations: ["Hbo Verpleegkunde", "Jaar 3"],
            EducationDirection: null,
            OwnWords: null,
            Motivation: null,
            Dna:
            [
                new(PassportDnaLayer.Competence, true, Generated.AddDays(-14), ["Samenwerken in team"], []),
                new(PassportDnaLayer.Career, false, null, [], []),
                new(PassportDnaLayer.Culture, false, null, [], []),
                new(PassportDnaLayer.Values, false, null, [], [])
            ],
            EmailVerified: false,
            PhoneVerified: false,
            PhoneVerificationRequired: false,
            GeneratedAtUtc: Generated,
            DreamTitle: null,
            ReferenceQuotes: null,
            LearningGoals: null,
            ShowDnaBlocks: true,
            QrVerifyUrl: "https://lobsy.nl/candidate/paspoort/share/demo-yara-sparse",
            QrValidUntilLabel: "Geldig t/m 14 okt 2026",
            StudentPassportLayout: true,
            StudentStudyChip: "Hbo Verpleegkunde · jaar 3",
            StudentInternshipChip: "Stage sep–dec 2026");

    private static PassportPdfFacts FullFacts()
        => new(
            Language: "nl",
            UserId: YaraUserId,
            FullName: "Yara de Vries",
            OpenForWork: true,
            WorkRegion: "Utrecht · 25 km",
            AvailableFrom: new DateOnly(2026, 9, 1),
            MinHours: 16,
            MaxHours: 24,
            FlexibleTimes: true,
            Availability: new Dictionary<string, string[]>
            {
                ["Ma"] = ["Middag"],
                ["Di"] = ["Middag"],
                ["Wo"] = ["Middag"],
                ["Do"] = ["Middag"],
                ["Vr"] = ["Middag"]
            },
            PreferredTransport: "OV",
            Licenses: ["B"],
            MaxTravelMinutes: 25,
            HasOwnCar: false,
            IncludeContact: false,
            Email: null,
            Phone: null,
            WhatsApp: false,
            SpokenLanguages: [new("nl", "moedertaal")],
            DutchLevel: "vloeiend",
            WorkPreferences: null,
            ShareEmployerPreferences: false,
            EmployerPreferences: null,
            Roles:
            [
                "Stage verpleegafdeling",
                "Bijbaan zorginstelling",
                "Assistent zorg"
            ],
            ContractPreferences: ["stage", "bijbaan"],
            Experience:
            [
                new PassportExperienceFact(
                    "Ziekenhuis (fictief)",
                    "Stage SEH (snuffel)",
                    "2025-03",
                    "2025-03",
                    null,
                    "Observatie · begeleid meelopen"),
                new PassportExperienceFact(
                    "OBS De Kompasroos",
                    "BHV-helper",
                    "2024-09",
                    "2025-06",
                    null,
                    "Eerste hulp team op school")
            ],
            ExperienceCountWithoutNames: 0,
            Certificates:
            [
                new PassportPaperFact("BHV", null),
                new PassportPaperFact("EHBO", null)
            ],
            Educations:
            [
                "Hbo Verpleegkunde",
                "Jaar 3 · stageperiode sep–dec 2026"
            ],
            EducationDirection: "Verpleegkundige · ziekenhuis",
            OwnWords:
            "Ik wil mensen echt helpen als ze bang of onzeker zijn — rustig blijven en uitleggen wat er gebeurt.",
            Motivation: "Verpleegkundige in het ziekenhuis",
            DreamTitle: "Verpleegkundige",
            Dna:
            [
                new(PassportDnaLayer.Competence, true, Generated.AddDays(-21), [
                    "Zorgzaam en betrouwbaar",
                    "Rustig onder druk",
                    "Zorgvuldig in de praktijk"
                ], []),
                new(PassportDnaLayer.Career, true, Generated.AddDays(-20), [
                    "Helpen en uitleggen",
                    "Werken met mensen in zorg"
                ], []),
                new(PassportDnaLayer.Culture, true, Generated.AddDays(-10), [
                    "Vast team op afdeling",
                    "Duidelijke afspraken met begeleider"
                ], []),
                new(PassportDnaLayer.Values, true, Generated.AddDays(-3), [
                    "Veiligheid voor patiënt",
                    "Respectvol contact"
                ], [])
            ],
            EmailVerified: true,
            PhoneVerified: false,
            PhoneVerificationRequired: false,
            GeneratedAtUtc: Generated,
            ReferenceQuotes: null,
            LearningGoals:
            [
                "Meer ervaring met acute zorg",
                "Medicatie-toediening onder begeleiding oefenen",
                "Communicatie met patiënten en familie"
            ],
            ShowDnaBlocks: true,
            QrVerifyUrl: "https://lobsy.nl/candidate/paspoort/share/demo-yara",
            QrValidUntilLabel: "Geldig t/m 14 okt 2026",
            FirstName: "Yara",
            StudentPassportLayout: true,
            StudentTagline: "Zorgzaam en rustig — wil weten hoe het zit en rustig uitleggen",
            StudentStudyChip: "Hbo Verpleegkunde · jaar 3",
            StudentInternshipChip: "Stage sep–dec 2026",
            StageVacancyMatches:
            [
                new("Stage verpleegafdeling", "Past bij zorgzaam en zorgvuldig · Meelopen met verpleegkundigen · Binnen 25 km"),
                new("Bijbaan zorginstelling (16+)", "Rustige uren na school · Helpen bij basiszorg · Avond/middag"),
                new("Vrijwilligerswerk buurtcentrum", "Contact met mensen · Korte shifts · Om te ontdekken")
            ],
            StudyDirectionSteps:
            [
                new("Hbo Verpleegkunde · jaar 3", "Nu"),
                new("Stage verpleegafdeling sep–dec 2026", "Stageperiode"),
                new("Afronden hbo · BIG-register", "Daarna"),
                new("Verpleegkundige ziekenhuis", "Doel")
            ],
            StudentBestOutTips:
            [
                "Laat haar eerst observeren en stel daarna korte, concrete vragen",
                "Geef een vaste begeleider op de afdeling tijdens stage",
                "Plan medicatie-momenten stap voor stap met dubbelcheck"
            ],
            StudentStrengthCards:
            [
                new("Zorgzaam en betrouwbaar", "checkt afspraken en volgt het zorgplan"),
                new("Rustig onder druk", "blijft kalm als het druk is op afdeling"),
                new("Zorgvuldig in de praktijk", "werkt stap voor stap volgens protocol")
            ],
            StudentHowWorkLines:
            [
                new("Veiligheid voor patiënt", "controleert naam en polsbandje"),
                new("Respectvol contact", "legt rustig uit wat er gebeurt")
            ]);
}
