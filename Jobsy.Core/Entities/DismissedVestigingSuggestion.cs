namespace Jobsy.Core.Entities;

/// <summary>
/// Employer chose "Niet nu" for a suggested KVK vestiging (D7 / 11.5). Hidden until <see cref="HiddenUntilUtc"/>.
/// </summary>
public class DismissedVestigingSuggestion
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    /// <summary>KVK vestigingsnummer or full {kvk}_{vestiging} id.</summary>
    public string KvkEstablishmentId { get; set; } = string.Empty;

    public DateTime HiddenUntilUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public Guid? DismissedByUserId { get; set; }
}
