namespace Jobsy.Core.Ops;

public interface ISystemAdminSettings
{
    bool Enabled { get; }

    bool RequireTwoFactor { get; }

    string? Email { get; }

    string? NotifyEmail { get; }
}
