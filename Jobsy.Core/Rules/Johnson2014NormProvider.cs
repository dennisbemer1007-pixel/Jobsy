using System.Reflection;
using System.Text.Json;
using Jobsy.Core.Interfaces;

namespace Jobsy.Core.Rules;

/// <summary>
/// Loads the embedded Johnson (2014) IPIP-NEO-120 Big Five norm dataset
/// (<c>Jobsy.Core.Data.Norms.big-five-norms.json</c>) once and answers band/mean lookups for
/// the deep-analysis competence report. When the resource is missing, malformed, or marked
/// unavailable, <see cref="IsAvailable"/> is false and callers must hide the comparison.
/// </summary>
public sealed class Johnson2014NormProvider : INormProvider
{
    public const string SourceDisclaimerText =
        "De vergelijking is een indicatie. We vergelijken je score met onderzoeksgegevens van een vergelijkbare vragenlijst, ingevuld door 2.707 volwassenen in Nederland (Johnson, 2014). Onze vragenlijst is niet precies dezelfde, dus zie dit als een globale inschatting.";

    private const string ResourceSuffix = "Data.Norms.big-five-norms.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly Lazy<BigFiveNormSet?> _lazyNorms;

    public Johnson2014NormProvider()
    {
        _lazyNorms = new Lazy<BigFiveNormSet?>(LoadEmbeddedNorms);
    }

    public bool IsAvailable => Norms?.Available == true;

    public BigFiveNormSet? Norms => _lazyNorms.Value;

    public string SourceDisclaimer => SourceDisclaimerText;

    public string? BandLabel(double score, string traitOrFacetKey)
    {
        var percentiles = FindPercentiles(traitOrFacetKey);
        if (percentiles is null)
        {
            return null;
        }

        var (p25, p75) = percentiles.Value;
        if (score < p25)
        {
            return "Lager dan de meeste mensen";
        }

        if (score > p75)
        {
            return "Hoger dan de meeste mensen";
        }

        return "Vergelijkbaar met de meeste mensen";
    }

    public double? Mean(string traitOrFacetKey)
    {
        var norms = Norms;
        if (norms is null || !IsAvailable || string.IsNullOrWhiteSpace(traitOrFacetKey))
        {
            return null;
        }

        if (norms.Traits.TryGetValue(traitOrFacetKey, out var trait))
        {
            return trait.Mean;
        }

        foreach (var t in norms.Traits.Values)
        {
            if (t.Facets.TryGetValue(traitOrFacetKey, out var facet))
            {
                return facet.Mean;
            }
        }

        return null;
    }

    private (double P25, double P75)? FindPercentiles(string traitOrFacetKey)
    {
        var norms = Norms;
        if (norms is null || !IsAvailable || string.IsNullOrWhiteSpace(traitOrFacetKey))
        {
            return null;
        }

        if (norms.Traits.TryGetValue(traitOrFacetKey, out var trait))
        {
            return (trait.P25, trait.P75);
        }

        foreach (var t in norms.Traits.Values)
        {
            if (t.Facets.TryGetValue(traitOrFacetKey, out var facet))
            {
                return (facet.P25, facet.P75);
            }
        }

        return null;
    }

    private static BigFiveNormSet? LoadEmbeddedNorms()
    {
        try
        {
            var assembly = typeof(Johnson2014NormProvider).Assembly;
            var resourceName = assembly
                .GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(ResourceSuffix, StringComparison.OrdinalIgnoreCase));

            if (resourceName is null)
            {
                return null;
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                return null;
            }

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            return JsonSerializer.Deserialize<BigFiveNormSet>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
