namespace Jobsy.Tests.Werkgever;

public class WerkgeverLegacyHrefTests
{
    private static readonly string[] OldPrefixes =
    [
        "\"/employer/", "\"/branch/", "\"/regional/",
        "'/employer/", "'/branch/", "'/regional/"
    ];

    private static readonly string[] Allowed =
    [
        "WerkgeverLegacyRoutes.cs",
        "WerkgeverLegacyRedirect.razor",
        "WerkgeverLegacyRedirectMiddleware.cs",
        "OnboardingCheckout.razor",
        "WerkgeverNav.cs", // aliases intentionally keep old paths
    ];

    [Fact]
    public void No_internal_links_use_old_employer_urls()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var roots = new[]
        {
            Path.Combine(root, "Jobsy.Web"),
            Path.Combine(root, "Jobsy.Core", "Email")
        };

        var offenders = new List<string>();
        foreach (var baseDir in roots)
        {
            foreach (var file in Directory.EnumerateFiles(baseDir, "*.*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) != true
                    && file.EndsWith(".razor", StringComparison.OrdinalIgnoreCase) != true)
                {
                    continue;
                }

                if (Allowed.Any(a => file.EndsWith(a, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                var text = File.ReadAllText(file);
                foreach (var prefix in OldPrefixes)
                {
                    // Allow onboarding-checkout only
                    var idx = 0;
                    while ((idx = text.IndexOf(prefix, idx, StringComparison.Ordinal)) >= 0)
                    {
                        var slice = text.Substring(idx, Math.Min(80, text.Length - idx));
                        if (slice.Contains("/employer/onboarding-checkout", StringComparison.Ordinal))
                        {
                            idx += prefix.Length;
                            continue;
                        }

                        offenders.Add($"{Path.GetRelativePath(root, file)}: {slice.Split('\n')[0]}");
                        idx += prefix.Length;
                    }
                }
            }
        }

        Assert.True(offenders.Count == 0, string.Join("\n", offenders.Take(40)));
    }
}
