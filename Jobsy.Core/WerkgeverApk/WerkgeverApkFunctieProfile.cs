using Jobsy.Core.Careers;

namespace Jobsy.Core.WerkgeverApk;

/// <summary>B1 competence labels for functie comparison (rule-based, not ESCO skill codes in UI).</summary>
public sealed record WerkgeverApkFunctieProfilePoint(
    string Label,
    string? CompetenceDimension,
    string? TrainingHint);

public sealed record WerkgeverApkFunctieProfile(
    string EscoOccupationId,
    string DisplayTitle,
    IReadOnlyList<WerkgeverApkFunctieProfilePoint> Points);

public static class WerkgeverApkFunctieProfileCatalog
{
  private static readonly IReadOnlyList<WerkgeverApkFunctieProfilePoint> DefaultPoints =
  [
      new("Nauwkeurig en netjes werken", "Resultaatgerichtheid", "Richting: werken volgens afspraken en kwaliteit op de werkvloer."),
      new("Samenwerken met collega's", "Samenwerken", "Richting: samenwerking en afstemmen in het team."),
      new("Kalm blijven bij drukte", "Stressbestendigheid", "Richting: omgaan met druk en piekmomenten."),
      new("Veilig tillen en werken", "Plannen & organiseren", "Richting: veilig en zorgvuldig werken volgens afspraken."),
      new("Tempo houden", "Plannen & organiseren", "Richting: ritme en planning op de werkvloer."),
      new("Flexibel schakelen bij wijzigingen", "Creativiteit / vernieuwen (Innovatie)",
          "Richting: training omgaan met last-minute wijzigingen op de werkvloer."),
      new("Werken met scanner en systeem", "Leren & ontwikkelen",
          "Richting: korte interne training magazijnsysteem en scanner."),
      new("Overzicht houden bij meerdere orders", "Plannen & organiseren",
          "Richting: workshop plannen en prioriteiten in het magazijn."),
      new("Problemen zelf oplossen", "Klantgerichtheid", "Richting: zelfstandig knelpunten signaleren en oplossen."),
      new("Duidelijk doorgeven aan collega's", "Communiceren", "Richting: helder overleg en overdracht in het team.")
  ];

  public static WerkgeverApkFunctieProfile Resolve(string? escoOccupationId)
  {
      if (string.IsNullOrWhiteSpace(escoOccupationId))
      {
          return new WerkgeverApkFunctieProfile("", "", []);
      }

      var id = escoOccupationId.Trim();
      var occ = OccupationCatalog.Shared.Get(id);
      var title = occ?.Nl ?? "jouw functie";
      if (title.Length > 0)
      {
          title = char.ToUpper(title[0]) + title[1..];
      }

      return new WerkgeverApkFunctieProfile(id, title, DefaultPoints);
  }

  public static string DisplayName(string? escoOccupationId)
  {
      if (string.IsNullOrWhiteSpace(escoOccupationId))
      {
          return "";
      }

      var occ = OccupationCatalog.Shared.Get(escoOccupationId.Trim());
      var nl = occ?.Nl ?? "";
      return nl.Length == 0 ? nl : char.ToUpper(nl[0]) + nl[1..];
  }
}