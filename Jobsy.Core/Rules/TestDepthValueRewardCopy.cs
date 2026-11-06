using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>Calm celebration copy for test-depth result pages (no scores).</summary>
public static class TestDepthValueRewardCopy
{
    public static string WellDoneKey(TestDepthLevel? reached, bool deepCompleted)
    {
        if (deepCompleted)
        {
            return "TestDepthValue.Reward.WellDone.Bottom";
        }

        return reached switch
        {
            TestDepthLevel.First => "TestDepthValue.Reward.WellDone.First",
            TestDepthLevel.Deeper => "TestDepthValue.Reward.WellDone.Deeper",
            TestDepthLevel.Full => "TestDepthValue.Reward.WellDone.Full",
            _ => "TestDepthValue.Reward.WellDone.First"
        };
    }

    public static string IllustrationVariant(TestDepthLevel? reached, bool deepCompleted)
    {
        if (deepCompleted)
        {
            return "150";
        }

        return reached switch
        {
            TestDepthLevel.First => "5",
            TestDepthLevel.Deeper => "10",
            TestDepthLevel.Full => "25",
            _ => "5"
        };
    }

}
