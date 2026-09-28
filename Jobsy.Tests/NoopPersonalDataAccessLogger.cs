using Jobsy.Core.Interfaces;

namespace Jobsy.Tests;

internal sealed class NoopPersonalDataAccessLogger : IPersonalDataAccessLogger
{
    public Task LogAsync(PersonalDataAccessEntry entry, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
