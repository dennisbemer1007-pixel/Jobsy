namespace Jobsy.Web.Localization;

/// <summary>Chrome around the sourced job outlook. The outlook sentences themselves stay the Dutch templates.</summary>
public static class UiStringsOutlook
{
    public static void MergeAll(
        Dictionary<string, string> nl, Dictionary<string, string> en,
        Dictionary<string, string> pl, Dictionary<string, string> ro,
        Dictionary<string, string> ar)
    {
        void Add(string key, string nlText, string enText, string plText, string roText, string arText)
        {
            nl[key] = nlText;
            en[key] = enText;
            pl[key] = plText;
            ro[key] = roText;
            ar[key] = arText;
        }

        Add("Outlook.Title",
            "Toekomst van dit werk",
            "The future of this work",
            "Przyszłość tej pracy",
            "Viitorul acestei munci",
            "مستقبل هذا العمل");
        Add("Outlook.GoodToKnow",
            "Goed om te weten",
            "Good to know",
            "Warto wiedzieć",
            "Bine de știut",
            "من الجيد أن تعرف");
        Add("Outlook.WhatChanges",
            "Wat verandert",
            "What can change",
            "Co może się zmienić",
            "Ce se poate schimba",
            "ما الذي قد يتغير");
        Add("Outlook.WhatStays",
            "Wat blijft mensenwerk",
            "What stays human work",
            "Co zostaje pracą dla ludzi",
            "Ce rămâne muncă pentru oameni",
            "ما يبقى عملاً للناس");
        Add("Outlook.Nearby",
            "Werk dat erop lijkt",
            "Work that is close to this",
            "Praca, która jest blisko",
            "Muncă apropiată de asta",
            "عمل قريب من هذا");
        Add("Outlook.Learn",
            "Slim om nu te leren",
            "Smart to learn now",
            "Warto nauczyć się teraz",
            "E bine să înveți acum",
            "من الحكمة أن تتعلم الآن");
        Add("Outlook.AskWork",
            "Wat voor werk is dit?",
            "What kind of work is this?",
            "Jaka to praca?",
            "Ce fel de muncă este?",
            "ما نوع هذا العمل؟");
        Add("Outlook.YesThis",
            "Ja, dit is mijn werk",
            "Yes, this is my work",
            "Tak, to moja praca",
            "Da, asta este munca mea",
            "نعم، هذا عملي");
        Add("Outlook.NotListed",
            "Staat er niet bij",
            "It is not in the list",
            "Nie ma jej na liście",
            "Nu este în listă",
            "غير موجود في القائمة");
        Add("Outlook.Confirmed",
            "Dit is je werk: {0}",
            "This is your work: {0}",
            "To twoja praca: {0}",
            "Aceasta este munca ta: {0}",
            "هذا عملك: {0}");
        Add("Outlook.NoAdjacent",
            "We hebben nu geen beroep gevonden dat dichtbij ligt en een betere vooruitblik heeft.",
            "We have not found work nearby with a better outlook.",
            "Nie znaleźliśmy bliskiej pracy z lepszą perspektywą.",
            "Nu am găsit o muncă apropiată cu o perspectivă mai bună.",
            "لم نجد عملاً قريباً له أفق أفضل.");
    }
}
