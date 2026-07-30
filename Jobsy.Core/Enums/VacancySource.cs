namespace Jobsy.Core.Enums;

/// <summary>How a vacancy was created: manual UI, external API, or CSV batch import.</summary>
public enum VacancySource
{
    Manual = 0,
    Api = 1,
    Csv = 2
}
