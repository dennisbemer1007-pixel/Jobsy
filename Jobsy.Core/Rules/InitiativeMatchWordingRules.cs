using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

public static class InitiativeMatchWordingRules
{
    public static InitiativeMatchLabel FromInternalScore(int? matchPercent)
    {
        if (matchPercent is >= 80)
        {
            return InitiativeMatchLabel.Strong;
        }

        if (matchPercent is >= 60)
        {
            return InitiativeMatchLabel.Good;
        }

        return InitiativeMatchLabel.Reasonable;
    }

    public static string ResourceKey(InitiativeMatchLabel label) => label switch
    {
        InitiativeMatchLabel.Strong => "Initiative.Match.Strong",
        InitiativeMatchLabel.Good => "Initiative.Match.Good",
        InitiativeMatchLabel.Reasonable => "Initiative.Match.Reasonable",
        _ => "Initiative.Match.Reasonable"
    };
}
