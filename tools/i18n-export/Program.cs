using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Jobsy.Web.Localization;

/// <summary>
/// Optional translator export (cleanup prompt 14): CSV of keys whose pl/ro/ar (or en)
/// value is still identical to Dutch, excluding the allow-list.
/// Usage: dotnet run --project tools/i18n-export -- [out.csv]
/// </summary>
var outPath = args.ElementAtOrDefault(0)
              ?? Path.Combine("docs", "i18n", "untranslated-for-translators.csv");

var catalogField = typeof(UiStrings).GetField(
    "Catalog",
    BindingFlags.NonPublic | BindingFlags.Static)
    ?? throw new InvalidOperationException("UiStrings.Catalog not found.");
var catalog = (Dictionary<string, Dictionary<string, string>>)catalogField.GetValue(null)!;
var nl = catalog["nl"];
var en = catalog["en"];
var pl = catalog["pl"];
var ro = catalog["ro"];
var ar = catalog["ar"];

var sb = new StringBuilder();
sb.AppendLine("key,nl,en,pl,ro,ar,identical_langs");

foreach (var key in nl.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
{
    var nlValue = nl[key];
    if (IsExempt(nlValue))
    {
        continue;
    }

    var identical = new List<string>();
    if (en.TryGetValue(key, out var enV) && enV == nlValue) identical.Add("en");
    if (pl.TryGetValue(key, out var plV) && plV == nlValue) identical.Add("pl");
    if (ro.TryGetValue(key, out var roV) && roV == nlValue) identical.Add("ro");
    if (ar.TryGetValue(key, out var arV) && arV == nlValue) identical.Add("ar");
    if (identical.Count == 0)
    {
        continue;
    }

    sb.Append(Csv(key)).Append(',')
        .Append(Csv(nlValue)).Append(',')
        .Append(Csv(en.GetValueOrDefault(key, ""))).Append(',')
        .Append(Csv(pl.GetValueOrDefault(key, ""))).Append(',')
        .Append(Csv(ro.GetValueOrDefault(key, ""))).Append(',')
        .Append(Csv(ar.GetValueOrDefault(key, ""))).Append(',')
        .Append(Csv(string.Join('|', identical)))
        .AppendLine();
}

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
File.WriteAllText(outPath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
Console.WriteLine($"Wrote {outPath}");

static string Csv(string value)
{
    if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
    {
        return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    return value;
}

static bool IsExempt(string value)
{
    var v = value.Trim();
    if (v.Length == 0) return true;
    if (string.Equals(v, "Lobsy", StringComparison.OrdinalIgnoreCase)) return true;
    if (string.Equals(v, "OK", StringComparison.OrdinalIgnoreCase)) return true;
    if (Regex.IsMatch(v, @"^\d+([.,]\d+)?$")) return true;
    if (Regex.IsMatch(v, @"^[\d½]+\s*min$", RegexOptions.IgnoreCase)) return true;
    if (Regex.IsMatch(v, @"^[A-Z] - [A-Z]$")) return true;
    string[] exact =
    [
        "Match", "match", "Admin", "Sales", "Coach", "Bug", "Tip", "Status", "Email", "E-mail",
        "Model", "Tests", "Open", "Later", "Nu", "Doel", "Basis", "min", "KVK", "SBI",
        "Arts", "Kok", "Meer", "Eens", "Samen", "Adres", "E-bike", "CV", "PDF",
        "WhatsApp", "IBAN", "BTW", "ID", "URL", "API", "OTP", "SMS", "GPS", "AI"
    ];
    return exact.Contains(v, StringComparer.Ordinal);
}
