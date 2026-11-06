using System.Globalization;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Scholen;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

/// <summary>SSR POST routes for school admin portal (no Blazor).</summary>
public static class SchoolPortalFormEndpoints
{
    private static readonly AuthorizeAttribute SchoolAdminOnly = new() { Roles = JobsyRoles.SchoolAdmin };

    public static IEndpointRouteBuilder MapSchoolPortalFormEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/school/klassen", CreateClassAsync).RequireAuthorization(SchoolAdminOnly);
        app.MapPost("/school/klassen/{classId:guid}", UpdateClassAsync).RequireAuthorization(SchoolAdminOnly);
        app.MapPost("/school/klassen/{classId:guid}/codes/add", AddCodesAsync).RequireAuthorization(SchoolAdminOnly);
        app.MapPost("/school/klassen/{classId:guid}/codes/{codeId:guid}/replace", ReplaceCodeAsync).RequireAuthorization(SchoolAdminOnly);
        app.MapPost("/school/klassen/{classId:guid}/codes/{codeId:guid}/delete", DeleteCodeAsync).RequireAuthorization(SchoolAdminOnly);
        app.MapPost("/school/klassen/{classId:guid}/parental", ParentalAsync).RequireAuthorization(SchoolAdminOnly);
        app.MapPost("/school/klassen/{classId:guid}/test-window", TestWindowAsync).RequireAuthorization(SchoolAdminOnly);
        app.MapPost("/school/klassen/{classId:guid}/delete", DeleteClassAsync).RequireAuthorization(SchoolAdminOnly);
        app.MapPost("/school/leraren/invite", InviteTeacherAsync).RequireAuthorization(SchoolAdminOnly);
        app.MapPost("/school/leraren/{teacherUserId:guid}/assign", AssignTeacherAsync).RequireAuthorization(SchoolAdminOnly);
        app.MapPost("/school/leraren/{teacherUserId:guid}/resend", ResendInviteAsync).RequireAuthorization(SchoolAdminOnly);
        app.MapPost("/school/leraren/{teacherUserId:guid}/remove", RemoveTeacherAsync).RequireAuthorization(SchoolAdminOnly);
        app.MapPost("/school/privacy/delete-year", DeleteYearAsync).RequireAuthorization(SchoolAdminOnly);
        return app;
    }

    private static async Task<IResult> CreateClassAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IAntiforgery antiforgery,
        CancellationToken ct)
    {
        if (!await antiforgery.IsRequestValidAsync(http))
        {
            return Redirect("/school/klassen/nieuw?error=" + Uri.EscapeDataString(culture["Common.Error.TryAgain"]));
        }

        var form = await http.Request.ReadFormAsync(ct);
        var seed = new SchoolClassFormSsrHtml.FormModel { ShowPupilCount = true };
        if (!SchoolClassFormSsrHtml.TryParseForm(form, seed, out var model, out var parseError))
        {
            return Redirect("/school/klassen/nieuw?error=" + Uri.EscapeDataString(parseError ?? PortalActionError.Fallback));
        }

        try
        {
            var created = await api.CreateSchoolClassAsync(new CreateSchoolClassRequest(
                model.ClassName,
                model.Level,
                model.Year,
                model.PupilCount,
                null,
                model.SelectedTeacherIds.ToList()), ct);
            if (created is null)
            {
                return Redirect("/school/klassen/nieuw?error=" + Uri.EscapeDataString(PortalActionError.Fallback));
            }

            return Redirect($"/school/klassen/{created.Id:D}");
        }
        catch (Exception ex)
        {
            return Redirect("/school/klassen/nieuw?error=" + Uri.EscapeDataString(PortalActionError.FromException(ex)));
        }
    }

    private static async Task<IResult> UpdateClassAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IAntiforgery antiforgery,
        Guid classId,
        CancellationToken ct)
    {
        if (!await antiforgery.IsRequestValidAsync(http))
        {
            return Redirect(ClassPath(classId, error: culture["Common.Error.TryAgain"], edit: true));
        }

        var form = await http.Request.ReadFormAsync(ct);
        var existing = await api.GetSchoolClassAsync(classId, ct);
        var seed = new SchoolClassFormSsrHtml.FormModel
        {
            ClassName = existing?.ClassName ?? "",
            Level = existing?.Level ?? Core.Enums.SchoolLevel.Havo,
            Year = existing?.Year ?? 2,
            LevelLocked = existing?.LevelLocked ?? true,
            ShowPupilCount = false,
        };
        if (existing is not null)
        {
            foreach (var t in existing.Teachers)
            {
                seed.SelectedTeacherIds.Add(t.UserId);
            }
        }

        if (!SchoolClassFormSsrHtml.TryParseForm(form, seed, out var model, out var parseError))
        {
            return Redirect(ClassPath(classId, error: parseError, edit: true));
        }

        try
        {
            _ = await api.UpdateSchoolClassAsync(classId, new UpdateSchoolClassRequest(
                model.ClassName,
                model.Level,
                model.Year,
                model.SelectedTeacherIds.ToList()), ct);
            return Redirect(ClassPath(classId));
        }
        catch (Exception ex)
        {
            var apiText = PortalActionError.FromException(ex);
            var msg = apiText.Contains("ligt vast", StringComparison.Ordinal)
                ? culture["School.Class.LevelLocked"]
                : apiText;
            return Redirect(ClassPath(classId, error: msg, edit: true));
        }
    }

    private static async Task<IResult> AddCodesAsync(
        HttpContext http,
        JobsyApiClient api,
        IAntiforgery antiforgery,
        Guid classId,
        CancellationToken ct)
    {
        if (!await antiforgery.IsRequestValidAsync(http))
        {
            return Redirect(ClassPath(classId, error: PortalActionError.Fallback));
        }

        try
        {
            _ = await api.AddSchoolClassCodesAsync(classId, 1, ct);
            return Redirect(ClassPath(classId));
        }
        catch (Exception ex)
        {
            return Redirect(ClassPath(classId, error: PortalActionError.FromException(ex)));
        }
    }

    private static async Task<IResult> ReplaceCodeAsync(
        HttpContext http,
        JobsyApiClient api,
        IAntiforgery antiforgery,
        Guid classId,
        Guid codeId,
        CancellationToken ct)
    {
        if (!await antiforgery.IsRequestValidAsync(http))
        {
            return Redirect(ClassPath(classId, error: PortalActionError.Fallback));
        }

        try
        {
            _ = await api.ReplaceSchoolCodeAsync(classId, codeId, ct);
            return Redirect(ClassPath(classId));
        }
        catch (Exception ex)
        {
            return Redirect(ClassPath(classId, error: PortalActionError.FromException(ex)));
        }
    }

    private static async Task<IResult> DeleteCodeAsync(
        HttpContext http,
        JobsyApiClient api,
        IAntiforgery antiforgery,
        Guid classId,
        Guid codeId,
        CancellationToken ct)
    {
        if (!await antiforgery.IsRequestValidAsync(http))
        {
            return Redirect(ClassPath(classId, error: PortalActionError.Fallback));
        }

        try
        {
            await api.DeleteSchoolCodeAsync(classId, codeId, ct);
            return Redirect(ClassPath(classId));
        }
        catch (Exception ex)
        {
            return Redirect(ClassPath(classId, error: PortalActionError.FromException(ex)));
        }
    }

    private static async Task<IResult> ParentalAsync(
        HttpContext http,
        JobsyApiClient api,
        IAntiforgery antiforgery,
        Guid classId,
        CancellationToken ct)
    {
        if (!await antiforgery.IsRequestValidAsync(http))
        {
            return Redirect(ClassPath(classId, error: PortalActionError.Fallback));
        }

        var form = await http.Request.ReadFormAsync(ct);
        var confirmed = string.Equals(form["confirmed"].ToString(), "true", StringComparison.OrdinalIgnoreCase);
        if (confirmed && !form.ContainsKey("parentsCheck"))
        {
            return Redirect(ClassPath(classId, error: "Bevestig eerst via het vinkje."));
        }

        try
        {
            _ = await api.ConfirmSchoolParentalAsync(classId, confirmed, ct);
            return Redirect(ClassPath(classId));
        }
        catch (Exception ex)
        {
            return Redirect(ClassPath(classId, error: PortalActionError.FromException(ex)));
        }
    }

    private static async Task<IResult> TestWindowAsync(
        HttpContext http,
        JobsyApiClient api,
        IAntiforgery antiforgery,
        Guid classId,
        CancellationToken ct)
    {
        if (!await antiforgery.IsRequestValidAsync(http))
        {
            return Redirect(ClassPath(classId, windowError: PortalActionError.Fallback));
        }

        var form = await http.Request.ReadFormAsync(ct);
        var action = form["action"].ToString().Trim();
        DateOnly? closesOn = null;
        var closesRaw = form["closesOn"].ToString();
        if (!string.IsNullOrWhiteSpace(closesRaw)
            && DateOnly.TryParse(closesRaw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            closesOn = parsed;
        }

        try
        {
            _ = await api.SetSchoolTestWindowAsync(classId, action, closesOn, ct);
            return Redirect(ClassPath(classId));
        }
        catch (Exception ex)
        {
            return Redirect(ClassPath(classId, windowError: PortalActionError.FromException(ex)));
        }
    }

    private static async Task<IResult> DeleteClassAsync(
        HttpContext http,
        JobsyApiClient api,
        IAntiforgery antiforgery,
        Guid classId,
        CancellationToken ct)
    {
        if (!await antiforgery.IsRequestValidAsync(http))
        {
            return Redirect(ClassPath(classId, error: PortalActionError.Fallback));
        }

        var form = await http.Request.ReadFormAsync(ct);
        var confirmName = form["confirmName"].ToString();
        try
        {
            await api.DeleteSchoolClassAsync(classId, confirmName, ct);
            return Redirect("/school/klassen");
        }
        catch (Exception ex)
        {
            return Redirect(ClassPath(classId, error: PortalActionError.FromException(ex)));
        }
    }

    private static async Task<IResult> InviteTeacherAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IAntiforgery antiforgery,
        CancellationToken ct)
    {
        if (!await antiforgery.IsRequestValidAsync(http))
        {
            return Redirect("/school/leraren?invite=1&error=" + Uri.EscapeDataString(culture["Common.Error.TryAgain"]));
        }

        var form = await http.Request.ReadFormAsync(ct);
        var name = form["name"].ToString().Trim();
        var email = form["email"].ToString().Trim();
        var classIds = form["classIds"].Select(v => Guid.TryParse(v, out var id) ? id : Guid.Empty).Where(id => id != Guid.Empty).ToList();
        var validation = ValidateInvite(name, email, classIds, await api.GetSchoolProfileAsync(ct));
        if (validation is not null)
        {
            return Redirect("/school/leraren?invite=1&error=" + Uri.EscapeDataString(validation));
        }

        try
        {
            await api.InviteSchoolTeacherAsync(new InviteTeacherRequest(name, email, classIds), ct);
            return Redirect("/school/leraren");
        }
        catch (Exception ex)
        {
            return Redirect("/school/leraren?invite=1&error=" + Uri.EscapeDataString(PortalActionError.FromException(ex)));
        }
    }

    private static async Task<IResult> AssignTeacherAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IAntiforgery antiforgery,
        Guid teacherUserId,
        CancellationToken ct)
    {
        if (!await antiforgery.IsRequestValidAsync(http))
        {
            return Redirect($"/school/leraren?assign={teacherUserId:D}&error=" + Uri.EscapeDataString(culture["Common.Error.TryAgain"]));
        }

        var form = await http.Request.ReadFormAsync(ct);
        var classIds = form["classIds"].Select(v => Guid.TryParse(v, out var id) ? id : Guid.Empty).Where(id => id != Guid.Empty).ToList();
        try
        {
            await api.AssignSchoolTeacherClassesAsync(teacherUserId, classIds, ct);
            return Redirect("/school/leraren");
        }
        catch (Exception ex)
        {
            return Redirect($"/school/leraren?assign={teacherUserId:D}&error=" + Uri.EscapeDataString(PortalActionError.FromException(ex)));
        }
    }

    private static async Task<IResult> ResendInviteAsync(
        HttpContext http,
        JobsyApiClient api,
        IAntiforgery antiforgery,
        Guid teacherUserId,
        CancellationToken ct)
    {
        if (!await antiforgery.IsRequestValidAsync(http))
        {
            return Redirect("/school/leraren?error=" + Uri.EscapeDataString(PortalActionError.Fallback));
        }

        try
        {
            await api.ResendSchoolTeacherInviteAsync(teacherUserId, ct);
        }
        catch
        {
            // Same as Blazor: reload silently
        }

        return Redirect("/school/leraren");
    }

    private static async Task<IResult> RemoveTeacherAsync(
        HttpContext http,
        JobsyApiClient api,
        IAntiforgery antiforgery,
        Guid teacherUserId,
        CancellationToken ct)
    {
        if (!await antiforgery.IsRequestValidAsync(http))
        {
            return Redirect("/school/leraren?error=" + Uri.EscapeDataString(PortalActionError.Fallback));
        }

        try
        {
            await api.RemoveSchoolTeacherAsync(teacherUserId, ct);
        }
        catch
        {
        }

        return Redirect("/school/leraren");
    }

    private static async Task<IResult> DeleteYearAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IAntiforgery antiforgery,
        CancellationToken ct)
    {
        if (!await antiforgery.IsRequestValidAsync(http))
        {
            return Redirect("/school/privacy?error=" + Uri.EscapeDataString(culture["Common.Error.TryAgain"]));
        }

        var form = await http.Request.ReadFormAsync(ct);
        var phrase = form["confirmPhrase"].ToString();
        try
        {
            await api.DeleteSchoolYearDataAsync(phrase, ct);
            return Redirect("/school/privacy?done=1");
        }
        catch (Exception ex)
        {
            return Redirect("/school/privacy?error=" + Uri.EscapeDataString(PortalActionError.FromException(ex)));
        }
    }

    private static string? ValidateInvite(string name, string email, IReadOnlyList<Guid> classIds, SchoolProfileDto? profile)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Vul een naam in.";
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return "Vul een school-e-mail in.";
        }

        var at = email.LastIndexOf('@');
        if (at <= 0 || at != email.IndexOf('@') || email.Any(char.IsWhiteSpace) || !email[(at + 1)..].Contains('.'))
        {
            return "Dit e-mailadres klopt niet.";
        }

        var allowed = profile?.AllowedEmailDomains ?? [];
        if (allowed.Count > 0)
        {
            var domain = email[(at + 1)..];
            if (!allowed.Any(d => string.Equals(d.Trim(), domain, StringComparison.OrdinalIgnoreCase)))
            {
                return "Gebruik een e-mailadres van de school.";
            }
        }

        if (classIds.Count == 0)
        {
            return "Koppel minstens één klas.";
        }

        return null;
    }

    private static string ClassPath(Guid classId, string? error = null, string? windowError = null, bool edit = false)
    {
        var q = new List<string>();
        if (edit)
        {
            q.Add("edit=1");
        }

        if (!string.IsNullOrEmpty(error))
        {
            q.Add("error=" + Uri.EscapeDataString(error));
        }

        if (!string.IsNullOrEmpty(windowError))
        {
            q.Add("windowError=" + Uri.EscapeDataString(windowError));
        }

        var basePath = $"/school/klassen/{classId:D}";
        return q.Count == 0 ? basePath : basePath + "?" + string.Join("&", q);
    }

    private static IResult Redirect(string url) => Results.Redirect(url, permanent: false, preserveMethod: false);
}
