using System.Text;
using Jobsy.Core.Contracts;

namespace Jobsy.Core.Rules;

public static class CoachTonePreferenceRules
{
    private static readonly HashSet<string> KnownQuestionKeys = CoachToneCatalog.Questions
        .Select(q => q.Key)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static bool HasAnyAnswers(CoachTonePreferencesDto? prefs)
        => prefs?.Answers is { Count: > 0 } dict && dict.Values.Any(v => v is { Length: > 0 });

    public static bool IsComplete(CoachTonePreferencesDto? prefs)
    {
        if (prefs?.Answers is not { Count: > 0 } dict)
        {
            return false;
        }

        foreach (var q in CoachToneCatalog.Questions)
        {
            if (!dict.TryGetValue(q.Key, out var codes) || codes is not { Length: > 0 })
            {
                return false;
            }
        }

        return true;
    }

    public static CoachTonePreferencesDto? Normalize(CoachTonePreferencesDto? incoming)
    {
        if (incoming is null)
        {
            return null;
        }

        if (incoming.Answers is not { Count: > 0 })
        {
            return incoming.Skipped
                ? incoming with { Answers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) }
                : null;
        }

        var answers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, raw) in incoming.Answers)
        {
            if (!KnownQuestionKeys.Contains(key))
            {
                continue;
            }

            var q = CoachToneCatalog.TryQuestion(key)!;
            var allowed = q.Options.Select(o => o.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var codes = (raw ?? [])
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Where(allowed.Contains)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(q.MultiSelect ? 8 : 1)
                .ToArray();
            if (codes.Length > 0)
            {
                answers[key] = codes;
            }
        }

        if (answers.Count == 0 && !incoming.Skipped)
        {
            return null;
        }

        return new CoachTonePreferencesDto(
            Version: 1,
            UpdatedAtUtc: incoming.UpdatedAtUtc ?? DateTime.UtcNow,
            Skipped: incoming.Skipped,
            Answers: answers);
    }

    public static CoachTonePreferencesDto Wipe() => new(1, DateTime.UtcNow, false, new Dictionary<string, string[]>());

    public static CoachTonePreferencesDto MergeAnswers(
        CoachTonePreferencesDto? existing,
        IReadOnlyDictionary<string, string[]>? patch,
        bool wipe = false)
    {
        if (wipe)
        {
            return Wipe();
        }

        var merged = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        if (existing?.Answers is { Count: > 0 })
        {
            foreach (var kv in existing.Answers)
            {
                merged[kv.Key] = kv.Value;
            }
        }

        if (patch is not null)
        {
            foreach (var kv in patch)
            {
                if (kv.Value is null || kv.Value.Length == 0)
                {
                    merged.Remove(kv.Key);
                }
                else
                {
                    merged[kv.Key] = kv.Value.ToArray();
                }
            }
        }

        return Normalize(new CoachTonePreferencesDto(1, DateTime.UtcNow, false, merged))
               ?? new CoachTonePreferencesDto(1, DateTime.UtcNow, false, merged);
    }

    /// <summary>System-prompt addendum for the coach (not user-visible).</summary>
    public static string BuildToneAddendum(CoachTonePreferencesDto? prefs)
    {
        if (prefs?.Answers is not { Count: > 0 })
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendLine("Tone preferences (self-reported; quote only when relevant, never invent):");

        void Codes(string key, Action<IReadOnlyList<string>> act)
        {
            if (prefs.Answers!.TryGetValue(key, out var c) && c is { Length: > 0 })
            {
                act(c);
            }
        }

        Codes("certainty_helps", codes =>
        {
            if (codes.Contains("duidelijke_volgende_stap", StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine("- Give short numbered next steps (1, 2, 3). Close each step clearly.");
            }

            if (codes.Contains("vaste_afspraken", StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine("- Mention concrete timings or fixed checkpoints when suggesting actions.");
            }

            if (codes.Contains("rustig_tempo", StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine("- Keep pacing calm; one focus at a time.");
            }
        });

        Codes("certainty_worries", codes =>
        {
            if (codes.Any(c => !string.Equals(c, "niks_hoeft_niet", StringComparison.OrdinalIgnoreCase)))
            {
                sb.AppendLine("- Acknowledge uncertainty briefly; stay factual; do not promise outcomes.");
            }
        });

        Codes("autonomy_choose", codes =>
        {
            if (codes.Any(c => !string.Equals(c, "rustig_vanuit_lobsy", StringComparison.OrdinalIgnoreCase)))
            {
                sb.AppendLine("- Offer 2–3 clear choices and ask what the candidate wants; never decide for them.");
            }
        });

        Codes("autonomy_too_much", codes =>
        {
            if (codes.Count > 0)
            {
                sb.AppendLine("- Avoid pushing many steps at once; let the candidate set the pace.");
            }
        });

        Codes("fairness_important", codes =>
        {
            if (codes.Contains("geen_valse_beloftes", StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine("- No optimistic claims about vacancies or hiring chances.");
            }
        });

        var thinking = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Codes("thinking_learn", c => thinking.UnionWith(c));
        Codes("thinking_new_task", c => thinking.UnionWith(c));

        if (thinking.Contains(CoachToneCatalog.ThinkingStappen))
        {
            sb.AppendLine("- Use bullet lists and small ordered steps.");
        }

        if (thinking.Contains(CoachToneCatalog.ThinkingPlaatje))
        {
            sb.AppendLine("- Start with the big picture, then details.");
        }

        if (thinking.Contains(CoachToneCatalog.ThinkingFeiten))
        {
            sb.AppendLine("- Prefer concrete facts and numbers where the facts list allows.");
        }

        if (thinking.Contains(CoachToneCatalog.ThinkingMensen))
        {
            sb.AppendLine("- Use warm, human wording; relate to people and feelings without inventing facts.");
        }

        sb.AppendLine(
            "Never label the candidate with a type, score or quadrant. "
            + "These preferences adjust explanation style only.");
        return sb.ToString().Trim();
    }

    public static string BuildAllowedFactLines(CoachTonePreferencesDto? prefs)
    {
        if (prefs?.Answers is not { Count: > 0 })
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        foreach (var q in CoachToneCatalog.Questions)
        {
            if (!prefs.Answers.TryGetValue(q.Key, out var codes) || codes.Length == 0)
            {
                continue;
            }

            foreach (var code in codes)
            {
                sb.Append("coachVoorkeur: ").Append(q.Key).Append('=').Append(code).Append("; ");
            }
        }

        return sb.ToString();
    }

    /// <summary>Strips coach tone from JSON blobs that may be logged or exported.</summary>
    public static CandidatePreferencesDto WithoutCoachTone(CandidatePreferencesDto prefs)
        => prefs with { CoachTonePreferences = null };
}
