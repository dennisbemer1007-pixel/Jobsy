using System.Reflection;
using Jobsy.Core.Features;
using Jobsy.Web.Admin;
using Xunit;

namespace Jobsy.Tests;

public sealed class FeatureFlagRegistryTests
{
    [Fact]
    public void Every_platform_feature_enum_value_is_in_registry()
    {
        var registryKeys = FeatureFlagRegistry.All
            .Where(d => d.PlatformFeature is not null)
            .Select(d => d.PlatformFeature!.Value)
            .ToHashSet();
        foreach (var feature in Enum.GetValues<PlatformFeature>())
        {
            Assert.True(registryKeys.Contains(feature), $"Missing registry entry for {feature}");
        }
    }

    [Fact]
    public void Every_public_endpoint_flag_has_catalog_entry()
    {
        var catalogKeys = PlatformSettingsCatalog.Entries
            .Where(e => e.Kind == PlatformSettingKind.Bool)
            .Select(e => e.Key)
            .ToHashSet(StringComparer.Ordinal);
        foreach (var def in FeatureFlagRegistry.PublicEndpointFlags)
        {
            Assert.True(
                catalogKeys.Contains(def.Key),
                $"Flag {def.Key} is on public feature-flags but missing from admin catalog.");
        }
    }

    [Fact]
    public void Feature_flags_endpoint_includes_registry_public_keys()
    {
        var method = typeof(Jobsy.Api.Controllers.SettingsController).GetMethod(
            "GetFeatureFlags",
            BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        var source = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..",
            "Jobsy.Api", "Controllers", "SettingsController.cs")));
        foreach (var def in FeatureFlagRegistry.PublicEndpointFlags)
        {
            var camel = char.ToLowerInvariant(def.Key[0]) + def.Key[1..];
            Assert.Contains(camel, source, StringComparison.OrdinalIgnoreCase);
        }
    }
}
