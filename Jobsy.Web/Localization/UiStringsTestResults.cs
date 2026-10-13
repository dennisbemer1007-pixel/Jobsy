namespace Jobsy.Web.Localization;

/// <summary>Result-page / locked teaser / gold block / edit-redo / quota strings (nl, en, pl, ro, ar).</summary>
internal static class UiStringsTestResults
{
    public static void MergeAll(
        Dictionary<string, string> nl,
        Dictionary<string, string> en,
        Dictionary<string, string> pl,
        Dictionary<string, string> ro,
        Dictionary<string, string> ar)
    {
        Merge(nl, Nl());
        Merge(en, En());
        Merge(pl, Pl());
        Merge(ro, Ro());
        Merge(ar, Ar());
    }

    private static void Merge(Dictionary<string, string> target, Dictionary<string, string> src)
    {
        foreach (var (k, v) in src)
        {
            target[k] = v;
        }
    }

    private static Dictionary<string, string> Nl() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["TestResult.Back"] = "Terug naar mijn tests",
        ["TestResult.ScoresCount"] = "{0} scores",
        ["TestResult.ScoresFromQuestions"] = "{0} scores uit {1} vragen",
        ["TestResult.Upsell.Self"] = "Een scherper beeld van hoe jij werkt",
        ["TestResult.EditAnswers"] = "Antwoorden wijzigen",
        ["TestResult.Retake"] = "Test opnieuw doen",
        ["TestResult.Quota.Line"] = "Je kunt deze test nog {0} van de {1} keer aanpassen.",
        ["TestResult.Quota.LineOne"] = "Je kunt deze test nog 1 van de {0} keer aanpassen.",
        ["TestResult.Quota.Zero"] = "Je hebt deze test al {0} van de {0} keer aangepast. Wijzigen en opnieuw doen kan niet meer; je huidige resultaat blijft staan.",
        ["TestResult.Locked.Title"] = "Uitgebreid rapport",
        ["TestResult.Locked.Lead"] = "Zo ziet jouw rapport eruit na de uitgebreide test: {0} vragen, {1} pagina's, volledig op jou afgestemd. Hieronder zie je een voorproefje.",
        ["TestResult.Locked.SamplePill"] = "Voorbeelddata – niet jouw resultaat",
        ["TestResult.Locked.Chip"] = "Vergrendeld",
        ["TestResult.Locked.VisuallyHidden"] = "Voorbeeld van {0}; beschikbaar na de uitgebreide test",
        ["TestResult.Locked.Foot.Pages"] = "{0} pagina's als PDF",
        ["TestResult.Locked.Foot.Apps"] = "Gebruik bij sollicitaties",
        ["TestResult.Locked.Foot.Matches"] = "Scherpere matches met vacatures",
        ["TestResult.Card.Radar"] = "Radar vs gemiddelde",
        ["TestResult.Card.Radar.Sub.Competence"] = "Gemiddelde NL (Johnson, 2014)",
        ["TestResult.Card.Radar.Sub.Lobsy"] = "Gemiddelde Lobsy-kandidaten",
        ["TestResult.Card.Comparison"] = "Vergelijking met anderen",
        ["TestResult.Card.Comparison.Sub.Competence"] = "Indicatie t.o.v. 2.707 Nederlandse volwassenen (Johnson, 2014)",
        ["TestResult.Card.Comparison.Sub.Lobsy"] = "Jij tegenover alle Lobsy-kandidaten die deze test deden",
        ["TestResult.Card.Facets"] = "Deelscores per eigenschap",
        ["TestResult.Card.Facets.Sub"] = "Zes facetten per eigenschap met normmarkers",
        ["TestResult.Card.Facets.Sub.Culture"] = "Elf domeinscores: werkcultuur en persoonlijkheid op het werk",
        ["TestResult.Card.Occupations"] = "Beroepen die bij je passen",
        ["TestResult.Card.Occupations.Sub"] = "Top 10 op basis van je profiel, met uitleg",
        ["TestResult.Card.Holland"] = "Jouw Holland-code uitgelegd",
        ["TestResult.Card.Holland.Sub"] = "Drielettercode met passende werkomgevingen",
        ["TestResult.Card.Employers"] = "Werkgevers die bij je passen",
        ["TestResult.Card.Employers.Sub"] = "Type organisatie op basis van je profiel",
        ["TestResult.Card.ValuesRank"] = "Jouw waarden op volgorde",
        ["TestResult.Card.ValuesRank.Sub"] = "Wat het zwaarst weegt – en wat minder",
        ["TestResult.Card.ActionPlan"] = "Jouw actieplan",
        ["TestResult.Card.ActionPlan.Sub"] = "Drie concrete stappen na de uitgebreide test",
        ["TestResult.Card.Strengths"] = "Sterke punten & valkuilen",
        ["TestResult.Card.Strengths.Sub"] = "Wat je helpt – en waar je op let",
        ["TestResult.Gold.Tag"] = "Uitgebreide test",
        ["TestResult.Gold.Title"] = "Haal alles uit je {0}",
        ["TestResult.Gold.Lead"] = "Na de uitgebreide test krijg je een volledig rapport met radar, vergelijking, actieplan en PDF – berekend op jouw antwoorden.",
        ["TestResult.Gold.Check.Pages"] = "{0} pagina's als PDF",
        ["TestResult.Gold.Check.Applications"] = "Gebruik bij sollicitaties",
        ["TestResult.Gold.Check.Matches"] = "Scherpere matches met vacatures",
        ["TestResult.Gold.Start"] = "Start uitgebreide test",
        ["TestResult.Gold.StartAria"] = "Start uitgebreide test, {0} euro",
        ["TestResult.Gold.Meta"] = "{0} vragen · ca. {1} min",
        ["TestResult.Gold.Fine"] = "Eenmalig · geen abonnement · veilig betalen via Mollie",
        ["TestResult.Gold.SamplePdf"] = "Bekijk voorbeeld-PDF",
        ["TestResult.Gold.Continue"] = "Ga verder met de uitgebreide test ({0}/{1})",
        ["TestResult.SamplePdf.Title"] = "Voorbeeld-PDF · {0}",
        ["TestResult.SamplePdf.Pages"] = "Pagina 1–{0} van {1} · met voorbeelddata, zo ziet jouw rapport eruit",
        ["TestResult.SamplePdf.Download"] = "Download voorbeeld",
        ["TestResult.SamplePdf.DownloadMobile"] = "Download voorbeeld (PDF)",
        ["TestResult.SamplePdf.Close"] = "Sluiten",
        ["TestResult.SamplePdf.PageAlt"] = "Voorbeeld-PDF pagina {0}",
        ["TestResult.SamplePdf.FooterNote"] = "Na de uitgebreide test krijg je dit rapport met jouw eigen scores.",
        ["TestResult.SamplePdf.Fallback"] = "Voorbeeld kon niet als afbeelding geladen worden. Download de PDF om hem te bekijken.",
        ["TestResult.Redo.Title"] = "Test opnieuw doen?",
        ["TestResult.Redo.Lead"] = "Je begint met lege antwoorden. Je huidige resultaat blijft zichtbaar tot je de nieuwe test afrondt.",
        ["TestResult.Redo.Info"] = "Opnieuw doen telt als 1 van je 3 aanpassingen. Daarna kun je deze test nog {0} van de 3 keer aanpassen.",
        ["TestResult.Redo.Hint"] = "Liever alleen een paar antwoorden aanpassen? Gebruik ‘Antwoorden wijzigen’ – dat telt ook als 1 aanpassing.",
        ["TestResult.Redo.Start"] = "Opnieuw beginnen",
        ["TestResult.Redo.PreferEdit"] = "Liever antwoorden wijzigen",
        ["TestResult.Redo.Cancel"] = "Annuleren",
        ["TestResult.History.Title"] = "Eerdere resultaten",
        ["TestResult.Stamp"] = "Voorbeelddata",
        ["TestResult.Sample.LabelA"] = "Eigenschap A",
        ["TestResult.Sample.LabelB"] = "Eigenschap B",
        ["TestResult.Sample.LabelC"] = "Eigenschap C",
        ["TestResult.Edit.FlagOff"] = "Antwoorden wijzigen volgt zodra de vragenlijst-fix op Acceptatie staat.",
        ["TestResult.Limit.Dialog"] = "Je hebt het maximum van 3 aanpassingen bereikt. Wijzigen en opnieuw doen kan niet meer."
    };

    private static Dictionary<string, string> En() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["TestResult.Back"] = "Back to my tests",
        ["TestResult.ScoresCount"] = "{0} scores",
        ["TestResult.ScoresFromQuestions"] = "{0} scores from {1} questions",
        ["TestResult.Upsell.Self"] = "A sharper picture of how you work",
        ["TestResult.EditAnswers"] = "Edit answers",
        ["TestResult.Retake"] = "Retake test",
        ["TestResult.Quota.Line"] = "You can adjust this test {0} more times (out of {1}).",
        ["TestResult.Quota.LineOne"] = "You can adjust this test 1 more time (out of {0}).",
        ["TestResult.Quota.Zero"] = "You've already adjusted this test {0} times (the maximum). Editing and retaking are no longer possible; your current result stays as it is.",
        ["TestResult.Locked.Title"] = "Full report",
        ["TestResult.Locked.Lead"] = "This is what your report looks like after the extended test: {0} questions, {1} pages, fully tailored to you. Below is a preview.",
        ["TestResult.Locked.SamplePill"] = "Sample data – not your result",
        ["TestResult.Locked.Chip"] = "Locked",
        ["TestResult.Locked.VisuallyHidden"] = "Sample of {0}; available after the extended test",
        ["TestResult.Locked.Foot.Pages"] = "{0} pages as PDF",
        ["TestResult.Locked.Foot.Apps"] = "Use in job applications",
        ["TestResult.Locked.Foot.Matches"] = "Sharper vacancy matches",
        ["TestResult.Card.Radar"] = "Radar vs average",
        ["TestResult.Card.Radar.Sub.Competence"] = "NL average (Johnson, 2014)",
        ["TestResult.Card.Radar.Sub.Lobsy"] = "Average of Lobsy candidates",
        ["TestResult.Card.Comparison"] = "Comparison with others",
        ["TestResult.Card.Comparison.Sub.Competence"] = "Indication vs 2,707 Dutch adults (Johnson, 2014)",
        ["TestResult.Card.Comparison.Sub.Lobsy"] = "You versus all Lobsy candidates who took this test",
        ["TestResult.Card.Facets"] = "Facet scores per trait",
        ["TestResult.Card.Facets.Sub"] = "Six facets per trait with norm markers",
        ["TestResult.Card.Facets.Sub.Culture"] = "Eleven domain scores: work culture and workplace personality",
        ["TestResult.Card.Occupations"] = "Occupations that fit you",
        ["TestResult.Card.Occupations.Sub"] = "Top 10 based on your profile, with reasons",
        ["TestResult.Card.Holland"] = "Your Holland code explained",
        ["TestResult.Card.Holland.Sub"] = "Three-letter code with fitting work environments",
        ["TestResult.Card.Employers"] = "Employers that fit you",
        ["TestResult.Card.Employers.Sub"] = "Organisation types based on your profile",
        ["TestResult.Card.ValuesRank"] = "Your values ranked",
        ["TestResult.Card.ValuesRank.Sub"] = "What weighs heaviest – and what less",
        ["TestResult.Card.ActionPlan"] = "Your action plan",
        ["TestResult.Card.ActionPlan.Sub"] = "Three concrete steps after the extended test",
        ["TestResult.Card.Strengths"] = "Strengths & pitfalls",
        ["TestResult.Card.Strengths.Sub"] = "What helps you – and what to watch",
        ["TestResult.Gold.Tag"] = "Extended test",
        ["TestResult.Gold.Title"] = "Get the most from your {0}",
        ["TestResult.Gold.Lead"] = "After the extended test you get a full report with radar, comparison, action plan and PDF – computed from your answers.",
        ["TestResult.Gold.Check.Pages"] = "{0} pages as PDF",
        ["TestResult.Gold.Check.Applications"] = "Use in job applications",
        ["TestResult.Gold.Check.Matches"] = "Sharper vacancy matches",
        ["TestResult.Gold.Start"] = "Start extended test",
        ["TestResult.Gold.StartAria"] = "Start extended test, {0} euro",
        ["TestResult.Gold.Meta"] = "{0} questions · approx. {1} min",
        ["TestResult.Gold.Fine"] = "One-off · no subscription · secure payment via Mollie",
        ["TestResult.Gold.SamplePdf"] = "View sample PDF",
        ["TestResult.Gold.Continue"] = "Continue the extended test ({0}/{1})",
        ["TestResult.SamplePdf.Title"] = "Sample PDF · {0}",
        ["TestResult.SamplePdf.Pages"] = "Pages 1–{0} of {1} · sample data, this is what your report looks like",
        ["TestResult.SamplePdf.Download"] = "Download sample",
        ["TestResult.SamplePdf.DownloadMobile"] = "Download sample (PDF)",
        ["TestResult.SamplePdf.Close"] = "Close",
        ["TestResult.SamplePdf.PageAlt"] = "Sample PDF page {0}",
        ["TestResult.SamplePdf.FooterNote"] = "After the extended test you get this report with your own scores.",
        ["TestResult.SamplePdf.Fallback"] = "The sample could not be loaded as images. Download the PDF to view it.",
        ["TestResult.Redo.Title"] = "Retake the test?",
        ["TestResult.Redo.Lead"] = "You start with empty answers. Your current result stays visible until you finish the new test.",
        ["TestResult.Redo.Info"] = "Retaking counts as 1 of your 3 adjustments. After that you can adjust this test {0} more times (out of 3).",
        ["TestResult.Redo.Hint"] = "Prefer changing only a few answers? Use ‘Edit answers’ – that also counts as 1 adjustment.",
        ["TestResult.Redo.Start"] = "Start again",
        ["TestResult.Redo.PreferEdit"] = "I'd rather edit answers",
        ["TestResult.Redo.Cancel"] = "Cancel",
        ["TestResult.History.Title"] = "Earlier results",
        ["TestResult.Stamp"] = "Sample data",
        ["TestResult.Sample.LabelA"] = "Trait A",
        ["TestResult.Sample.LabelB"] = "Trait B",
        ["TestResult.Sample.LabelC"] = "Trait C",
        ["TestResult.Edit.FlagOff"] = "Edit answers will unlock once the questionnaire fix is on Acceptatie.",
        ["TestResult.Limit.Dialog"] = "You've reached the maximum of 3 adjustments. Editing and retaking are no longer possible."
    };

    private static Dictionary<string, string> Pl()
    {
        var d = new Dictionary<string, string>(En(), StringComparer.OrdinalIgnoreCase);
        d["TestResult.ScoresFromQuestions"] = "{0} wyników z {1} pytań";
        d["TestResult.Upsell.Self"] = "Wyraźniejszy obraz tego, jak pracujesz";
        d["TestResult.EditAnswers"] = "Edytuj odpowiedzi";
        d["TestResult.Retake"] = "Zrób test ponownie";
        d["TestResult.Quota.Line"] = "Możesz jeszcze {0} z {1} razy dostosować ten test.";
        d["TestResult.Quota.LineOne"] = "Możesz jeszcze 1 z {0} razy dostosować ten test.";
        d["TestResult.Quota.Zero"] = "Dostosowałeś już ten test {0} z {0} razy. Edycja i ponowne podejście nie są już możliwe; wynik zostaje.";
        return d;
    }

    private static Dictionary<string, string> Ro()
    {
        var d = new Dictionary<string, string>(En(), StringComparer.OrdinalIgnoreCase);
        d["TestResult.ScoresFromQuestions"] = "{0} scoruri din {1} întrebări";
        d["TestResult.Upsell.Self"] = "O imagine mai clară despre cum lucrezi";
        d["TestResult.EditAnswers"] = "Modifică răspunsurile";
        d["TestResult.Retake"] = "Refă testul";
        d["TestResult.Quota.Line"] = "Poți ajusta acest test încă de {0} ori din {1}.";
        d["TestResult.Quota.LineOne"] = "Poți ajusta acest test încă o dată din {0}.";
        d["TestResult.Quota.Zero"] = "Ai ajustat deja acest test de {0} ori din {0}. Editarea și reluarea nu mai sunt posibile; rezultatul rămâne.";
        return d;
    }

    private static Dictionary<string, string> Ar()
    {
        var d = new Dictionary<string, string>(En(), StringComparer.OrdinalIgnoreCase);
        d["TestResult.ScoresFromQuestions"] = "{0} درجات من {1} أسئلة";
        d["TestResult.Upsell.Self"] = "صورة أوضح لكيفية عملك";
        d["TestResult.EditAnswers"] = "تعديل الإجابات";
        d["TestResult.Retake"] = "أعد الاختبار";
        d["TestResult.Quota.Line"] = "يمكنك تعديل هذا الاختبار {0} مرات أخرى من أصل {1}.";
        d["TestResult.Quota.LineOne"] = "يمكنك تعديل هذا الاختبار مرة واحدة أخرى من أصل {0}.";
        d["TestResult.Quota.Zero"] = "لقد عدّلت هذا الاختبار بالفعل {0} من {0} مرات. لم يعد التعديل أو الإعادة ممكنين؛ تبقى نتيجتك الحالية.";
        return d;
    }
}
