using Jobsy.Core.Uwv;

namespace Jobsy.Web.UwvMockups;

public static class UwvAdviseurSectionCatalog
{
    public static IReadOnlyList<UwvAdviseurSectionOption> All { get; } =
    [
        new(UwvAdviseurSectionKeys.Career, "Carrièreroute (voorbeeld)", "Career path (sample)"),
        new(UwvAdviseurSectionKeys.Kansberoepen, "Kansberoepen en omscholingsideeën", "Opportunity occupations and retraining ideas"),
        new(UwvAdviseurSectionKeys.Drijfveren, "Drijfveren en motivatie", "Drivers and motivation"),
        new(UwvAdviseurSectionKeys.Competences, "Competenties (paspoort)", "Competencies (passport)"),
        new(UwvAdviseurSectionKeys.Banenkaart, "Banen op de kaart (selectie)", "Jobs on the map (selection)"),
    ];
}

public sealed record UwvAdviseurSectionOption(string Key, string LabelNl, string LabelEn);
