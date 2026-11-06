using Jobsy.Core.Ops;

namespace Jobsy.Tests.TestSupport;

public sealed class StubSystemAdminSettings : ISystemAdminSettings
{
    public bool Enabled { get; init; }

    public bool RequireTwoFactor { get; init; }

    public string? Email { get; init; }

    public string? NotifyEmail { get; init; }
}
