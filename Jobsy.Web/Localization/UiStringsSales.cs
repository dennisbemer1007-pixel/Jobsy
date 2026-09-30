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
        nl["Sales.Label.EmployerStatus.NoPurchase"] = "Nog geen aankoop";
        nl["Sales.Label.EmployerStatus.Active"] = "Actief";
        nl["Sales.Label.EmployerStatus.Quiet"] = "Stil · {0} dagen";
        nl["Sales.Label.EmployerStatus.Ended"] = "Afgelopen";

        // —— Dashboard ——
        nl["Sales.Dash.GreetingMorning"] = "Goedemorgen, {0}";
        nl["Sales.Dash.GreetingAfternoon"] = "Goedemiddag, {0}";
        nl["Sales.Dash.GreetingEvening"] = "Goedenavond, {0}";
        nl["Sales.Dash.Hi"] = "Hoi {0}";
        nl["Sales.Dash.Lead"] = "Zo gaat het met je verkoop. Bedragen zijn excl. btw.";
        nl["Sales.Dash.Period.Month"] = "Maand";
        nl["Sales.Dash.Period.Year"] = "Dit jaar";
        nl["Sales.Dash.Period.All"] = "Alles";
        nl["Sales.Dash.CopyLink"] = "Kopieer mijn link";
        nl["Sales.Dash.LinkCopied"] = "Link gekopieerd";
        nl["Sales.Dash.Kpi.Available"] = "Beschikbaar om uit te betalen";
        nl["Sales.Dash.Kpi.AvailableSub"] = "excl. btw · volgende uitbetaalronde {0}";
        nl["Sales.Dash.Kpi.RequestPayout"] = "Uitbetaling aanvragen";
        nl["Sales.Dash.Kpi.Earned"] = "Verdiend {0}";
        nl["Sales.Dash.Kpi.EarnedDelta"] = "+{0} % t.o.v. {1}";
        nl["Sales.Dash.Kpi.Pending"] = "In behandeling";
        nl["Sales.Dash.Kpi.PendingSub"] = "vrij na {0} dagen";
        nl["Sales.Dash.Kpi.ActiveEmployers"] = "Actieve werkgevers";
        nl["Sales.Dash.Kpi.ActiveOf"] = "{0} van {1}";
        nl["Sales.Dash.Kpi.NewThisMonth"] = "+{0} deze maand";
        nl["Sales.Dash.Kpi.Registrations"] = "Aanmeldingen";
        nl["Sales.Dash.MonthlyTitle"] = "Je commissie per maand";
        nl["Sales.Dash.MonthlyTitleShort"] = "Commissie per maand";
        nl["Sales.Dash.MonthlySum"] = "{0} in 12 maanden";
        nl["Sales.Dash.MonthlyLegendPaid"] = "Uitbetaald of beschikbaar";
        nl["Sales.Dash.MonthlyLegendCurrent"] = "Deze maand (deels in behandeling)";
        nl["Sales.Dash.AllMutations"] = "Alle mutaties";
        nl["Sales.Dash.FunnelTitle"] = "Van link naar klant";
        nl["Sales.Dash.Funnel.Visits"] = "Bezoeken via je link";
        nl["Sales.Dash.Funnel.Registered"] = "Aangemeld";
        nl["Sales.Dash.Funnel.FirstPurchase"] = "Eerste aankoop";
        nl["Sales.Dash.Funnel.Active"] = "Nog actief";
        nl["Sales.Dash.Funnel.OfVisits"] = "{0} % van bezoeken";
        nl["Sales.Dash.Funnel.OfRegistered"] = "{0} % van aanmeldingen";
        nl["Sales.Dash.Funnel.ActiveHint"] = "aankoop < 90 dagen";
        nl["Sales.Dash.Funnel.Empty"] = "Deel je link om te beginnen.";
        nl["Sales.Dash.Funnel.Cta"] = "Naar mijn link";
        nl["Sales.Funnel.Period.Month"] = "deze maand";
        nl["Sales.Funnel.Period.Year"] = "dit jaar";
        nl["Sales.Funnel.Period.All"] = "sinds start";
        nl["Sales.Dash.Todos"] = "Te doen";
        nl["Sales.Dash.TodosAll"] = "Alles";
        nl["Sales.Dash.TopEmployers"] = "Beste werkgevers";
        nl["Sales.Dash.TopEmployersSub"] = "Jouw commissie in {0}";
        nl["Sales.Dash.TopEmployersAll"] = "Alle {0}";
        nl["Sales.Dash.ShareCard"] = "Deel je link";
        nl["Sales.Dash.ShareCardSub"] = "QR laten scannen bij de klant";
        nl["Sales.Dash.Share"] = "Delen";
        nl["Sales.Todo.View"] = "Bekijken";
        nl["Sales.Todo.Download"] = "Download";
        nl["Sales.Todo.Profile"] = "Naar profiel";
        nl["Sales.Todo.NoPurchase"] = "{0} heeft nog niets gekocht";
        nl["Sales.Todo.NoPurchaseSub"] = "Aangemeld op {0} · tip: bel over de gratis start-highlight";
        nl["Sales.Todo.YearChange"] = "{0} gaat naar jaar {1}";
        nl["Sales.Todo.YearChangeSub"] = "Vanaf {0} krijg je {1} % in plaats van {2} %";
        nl["Sales.Todo.InvoicePaid"] = "Factuur {0} is betaald";
        nl["Sales.Todo.InvoicePaidSub"] = "{0}";
        nl["Sales.Todo.SelfBilling"] = "Geef toestemming voor self-billing";
        nl["Sales.Todo.SelfBillingSub"] = "Nodig om een uitbetaling aan te vragen";
        nl["Sales.Todo.Iban"] = "Vul je uitbetaalrekening in";
        nl["Sales.Todo.IbanSub"] = "Nodig om een uitbetaling aan te vragen";
        nl["Sales.Todo.Quiet"] = "{0} is stil sinds {1} dagen";
        nl["Sales.Todo.QuietSub"] = "Even bellen?";
        nl["Sales.Payout.BelowMinimum"] = "Je hebt nog niet genoeg beschikbaar saldo (minimaal {0}).";
        nl["Sales.Payout.NeedsIban"] = "Vul eerst je uitbetaalrekening in.";
        nl["Sales.Payout.NeedsConsent"] = "Geef eerst toestemming voor self-billing.";

        // —— Mijn werkgevers ——
        nl["Sales.Employers.Title"] = "Mijn werkgevers";
        nl["Sales.Employers.Lead"] = "{0} werkgevers via jouw link of code · {1} actief";
        nl["Sales.Employers.Kpi.Registered"] = "Aangemeld";
        nl["Sales.Employers.Kpi.RegisteredSub"] = "sinds start";
        nl["Sales.Employers.Kpi.FirstPurchase"] = "Eerste aankoop";
        nl["Sales.Employers.Kpi.FirstPurchaseSub"] = "{0} % van aanmeldingen";
        nl["Sales.Employers.Kpi.Quiet"] = "Stil";
        nl["Sales.Employers.Kpi.QuietSub"] = "even bellen?";
        nl["Sales.Employers.Kpi.Average"] = "Gem. per werkgever";
        nl["Sales.Employers.Search"] = "Zoek werkgever of plaats…";
        nl["Sales.Employers.Filter.Status"] = "Status";
        nl["Sales.Employers.Filter.Year"] = "Commissiejaar";
        nl["Sales.Employers.Filter.All"] = "Alle";
        nl["Sales.Employers.Filter.Ended"] = "Afgelopen";
        nl["Sales.Employers.Col.Name"] = "Werkgever";
        nl["Sales.Employers.Col.Attributed"] = "Aangemeld";
        nl["Sales.Employers.Col.Status"] = "Status";
        nl["Sales.Employers.Col.Year"] = "Commissiejaar";
        nl["Sales.Employers.Col.Commission"] = "Commissie {0}";
        nl["Sales.Employers.Col.LastPurchase"] = "Laatste aankoop";
        nl["Sales.Employers.Branches"] = "+{0} vestigingen";
        nl["Sales.Employers.Branch"] = "+{0} vestiging";
        nl["Sales.Employers.YearLabel"] = "Jaar {0} · {1} %";
        nl["Sales.Employers.Empty"] = "Nog geen werkgevers. Deel je link of geef je code aan een werkgever.";
        nl["Sales.Employers.EmptyCta"] = "Naar mijn link";
        nl["Sales.Employers.Drawer.Sub"] = "{0} · {1} · sinds {2}";
        nl["Sales.Employers.Drawer.SubNoPlace"] = "{0} · sinds {1}";
        nl["Sales.Employers.Drawer.Total"] = "Jouw commissie totaal";
        nl["Sales.Employers.Drawer.Purchases"] = "Aankopen";
        nl["Sales.Employers.Drawer.Now"] = "Nu";
        nl["Sales.Employers.Drawer.NowYear"] = "Jaar {0} · {1} %";
        nl["Sales.Employers.Drawer.NowEnded"] = "Afgelopen";
        nl["Sales.Employers.Drawer.Years"] = "Commissiejaren";
        nl["Sales.Employers.Drawer.YearSeg"] = "Jaar {0} · {1} % · t/m {2}";
        nl["Sales.Employers.Drawer.Timeline"] = "Zo ging het";
        nl["Sales.Employers.Drawer.Lines"] = "Commissie per aankoop";
        nl["Sales.Employers.Drawer.Purchase"] = "Aankoop (excl. btw)";
        nl["Sales.Employers.Drawer.YouGet"] = "Jij krijgt";
        nl["Sales.Employers.Drawer.ShowAll"] = "Toon alles";
        nl["Sales.Employers.Drawer.Privacy"] =
            "Je ziet bedrijfsnaam, plaats en je eigen commissie. Geen contactpersonen, kandidaten of vacatures.";
        nl["Sales.Employers.Drawer.Help"] = "Vraag Lobsy om hulp";
        nl["Sales.Employers.Drawer.HelpSubject"] = "Vraag over werkgever {0}";
        nl["Sales.Employers.LineSub"] = "{0} · aankoop {1}";
        nl["Sales.Timeline.StartHighlight"] = "Start-highlight ontvangen";
        nl["Sales.Timeline.FirstPurchase"] = "Eerste aankoop";
        nl["Sales.Timeline.Year2"] = "Jaar 2 begint";
        nl["Sales.Timeline.Year3"] = "Jaar 3 begint";
        nl["Sales.Timeline.Ends"] = "Commissie stopt";
        nl["Sales.Timeline.Attributed"] = "Aangemeld ({0})";
        nl["Sales.Search.Shortcut"] = "Ctrl K";
        nl["Sales.Search.NoResults"] = "Geen werkgevers gevonden";
        nl["Sales.TokensPack"] = "{0} tokens";

        // —— Admin / parking ——
        nl["SalesAdmin.AmbassadorsToggle"] = "Ambassadeursprogramma";
        nl["SalesAdmin.AmbassadorsToggleHelp"] =
            "Zet het ambassadeursprogramma aan of uit. Uit: geen pagina's, geen links, geen nieuwe commissie. Gegevens blijven bewaard.";
        nl["SalesAdmin.ParkedBalancesNote"] =
            "{0} geparkeerde ambassadeurs hebben nog € {1} tegoed. Het programma staat uit; betaal of verreken dit met de hand.";
        nl["SalesAdmin.SalesSection"] = "Sales";
        nl["SalesAdmin.Settings.HoldDays"] = "Wachttijd commissie (dagen)";
        nl["SalesAdmin.Settings.HoldDaysHelp"] =
            "Zo lang staat nieuwe commissie op 'In behandeling'. Dan kan een klant nog geld terugvragen.";
        nl["SalesAdmin.Settings.PayoutMinimum"] = "Minimum uitbetaling (€ excl. btw)";
        nl["SalesAdmin.Settings.PayoutMinimumHelp"] =
            "Vanaf dit beschikbare saldo mag een salesmanager een uitbetaling aanvragen.";
        nl["SalesAdmin.Settings.IbanHoldDays"] = "IBAN-wachtperiode (dagen)";
        nl["SalesAdmin.Settings.IbanHoldDaysHelp"] =
            "Na een IBAN-wijziging wachten uitbetalingen naar het nieuwe rekeningnummer zo lang.";
        nl["SalesAdmin.Settings.AttributionCookieDays"] = "Attributie-cookie (dagen)";
        nl["SalesAdmin.Settings.AttributionCookieDaysHelp"] =
            "Hoe lang een first-click partnerlink blijft gelden.";
        nl["SalesAdmin.Settings.PayoutSection"] = "Uitbetaling & hold";

        // —— Auth / parked sign-in ——
        nl["Sales.Ambassadors.ParkedLogin"] =
            "Het ambassadeursprogramma is gepauzeerd. Je gegevens en je tegoed blijven bewaard. Vragen? Mail support@lobsy.nl.";
    }
}
