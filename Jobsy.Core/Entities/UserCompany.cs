namespace Jobsy.Core.Entities;

/// <summary>
/// Many-to-many link: user ↔ company (for multi-site managers and intermediaries).
/// </summary>
public class UserCompany
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
}
