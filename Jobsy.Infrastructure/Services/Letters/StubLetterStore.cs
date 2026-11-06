using Jobsy.Core.Interfaces;

namespace Jobsy.Infrastructure.Services.Letters;

/// <summary>In-memory PDF store for <see cref="StubLetterService"/> (test / Dev / Acc).</summary>
public sealed class StubLetterStore : IStubLetterStore
{
    private readonly object _gate = new();
    private readonly Dictionary<string, (byte[] Pdf, string Reference)> _items = new(StringComparer.Ordinal);

    public void Save(string providerLetterId, byte[] pdf, string reference)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerLetterId);
        ArgumentNullException.ThrowIfNull(pdf);
        lock (_gate)
        {
            _items[providerLetterId] = (pdf, reference);
        }
    }

    public bool TryGet(string providerLetterId, out byte[]? pdf, out string? reference)
    {
        lock (_gate)
        {
            if (_items.TryGetValue(providerLetterId, out var row))
            {
                pdf = row.Pdf;
                reference = row.Reference;
                return true;
            }
        }

        pdf = null;
        reference = null;
        return false;
    }
}
