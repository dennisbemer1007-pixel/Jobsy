using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

public class VacancyShare
{
    public Guid Id { get; set; }
    public Guid VacancyId { get; set; }
    public Vacancy Vacancy { get; set; } = null!;
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public ShareChannel Channel { get; set; }
    public DateTime CreatedAt { get; set; }
}
