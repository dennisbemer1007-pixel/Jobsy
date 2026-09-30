namespace Jobsy.Web.Localization;

/// <summary>
/// Dutch-only Sales / Lobsy Partner UI strings (D10). Other languages fall back to nl via <see cref="UiStrings.Get"/>.
/// Prefixes Sales. / SalesAdmin. / SalesMail. / SalesPdf. / EntUi. are exempt from localization parity.
/// </summary>
public static class UiStringsSales
{
    public static bool IsNlOnlyPrefix(string key) =>
        key.StartsWith("Sales.", StringComparison.Ordinal)
        || key.StartsWith("SalesAdmin.", StringComparison.Ordinal)
        || key.StartsWith("SalesMail.", StringComparison.Ordinal)
        || key.StartsWith("SalesPdf.", StringComparison.Ordinal)
        || key.StartsWith("EntUi.", StringComparison.Ordinal);

    public static void MergeNl(Dictionary<string, string> nl)
    {
        // —— Enterprise UI primitives ——
        nl["EntUi.FilterSearch"] = "Zoeken…";
        nl["EntUi.CloseDrawer"] = "Sluiten";
        nl["EntUi.Loading"] = "Laden…";
        nl["EntUi.Empty"] = "Nog niets hier.";
        nl["EntUi.Selected"] = "{0} geselecteerd";
        nl["EntUi.Actions"] = "Acties";
        nl["EntUi.Pager"] = "Paginering";
        nl["EntUi.PrevPage"] = "Vorige";
        nl["EntUi.NextPage"] = "Volgende";
        nl["EntUi.PageOf"] = "Pagina {0} van {1}";
        nl["EntUi.ClearSelection"] = "Selectie wissen";
        nl["EntUi.ScopeChip"] = "Rol";
        nl["EntUi.ReadOnly"] = "Alleen lezen";

        // —— Shell / nav ——
        nl["Sales.ProductLabel"] = "Lobsy Partner";
        nl["Sales.RoleChip"] = "Salesmanager";
        nl["Sales.RoleChipNoCode"] = "Nog geen code";
        nl["Sales.BreadcrumbRoot"] = "Partner";
        nl["Sales.WalletAvailable"] = "Beschikbaar {0}";
        nl["Sales.SearchEmployers"] = "Zoek een werkgever…";
        nl["Sales.Account.MfaOn"] = "2FA aan";
        nl["Sales.Account.Logout"] = "Uitloggen";
        nl["Sales.PrivacyFooter"] =
            "Je ziet bedrijfsnamen en je eigen commissie. Geen contactpersonen, kandidaten of vacaturedetails.";
        nl["Sales.Nav.Group.Overview"] = "Overzicht";
        nl["Sales.Nav.Group.Sell"] = "Verkopen";
        nl["Sales.Nav.Group.Money"] = "Geld";
        nl["Sales.Nav.Group.Account"] = "Account";
        nl["Sales.Nav.Dashboard"] = "Dashboard";
        nl["Sales.Nav.Link"] = "Mijn link & materiaal";
        nl["Sales.Nav.Employers"] = "Mijn werkgevers";
        nl["Sales.Nav.Recommend"] = "Salesmanager aanbevelen";
        nl["Sales.Nav.Wallet"] = "Wallet & uitbetalingen";
        nl["Sales.Nav.Profile"] = "Profiel & gegevens";
        nl["Sales.Nav.Help"] = "Hulp & afspraken";
        nl["Sales.Nav.Bottom.Overview"] = "Overzicht";
        nl["Sales.Nav.Bottom.Link"] = "Mijn link";
        nl["Sales.Nav.Bottom.Employers"] = "Werkgevers";
        nl["Sales.Nav.Bottom.Wallet"] = "Wallet";
        nl["Sales.Nav.Bottom.More"] = "Meer";
        nl["Sales.Nav.MoreSheet"] = "Meer";
        nl["Sales.Nav.CloseMenu"] = "Menu sluiten";

        // —— Labels (D12) ——
        nl["Sales.Label.Kind.TokenCommission"] = "Commissie";
        nl["Sales.Label.Kind.FounderBonus"] = "Founder-bonus";
        nl["Sales.Label.Kind.Payout"] = "Uitbetaling";
        nl["Sales.Label.Kind.Adjustment"] = "Correctie";
        nl["Sales.Label.Kind.IndirectTokenCommission"] = "Commissie via aanbeveling";
        nl["Sales.Label.Kind.RefundCorrection"] = "Correctie: terugbetaling";
        nl["Sales.Label.Kind.ChargebackCorrection"] = "Correctie: chargeback";
        nl["Sales.Label.State.Pending"] = "In behandeling";
        nl["Sales.Label.State.Available"] = "Beschikbaar";
        nl["Sales.Label.State.Requested"] = "Aangevraagd";
        nl["Sales.Label.State.Paid"] = "Uitbetaald";
        nl["Sales.Label.State.Settled"] = "Verrekend";
        nl["Sales.Label.PayoutRequest.Requested"] = "Aangevraagd";
        nl["Sales.Label.PayoutRequest.InRun"] = "In ronde";
        nl["Sales.Label.PayoutRequest.Approved"] = "Goedgekeurd";
        nl["Sales.Label.PayoutRequest.Rejected"] = "Afgewezen";
        nl["Sales.Label.PayoutRequest.Paid"] = "Uitbetaald";
        nl["Sales.Label.PayoutRequest.Cancelled"] = "Geannuleerd";
        nl["Sales.Label.PayoutRun.Draft"] = "Concept";
        nl["Sales.Label.PayoutRun.Approved"] = "Goedgekeurd";
        nl["Sales.Label.PayoutRun.Exported"] = "Geëxporteerd";
        nl["Sales.Label.PayoutRun.Paid"] = "Uitbetaald";
        nl["Sales.Label.PayoutRun.Closed"] = "Gesloten";
        nl["Sales.Label.Invoice.Draft"] = "Concept";
        nl["Sales.Label.Invoice.Issued"] = "Uitgereikt";
        nl["Sales.Label.Invoice.Paid"] = "Betaald";
        nl["Sales.Label.Invoice.Cancelled"] = "Geannuleerd";
        nl["Sales.Label.Vat.Standard21"] = "21 % btw";
        nl["Sales.Label.Vat.KOR"] = "Kleineondernemersregeling (KOR)";
        nl["Sales.Label.Vat.ReverseCharge"] = "Btw-verlegging";
        nl["Sales.Label.Vat.Exempt"] = "Vrijgesteld";
        nl["Sales.Label.Attribution.TypedCode"] = "Via code";
        nl["Sales.Label.Attribution.LinkCookie"] = "Via link";
        nl["Sales.Label.Attribution.Admin"] = "Door Lobsy";
        nl["Sales.Label.Attribution.Legacy"] = "Eerder";
        nl["Sales.Label.Application.Pending"] = "In behandeling";
        nl["Sales.Label.Application.Approved"] = "Goedgekeurd";
        nl["Sales.Label.Application.Rejected"] = "Afgewezen";
        nl["Sales.Label.Application.Expired"] = "Verlopen";
        nl["Sales.Label.Employers"] = "Werkgevers";

        // —— Admin / parking ——
        nl["SalesAdmin.AmbassadorsToggle"] = "Ambassadeursprogramma";
        nl["SalesAdmin.AmbassadorsToggleHelp"] =
            "Zet het ambassadeursprogramma aan of uit. Uit: geen pagina's, geen links, geen nieuwe commissie. Gegevens blijven bewaard.";
        nl["SalesAdmin.ParkedBalancesNote"] =
            "{0} geparkeerde ambassadeurs hebben nog € {1} tegoed. Het programma staat uit; betaal of verreken dit met de hand.";
        nl["SalesAdmin.SalesSection"] = "Sales";

        // —— Auth / parked sign-in ——
        nl["Sales.Ambassadors.ParkedLogin"] =
            "Het ambassadeursprogramma is gepauzeerd. Je gegevens en je tegoed blijven bewaard. Vragen? Mail support@lobsy.nl.";
    }
}
