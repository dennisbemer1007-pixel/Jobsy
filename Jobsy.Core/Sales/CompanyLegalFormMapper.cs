using Jobsy.Core.Enums;

namespace Jobsy.Core.Sales;

/// <summary>Maps KVK rechtsvorm strings to <see cref="CompanyLegalForm"/>.</summary>
public static class CompanyLegalFormMapper
{
    public static CompanyLegalForm? FromKvk(string? rechtsvorm, string? uitgebreideRechtsvorm = null)
    {
        var raw = FirstNonEmpty(uitgebreideRechtsvorm, rechtsvorm);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var text = raw.Trim().ToLowerInvariant();
        if (ContainsAny(text, "eenmanszaak", "eenmans zaak", "eenmanszaak met"))
        {
            return CompanyLegalForm.Eenmanszaak;
        }

        if (ContainsAny(text, "vennootschap onder firma", "v.o.f", "vof"))
        {
            return CompanyLegalForm.Vof;
        }

        if (ContainsAny(text, "besloten vennootschap", "b.v", "bv"))
        {
            return CompanyLegalForm.Bv;
        }

        if (ContainsAny(text, "naamloze vennootschap", "n.v", "nv"))
        {
            return CompanyLegalForm.Nv;
        }

        if (ContainsAny(text, "stichting"))
        {
            return CompanyLegalForm.Stichting;
        }

        if (ContainsAny(text, "vereniging", "coöperatie", "cooperatie"))
        {
            // Coöperatie is closest to vereniging for portal place rules; keep Other when unsure.
            if (ContainsAny(text, "vereniging"))
            {
                return CompanyLegalForm.Vereniging;
            }

            return CompanyLegalForm.Other;
        }

        return CompanyLegalForm.Other;
    }

    private static bool ContainsAny(string text, params string[] needles)
    {
        foreach (var needle in needles)
        {
            if (text.Contains(needle, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
        {
            if (!string.IsNullOrWhiteSpace(v))
            {
                return v;
            }
        }

        return null;
    }
}
