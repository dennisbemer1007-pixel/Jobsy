namespace Jobsy.Web.Models;

/// <summary>
/// History sent to the coach. A rate-limited or timed-out turn is not an answer,
/// so the next request must not repeat that user line.
/// </summary>
public static class AssistantChatHistory
{
    public static List<AssistantChatMessage> DropUnansweredUser(IReadOnlyList<AssistantChatMessage> messages)
    {
        var copy = messages.ToList();
        if (copy.Count > 0 && string.Equals(copy[^1].Role, "user", StringComparison.OrdinalIgnoreCase))
        {
            copy.RemoveAt(copy.Count - 1);
        }

        return copy;
    }

    public static IReadOnlyList<AssistantChatMessage> ForRequest(IReadOnlyList<AssistantChatMessage> messages)
        => messages.TakeLast(20).ToList();

    /// <summary>After HTTP 429 or a timeout: drop the turn and put the text back in the box.</summary>
    public static (List<AssistantChatMessage> Messages, string Draft) RestoreAfterUnanswered(
        IReadOnlyList<AssistantChatMessage> messages,
        string sentText)
        => (DropUnansweredUser(messages), sentText);
}
