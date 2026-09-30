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

        // —— Mijn link & materiaal ——
        nl["Sales.Link.Title"] = "Mijn link & materiaal";
        nl["Sales.Link.Lead"] =
            "Deel de link. Meldt een werkgever zich aan? Dan hoort hij bij jou en krijg je commissie op zijn aankopen.";
        nl["Sales.Link.MobileTitle"] = "Mijn link";
        nl["Sales.Link.MobileLead"] = "Laat de klant scannen of stuur je link";
        nl["Sales.Link.PersonalTitle"] = "Jouw persoonlijke link";
        nl["Sales.Link.Copy"] = "Kopieer";
        nl["Sales.Link.Copied"] = "Gekopieerd";
        nl["Sales.Link.OrCode"] = "Of geef je code:";
        nl["Sales.Link.Active"] = "Actief";
        nl["Sales.Link.WhatsApp"] = "WhatsApp";
        nl["Sales.Link.Mail"] = "Mail";
        nl["Sales.Link.QrDownload"] = "QR downloaden";
        nl["Sales.Link.Preview"] = "Bekijk wat de werkgever ziet";
        nl["Sales.Link.QrCaption"] = "Scan voor {0}";
        nl["Sales.Link.Info"] =
            "Zo tellen we een aanmelding voor jou. Klikt iemand op je link? Dan onthouden we dat {0} dagen. Of de werkgever vult je code in bij het aanmelden.";
        nl["Sales.Link.EmployerGift"] =
            "Voor de werkgever: de eerste vacature wordt gratis uitgelicht";
        nl["Sales.Link.Materials"] = "Materiaal";
        nl["Sales.Link.MaterialsLead"] = "Alles staat al klaar met jouw code";
        nl["Sales.Link.MaterialsAll"] = "Alles";
        nl["Sales.Link.TypePdf"] = "PDF";
        nl["Sales.Link.TypeText"] = "Tekst";
        nl["Sales.Link.Download"] = "Download";
        nl["Sales.Link.Share"] = "Delen";
        nl["Sales.Link.Mat.Flyer"] = "Flyer A4 met QR";
        nl["Sales.Link.Mat.FlyerHelp"] = "Persoonlijke flyer met jouw code, QR en actuele prijzen.";
        nl["Sales.Link.Mat.Cards"] = "Visitekaartje met QR";
        nl["Sales.Link.Mat.CardsHelp"] = "Voor op tafel of in je tas. 10 per A4.";
        nl["Sales.Link.Mat.Pres"] = "Presentatie voor een klant";
        nl["Sales.Link.Mat.PresHelp"] = "7 pagina's: wat Lobsy doet, wat het kost, zo start u.";
        nl["Sales.Link.Mat.Email"] = "E-mail om te sturen";
        nl["Sales.Link.Mat.EmailHelp"] = "Korte tekst met jouw link erin. Klaar om te plakken.";
        nl["Sales.Link.Mat.Price"] = "Prijskaart";
        nl["Sales.Link.Mat.PriceHelp"] = "Tokenpakketten en wat een vacature kost.";
        nl["Sales.Link.Mat.WhatsApp"] = "WhatsApp-bericht";
        nl["Sales.Link.Mat.WhatsAppHelp"] = "Klaar om te sturen. Jouw link staat erin.";
        nl["Sales.Link.Mat.PitchMobile"] = "Pitch in 60 seconden";
        nl["Sales.Link.Mat.PitchMobileHelp"] = "4 zinnen om te oefenen";
        nl["Sales.Link.PitchTitle"] = "Pitch in 60 seconden";
        nl["Sales.Pitch.Step1Title"] = "Het probleem";
        nl["Sales.Pitch.Step1Body"] = "Lange vacatureteksten die niemand leest.";
        nl["Sales.Pitch.Step2Title"] = "Wat Lobsy doet";
        nl["Sales.Pitch.Step2Body"] = "Toont je vacature op de banenkaart, dichtbij huis.";
        nl["Sales.Pitch.Step3Title"] = "Wat het kost";
        nl["Sales.Pitch.Step4Title"] = "Zo start u";
        nl["Sales.Pitch.Step4Body"] = "Aanmelden via mijn link; eerste vacature gratis highlight.";
        nl["Sales.Pitch.Tip"] = "Tip: laat de banenkaart even zien op je telefoon.";
        nl["Sales.Link.EmployerGets"] = "Wat krijgt de werkgever?";
        nl["Sales.Link.EmployerGets.Highlight"] =
            "Gratis start-highlight op de eerste vacature ({0} tokens)";
        nl["Sales.Link.EmployerGets.Contact"] = "Eén vast aanspreekpunt: jij";
        nl["Sales.Link.EmployerGets.NoSub"] = "Geen abonnement";
        nl["Sales.Link.Commission"] = "Jouw commissie";
        nl["Sales.Link.CommissionYear"] = "Jaar {0}";
        nl["Sales.Link.CommissionSub"] =
            "Over elke tokenaankoop van jouw werkgevers, excl. btw. Jaar 1 start bij de eerste aankoop.";
        nl["Sales.Link.Onboarding"] = "Rond eerst je onboarding af om een trackingcode te ontvangen.";
        nl["Sales.Link.OnboardingCta"] = "Start onboarding";
        nl["Sales.Link.NeedsCode"] = "Nog geen code. Rond onboarding af.";

        nl["SalesAdmin.Settings.BaseTokenHint"] =
            "Wordt niet meer getoond aan werkgevers of salesmanagers; prijzen komen uit de tokenpakketten.";

        // —— Profiel & gegevens ——
        nl["Sales.Profile.Title"] = "Profiel & gegevens";
        nl["Sales.Profile.Lead"] = "Deze gegevens staan op je facturen. Houd ze goed bij.";
        nl["Sales.Profile.Company"] = "Bedrijf";
        nl["Sales.Profile.CompanyHelp"] = "Staat op elke factuur die Lobsy voor je maakt.";
        nl["Sales.Profile.CompanyName"] = "Bedrijfsnaam";
        nl["Sales.Profile.Kvk"] = "KvK-nummer";
        nl["Sales.Profile.KvkHelp"] = "Lobsy werkt samen met ondernemers. Je hebt een KvK-nummer nodig.";
        nl["Sales.Profile.Country"] = "Land";
        nl["Sales.Profile.Address"] = "Adres";
        nl["Sales.Profile.PostalCode"] = "Postcode";
        nl["Sales.Profile.City"] = "Plaats";
        nl["Sales.Profile.Save"] = "Opslaan";
        nl["Sales.Profile.Saved"] = "Opgeslagen.";
        nl["Sales.Profile.Vat"] = "Btw";
        nl["Sales.Profile.VatHelp"] = "Bepaalt of er btw op je factuur komt.";
        nl["Sales.Profile.VatStandard"] = "Ik ben btw-plichtig";
        nl["Sales.Profile.VatStandardHelp"] = "21 % btw op de factuur.";
        nl["Sales.Profile.VatNumber"] = "Btw-nummer";
        nl["Sales.Profile.VatKor"] = "Ik gebruik de KOR";
        nl["Sales.Profile.VatKorHelp"] = "Kleineondernemersregeling: geen btw op de factuur.";
        nl["Sales.Profile.VatKorConfirm"] =
            "Ik ben aangemeld voor de KOR bij de Belastingdienst.";
        nl["Sales.Profile.Payout"] = "Uitbetaalrekening";
        nl["Sales.Profile.PayoutHelp"] = "Hier maakt Lobsy je geld naartoe over.";
        nl["Sales.Profile.Iban"] = "IBAN";
        nl["Sales.Profile.Holder"] = "Op naam van";
        nl["Sales.Profile.Change"] = "Wijzigen";
        nl["Sales.Profile.IbanSafeTitle"] = "Veilig wijzigen";
        nl["Sales.Profile.IbanSafeBody"] =
            "Voor een nieuwe IBAN vragen we je 2FA-code. Je krijgt een mail. De eerste uitbetaling naar een nieuwe rekening wacht {0} dagen.";
        nl["Sales.Profile.IbanHold"] = "Uitbetalingen naar deze rekening kunnen vanaf {0}.";
        nl["Sales.Profile.IbanDrawerTitle"] = "Uitbetaalrekening wijzigen";
        nl["Sales.Profile.IbanConfirmTitle"] = "Bevestig met je authenticator";
        nl["Sales.Profile.IbanConfirmCode"] = "Authenticatorcode";
        nl["Sales.Profile.IbanConfirm"] = "Bevestigen";
        nl["Sales.Profile.IbanEmailSent"] =
            "We hebben een bevestigingslink naar je e-mail gestuurd. Open die link om de wijziging af te ronden.";
        nl["Sales.Profile.Agreements"] = "Afspraken";
        nl["Sales.Profile.AgreementsHelp"] = "Wat je met Lobsy hebt afgesproken.";
        nl["Sales.Profile.Agreement"] = "Samenwerking";
        nl["Sales.Profile.AgreementValue"] = "Bemiddelingsovereenkomst · versie {0}";
        nl["Sales.Profile.AgreementMissing"] = "Nog niet ondertekend";
        nl["Sales.Profile.Pdf"] = "PDF";
        nl["Sales.Profile.SelfBilling"] = "Self-billing";
        nl["Sales.Profile.SelfBillingOk"] = "Akkoord op {0}: Lobsy maakt je facturen";
        nl["Sales.Profile.SelfBillingGive"] = "Toestemming geven";
        nl["Sales.Profile.SelfBillingRevoke"] = "Intrekken";
        nl["Sales.Profile.SelfBillingRevokeConfirm"] =
            "Weet je zeker dat je self-billing wilt intrekken? Uitbetalingen vragen dan een handmatige factuur.";
        nl["Sales.Profile.Commission"] = "Commissie";
        nl["Sales.Profile.CommissionValue"] = "25 % · 10 % · 5 % over 3 jaar per werkgever";
        nl["Sales.Profile.CommissionReferred"] = "20 % · 10 % · 5 % over 3 jaar (aangewezen salesmanager)";
        nl["Sales.Profile.Security"] = "Beveiliging";
        nl["Sales.Profile.SecurityHelp"] =
            "Je ziet geld en een rekeningnummer. Daarom is 2FA verplicht.";
        nl["Sales.Profile.Mfa"] = "Tweestapsverificatie";
        nl["Sales.Profile.MfaOn"] = "Aan · authenticator-app";
        nl["Sales.Profile.MfaOff"] = "Uit";
        nl["Sales.Profile.MfaManage"] = "Beheren";
        nl["Sales.Profile.MailPrefs"] = "Meldingen per mail";
        nl["Sales.Profile.MailNewEmployer"] = "Nieuwe werkgever";
        nl["Sales.Profile.MailCommission"] = "Commissie beschikbaar";
        nl["Sales.Profile.MailPayout"] = "Uitbetaling";
        nl["Sales.SelfBilling.ConsentText"] =
            "Lobsy maakt namens jou de facturen voor je commissie (self-billing / \"factuur uitgereikt door afnemer\"). Je gaat akkoord met deze manier van factureren. Je controleert elke factuur en laat Lobsy binnen 14 dagen weten als er iets niet klopt. Je laat Lobsy weten wanneer je btw-situatie verandert. Je kunt deze toestemming op elk moment intrekken; daarna is een handmatige factuur via Lobsy-support nodig voor uitbetalingen.";
        nl["SalesMail.IbanChanged"] = "Je uitbetaalrekening is gewijzigd";
        nl["SalesMail.IbanConfirm"] = "Bevestig je nieuwe uitbetaalrekening";

        // —— Onboarding ——
        nl["Sales.Start.Title"] = "Starten";
        nl["Sales.Start.Lead"] = "Drie stappen en je hebt je persoonlijke code.";
        nl["Sales.Start.Step1"] = "Gegevens";
        nl["Sales.Start.Step2"] = "Uitbetaalrekening";
        nl["Sales.Start.Step3"] = "Afspraken";
        nl["Sales.Start.StepDone"] = "Klaar";
        nl["Sales.Start.Next"] = "Volgende";
        nl["Sales.Start.Back"] = "Terug";
        nl["Sales.Start.Finish"] = "Afronden";
        nl["Sales.Start.AgreementCheck"] = "Ik ga akkoord met de samenwerking";
        nl["Sales.Start.ConsentCheck"] = "Ik geef toestemming voor self-billing";
        nl["Sales.Start.AgreementText"] =
            "Bemiddelingsovereenkomst Lobsy Partner. Commissie 25 % · 10 % · 5 % over 3 jaar per werkgever (of 20 % in jaar 1 bij aanbeveling). Jaar 1 start bij de eerste aankoop.";
        nl["Sales.Start.DoneTitle"] = "Je code is {0}";
        nl["Sales.Start.DoneLead"] = "Deel je link met werkgevers.";
        nl["Sales.Start.ToLink"] = "Naar mijn link";
        nl["Sales.Start.ToDash"] = "Naar dashboard";

        // —— Hulp ——
        nl["Sales.Help.Title"] = "Hulp & afspraken";
        nl["Sales.Help.Lead"] = "Antwoorden op de meest gestelde vragen.";
        nl["Sales.Help.Faq.Attribution"] = "Hoe tel je een aanmelding voor mij?";
        nl["Sales.Help.Faq.AttributionBody"] =
            "Klikt iemand op je link? Dan onthouden we dat 30 dagen (first-click). Of de werkgever vult je code in bij het aanmelden — dat wint van de cookie.";
        nl["Sales.Help.Faq.Available"] = "Wanneer is mijn commissie beschikbaar?";
        nl["Sales.Help.Faq.AvailableBody"] =
            "Nieuwe commissie staat 14 dagen 'In behandeling' (terugbetalingsvenster). Daarna wordt die beschikbaar voor uitbetaling.";
        nl["Sales.Help.Faq.Payout"] = "Hoe en wanneer krijg ik geld?";
        nl["Sales.Help.Faq.PayoutBody"] =
            "Vanaf € 50 beschikbaar vraag je een uitbetaling aan. Lobsy keurt één maandelijkse ronde goed en betaalt per bankoverschrijving.";
        nl["Sales.Help.Faq.SelfBilling"] = "Wat is self-billing?";
        nl["Sales.Help.Faq.SelfBillingBody"] =
            "Lobsy maakt de factuur voor jouw commissie namens jou (\"factuur uitgereikt door afnemer\"). Je geeft daar apart toestemming voor.";
        nl["Sales.Help.Faq.Kor"] = "Ik gebruik de KOR, wat nu?";
        nl["Sales.Help.Faq.KorBody"] =
            "Kies KOR in Profiel & gegevens. Op je factuur komt dan geen btw; de tekst vermeldt de kleineondernemersregeling.";
        nl["Sales.Help.Faq.Privacy"] = "Wat zie ik van werkgevers (en wat niet)?";
        nl["Sales.Help.Faq.PrivacyBody"] =
            "Je ziet bedrijfsnaam, plaats (niet bij eenmanszaak) en je eigen commissie. Geen contactpersonen, KvK, adres, vacatures of kandidaten.";
        nl["Sales.Help.Faq.Refund"] = "Een klant vraagt geld terug, wat gebeurt er?";
        nl["Sales.Help.Faq.RefundBody"] =
            "Bij een refund of chargeback boeken we een negatieve correctie op je commissie, zodat je saldo klopt.";
        nl["Sales.Help.Faq.Iban"] = "Mijn IBAN wijzigen";
        nl["Sales.Help.Faq.IbanBody"] =
            "Wijzig je IBAN via Profiel & gegevens. We vragen je 2FA-code, sturen een mail, en de eerste uitbetaling naar de nieuwe rekening wacht 3 dagen.";
        nl["Sales.Help.Downloads"] = "Downloads";
        nl["Sales.Help.Contact"] = "Contact";
        nl["Sales.Help.ContactBody"] = "Vragen? Mail support@lobsy.nl of gebruik de feedbackknop.";
        nl["Sales.Help.IbanConfirm.Title"] = "IBAN bevestigen";
        nl["Sales.Help.IbanConfirm.Ok"] = "Je uitbetaalrekening is gewijzigd.";
        nl["Sales.Help.IbanConfirm.Fail"] = "Deze link is ongeldig of verlopen.";

        // —— Wallet & uitbetalingen ——
        nl["Sales.Wallet.Title"] = "Wallet & uitbetalingen";
        nl["Sales.Wallet.Lead"] =
            "Je commissie, je uitbetalingen en je facturen. Lobsy maakt de factuur voor je (self-billing).";
        nl["Sales.Wallet.MobileTitle"] = "Wallet";
        nl["Sales.Wallet.MobileLead"] = "Bedragen excl. btw";
        nl["Sales.Wallet.YearOverview"] = "Jaaroverzicht {0} (PDF)";
        nl["Sales.Wallet.Request"] = "Uitbetaling aanvragen";
        nl["Sales.Wallet.RequestLead"] = "Lobsy maakt de factuur namens jou (self-billing).";
        nl["Sales.Wallet.RequestCta"] = "Aanvragen · {0}";
        nl["Sales.Wallet.CancelRequest"] = "Aanvraag annuleren";
        nl["Sales.Wallet.AvailableVat"] = "excl. btw · + {0} btw";
        nl["Sales.Wallet.AvailableKor"] = "excl. btw · geen btw (KOR)";
        nl["Sales.Wallet.PendingSub"] = "vrij na {0} dagen (bedenktijd)";
        nl["Sales.Wallet.NoOpenRequest"] = "geen open aanvraag";
        nl["Sales.Wallet.OpenRequest"] = "open aanvraag";
        nl["Sales.Wallet.PaidYear"] = "Uitbetaald in {0}";
        nl["Sales.Wallet.InvoiceCount"] = "{0} facturen";
        nl["Sales.Wallet.Tab.Mutaties"] = "Mutaties";
        nl["Sales.Wallet.Tab.Payouts"] = "Uitbetalingen";
        nl["Sales.Wallet.Tab.Invoices"] = "Facturen";
        nl["Sales.Wallet.Filter.Period"] = "Periode";
        nl["Sales.Wallet.Filter.Kind"] = "Soort";
        nl["Sales.Wallet.Filter.State"] = "Status";
        nl["Sales.Wallet.Col.Date"] = "Datum";
        nl["Sales.Wallet.Col.Description"] = "Omschrijving";
        nl["Sales.Wallet.Col.Status"] = "Status";
        nl["Sales.Wallet.Col.Amount"] = "Bedrag excl. btw";
        nl["Sales.Wallet.Col.Invoice"] = "Factuurnummer";
        nl["Sales.Wallet.Col.Total"] = "Totaal incl. btw";
        nl["Sales.Wallet.FreeOn"] = "vrij op {0}";
        nl["Sales.Wallet.EmptyEntries"] = "Nog geen mutaties.";
        nl["Sales.Wallet.EmptyPayouts"] = "Nog geen uitbetalingen.";
        nl["Sales.Wallet.EmptyInvoices"] = "Nog geen facturen.";
        nl["Sales.Wallet.LegacyPayout"] = "Eerdere uitbetaling";
        nl["Sales.Wallet.HowTitle"] = "Zo werkt uitbetalen";
        nl["Sales.Wallet.How1Title"] = "Commissie komt binnen";
        nl["Sales.Wallet.How1Body"] = "{0} dagen in behandeling (bedenktijd).";
        nl["Sales.Wallet.How2Title"] = "Jij vraagt uitbetaling aan";
        nl["Sales.Wallet.How2Body"] = "Vanaf {0}. Lobsy maakt de self-billing factuur.";
        nl["Sales.Wallet.How3Title"] = "Lobsy keurt goed";
        nl["Sales.Wallet.How3Body"] = "Op de 1e werkdag van de maand.";
        nl["Sales.Wallet.How4Title"] = "Geld op je rekening";
        nl["Sales.Wallet.How4Body"] = "Binnen 3 werkdagen op rekening eindigend op {0}.";
        nl["Sales.Wallet.HowStrip"] = "Zo werkt het";
        nl["Sales.Wallet.HowStripSub"] = "Vanaf {0} · uitbetaling op de 1e werkdag · naar NL•• {1}";
        nl["Sales.Wallet.LatestInvoices"] = "Laatste facturen";
        nl["Sales.Wallet.AllInvoices"] = "Alle facturen";
        nl["Sales.Wallet.AmountExVat"] = "Bedrag excl. btw";
        nl["Sales.Wallet.AmountHint"] = "Beschikbaar {0} · minimaal {1}";
        nl["Sales.Wallet.CommissionEx"] = "Commissie excl. btw";
        nl["Sales.Wallet.Vat21"] = "Btw 21 %";
        nl["Sales.Wallet.NoVatKor"] = "Geen btw (KOR)";
        nl["Sales.Wallet.YouReceive"] = "Jij ontvangt";
        nl["Sales.Wallet.ToAccount"] = "Naar rekening";
        nl["Sales.Wallet.PayoutWhen"] = "Uitbetaling";
        nl["Sales.Wallet.PayoutTiming"] = "{0} · daarna binnen 3 werkdagen";
        nl["Sales.Wallet.PayoutAfterHold"] = "na {0}";
        nl["Sales.Wallet.InvoicePreview"] = "Voorbeeld factuur";
        nl["Sales.Wallet.InvoiceTo"] = "Aan";
        nl["Sales.Wallet.InvoiceNumberPending"] = "Factuurnummer wordt toegekend bij goedkeuring";
        nl["Sales.Wallet.InvoiceLines"] = "{0} commissieregels";
        nl["Sales.Wallet.ConsentRef"] = "Self-billing volgens afspraak van {0} (versie {1})";
        nl["Sales.Wallet.VatNote"] =
            "Klopt je btw niet? Gebruik je bijvoorbeeld de kleineondernemersregeling (KOR)? Pas dit eerst aan bij Profiel & gegevens.";
        nl["Sales.Wallet.BlockerLink"] = "Naar profiel";
        nl["Sales.Wallet.Step.Requested"] = "Aangevraagd";
        nl["Sales.Wallet.Step.InRun"] = "In ronde";
        nl["Sales.Wallet.Step.Approved"] = "Goedgekeurd";
        nl["Sales.Wallet.Step.Paid"] = "Betaald";
        nl["Sales.Payout.Block.BelowMinimum"] = "Je kunt uitbetalen vanaf {0}. Nu beschikbaar: {1}.";
        nl["Sales.Payout.Block.NoConsent"] = "Geef eerst toestemming voor self-billing.";
        nl["Sales.Payout.Block.IncompleteProfile"] = "Rond je uitbetaalgegevens af (IBAN, bedrijf, btw).";
        nl["Sales.Payout.Block.OpenRequest"] = "Je hebt al een openstaande aanvraag.";
        nl["Sales.Payout.Block.Mfa"] = "Bevestig je sessie met 2FA om uit te betalen.";
        nl["SalesMail.PayoutRequested"] = "We hebben je uitbetalingsaanvraag";
        nl["SalesMail.PayoutApproved"] = "Je uitbetaling is goedgekeurd";
        nl["SalesMail.PayoutRejected"] = "Je uitbetalingsaanvraag is afgewezen";
        nl["SalesMail.PayoutPaid"] = "Je uitbetaling is betaald";
        nl["SalesMail.PayoutDeferredIbanHold"] = "Je uitbetaling wacht op je nieuwe rekening";
        nl["SalesMail.PayoutDeferredMissingConsent"] = "Je uitbetalingsaanvraag is uitgesteld";
        nl["SalesPdf.InvoiceTitle"] = "Factuur";
        nl["SalesPdf.IssuedByCustomer"] = "Factuur uitgereikt door afnemer";
        nl["SalesPdf.SelfBillingAccordingTo"] = "Self-billing volgens de afspraak van {0} (versie {1})";
        nl["SalesPdf.Vat21"] = "Btw 21 %";
        nl["SalesPdf.VatKor"] = "Btw-vrijgesteld op grond van de kleineondernemersregeling (KOR)";
        nl["SalesPdf.PaidToAccount"] = "Betaald aan rekening {0}";
        nl["SalesPdf.JaaroverzichtTitle"] = "Jaaroverzicht commissie";

        // —— Admin uitbetaalrondes (08) ——
        nl["SalesAdmin.Runs.Title"] = "Uitbetaalrondes";
        nl["SalesAdmin.Runs.Lead"] = "Maandelijkse ronde: goedkeuren, SEPA exporteren, markeren als betaald.";
        nl["SalesAdmin.Runs.CreateExtra"] = "Extra ronde maken";
        nl["SalesAdmin.Runs.CreateExtraConfirm"] = "Extra uitbetaalronde maken met alle openstaande aanvragen?";
        nl["SalesAdmin.Runs.Created"] = "Extra ronde voor {0} aangemaakt.";
        nl["SalesAdmin.Runs.Empty"] = "Nog geen uitbetaalrondes.";
        nl["SalesAdmin.Runs.Col.Date"] = "Datum";
        nl["SalesAdmin.Runs.Col.Kind"] = "Soort";
        nl["SalesAdmin.Runs.Col.Status"] = "Status";
        nl["SalesAdmin.Runs.Col.Lines"] = "Regels";
        nl["SalesAdmin.Runs.Col.Total"] = "Totaal incl. btw";
        nl["SalesAdmin.Runs.Col.ApprovedBy"] = "Goedgekeurd door";
        nl["SalesAdmin.Runs.Kind.Monthly"] = "Maandelijks";
        nl["SalesAdmin.Runs.Kind.Extra"] = "Extra";
        nl["SalesAdmin.Runs.DrawerTitle"] = "Uitbetaalronde";
        nl["SalesAdmin.Runs.DrawerTitleDated"] = "Ronde {0}";
        nl["SalesAdmin.Runs.NoLines"] = "Geen regels in deze ronde.";
        nl["SalesAdmin.Runs.Line.Name"] = "Begunstigde";
        nl["SalesAdmin.Runs.Line.Amount"] = "Bedrag incl. btw";
        nl["SalesAdmin.Runs.Line.Iban"] = "IBAN";
        nl["SalesAdmin.Runs.Line.Flags"] = "Signalen";
        nl["SalesAdmin.Runs.Line.Status"] = "Status";
        nl["SalesAdmin.Runs.Reject"] = "Afwijzen";
        nl["SalesAdmin.Runs.RejectTitle"] = "Regel afwijzen";
        nl["SalesAdmin.Runs.RejectReason"] = "Reden (5–500 tekens)";
        nl["SalesAdmin.Runs.Approve"] = "Ronde goedkeuren";
        nl["SalesAdmin.Runs.ApproveConfirm"] = "Ronde goedkeuren? Lobsy maakt daarna de self-billing facturen.";
        nl["SalesAdmin.Runs.ExportSepa"] = "SEPA-bestand downloaden";
        nl["SalesAdmin.Runs.ExportCsv"] = "CSV";
        nl["SalesAdmin.Runs.MarkPaid"] = "Markeer als betaald";
        nl["SalesAdmin.Runs.MarkPaidConfirm"] = "Markeer {0} als betaald?";
        nl["SalesAdmin.Runs.Next.Approve"] = "Na goedkeuring maakt Lobsy de facturen. Daarna download je het SEPA-bestand voor je bank.";
        nl["SalesAdmin.Runs.Next.Export"] = "Download het SEPA-bestand (of de CSV) en upload het bij je bank.";
        nl["SalesAdmin.Runs.Next.MarkPaid"] = "Zodra de bank heeft overgemaakt: markeer als betaald.";
        nl["SalesAdmin.Runs.Next.Closing"] = "Nog openstaande regels afronden of de ronde sluit automatisch.";
        nl["SalesAdmin.Runs.Next.Done"] = "Deze ronde is afgerond.";
        nl["SalesAdmin.Runs.CrossLinkUsers"] = "Salesmanagers beheer je bij Gebruikers & rollen › Salesmanagers.";
        nl["SalesAdmin.Runs.CrossLinkFinance"] = "Uitbetalingen staan bij Financiën › Uitbetalingen & btw (tab Rondes na admin-redesign 06.4).";
        nl["SalesAdmin.Parked.Title"] = "Geparkeerde ambassadeurs met saldo";
        nl["SalesAdmin.Parked.Intro"] = "Het ambassadeursprogramma staat uit. Deze tegoeden worden niet automatisch uitbetaald.";
        nl["SalesAdmin.Parked.Col.Name"] = "Naam";
        nl["SalesAdmin.Parked.Col.Balance"] = "Openstaand saldo excl. btw";
        nl["SalesAdmin.Parked.Col.Last"] = "Laatste regel";
        nl["SalesAdmin.Correction.Open"] = "Correctie boeken";
        nl["SalesAdmin.Correction.Title"] = "Correctie boeken";
        nl["SalesAdmin.Correction.Lead"] = "Boek een handmatige correctie (±) op het grootboek. MFA-sessie vereist.";
        nl["SalesAdmin.Correction.Amount"] = "Bedrag excl. btw (±)";
        nl["SalesAdmin.Correction.Reason"] = "Reden (verplicht)";
        nl["SalesAdmin.Correction.Save"] = "Boeken";
        nl["SalesAdmin.Correction.NoBeneficiary"] = "Geen begunstigde geselecteerd.";
        nl["SalesAdmin.Attribution.Title"] = "Toewijzing wijzigen";
        nl["SalesAdmin.Attribution.Lead"] = "Wijs een organisatie opnieuw toe aan een salesmanager (of geen). Alleen toekomstige aankopen.";
        nl["SalesAdmin.Attribution.CompanyId"] = "Organisatie (company id)";
        nl["SalesAdmin.Attribution.ToUserId"] = "Nieuwe salesmanager (user id)";
        nl["SalesAdmin.Attribution.ToUserIdNone"] = "Leeg = geen toewijzing";
        nl["SalesAdmin.Attribution.Reason"] = "Reden (5–500 tekens)";
        nl["SalesAdmin.Attribution.History"] = "Geschiedenis laden";
        nl["SalesAdmin.Attribution.HistoryTitle"] = "Geschiedenis";
        nl["SalesAdmin.Attribution.Save"] = "Toewijzing wijzigen";
        nl["SalesAdmin.Attribution.Saved"] = "Toewijzing opgeslagen.";
        nl["SalesAdmin.Attribution.None"] = "Geen";
        nl["SalesAdmin.Attribution.InvalidCompany"] = "Ongeldige company id.";
        nl["SalesAdmin.Attribution.InvalidUser"] = "Ongeldige user id.";
    }
}
