using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

public class PlatformLog
{
    public Guid Id { get; set; }
    public PlatformLogLevel Level { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? DetailsJson { get; set; }
    public DateTime CreatedAt { get; set; }
}
