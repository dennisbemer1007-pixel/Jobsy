using Jobsy.Core.Rules;
using Jobsy.Web.Auth;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Jobsy.Web.Passport;

internal static class PassportClientCoachToneSaver
{
    internal static async Task<IResult> SaveAsync(
        IFormCollection form,
        JobsyApiClient api,
        CultureState culture,
        CancellationToken cancellationToken)
    {
        var returnTab = PassportTabs.Normalize(form["returnTab"].ToString());
        var action = (form["coachAction"].ToString() ?? "save").Trim();

        try
        {
            if (string.Equals(action, "wipe", StringComparison.OrdinalIgnoreCase))
            {
                await api.UpdateMyCoachTonePreferencesAsync(wipe: true, ct: cancellationToken);
            }
            else if (string.Equals(action, "edit", StringComparison.OrdinalIgnoreCase))
            {
                var url = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(
                    PassportRedirects.BuildPassportUrl(returnTab),
                    new Dictionary<string, string?>(StringComparer.Ordinal)
                    {
                        ["section"] = "coach-voorkeuren",
                        ["coachEdit"] = "1",
                    });
                return TypedResults.Redirect(url);
            }
            else if (string.Equals(action, "later", StringComparison.OrdinalIgnoreCase))
            {
                await SavePatchAsync(form, api, cancellationToken);
            }
            else
            {
                await SavePatchAsync(form, api, cancellationToken);
            }
        }
        catch (ApiErrorException ex)
        {
            return PassportClientDataSaver.RedirectWithError(returnTab, ex.UserMessage ?? culture["Common.Error.TryAgain"], "coach-voorkeuren");
        }
        catch (Exception)
        {
            return PassportClientDataSaver.RedirectWithError(returnTab, culture["Common.Error.TryAgain"], "coach-voorkeuren");
        }

        var savedUrl = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(
            PassportRedirects.BuildPassportUrl(returnTab),
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["saved"] = "1",
                ["section"] = "coach-voorkeuren",
            });
        return TypedResults.Redirect(savedUrl);
    }

    private static async Task SavePatchAsync(
        IFormCollection form,
        JobsyApiClient api,
        CancellationToken cancellationToken)
    {
        var patch = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var q in CoachToneCatalog.Questions)
        {
            var values = form[$"answer_{q.Key}"].ToArray()
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (values.Length > 0)
            {
                patch[q.Key] = values;
            }
        }

        await api.UpdateMyCoachTonePreferencesAsync(answers: patch, ct: cancellationToken);
    }
}
