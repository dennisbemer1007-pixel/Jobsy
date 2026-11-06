using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

public class NotificationPreference
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public NotificationPreferenceSoort Soort { get; set; }

    public bool ViaPush { get; set; } = true;
    public bool ViaMail { get; set; } = true;

    /// <summary>Local time start of quiet hours (default 21:00).</summary>
    public TimeOnly NietStorenVan { get; set; } = new(21, 0);

    /// <summary>Local time end of quiet hours (default 8:00).</summary>
    public TimeOnly NietStorenTot { get; set; } = new(8, 0);

    public int MaxPerDag { get; set; } = 3;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
