namespace Jobsy.Core.Scholen;

/// <summary>Cross-tile / cross-block theme keys (dedupe near-duplicate answers).</summary>
public static class PupilG78ThemeKeys
{
    public static string? DedupeKey(int itemId) => itemId switch
    {
        9001 => "empathy_friend",
        9006 or 9019 => "help_someone_confused",
        9024 or 9030 => "support_or_practice_people",
        9047 or 9053 or 9059 => "team_work",
        9016 or 9022 or 9028 => "make_fix_hands",
        9018 or 9023 or 9029 => "creative_show",
        9031 or 9036 or 9041 or 9052 => "self_choose",
        9032 or 9037 or 9042 => "close_friends",
        9027 or 9055 => "clear_rules_steps",
        9050 or 9056 or 9057 or 9009 or 9004 or 9051 => "change_variety",
        9046 or 9058 or 9015 => "own_space_alone",
        9049 or 9010 => "busy_social",
        _ => null
    };
}
