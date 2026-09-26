namespace Jobsy.Core.Rules;

/// <summary>
/// IPIP-NEO facet codes for each of the 150 competence deep-analysis items (question id 1–150).
/// Exactly 6 facets × 5 items per Big Five trait. Blocks of 5 consecutive items are assigned
/// to the facet that best matches their workplace wording. Full q→facet table is in the PR.
/// </summary>
public static class DeepAnalysisCompetenceFacets
{
    public const int FacetsPerTrait = 6;
    public const int ItemsPerFacet = 5;

    /// <summary>Facet code by 0-based index into <see cref="DeepAnalysisCompetenceItems.All"/>.</summary>
    public static readonly string[] ByIndex =
    [
        // Openheid q1–30
        "O4", "O4", "O4", "O4", "O4", // nieuwe aanpak / uitproberen
        "O5", "O5", "O5", "O5", "O5", // verbanden, vragen, leren
        "O1", "O1", "O1", "O1", "O1", // inspiratie, voorstellen, eigen invulling
        "O3", "O3", "O3", "O3", "O3", // nieuwsgierigheid, inleving, abstract
        "O6", "O6", "O6", "O6", "O6", // verandering, verbetering, feedback
        "O2", "O2", "O2", "O2", "O2", // creatief delen, producten, zonder voorbeeld

        // Consciëntieusheid q31–60
        "C5", "C5", "C5", "C5", "C5", // afmaken, plannen, controleren, rommel, op tijd
        "C2", "C2", "C2", "C2", "C2", // netjes, openstaand, uitstellen, opruimen, foutjes
        "C3", "C3", "C3", "C3", "C3", // afspraken, controles, voorbereiden, fout toegeven, stukken
        "C4", "C4", "C4", "C4", "C4", // nieuw beginnen, overzicht, afspraak, doorwerken, admin
        "C1", "C1", "C1", "C1", "C1", // pieken, spoor, procedures, ja zeggen, check belofte
        "C6", "C6", "C6", "C6", "C6", // voorraad, documenteren, dubbel check, afronden, opruimen

        // Extraversie q61–90
        "E2", "E2", "E2", "E2", "E2", // samenwerken, aanspreken, drukte, terugtrekken, woord nemen
        "E1", "E1", "E1", "E1", "E1", // vriendelijke toon, pauze, vermijden klant, groep, telefoneren
        "E3", "E3", "E3", "E3", "E3", // duidelijk vragen, zonder overleg, voorstellen, tempo, grap
        "E4", "E4", "E4", "E4", "E4", // stil in overleg, aandacht, schakelen, zichtbaar, aangesproken
        "E5", "E5", "E5", "E5", "E5", // uitzend, e-mail, conflict, netwerken, contactwerk
        "E6", "E6", "E6", "E6", "E6", // energie team, afspreken wie wat, overdracht, prikkels, ontwijken

        // Vriendelijkheid q91–120
        "A3", "A3", "A3", "A3", "A3", // helpen, luisteren, toon, geduld, teamoplossing
        "A6", "A6", "A6", "A6", "A6", // compliment, inleven, eigen belang, kennis delen, denigrerend
        "A4", "A4", "A4", "A4", "A4", // tempo aanpassen, wrok, rustig oneens, respect, hulp bieden
        "A5", "A5", "A5", "A5", "A5", // sfeer, stil ruimte, toegeven, beleefdheid, sarcasme
        "A1", "A1", "A1", "A1", "A1", // niemand buiten, klem lopen, barrières, hulp zwak, conflict afronden
        "A2", "A2", "A2", "A2", "A2", // privacy, dankjewel, onderbreken, win-win, uitspelen

        // EmotioneleStabiliteit q121–150 (N-facets inverted = “kalm blijven”)
        "N1", "N1", "N1", "N1", "N1", // kalm, herstel, multi, piekeren, overzicht
        "N2", "N2", "N2", "N2", "N2", // beleefd scherp, schoon starten, van slag, ademen, aangevallen
        "N6", "N6", "N6", "N6", "N6", // functioneren, vermijden gesprek, fout herstellen, rustig, hulp
        "N3", "N3", "N3", "N3", "N3", // spanning mee, beslissen, piekdagen, conflict laten, ramp
        "N5", "N5", "N5", "N5", "N5", // veilig gehaast, prikkelbaar, tegenslag→stap, verantwoordelijkheid, rumoer
        "N4", "N4", "N4", "N4", "N4"  // ja/nee, vriendelijk rij, bevriezen, uitdaging, piekeren/boosheid
    ];

    public static string CodeForIndex(int zeroBasedIndex)
    {
        if (zeroBasedIndex < 0 || zeroBasedIndex >= ByIndex.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(zeroBasedIndex));
        }

        return ByIndex[zeroBasedIndex];
    }

    public static string LabelNl(string facetCode) => facetCode.ToUpperInvariant() switch
    {
        "C1" => "Zelfvertrouwen",
        "C2" => "Ordelijk",
        "C3" => "Plichtsgetrouw",
        "C4" => "Wil presteren",
        "C5" => "Doorzetten",
        "C6" => "Eerst nadenken",
        "A1" => "Vertrouwen",
        "A2" => "Eerlijk & netjes",
        "A3" => "Behulpzaam",
        "A4" => "Samenwerken",
        "A5" => "Bescheiden",
        "A6" => "Meelevend",
        "N1" => "Weinig zorgen maken",
        "N2" => "Rustig blijven",
        "N3" => "Positief blijven",
        "N4" => "Op je gemak",
        "N5" => "Matig & beheerst",
        "N6" => "Tegen een stootje",
        "O1" => "Verbeelding",
        "O2" => "Creatieve interesse",
        "O3" => "Gevoelens merken",
        "O4" => "Avontuurlijk",
        "O5" => "Nadenken & leren",
        "O6" => "Open voor verandering",
        "E1" => "Vriendelijk",
        "E2" => "Graag onder mensen",
        "E3" => "Voor jezelf opkomen",
        "E4" => "Actief tempo",
        "E5" => "Spanning zoeken",
        "E6" => "Vrolijk",
        _ => facetCode
    };

    public static string TraitNormKey(string domain) => domain switch
    {
        DeepAnalysisCompetenceItems.Consciëntieusheid => "Conscientiousness",
        DeepAnalysisCompetenceItems.Vriendelijkheid => "Agreeableness",
        DeepAnalysisCompetenceItems.EmotioneleStabiliteit => "EmotionalStability",
        DeepAnalysisCompetenceItems.Openheid => "Openness",
        DeepAnalysisCompetenceItems.Extraversie => "Extraversion",
        _ => domain
    };

    public static readonly string[] ReportTraitOrder =
    [
        DeepAnalysisCompetenceItems.Consciëntieusheid,
        DeepAnalysisCompetenceItems.Vriendelijkheid,
        DeepAnalysisCompetenceItems.EmotioneleStabiliteit,
        DeepAnalysisCompetenceItems.Openheid,
        DeepAnalysisCompetenceItems.Extraversie
    ];

    public static string[] FacetCodesForTrait(string domain) => domain switch
    {
        DeepAnalysisCompetenceItems.Consciëntieusheid => ["C1", "C2", "C3", "C4", "C5", "C6"],
        DeepAnalysisCompetenceItems.Vriendelijkheid => ["A1", "A2", "A3", "A4", "A5", "A6"],
        DeepAnalysisCompetenceItems.EmotioneleStabiliteit => ["N1", "N2", "N3", "N4", "N5", "N6"],
        DeepAnalysisCompetenceItems.Openheid => ["O1", "O2", "O3", "O4", "O5", "O6"],
        DeepAnalysisCompetenceItems.Extraversie => ["E1", "E2", "E3", "E4", "E5", "E6"],
        _ => []
    };
}
