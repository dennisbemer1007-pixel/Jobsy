using Jobsy.Core.Interfaces;

namespace Jobsy.Infrastructure.Security;

/// <summary>Static hook for Data Protection XML at rest (same AES-GCM as EF fields).</summary>
public static class FieldEncryptionAtRest
{
    private static IFieldEncryptionService _crypto = PassthroughFieldEncryption.Instance;

    public static void Configure(IFieldEncryptionService crypto)
        => _crypto = crypto ?? PassthroughFieldEncryption.Instance;

    public static string ProtectXml(string xml)
        => _crypto.EncryptString(xml) ?? xml;

    public static string UnprotectXml(string stored)
        => _crypto.DecryptString(stored) ?? stored;
}
