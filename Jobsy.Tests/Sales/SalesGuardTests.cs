using System.Reflection;
using System.Text.RegularExpressions;
using Jobsy.Core.Contracts.Sales;
using Jobsy.Core.Enums;
using Jobsy.Core.Sales;
using Jobsy.Web.Localization;

namespace Jobsy.Tests.Sales;

public class SalesLabelsCompletenessTests
{
    [Fact]
    public void Every_labeled_enum_has_nl_string()
    {
        var nl = typeof(UiStrings).GetField("Catalog", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null) as Dictionary<string, Dictionary<string, string>>;
        Assert.NotNull(nl);
        var map = nl!["nl"];

        foreach (var type in SalesLabels.LabeledEnumTypes())
        {
            foreach (var value in Enum.GetValues(type))
            {
                var key = ResolveKey(type, value);
                Assert.True(map.ContainsKey(key), $"Missing nl label for {type.Name}.{value} → {key}");
                Assert.False(string.IsNullOrWhiteSpace(map[key]));
            }
        }
    }

    private static string ResolveKey(Type type, object value) => type.Name switch
    {
        nameof(Jobsy.Core.Entities.CommissionEntryKind) =>
            SalesLabels.Key((Jobsy.Core.Entities.CommissionEntryKind)value),
        nameof(CommissionEntryState) => SalesLabels.Key((CommissionEntryState)value),
        nameof(SalesPayoutRequestStatus) => SalesLabels.Key((SalesPayoutRequestStatus)value),
        nameof(SalesPayoutRunStatus) => SalesLabels.Key((SalesPayoutRunStatus)value),
        nameof(Jobsy.Core.Entities.SelfBillingInvoiceStatus) =>
            SalesLabels.Key((Jobsy.Core.Entities.SelfBillingInvoiceStatus)value),
        nameof(Jobsy.Core.Entities.SalesManagerVatTreatment) =>
            SalesLabels.Key((Jobsy.Core.Entities.SalesManagerVatTreatment)value),
        nameof(SalesAttributionSource) => SalesLabels.Key((SalesAttributionSource)value),
        nameof(Jobsy.Core.Entities.SalesManagerApplicationStatus) =>
            SalesLabels.Key((Jobsy.Core.Entities.SalesManagerApplicationStatus)value),
        nameof(Jobsy.Core.Contracts.Sales.SalesEmployerStatus) =>
            SalesLabels.Key((Jobsy.Core.Contracts.Sales.SalesEmployerStatus)value),
        _ => throw new InvalidOperationException(type.Name)
    };
}

public class SalesPortalNoInlineStyleTests
{
    private static readonly HashSet<string> AllowList = new(StringComparer.OrdinalIgnoreCase)
    {
        // Emptied by salesmanager-09 — portal and remaining admin pages must not use style="".
    };

    [Fact]
    public void Inline_style_allow_list_is_empty()
    {
        Assert.Empty(AllowList);
    }

    [Fact]
    public void Sales_portal_shell_has_no_inline_style()
    {
        var root = FindRepoRoot();
        var paths = new[]
        {
            "Jobsy.Web/Components/Pages/Sales",
            "Jobsy.Web/Components/Sales",
            "Jobsy.Web/Components/Layout/SalesLayout.razor",
            "Jobsy.Web/Components/Layout/SalesSidebar.razor",
            "Jobsy.Web/Components/Layout/SalesWalletChipV2.razor",
        };

        var hits = new List<string>();
        foreach (var rel in paths)
        {
            var full = Path.Combine(root, rel);
            IEnumerable<string> files = File.Exists(full)
                ? [full]
                : Directory.Exists(full)
                    ? Directory.EnumerateFiles(full, "*.razor", SearchOption.AllDirectories)
                    : [];
            foreach (var file in files)
            {
                var repoRel = Path.GetRelativePath(root, file).Replace('\\', '/');
                if (AllowList.Contains(repoRel))
                {
                    continue;
                }

                var text = File.ReadAllText(file);
                if (text.Contains("style=\"", StringComparison.Ordinal))
                {
                    hits.Add(repoRel);
                }
            }
        }

        Assert.True(hits.Count == 0, "Inline style= found: " + string.Join(", ", hits));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root");
    }
}

public class SalesPortalDtoPrivacyTests
{
    private static readonly Regex Forbidden = new(
        "(?i)(kvk|address|adres|street|straat|postcode|email|phone|telefoon|contact|firstname|lastname|fullname|initials|vacancy|vacature|candidate|kandidaat|applicant)",
        RegexOptions.Compiled);

    [Fact]
    public void Sales_contract_dtos_have_no_forbidden_pii_names()
    {
        var asm = typeof(SalesEmployerDtoPlaceholder).Assembly;
        var types = asm.GetTypes()
            .Where(t => t.Namespace == "Jobsy.Core.Contracts.Sales")
            .ToList();

        var bad = new List<string>();
        foreach (var type in types)
        {
            if (type.Name.Contains("Profile", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var prop in type.GetProperties())
            {
                if (prop.Name is "MaskedIban" or "CandidateCount" or "ApplicationCount")
                {
                    continue;
                }

                if (Forbidden.IsMatch(prop.Name))
                {
                    bad.Add($"{type.Name}.{prop.Name}");
                }
            }
        }

        Assert.True(bad.Count == 0, string.Join(", ", bad));
    }
}
