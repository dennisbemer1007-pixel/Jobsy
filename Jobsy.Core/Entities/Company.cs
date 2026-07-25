using Jobsy.Core.Enums;
using Jobsy.Core.ValueObjects;

namespace Jobsy.Core.Entities;

public class Company
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string KvkNumber { get; set; } = string.Empty;

    /// <summary>
    /// Unique establishment key: {kvkNumber}_{vestigingsnummer}.
    /// </summary>
    public string? KvkEstablishmentId { get; set; }

    public string Address { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public GeoPoint Location { get; set; } = null!;
    public CompanyType Type { get; set; } = CompanyType.Employer;

    public Guid? ParentCompanyId { get; set; }
    public Company? ParentCompany { get; set; }
    public ICollection<Company> ChildCompanies { get; set; } = new List<Company>();

    /// <summary>
    /// True after the one-time welcome token was granted on successful registration activation.
    /// Balance itself lives in <see cref="TokenTransactions"/> (ledger sum).
    /// </summary>
    public bool HasReceivedWelcomeToken { get; set; }

    public ICollection<Vacancy> Vacancies { get; set; } = new List<Vacancy>();
    public ICollection<TokenTransaction> TokenTransactions { get; set; } = new List<TokenTransaction>();
    public ICollection<User> PrimaryUsers { get; set; } = new List<User>();
    public ICollection<UserCompany> UserMemberships { get; set; } = new List<UserCompany>();
    public ICollection<RegionCompany> RegionMemberships { get; set; } = new List<RegionCompany>();
    public ICollection<CompanySalaryTable> SalaryTables { get; set; } = new List<CompanySalaryTable>();
}
