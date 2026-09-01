using Microsoft.JSInterop;

namespace Jobsy.Web.Navigation;

/// <summary>
/// Client-only recently viewed vacancy IDs (localStorage). Stores GUIDs, never emails or titles.
/// </summary>
public static class RecentlyViewedVacancies
{
    public const int MaxItems = 20;

    public static async Task RememberAsync(IJSRuntime js, Guid vacancyId, Guid? userId)
    {
        if (vacancyId == Guid.Empty)
        {
            return;
        }

        try
        {
            await js.InvokeVoidAsync(
                "jobsyGeo.rememberViewedVacancy",
                vacancyId.ToString("D"),
                userId is { } id && id != Guid.Empty ? id.ToString("D") : null);
        }
        catch (JSDisconnectedException)
        {
        }
        catch (JSException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }

    public static async Task<IReadOnlyList<Guid>> ListAsync(IJSRuntime js, Guid? userId)
    {
        try
        {
            var ids = await js.InvokeAsync<string[]?>(
                "jobsyGeo.listRecentlyViewed",
                userId is { } id && id != Guid.Empty ? id.ToString("D") : null);
            if (ids is null || ids.Length == 0)
            {
                return [];
            }

            var result = new List<Guid>(Math.Min(ids.Length, MaxItems));
            foreach (var raw in ids)
            {
                if (Guid.TryParse(raw, out var guid) && guid != Guid.Empty && !result.Contains(guid))
                {
                    result.Add(guid);
                }

                if (result.Count >= MaxItems)
                {
                    break;
                }
            }

            return result;
        }
        catch (JSDisconnectedException)
        {
            return [];
        }
        catch (JSException)
        {
            return [];
        }
        catch (InvalidOperationException)
        {
            return [];
        }
    }
}
