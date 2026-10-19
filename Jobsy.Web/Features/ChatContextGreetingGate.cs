namespace Jobsy.Web.Features;

/// <summary>
/// Web-only toggle for page-aware Lobsy greeting and suggestion chips (candidates).
/// Config key: <c>ChatContextGreetingEnabled</c> (default false; on in acceptatie).
/// </summary>
public interface IChatContextGreetingGate
{
    bool Enabled { get; }
}

public sealed class ChatContextGreetingGate(IConfiguration configuration) : IChatContextGreetingGate
{
    public bool Enabled => configuration.GetValue("ChatContextGreetingEnabled", false);
}
