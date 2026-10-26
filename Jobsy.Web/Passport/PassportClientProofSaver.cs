using Jobsy.Core.Rules;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Navigation;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Http;

namespace Jobsy.Web.Passport;

internal static class PassportClientProofSaver
{
    internal static async Task<IResult> SaveAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        CancellationToken cancellationToken)
    {
        if (!http.Request.HasFormContentType)
        {
            return Results.BadRequest();
        }

        var form = await http.Request.ReadFormAsync(cancellationToken);
        var sheet = (form["proofSheet"].ToString() ?? "").Trim().ToLowerInvariant();
        var returnTab = PassportTabs.Proof;

        try
        {
            var profile = await api.GetMyProfileAsync(cancellationToken) ?? new MeProfile();
            var preferences = profile.Preferences ?? new CandidatePreferences();

            switch (sheet)
            {
                case "experience":
                    preferences.Employers = ParseEmployers(form);
                    await api.UpdateMyProfileAsync(preferences: preferences, ct: cancellationToken);
                    break;
                case "certificates":
                    preferences.Educations = ParseEducations(form);
                    preferences.EducationDirection = (form["educationDirection"].ToString() ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(preferences.EducationDirection))
                    {
                        preferences.EducationDirection = null;
                    }

                    preferences.Certificates = ParseCertificates(form);
                    await api.UpdateMyProfileAsync(preferences: preferences, ct: cancellationToken);
                    break;
                case "references":
                    var refs = ParseReferences(form);
                    await api.UpdateMyProfileAsync(references: refs, ct: cancellationToken);
                    break;
                case "cv":
                    var file = form.Files.GetFile("cvFile");
                    if (file is null || file.Length == 0)
                    {
                        return RedirectWithError(returnTab, culture["Common.Error.TryAgain"]);
                    }

                    await api.UploadMyCvFromFormAsync(file, cancellationToken);
                    break;
                default:
                    return Results.BadRequest();
            }
        }
        catch (ApiErrorException ex)
        {
            return RedirectWithError(returnTab, ex.UserMessage ?? culture["Common.Error.TryAgain"]);
        }
        catch (Exception)
        {
            return RedirectWithError(returnTab, culture["Common.Error.TryAgain"]);
        }

        var url = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(
            PassportRedirects.BuildPassportUrl(returnTab),
            "saved",
            "1");
        return Results.Redirect(url);
    }

    private static List<CandidateEmployerHistory> ParseEmployers(IFormCollection form)
    {
        var list = new List<CandidateEmployerHistory>();
        for (var i = 0; i < 8; i++)
        {
            var name = (form[$"empName_{i}"].ToString() ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var entryRaw = form[$"empEntryId_{i}"].ToString();
            Guid? entryId = Guid.TryParse(entryRaw, out var parsed) ? parsed : Guid.NewGuid();
            var current = string.Equals(form[$"empCurrent_{i}"].ToString(), "true", StringComparison.OrdinalIgnoreCase);
            list.Add(new CandidateEmployerHistory
            {
                EntryId = entryId,
                EmployerName = name,
                Role = NullIfEmpty(form[$"empRole_{i}"].ToString()),
                StartMonth = NullIfEmpty(form[$"empStart_{i}"].ToString()),
                EndMonth = current ? null : NullIfEmpty(form[$"empEnd_{i}"].ToString()),
                Description = NullIfEmpty(form[$"empDesc_{i}"].ToString()),
                IsCurrent = current
            });
        }

        return list;
    }

    private static List<string> ParseEducations(IFormCollection form)
    {
        return form["education"]
            .Select(v => v?.ToString() ?? "")
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<CandidateCertificate> ParseCertificates(IFormCollection form)
    {
        var list = new List<CandidateCertificate>();
        for (var i = 0; i < 10; i++)
        {
            var name = (form[$"certName_{i}"].ToString() ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            int? year = int.TryParse(form[$"certYear_{i}"].ToString(), out var y) ? y : null;
            list.Add(new CandidateCertificate { Name = name, Year = year });
        }

        return list.Take(30).ToList();
    }

    private static List<CandidateReferenceItem> ParseReferences(IFormCollection form)
    {
        var list = new List<CandidateReferenceItem>();
        for (var i = 0; i < 3; i++)
        {
            var employer = (form[$"refEmployer_{i}"].ToString() ?? "").Trim();
            var contact = (form[$"refContact_{i}"].ToString() ?? "").Trim();
            var email = (form[$"refEmail_{i}"].ToString() ?? "").Trim();
            var phone = (form[$"refPhone_{i}"].ToString() ?? "").Trim();
            if (string.IsNullOrWhiteSpace(employer)
                && string.IsNullOrWhiteSpace(contact)
                && string.IsNullOrWhiteSpace(email)
                && string.IsNullOrWhiteSpace(phone))
            {
                continue;
            }

            list.Add(new CandidateReferenceItem
            {
                EmployerName = employer,
                ContactName = contact,
                Email = email,
                Phone = phone
            });
        }

        return list;
    }

    private static string? NullIfEmpty(string? value)
    {
        var t = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(t) ? null : t;
    }

    private static IResult RedirectWithError(string tab, string message)
    {
        var url = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(
            PassportRedirects.BuildPassportUrl(tab),
            "error",
            message);
        return Results.Redirect(url);
    }
}
