using Jobsy.Core.Interfaces;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Jobsy.Infrastructure.Data;

internal static class FieldEncryptionConverters
{
    internal sealed class EncryptedStringConverter(IFieldEncryptionService crypto)
        : ValueConverter<string?, string?>(
            v => crypto.EncryptString(v),
            v => crypto.DecryptString(v));

    internal sealed class EncryptedRequiredStringConverter(IFieldEncryptionService crypto)
        : ValueConverter<string, string>(
            v => crypto.EncryptString(v) ?? "",
            v => crypto.DecryptString(v) ?? "");

    internal sealed class EncryptedDateOnlyConverter(IFieldEncryptionService crypto)
        : ValueConverter<DateOnly?, string?>(
            v => EncryptDate(crypto, v),
            v => DecryptDate(crypto, v));

    internal sealed class EncryptedBytesConverter(IFieldEncryptionService crypto)
        : ValueConverter<byte[], byte[]>(
            v => crypto.EncryptBytes(v),
            v => crypto.DecryptBytes(v));

    private static string? EncryptDate(IFieldEncryptionService crypto, DateOnly? value)
        => value.HasValue ? crypto.EncryptString(value.Value.ToString("O")) : null;

    private static DateOnly? DecryptDate(IFieldEncryptionService crypto, string? stored)
    {
        var plain = crypto.DecryptString(stored);
        return string.IsNullOrEmpty(plain) ? null : DateOnly.Parse(plain);
    }
}
