using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Jobsy.Web.Navigation;

public static class ResearchPostTestNavigation
{
    public static async Task GoAfterQuickScanCompleteAsync(
        JobsyApiClient api,
        NavigationManager navigation,
        string returnUrl,
        string testKey)
    {
        var research = await api.GetResearchFeatureStatusAsync();
        if (research?.Enabled == true)
        {
            var encodedReturn = Uri.EscapeDataString(returnUrl);
            navigation.NavigateTo($"/candidate/research-consent?return={encodedReturn}&test={testKey}");
        }
        else
        {
            navigation.NavigateTo(returnUrl);
        }
    }
}
