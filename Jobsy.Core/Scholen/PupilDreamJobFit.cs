using System.Text.Json;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Scholen;

/// <summary>Deterministic dream-job fit: same inputs → same have/need split (no AI).</summary>
public static class PupilDreamJobFit
{
    public const string UndecidedKey = ClassResultsAggregator.UndecidedDreamJobKey;
    public const string GenericHaveKey = "LeerlingDroom.Have.Curious";
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public sealed record FitResult(
        string JobKey,
        string JobTitle,
        int HaveCount,
        int TotalCount,
        IReadOnlyList<string> HaveSentenceKeys,
        IReadOnlyList<string> NeedSentenceKeys,
        IReadOnlyList<string> RouteStepKeys,
        string? AltRouteKey,
        string EncouragementKey,
        PupilFitSnapshot Snapshot);

    public static FitResult? Evaluate(PupilResult result, PupilProgress? progress, string? jobKey)
    {
        ArgumentNullException.ThrowIfNull(result);
        var key = jobKey ?? result.DreamJobKey ?? progress?.DreamJobKey;
        if (string.IsNullOrWhiteSpace(key)
            || string.Equals(key, UndecidedKey, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var route = PupilDreamJobRoutes.TryGet(key);
        if (route is null)
        {
            return null;
        }

        var competence = Deserialize<CompetencyScores>(result.CompetenceScoresJson);
        var riasec = Deserialize<RiasecScores>(result.RiasecScoresJson);
        var values = Deserialize<SchwartzValuesScores>(result.ValuesScoresJson);
        var likes = ParseChips(progress?.LikesJson);

        var haveKeys = new List<string>();
        var needKeys = new List<string>();
        var metFlags = new List<bool>(route.Needs.Count);
        foreach (var need in route.Needs)
        {
            var met = IsMet(need, competence, riasec, values, likes);
            metFlags.Add(met);
            var sentenceKey = $"LeerlingDroom.Need.{need.NeedKey}";
            if (met)
            {
                haveKeys.Add(sentenceKey);
            }
            else
            {
                needKeys.Add(sentenceKey);
            }
        }

        if (haveKeys.Count == 0)
        {
            haveKeys.Add(GenericHaveKey);
        }

        var band = haveKeys.Contains(GenericHaveKey) && haveKeys.Count == 1
            ? 0
            : metFlags.Count(m => m);
        var encouragement = band switch
        {
            <= 1 => "LeerlingDroom.Cheer.Low",
            <= 3 => "LeerlingDroom.Cheer.Mid",
            _ => "LeerlingDroom.Cheer.High"
        };

        var stepKeys = new List<string>();
        for (var i = 1; i <= route.RouteStepCount; i++)
        {
            stepKeys.Add($"LeerlingDroom.Route.{route.JobKey}.{i}");
        }

        stepKeys.Add($"LeerlingDroom.Route.{route.JobKey}.Goal");

        var title = DreamJobCatalog.All.FirstOrDefault(j =>
            string.Equals(j.Key, route.JobKey, StringComparison.OrdinalIgnoreCase))?.TitleNl
            ?? route.JobKey;

        var snapshot = new PupilFitSnapshot(
            JobKey: route.JobKey,
            HaveCount: metFlags.Count(m => m),
            TotalCount: route.Needs.Count,
            NeedKeys: route.Needs.Select(n => n.NeedKey).ToList(),
            Met: metFlags);

        return new FitResult(
            JobKey: route.JobKey,
            JobTitle: title,
            HaveCount: snapshot.HaveCount,
            TotalCount: snapshot.TotalCount,
            HaveSentenceKeys: haveKeys,
            NeedSentenceKeys: needKeys,
            RouteStepKeys: stepKeys,
            AltRouteKey: route.HasAltRoute ? $"LeerlingDroom.Alt.{route.JobKey}" : null,
            EncouragementKey: encouragement,
            Snapshot: snapshot);
    }

    public static string SerializeSnapshot(PupilFitSnapshot snapshot)
        => JsonSerializer.Serialize(snapshot, JsonOpts);

    public static bool IsKnownJobKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        if (string.Equals(key, UndecidedKey, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return DreamJobCatalog.All.Any(j =>
            string.Equals(j.Key, key, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsMet(
        PupilDreamJobNeed need,
        CompetencyScores? competence,
        RiasecScores? riasec,
        SchwartzValuesScores? values,
        IReadOnlySet<string> likes)
    {
        return need.Signal switch
        {
            PupilNeedSignal.RiasecR => (riasec?.Get(CareerTestCatalog.Realistic) ?? 0) >= need.Threshold,
            PupilNeedSignal.RiasecI => (riasec?.Get(CareerTestCatalog.Investigative) ?? 0) >= need.Threshold,
            PupilNeedSignal.RiasecA => (riasec?.Get(CareerTestCatalog.Artistic) ?? 0) >= need.Threshold,
            PupilNeedSignal.RiasecS => (riasec?.Get(CareerTestCatalog.Social) ?? 0) >= need.Threshold,
            PupilNeedSignal.RiasecE => (riasec?.Get(CareerTestCatalog.Enterprising) ?? 0) >= need.Threshold,
            PupilNeedSignal.RiasecC => (riasec?.Get(CareerTestCatalog.Conventional) ?? 0) >= need.Threshold,
            PupilNeedSignal.CompetenceSamenwerken => (competence?.Samenwerken ?? 0) >= need.Threshold,
            PupilNeedSignal.CompetenceResultaat => (competence?.Resultaatgerichtheid ?? 0) >= need.Threshold,
            PupilNeedSignal.CompetenceStress => (competence?.Stressbestendigheid ?? 0) >= need.Threshold,
            PupilNeedSignal.CompetenceInnovatie => (competence?.Innovatie ?? 0) >= need.Threshold,
            PupilNeedSignal.CompetenceExtraversie => (competence?.Extraversie ?? 0) >= need.Threshold,
            PupilNeedSignal.ValueAutonomy => (values?.Autonomy ?? 0) >= need.Threshold,
            PupilNeedSignal.ValueConnection => (values?.Connection ?? 0) >= need.Threshold,
            PupilNeedSignal.ValueAchievement => (values?.Achievement ?? 0) >= need.Threshold,
            PupilNeedSignal.ValueStability => (values?.Stability ?? 0) >= need.Threshold,
            PupilNeedSignal.ValueImpact => (values?.Impact ?? 0) >= need.Threshold,
            PupilNeedSignal.ChipDieren => likes.Contains("dieren"),
            PupilNeedSignal.ChipTechniek => likes.Contains("techniek"),
            PupilNeedSignal.ChipKoken => likes.Contains("koken"),
            PupilNeedSignal.ChipSport => likes.Contains("sport"),
            PupilNeedSignal.ChipMuziek => likes.Contains("muziek"),
            PupilNeedSignal.ChipTekenen => likes.Contains("tekenen"),
            PupilNeedSignal.ChipComputers => likes.Contains("computers"),
            PupilNeedSignal.ChipBouwen => likes.Contains("bouwen"),
            PupilNeedSignal.ChipKleineKinderen => likes.Contains("kleine-kinderen"),
            PupilNeedSignal.ChipNatuur => likes.Contains("natuur"),
            PupilNeedSignal.ChipProgrammeren => likes.Contains("programmeren") || likes.Contains("computers"),
            PupilNeedSignal.ChipRekenen => likes.Contains("rekenen") || likes.Contains("puzzels"),
            PupilNeedSignal.ChipGamen => likes.Contains("gamen") || likes.Contains("filmpjes"),
            PupilNeedSignal.AlwaysCurious => true,
            _ => false
        };
    }

    private static HashSet<string> ParseChips(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json) ?? [];
            return new HashSet<string>(list, StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static T? Deserialize<T>(string? json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOpts);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
