using Jobsy.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services.Letters;

/// <summary>
/// Default letter provider for Development / CI / acceptatie: keeps the PDF (with code)
/// in <see cref="IStubLetterStore"/> so testers can finish the flow without PostNL.
/// The plaintext code is never logged.
/// </summary>
public sealed class StubLetterService : ILetterService
{
    private readonly IStubLetterStore _store;
    private readonly ILogger<StubLetterService> _logger;

    public StubLetterService(IStubLetterStore store, ILogger<StubLetterService> logger)
    {
        _store = store;
        _logger = logger;
    }

    public string ProviderName => "Stub";

    public Task<LetterSendResult> SendAsync(LetterRequest request, CancellationToken cancellationToken = default)
    {
        var id = "stub-" + Guid.NewGuid().ToString("N");
        _store.Save(id, request.Pdf, request.Reference);
        _logger.LogInformation(
            "Stub letter queued id={LetterId} ref={Reference} bytes={Bytes} (code not logged)",
            id,
            request.Reference,
            request.Pdf.Length);
        return Task.FromResult(new LetterSendResult(true, id, null, null));
    }

    public Task<LetterStatus> GetStatusAsync(string providerLetterId, CancellationToken cancellationToken = default)
    {
        if (_store.TryGet(providerLetterId, out _, out _))
        {
            return Task.FromResult(new LetterStatus(
                providerLetterId,
                LetterDeliveryStatus.Sent,
                DateTime.UtcNow,
                "stub"));
        }

        return Task.FromResult(new LetterStatus(
            providerLetterId,
            LetterDeliveryStatus.Unknown,
            null,
            "not_found"));
    }
}
