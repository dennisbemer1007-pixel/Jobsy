namespace Jobsy.Core.Interfaces;

/// <summary>AES-GCM encryption for selected EF columns and binary blobs.</summary>
public interface IFieldEncryptionService
{
    /// <summary>True when keys are configured and new values will be encrypted.</summary>
    bool IsEnabled { get; }

    bool IsEncrypted(string? stored);

    string? EncryptString(string? plaintext);

    string? DecryptString(string? stored);

    byte[] EncryptBytes(byte[] plaintext);

    byte[] DecryptBytes(byte[] stored);
}
