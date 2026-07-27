namespace Jobsy.Core.Entities;

/// <summary>
/// Site visit for platform analytics (admin). Unique visitors use AnonymousKey or UserId.
/// </summary>
public class SiteVisit
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string? AnonymousKey { get; set; }
    public string? Path { get; set; }
    public DateTime CreatedAt { get; set; }
}
