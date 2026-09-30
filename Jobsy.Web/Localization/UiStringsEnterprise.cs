namespace Jobsy.Web.Localization;

/// <summary>Shared enterprise UI primitive strings (Ent*). Consumed by scholen / admin / werkgever.</summary>
public static class UiStringsEnterprise
{
    public static void MergeAll(
        Dictionary<string, string> nl, Dictionary<string, string> en,
        Dictionary<string, string> pl, Dictionary<string, string> ro,
        Dictionary<string, string> ar)
    {
        void Add(string key, string nlText, string enText)
        {
            nl[key] = nlText;
            en[key] = enText;
            pl[key] = enText;
            ro[key] = enText;
            ar[key] = enText;
        }

        Add("EntUi.Loading", "Laden…", "Loading…");
        Add("EntUi.Empty", "Niets gevonden.", "Nothing found.");
        Add("EntUi.Selected", "{0} geselecteerd", "{0} selected");
        Add("EntUi.ClearSelection", "Selectie wissen", "Clear selection");
        Add("EntUi.FilterSearch", "Zoeken", "Search");
        Add("EntUi.PrevPage", "Vorige", "Previous");
        Add("EntUi.NextPage", "Volgende", "Next");
        Add("EntUi.PageOf", "Pagina {0} van {1}", "Page {0} of {1}");
        Add("EntUi.Pager", "Paginering", "Pagination");
        Add("EntUi.CloseDrawer", "Sluiten", "Close");
        Add("EntUi.Actions", "Acties", "Actions");
        Add("EntUi.ScopeChip", "Scopekiezer", "Scope selector");
        Add("EntUi.ReadOnly", "Alleen lezen", "Read only");
        Add("Nav.Scholen", "Scholen", "Schools");
    }
}
