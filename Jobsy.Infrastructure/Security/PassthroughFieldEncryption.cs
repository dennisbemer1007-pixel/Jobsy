using Jobsy.Core.Interfaces;

namespace Jobsy.Infrastructure.Security;

/// <summary>Design-time / unit tests when DI has no field encryption keys.</summary>
internal sealed class PassthroughFieldEncryption : IFieldEncryptionService
{
    public static readonly PassthroughFieldEncryption Instance = new();

    public bool IsEnabled => false;

    public bool IsEncrypted(string? stored) => false;

    public string? EncryptString(string? plaintext) => plaintext;

    public string? DecryptString(string? stored) => stored;

    public byte[] EncryptBytes(byte[] plaintext) => plaintext;

    public byte[] DecryptBytes(byte[] stored) => stored;
}
