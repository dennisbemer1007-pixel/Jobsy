namespace Jobsy.Core.Scholen;

public interface ISchoolCodeListPdfService
{
    /// <summary>
    /// Builds an on-demand A4 code list PDF (never stored). Name column is always empty.
    /// </summary>
    byte[] Render(
        string schoolName,
        string className,
        string schoolYearLabel,
        IReadOnlyList<SchoolCodeListRow> rows,
        bool includeCutoutCards = true);
}

public sealed record SchoolCodeListRow(int Number, string DisplayCode);

public static class SchoolCodeListPdfCopy
{
    public const string NameColumnHeader = "Naam (vul zelf in)";
    public const string NoNamesFooter =
        "Lobsy bewaart geen namen. Deze lijst blijft op school. Leerlingen loggen in op lobsy.nl/leerling met school, klas en code.";
    public const string Title = "Codelijst — bewaar deze lijst op school";
    public const string CutoutTitle = "Kaartjes om uit te knippen";
}

public static class SchoolCodeListCsv
{
    public const string Header = "Nr;Code;Naam (vul zelf in)";

    /// <summary>UTF-8 BOM + Dutch Excel semicolon CSV. Name column always empty.</summary>
    public static byte[] Build(IReadOnlyList<SchoolCodeListRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        using var ms = new MemoryStream();
        // UTF-8 BOM for Excel
        ms.WriteByte(0xEF);
        ms.WriteByte(0xBB);
        ms.WriteByte(0xBF);
        using (var writer = new StreamWriter(ms, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true))
        {
            writer.WriteLine(Header);
            foreach (var row in rows.OrderBy(r => r.Number))
            {
                writer.WriteLine($"{row.Number};{row.DisplayCode};");
            }
        }

        return ms.ToArray();
    }
}
