using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>Base placement token cost per <see cref="VacancyKind"/>.</summary>
public class VacancyTypeTokenCost
{
    public Guid Id { get; set; }
    public VacancyKind Kind { get; set; }
    public decimal CostTokens { get; set; }
    public bool IsActive { get; set; } = true;
}
