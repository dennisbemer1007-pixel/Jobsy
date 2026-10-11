using Jobsy.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests.TestSupport;

internal static class SystemAdminTestDoubles
{
    public static SystemAdminLoginNotifier LoginNotifier(StubSystemAdminSettings? settings = null)
        => new(
            settings ?? new StubSystemAdminSettings(),
            new NoOpAdminAuditLog(),
            mailer: null,
            new AlwaysOnFeatures(),
            NullLogger<SystemAdminLoginNotifier>.Instance);
}
