using Jobsy.Core.Ops;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Ops;

public sealed class SystemAdminSettings : ISystemAdminSettings
{
    private readonly IOptionsMonitor<SystemAdminOptions> _options;

    public SystemAdminSettings(IOptionsMonitor<SystemAdminOptions> options) => _options = options;

    public bool Enabled => _options.CurrentValue.Enabled;

    public bool RequireTwoFactor => _options.CurrentValue.RequireTwoFactor;

    public string? Email => _options.CurrentValue.Email?.Trim();

    public string? NotifyEmail => _options.CurrentValue.NotifyEmail?.Trim();
}
