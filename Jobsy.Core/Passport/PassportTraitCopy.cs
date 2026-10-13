using Jobsy.Core.Localization;

namespace Jobsy.Core.Passport;

/// <summary>
/// Short B1 glosses for known DNA words. A gloss explains the word. It is not a new fact about the person.
/// Index: 0 nl, 1 en, 2 pl, 3 ro, 4 ar.
/// </summary>
public static class PassportTraitCopy
{
    public static IReadOnlyCollection<string> Keys => Map.Keys;

    public static string? Gloss(string layer, string? code, string? language)
    {
        if (string.IsNullOrWhiteSpace(code) || !Map.TryGetValue(layer + "." + code, out var row))
        {
            return null;
        }

        return row[Index(language)];
    }

    private static int Index(string? language) => JobsyLanguages.Normalize(language) switch
    {
        "en" => 1,
        "pl" => 2,
        "ro" => 3,
        "ar" => 4,
        _ => 0
    };

    private static readonly Dictionary<string, string[]> Map = new(StringComparer.Ordinal)
    {
        ["competence.Samenwerken"] = ["denkt mee in het team", "thinks along with the team", "myśli razem z zespołem", "gândește împreună cu echipa", "يفكر مع الفريق"],
        ["competence.Resultaatgerichtheid"] = ["maakt taken af en werkt netjes", "finishes tasks and works neatly", "kończy zadania i pracuje starannie", "termină sarcinile și lucrează îngrijit", "ينهي المهام ويعمل بترتيب"],
        ["competence.Stressbestendigheid"] = ["blijft kalm als het druk is", "stays calm when it is busy", "zostaje spokojny, gdy jest dużo pracy", "rămâne calm când e aglomerat", "يبقى هادئاً حين يكون العمل مزدحماً"],
        ["competence.Innovatie"] = ["pakt nieuwe taken vlot op", "picks up new tasks quickly", "szybko łapie nowe zadania", "preia repede sarcini noi", "يلتقط المهام الجديدة بسرعة"],
        ["competence.Extraversie"] = ["krijgt energie van contact met mensen", "gets energy from contact with people", "czerpie energię z kontaktu z ludźmi", "primește energie din contactul cu oamenii", "يستمد طاقة من التواصل مع الناس"],
        ["career.Realistic"] = ["praktisch werk met je handen", "practical work with your hands", "praktyczna praca rękami", "muncă practică cu mâinile", "عمل عملي باليدين"],
        ["career.Investigative"] = ["uitzoeken hoe iets werkt", "finding out how something works", "sprawdzanie jak coś działa", "a afla cum funcționează ceva", "معرفة كيف يعمل شيء"],
        ["career.Artistic"] = ["iets nieuws of moois maken", "making something new or beautiful", "tworzenie czegoś nowego lub ładnego", "a face ceva nou sau frumos", "صنع شيء جديد أو جميل"],
        ["career.Social"] = ["helpen en contact met mensen", "helping and contact with people", "pomaganie i kontakt z ludźmi", "ajutor și contact cu oamenii", "المساعدة والتواصل مع الناس"],
        ["career.Enterprising"] = ["regelen, overtuigen en aanjagen", "organising, convincing and getting things moving", "organizowanie, przekonywanie i popychanie spraw", "organizare, convingere și punere în mișcare", "التنظيم والإقناع وتحريك الأمور"],
        ["career.Conventional"] = ["orde, afspraken en overzicht", "order, agreements and overview", "porządek, ustalenia i przegląd", "ordine, înțelegeri și vedere de ansamblu", "نظام واتفاقات ونظرة عامة"],
        ["culture.Autonomy"] = ["Ruimte om zelf keuzes te maken", "Room to make your own choices", "Przestrzeń na własne wybory", "Spațiu să iei singur decizii", "مساحة لاتخاذ قراراتك"],
        ["culture.Informal"] = ["Een informele, gewone sfeer", "An informal, ordinary atmosphere", "Swobodna, zwykła atmosfera", "O atmosferă lejeră și obișnuită", "جو عادي وغير رسمي"],
        ["culture.Collaboration"] = ["Een team dat samenwerkt", "A team that works together", "Zespół, który współpracuje", "O echipă care lucrează împreună", "فريق يعمل معاً"],
        ["culture.Flexibility"] = ["Werk dat meebeweegt als het verandert", "Work that moves with you when it changes", "Praca, która dopasowuje się do zmian", "Muncă ce se adaptează când se schimbă", "عمل يتكيف حين يتغير"],
        ["culture.Innovation"] = ["Ruimte voor nieuwe ideeën", "Room for new ideas", "Miejsce na nowe pomysły", "Spațiu pentru idei noi", "مساحة لأفكار جديدة"],
        ["culture.PeopleFirst"] = ["Mensen staan voorop", "People come first", "Ludzie są na pierwszym miejscu", "Oamenii sunt pe primul loc", "الناس أولاً"],
        ["values.Autonomy"] = ["zelf kiezen en een uitdaging", "choosing for yourself and a challenge", "własny wybór i wyzwanie", "alegere proprie și o provocare", "اختيارك وتحدٍ"],
        ["values.Connection"] = ["mensen die ertoe doen", "people who matter", "ludzie, którzy są ważni", "oameni care contează", "أشخاص يهمون"],
        ["values.Achievement"] = ["stappen zetten in je vak", "taking steps in your trade", "robienie kroków w swoim fachu", "pași în meseria ta", "خطوات في مهنتك"],
        ["values.Stability"] = ["duidelijke, zekere afspraken", "clear, steady agreements", "jasne, pewne ustalenia", "înțelegeri clare și sigure", "اتفاقات واضحة وثابتة"],
        ["values.Impact"] = ["iets betekenen voor een ander", "meaning something to someone else", "znaczyć coś dla kogoś innego", "a însemna ceva pentru altcineva", "أن تعني شيئاً لشخص آخر"]
    };
}
