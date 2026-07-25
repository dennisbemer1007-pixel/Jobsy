namespace Jobsy.Core.Entities;

public class VacancyClick
{
    public Guid Id { get; set; }
    public Guid VacancyId { get; set; }
    public Vacancy Vacancy { get; set; } = null!;
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string? AnonymousKey { get; set; }
    public DateTime CreatedAt { get; set; }
}
