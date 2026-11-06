using Jobsy.Core.Careers;

namespace Jobsy.Core.Rules;

public static class VacancyOccupationEsco
{
    public const string FieldKey = "occupationEscoId";

    public static string? FromCategoryFields(IReadOnlyDictionary<string, string>? fields)
        => fields is not null
           && fields.TryGetValue(FieldKey, out var raw)
           && !string.IsNullOrWhiteSpace(raw)
            ? raw.Trim()
            : null;

    public static string? Resolve(string? title, IReadOnlyDictionary<string, string>? categoryFields)
    {
        var fromField = FromCategoryFields(categoryFields);
        if (!string.IsNullOrWhiteSpace(fromField) && OccupationCatalog.Shared.Get(fromField) is not null)
        {
            return fromField;
        }

        return OccupationCatalog.Shared.Resolve(title)?.Id;
    }
}
