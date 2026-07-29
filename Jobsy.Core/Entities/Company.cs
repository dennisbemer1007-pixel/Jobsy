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

    /// <summary>
    /// When true, the enterprise manager (bedrijfsmanager) manages tokens for this vestiging:
    /// purchases go into the organisation pot and the EM issues tokens to this branch.
    /// When false, the vestiging manages its own token purchases.
    /// </summary>
    public bool TokensManagedByEnterprise { get; set; }

    /// <summary>Salesmanager who referred this supplier (via tracking code).</summary>
    public Guid? ReferredBySalesManagerUserId { get; set; }
    public User? ReferredBySalesManagerUser { get; set; }

    /// <summary>Platform-wide founder slot 1–10 when eligible; null otherwise.</summary>
    public int? FirstYearSupplierSlot { get; set; }

    /// <summary>Start of the first-year partnership window (for token commission year 1/2).</summary>
    public DateTime? FirstYearStartedAt { get; set; }

    public ICollection<Vacancy> Vacancies { get; set; } = new List<Vacancy>();
    public ICollection<TokenTransaction> TokenTransactions { get; set; } = new List<TokenTransaction>();
    public ICollection<User> PrimaryUsers { get; set; } = new List<User>();
    public ICollection<UserCompany> UserMemberships { get; set; } = new List<UserCompany>();
    public ICollection<RegionCompany> RegionMemberships { get; set; } = new List<RegionCompany>();
    public ICollection<CompanySalaryTable> SalaryTables { get; set; } = new List<CompanySalaryTable>();
}
