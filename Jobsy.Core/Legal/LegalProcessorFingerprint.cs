using System.Security.Cryptography;
using System.Text;

namespace Jobsy.Core.Legal;

/// <summary>
/// Stable hash of the processor catalog. A change here must ship with a new privacy version
/// (<see cref="LegalDocumentVersions.PrivacyCatalogSnapshots"/>).
/// </summary>
public static class LegalProcessorFingerprint
{
    public static string Compute(IEnumerable<LegalProcessor> processors)
    {
        var builder = new StringBuilder();
        foreach (var processor in processors.OrderBy(p => p.Id, StringComparer.Ordinal))
        {
            builder.Append(processor.Id).Append('|')
                .Append(processor.Name).Append('|')
                .Append(processor.CompanyHq).Append('|')
                .Append(processor.DataRegion).Append('|')
                .Append(processor.PurposeKey).Append('|')
                .Append(processor.DataKey).Append('|')
                .Append(processor.Status).Append('|')
                .Append(processor.TransferBasisKey).Append('|')
                .Append(processor.WhenMailProvider).Append('|')
                .Append(processor.PlannedNoteKey).Append('\n');
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
