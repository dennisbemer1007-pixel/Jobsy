namespace Jobsy.Web.Navigation;

/// <summary>
/// Lets the header pick up a first-name change without waiting for a new login cookie.
/// </summary>
public static class CandidateNameBroadcast
{
    public static event Action<string>? Changed;

    public static void Publish(string? firstName, string? lastName)
    {
        var name = $"{firstName} {lastName}".Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        Changed?.Invoke(name);
    }
}
