Functioneel Testplan: Jobsy (MVP)

1\. Doel

Valideer de kritieke user-flows: Inloggen, Vacature plaatsen (incl. Token-check), en Vacature vindbaarheid (Kaart-interface).



2\. Test-scenario's (Automatisering met Playwright \& xUnit)

A. Backend \& Business Logic (xUnit)

TokenServiceTest: Test of een nieuwe geregistreerde ondernemer direct 1 token krijgt.



VacancyVisibilityTest: Test of een vacature met StartDate: 01-11 en EndDate: 31-01 correct verschijnt in de "Active" list op 1 januari.



AuthServiceTest: Test of MS Entra ID claims correct worden omgezet naar rollen (Kandidaat/Manager/Admin).



B. UI/UX Flow (Playwright - Browser simulatie)

RegistratieFlow: Ga naar login -> MS Entra redirect -> Terugkeer naar Dashboard -> Check: "1 Token beschikbaar" banner aanwezig.



VacaturePlaatsFlow:



Klik "Plaats vacature".



Vul formulier in.



Bevestig (check: Token-saldo wordt 0).



Check: Vacature verschijnt direct op de kaart-interface.



ZoekFlow: Gebruik de filter "Reistijd" -> Check: Alleen markers binnen opgegeven straal blijven zichtbaar op de OpenStreetMap kaart.

