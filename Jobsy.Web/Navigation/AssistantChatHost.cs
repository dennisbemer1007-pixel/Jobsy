using System.Security.Claims;
using Jobsy.Core.Authorization;

namespace Jobsy.Web.Navigation;

/// <summary>
/// Opens the Lobsy assistant from chrome (right-edge tab) without a floating FAB.
/// </summary>
public sealed class AssistantChatHost
{
    public event Action? ToggleRequested;
    public event Action? Changed;

    public bool IsOpen { get; private set; }

    public void RequestToggle() => ToggleRequested?.Invoke();

    public void NotifyOpen(bool open)
    {
        if (IsOpen == open)
        {
            return;
        }

        IsOpen = open;
        Changed?.Invoke();
    }

    public static bool IsAvailableFor(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        return RoleClaimMatching.HasRole(user, JobsyRoles.Candidate)
            || RoleClaimMatching.HasRole(user, JobsyRoles.SalesManager)
            || RoleClaimMatching.HasRole(user, JobsyRoles.Admin)
            || RoleClaimMatching.HasAnyRole(user, JobsyRoles.EmployerRoles);
    }
}
