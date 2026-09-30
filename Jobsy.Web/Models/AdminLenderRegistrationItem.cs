namespace Jobsy.Web.Models;

public sealed class AdminLenderRegistrationItem
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = "";
    public string KvkNumber { get; set; } = "";
    public string Status { get; set; } = "";
    public string? Source { get; set; }
    public string? Reference { get; set; }
    public DateTime? CheckedAtUtc { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string? Note { get; set; }
    public string? WaadiCheckUrl { get; set; }
}
