using Jobsy.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// No SMS provider yet (decision 22). Development logs the code; every other environment is a no-op.
/// </summary>
public sealed class SmsSenderStub : ISmsSender
{
    private readonly IHostEnvironment _environment;
    private readonly ILogger<SmsSenderStub> _logger;

    public SmsSenderStub(IHostEnvironment environment, ILogger<SmsSenderStub> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public Task SendAsync(string e164, string body, CancellationToken cancellationToken = default)
    {
        if (_environment.IsDevelopment())
        {
            _logger.LogInformation("SMS stub to {Phone}: {Body}", e164, body);
        }

        return Task.CompletedTask;
    }
}
