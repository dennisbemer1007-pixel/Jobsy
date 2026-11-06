namespace Jobsy.Core.Uwv;

public sealed record UwvKansberoepenDataset(
    string DatasetId,
    string RegionKeyNl,
    string RegionKeyEn,
    string SourceLineNl,
    string SourceLineEn,
    IReadOnlyList<UwvKansberoepEntry> Entries);

public sealed record UwvKansberoepEntry(
    string Id,
    string TitleNl,
    string TitleEn,
    string ProspectNl,
    string ProspectEn,
    string RouteNl,
    string RouteEn,
    string SectorIcon,
    bool Baangarantie,
    int VacancyCount);
