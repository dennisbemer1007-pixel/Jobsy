namespace Jobsy.Web.Localization;

internal static class UiStringsKreeftReiskaart
{
    internal static void MergeAll(
        Dictionary<string, string> nl,
        Dictionary<string, string> en,
        Dictionary<string, string> pl,
        Dictionary<string, string> ro,
        Dictionary<string, string> ar)
    {
        Merge(nl, false);
        Merge(en, true);
        foreach (var map in new[] { pl, ro, ar })
        {
            Merge(map, false);
        }
    }

    private static void Merge(Dictionary<string, string> map, bool english)
    {
        map["Reiskaart.Aria"] = english ? "Where you are on your journey" : "Waar je bent op je reis";

        map["Reiskaart.Zone.Strand"] = english ? "Beach" : "Strand";
        map["Reiskaart.Zone.Kust"] = english ? "Coast" : "Kust";
        map["Reiskaart.Zone.Rotsen"] = english ? "Rocks" : "Rotsen";
        map["Reiskaart.Zone.Diepte"] = english ? "Depth" : "Diepte";
        map["Reiskaart.Zone.Licht"] = english ? "Light" : "Licht";

        map["Reiskaart.Shell.Intro"] = english
            ? "Still 10 layers of shell. With every step one comes off. In the Light you are out of your shell."
            : "Nog 10 lagen schild. Bij elke stap laat er een los. In het Licht ben je uit je schild.";
        map["Reiskaart.Shell.AllReleased"] = english
            ? "You are out of your shell. All ten layers are off and your new shell fits well."
            : "Je bent uit je schild. Alle tien lagen zijn los en je nieuwe schild zit goed.";
        map["Reiskaart.Shell.MidDepth"] = english
            ? "Still {0} layers of shell. {1} have already come off."
            : "Nog {0} lagen schild. Er zijn er al {1} los.";
        map["Reiskaart.Shell.AfterStepOneLeft"] = english
            ? "Still 1 layer of shell. After this step layer {0} comes off."
            : "Nog 1 laag schild. Na deze stap laat laag {0} los.";
        map["Reiskaart.Shell.AfterStepManyLeft"] = english
            ? "Still {0} layers of shell. After this step layer {1} comes off."
            : "Nog {0} lagen schild. Na deze stap laat laag {1} los.";

        map["Reiskaart.S01.Kop"] = english ? "Moment 0" : "Moment 0";
        map["Reiskaart.S01.Sub"] = english ? "You are at the beginning" : "Je staat aan het begin";
        map["Reiskaart.S02.Kop"] = english ? "Moment 0" : "Moment 0";
        map["Reiskaart.S02.Sub"] = english ? "You step onto the beach" : "Je stapt het strand op";
        map["Reiskaart.S02.Tip"] = english
            ? "After you sign in, your journey begins on the beach."
            : "Na het inloggen begint je reis op het strand.";
        map["Reiskaart.S03.Kop"] = english ? "Beach · Start" : "Strand · Start";
        map["Reiskaart.S03.Sub"] = english ? "welcome and consent" : "welkom en toestemming";
        map["Reiskaart.S03.Tip"] = english
            ? "Your old shell feels a bit tight. Along the way you discover who is underneath."
            : "Je oude schild zit wat krap. Onderweg ontdek je wie eronder zit.";

        map["Reiskaart.S04.Kop"] = english ? "Coast · About you" : "Kust · Over jou";
        map["Reiskaart.S04.Sub"] = english ? "step 1 of 3" : "stap 1 van 3";
        map["Reiskaart.S04.Tip"] = english
            ? "You start calmly: just your name and where you live."
            : "Je begint rustig: alleen je naam en waar je woont.";
        map["Reiskaart.S05.Kop"] = english ? "Coast · About you" : "Kust · Over jou";
        map["Reiskaart.S05.Sub"] = english ? "step 2 of 3" : "stap 2 van 3";
        map["Reiskaart.S05.Tip"] = english
            ? "There is no right or wrong. Choose what fits your life now."
            : "Er is geen goed of fout. Kies wat nu bij je leven past.";
        map["Reiskaart.S06.Kop"] = english ? "Coast · About you" : "Kust · Over jou";
        map["Reiskaart.S06.Sub"] = english ? "step 3 of 3" : "stap 3 van 3";
        map["Reiskaart.S06.Tip"] = english
            ? "Choose how far you want to travel. You will only see work within that distance."
            : "Kies hoe ver je wilt reizen. Je ziet dan alleen werk binnen die afstand.";

        map["Reiskaart.S07.Kop"] = english ? "Rocks · Background and Dream" : "Rotsen · Achtergrond en Droom";
        map["Reiskaart.S07.Sub"] = english ? "background · optional" : "achtergrond · optioneel";
        map["Reiskaart.S07.Tip"] = english
            ? "Everything counts. Volunteer work and caring for others too."
            : "Alles telt mee. Ook vrijwilligerswerk en zorgen voor anderen.";
        map["Reiskaart.S08.Kop"] = english ? "Rocks · Background and Dream" : "Rotsen · Achtergrond en Droom";
        map["Reiskaart.S08.Sub"] = english ? "background · optional" : "achtergrond · optioneel";
        map["Reiskaart.S08.Tip"] = english
            ? "A diploma is nice, but what you can do matters just as much."
            : "Een diploma is fijn, maar wat je kunt telt net zo zwaar.";
        map["Reiskaart.S09.Kop"] = english ? "Rocks · Background and Dream" : "Rotsen · Achtergrond en Droom";
        map["Reiskaart.S09.Sub"] = english ? "dream · 1 step" : "droom · 1 stap";
        map["Reiskaart.S09.Tip"] = english
            ? "Think big. You will find the path step by step later."
            : "Denk gerust groot. De weg ernaartoe zoek je later stap voor stap uit.";

        map["Reiskaart.S10.Kop"] = english ? "Depth · Who you are" : "Diepte · Wie ben jij";
        map["Reiskaart.S10.Sub"] = english ? "test 1 of 4" : "test 1 van 4";
        map["Reiskaart.S10.Tip"] = english
            ? "Choose what feels most like you. Your first instinct is usually right."
            : "Kies wat het meest op je lijkt. Je eerste gevoel is meestal goed.";
        map["Reiskaart.S11.Kop"] = english ? "Depth · Who you are" : "Diepte · Wie ben jij";
        map["Reiskaart.S11.Sub"] = english ? "test 2 of 4" : "test 2 van 4";
        map["Reiskaart.S11.Tip"] = english
            ? "Think of days when time flew by."
            : "Denk aan dagen waarop de tijd voorbij vloog.";
        map["Reiskaart.S12.Kop"] = english ? "Depth · Who you are" : "Diepte · Wie ben jij";
        map["Reiskaart.S12.Sub"] = english ? "halfway" : "halverwege";
        map["Reiskaart.S12.Tip"] = english
            ? "You are halfway through the depth. Continue whenever you like."
            : "Je bent halverwege de diepte. Ga verder wanneer je wilt.";
        map["Reiskaart.S13.Kop"] = english ? "Depth · Who you are" : "Diepte · Wie ben jij";
        map["Reiskaart.S13.Sub"] = english ? "test 3 of 4" : "test 3 van 4";
        map["Reiskaart.S13.Tip"] = english
            ? "Some people like bustle, others like calm. Both are fine."
            : "Sommige mensen houden van drukte, anderen van rust. Beide is goed.";
        map["Reiskaart.S14.Kop"] = english ? "Depth · Who you are" : "Diepte · Wie ben jij";
        map["Reiskaart.S14.Sub"] = english ? "test 4 of 4" : "test 4 van 4";
        map["Reiskaart.S14.Tip"] = english
            ? "Last test. Choose what you truly value, not what you think you should."
            : "Laatste test. Kies wat je echt belangrijk vindt, niet wat hoort.";

        map["Reiskaart.S15.Kop"] = english ? "Light · Your Compass" : "Licht · Jouw Kompas";
        map["Reiskaart.S15.Sub"] = english ? "journey complete" : "reis afgerond";
        map["Reiskaart.S15.Tip"] = english
            ? "See how far you have come. This is your new shell."
            : "Kijk hoe ver je gekomen bent. Dit is je nieuwe schild.";
        map["Reiskaart.S16.Kop"] = english ? "Light · Passport" : "Licht · Paspoort";
        map["Reiskaart.S16.Sub"] = english ? "ready to share" : "klaar om te delen";
        map["Reiskaart.S16.Tip"] = english
            ? "Your passport is ready. Employers see who you are, not just your CV."
            : "Je paspoort is klaar. Werkgevers zien wie je bent, niet alleen je cv.";
    }
}
