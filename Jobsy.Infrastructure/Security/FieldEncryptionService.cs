using System.Security.Cryptography;
using System.Text;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Security;

public sealed class FieldEncryptionService : IFieldEncryptionService
{
    public const string StringPrefix = "lobsy1:";
    public const string BytesMagic = "LBSY1ENC";

    private readonly IReadOnlyDictionary<string, byte[]> _keys;
    private readonly string? _activeKeyId;
    private readonly bool _enabled;

    public FieldEncryptionService(IOptions<FieldEncryptionOptions> options, IHostEnvironment environment)
    {
        var o = options.Value;
        _keys = ParseKeys(o.Keys);
        _activeKeyId = string.IsNullOrWhiteSpace(o.ActiveKeyId) ? null : o.ActiveKeyId.Trim();

        if (_keys.Count == 0 || string.IsNullOrEmpty(_activeKeyId))
        {
            if (!environment.IsDevelopment() && !o.AllowPassthroughWithoutKeys)
            {
                throw new InvalidOperationException(
                    "JobsyFieldEncryption:ActiveKeyId and at least one JobsyFieldEncryption:Keys:{id} " +
                    "(32-byte base64 AES key) are required outside Development. " +
                    "Set JobsyFieldEncryption__ActiveKeyId and JobsyFieldEncryption__Keys__v1 on Render.");
            }

            _enabled = false;
            return;
        }

        if (!_keys.ContainsKey(_activeKeyId))
        {
            throw new InvalidOperationException(
                $"JobsyFieldEncryption:ActiveKeyId '{_activeKeyId}' is not present in JobsyFieldEncryption:Keys.");
        }

        _enabled = true;
        FieldEncryptionAtRest.Configure(this);
    }

    public bool IsEnabled => _enabled;

    public bool IsEncrypted(string? stored)
        => !string.IsNullOrEmpty(stored) && stored.StartsWith(StringPrefix, StringComparison.Ordinal);

    public string? EncryptString(string? plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            return plaintext;
        }

        if (!_enabled)
        {
            return plaintext;
        }

        if (IsEncrypted(plaintext))
        {
            return plaintext;
        }

        var keyId = _activeKeyId!;
        var key = _keys[keyId];
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[plainBytes.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(key, tagSizeInBytes: 16);
        aes.Encrypt(nonce, plainBytes, cipher, tag);

        var payload = new byte[nonce.Length + tag.Length + cipher.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, payload, nonce.Length, tag.Length);
        Buffer.BlockCopy(cipher, 0, payload, nonce.Length + tag.Length, cipher.Length);

        return $"{StringPrefix}{keyId}:{Convert.ToBase64String(payload)}";
    }

    public string? DecryptString(string? stored)
    {
        if (string.IsNullOrEmpty(stored))
        {
            return stored;
        }

        if (!IsEncrypted(stored))
        {
            return stored;
        }

        var body = stored[StringPrefix.Length..];
        var sep = body.IndexOf(':');
        if (sep <= 0)
        {
            throw new CryptographicException("Invalid field encryption envelope.");
        }

        var keyId = body[..sep];
        if (!_keys.TryGetValue(keyId, out var key))
        {
            throw new CryptographicException($"Unknown field encryption key id '{keyId}'.");
        }

        var payload = Convert.FromBase64String(body[(sep + 1)..]);
        if (payload.Length < 12 + 16)
        {
            throw new CryptographicException("Truncated field encryption payload.");
        }

        var nonce = payload.AsSpan(0, 12);
        var tag = payload.AsSpan(12, 16);
        var cipher = payload.AsSpan(28);
        var plain = new byte[cipher.Length];
        using var aes = new AesGcm(key, tagSizeInBytes: 16);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }

    public byte[] EncryptBytes(byte[] plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        if (plaintext.Length == 0 || !_enabled)
        {
            return plaintext;
        }

        if (TryParseEncryptedBytes(plaintext, out _, out _))
        {
            return plaintext;
        }

        var keyId = _activeKeyId!;
        var key = _keys[keyId];
        var nonce = RandomNumberGenerator.GetBytes(12);
        var cipher = new byte[plaintext.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(key, tagSizeInBytes: 16);
        aes.Encrypt(nonce, plaintext, cipher, tag);

        var keyIdBytes = Encoding.UTF8.GetBytes(keyId);
        using var ms = new MemoryStream();
        ms.Write(Encoding.UTF8.GetBytes(BytesMagic));
        ms.WriteByte((byte)keyIdBytes.Length);
        ms.Write(keyIdBytes);
        ms.Write(nonce);
        ms.Write(tag);
        ms.Write(cipher);
        return ms.ToArray();
    }

    public byte[] DecryptBytes(byte[] stored)
    {
        ArgumentNullException.ThrowIfNull(stored);
        if (stored.Length == 0 || !TryParseEncryptedBytes(stored, out var keyId, out var inner))
        {
            return stored;
        }

        if (!_keys.TryGetValue(keyId, out var key))
        {
            throw new CryptographicException($"Unknown field encryption key id '{keyId}'.");
        }

        if (inner.Length < 12 + 16)
        {
            throw new CryptographicException("Truncated encrypted byte payload.");
        }

        var nonce = inner.AsSpan(0, 12);
        var tag = inner.AsSpan(12, 16);
        var cipher = inner.AsSpan(28);
        var plain = new byte[cipher.Length];
        using var aes = new AesGcm(key, tagSizeInBytes: 16);
        aes.Decrypt(nonce, cipher, tag, plain);
        return plain;
    }

    private static bool TryParseEncryptedBytes(byte[] stored, out string keyId, out byte[] payload)
    {
        keyId = "";
        payload = [];
        var magic = Encoding.UTF8.GetBytes(BytesMagic);
        if (stored.Length <= magic.Length + 2)
        {
            return false;
        }

        for (var i = 0; i < magic.Length; i++)
        {
            if (stored[i] != magic[i])
            {
                return false;
            }
        }

        var keyLen = stored[magic.Length];
        var keyStart = magic.Length + 1;
        if (stored.Length < keyStart + keyLen + 12 + 16)
        {
            return false;
        }

        keyId = Encoding.UTF8.GetString(stored, keyStart, keyLen);
        payload = stored.AsSpan(keyStart + keyLen).ToArray();
        return true;
    }

    private static IReadOnlyDictionary<string, byte[]> ParseKeys(Dictionary<string, string> raw)
    {
        var map = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var (id, b64) in raw)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(b64))
            {
                continue;
            }

            byte[] key;
            try
            {
                key = Convert.FromBase64String(b64.Trim());
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException(
                    $"JobsyFieldEncryption:Keys:{id} is not valid base64.", ex);
            }

            if (key.Length != 32)
            {
                throw new InvalidOperationException(
                    $"JobsyFieldEncryption:Keys:{id} must decode to 32 bytes (AES-256), got {key.Length}.");
            }

            map[id.Trim()] = key;
        }

        return map;
    }
}
