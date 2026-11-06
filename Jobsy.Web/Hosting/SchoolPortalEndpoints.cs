using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Scholen;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

/// <summary>SSR GET routes for school admin portal (no Blazor).</summary>
public static class SchoolPortalEndpoints
{
    private static readonly AuthorizeAttribute SchoolAdminOnly = new() { Roles = JobsyRoles.SchoolAdmin };

    public static IEndpointRouteBuilder MapSchoolPortalEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/school", DashboardAsync).RequireAuthorization(SchoolAdminOnly);
        app.MapGet("/school/te-doen", TodosAsync).RequireAuthorization(SchoolAdminOnly);
        app.MapGet("/school/klassen", ClassesAsync).RequireAuthorization(SchoolAdminOnly);
        app.MapGet("/school/klassen/nieuw", ClassCreateAsync).RequireAuthorization(SchoolAdminOnly);
        app.MapGet("/school/klassen/{classId:guid}", ClassDetailAsync).RequireAuthorization(SchoolAdminOnly);
        app.MapGet("/school/leraren", TeachersAsync).RequireAuthorization(SchoolAdminOnly);
        app.MapGet("/school/resultaten", ResultsAsync).RequireAuthorization(SchoolAdminOnly);
        app.MapGet("/school/gegevens", DetailsAsync).RequireAuthorization(SchoolAdminOnly);
        app.MapGet("/school/materiaal", MaterialsAsync).RequireAuthorization(SchoolAdminOnly);
        app.MapGet("/school/privacy", PrivacyAsync).RequireAuthorization(SchoolAdminOnly);
        return app;
    }

    private static Task<IResult> DashboardAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IFeatureFlags features,
        CancellationToken ct)
        => SchoolPortalRender.HtmlAsync(
            http,
            api,
            culture,
            features,
            "/school",
            culture["School.DashboardTitle"],
            async () =>
            {
                try
                {
                    var dash = await api.GetSchoolDashboardAsync(ct);
                    return dash is null
                        ? SchoolPortalRender.ErrorBody(culture["School.LoadFailed"])
                        : SchoolPortalSsrHtml.Dashboard(dash, culture);
                }
                catch
                {
                    return SchoolPortalRender.ErrorBody(culture["School.LoadFailed"]);
                }
            },
            ct);

    private static Task<IResult> TodosAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IFeatureFlags features,
        CancellationToken ct)
        => SchoolPortalRender.HtmlAsync(
            http,
            api,
            culture,
            features,
            "/school/te-doen",
            culture["School.Todo.Title"],
            async () =>
            {
                var items = await api.GetSchoolTodosAsync(ct);
                return SchoolPortalSsrHtml.Todos(items, culture);
            },
            ct);

    private static Task<IResult> ClassesAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IFeatureFlags features,
        CancellationToken ct)
    {
        if (http.Request.Query.ContainsKey("create"))
        {
            return Task.FromResult(Results.Redirect("/school/klassen/nieuw"));
        }

        return SchoolPortalRender.HtmlAsync(
            http,
            api,
            culture,
            features,
            "/school/klassen",
            culture["School.Class.Title"],
            async () =>
            {
                try
                {
                    var rows = await api.GetSchoolClassesAsync(ct: ct);
                    return SchoolPortalSsrHtml.ClassList(rows, culture);
                }
                catch
                {
                    return SchoolPortalRender.ErrorBody(culture["School.LoadFailed"]);
                }
            },
            ct);
    }

    private static Task<IResult> ClassCreateAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IFeatureFlags features,
        IAntiforgery antiforgery,
        CancellationToken ct)
        => SchoolPortalRender.HtmlAsync(
            http,
            api,
            culture,
            features,
            "/school/klassen/nieuw",
            culture["School.Class.New"],
            async () =>
            {
                var model = SchoolClassFormSsrHtml.FromQuery(http.Request.Query);
                model.ShowPupilCount = true;
                var teachers = await LoadTeacherOptionsAsync(api, ct);
                if (teachers.Count == 1)
                {
                    model.SelectedTeacherIds.Add(teachers[0].UserId);
                }

                var tokens = antiforgery.GetAndStoreTokens(http);
                return SchoolPortalSsrHtml.ClassCreatePage(
                    model,
                    teachers,
                    SchoolPortalSsrHelpers.AntiforgeryField(tokens),
                    culture,
                    ResolveFormError(http));
            },
            ct);

    private static Task<IResult> ClassDetailAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IFeatureFlags features,
        IAntiforgery antiforgery,
        Guid classId,
        CancellationToken ct)
    {
        var path = $"/school/klassen/{classId:D}";
        var edit = http.Request.Query.ContainsKey("edit");
        return SchoolPortalRender.HtmlAsync(
            http,
            api,
            culture,
            features,
            path,
            culture["School.Class.Title"],
            async () =>
            {
                SchoolPortalClassDetailDto? detail;
                try
                {
                    detail = await api.GetSchoolClassAsync(classId, ct);
                }
                catch (Exception ex) when (PortalLoad.IsNotFound(ex))
                {
                    return SchoolPortalRender.ErrorBody(culture["School.Class.NotFound"]);
                }
                catch
                {
                    return SchoolPortalRender.ErrorBody(culture["School.LoadFailed"]);
                }

                if (detail is null)
                {
                    return SchoolPortalRender.ErrorBody(culture["School.Class.NotFound"]);
                }

                var tokens = antiforgery.GetAndStoreTokens(http);
                var tokenHtml = SchoolPortalSsrHelpers.AntiforgeryField(tokens);
                SchoolClassFormSsrHtml.FormModel? editModel = null;
                IReadOnlyList<SchoolClassFormSsrHtml.TeacherOption>? editTeachers = null;
                if (edit)
                {
                    editModel = new SchoolClassFormSsrHtml.FormModel
                    {
                        ClassName = detail.ClassName,
                        Level = detail.Level,
                        Year = detail.Year,
                        LevelLocked = detail.LevelLocked,
                        ShowPupilCount = false,
                    };
                    foreach (var t in detail.Teachers)
                    {
                        editModel.SelectedTeacherIds.Add(t.UserId);
                    }

                    editTeachers = await LoadTeacherOptionsAsync(api, ct);
                    if (editTeachers.Count == 0)
                    {
                        editTeachers = detail.Teachers
                            .Select(t => new SchoolClassFormSsrHtml.TeacherOption(t.UserId, t.DisplayName))
                            .ToList();
                    }
                }

                var actionError = ResolveFormError(http);
                var windowError = ResolveWindowError(http);
                return SchoolPortalSsrHtml.ClassDetail(
                    classId,
                    detail,
                    edit,
                    editModel,
                    editTeachers,
                    tokenHtml,
                    culture,
                    actionError,
                    windowError);
            },
            ct);
    }

    private static Task<IResult> TeachersAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IFeatureFlags features,
        IAntiforgery antiforgery,
        CancellationToken ct)
        => SchoolPortalRender.HtmlAsync(
            http,
            api,
            culture,
            features,
            "/school/leraren",
            culture["School.Teachers.Title"],
            async () =>
            {
                var teachers = await api.GetSchoolTeachersAsync(ct: ct);
                var classes = await api.GetSchoolClassesAsync(ct: ct);
                Guid? filterClassId = Guid.TryParse(http.Request.Query["classId"].ToString(), out var fc) ? fc : null;
                var inviteMode = http.Request.Query.ContainsKey("invite");
                Guid? assignId = Guid.TryParse(http.Request.Query["assign"].ToString(), out var aid) ? aid : null;
                var assignTarget = assignId is Guid uid ? teachers.FirstOrDefault(t => t.UserId == uid) : null;
                var assignMode = assignTarget is not null;
                var profile = await api.GetSchoolProfileAsync(ct);
                var domainHint = profile?.AllowedEmailDomains is { Count: > 0 } domains
                    ? domains[0]
                    : null;
                var tokens = antiforgery.GetAndStoreTokens(http);
                var inviteClasses = new HashSet<Guid>();
                var assignClasses = new HashSet<Guid>();
                if (assignTarget is not null)
                {
                    foreach (var c in assignTarget.Classes)
                    {
                        assignClasses.Add(c.ClassId);
                    }
                }

                return SchoolPortalSsrHtml.TeachersList(
                    teachers,
                    classes,
                    filterClassId,
                    inviteMode,
                    assignMode,
                    assignTarget,
                    inviteClasses,
                    assignClasses,
                    http.Request.Query["name"].ToString(),
                    http.Request.Query["email"].ToString(),
                    domainHint,
                    SchoolPortalSsrHelpers.AntiforgeryField(tokens),
                    culture,
                    ResolveFormError(http));
            },
            ct);

    private static Task<IResult> ResultsAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IFeatureFlags features,
        CancellationToken ct)
        => SchoolPortalRender.HtmlAsync(
            http,
            api,
            culture,
            features,
            "/school/resultaten",
            culture["School.Results.Title"],
            async () =>
            {
                IReadOnlyList<SchoolPortalClassListItemDto> classes;
                try
                {
                    classes = await api.GetSchoolClassesAsync(ct: ct);
                }
                catch
                {
                    return SchoolPortalRender.ErrorBody(culture["School.LoadFailed"]);
                }

                var sets = classes.Select(c => c.QuestionSet).Distinct().ToList();
                var hasBothSets = sets.Count > 1;
                PupilQuestionSet? filterSet = null;
                if (Enum.TryParse<PupilQuestionSet>(http.Request.Query["set"].ToString(), ignoreCase: true, out var setFromQuery))
                {
                    filterSet = setFromQuery;
                }
                else if (sets.Count > 0)
                {
                    filterSet = sets.OrderByDescending(s => classes.Count(c => c.QuestionSet == s)).First();
                }

                Guid? classId = null;
                if (Guid.TryParse(http.Request.Query["classId"].ToString(), out var cid))
                {
                    classId = cid;
                }
                else if (Guid.TryParse(http.Request.Query["klas"].ToString(), out var legacy))
                {
                    classId = legacy;
                }

                if (classId is Guid requested)
                {
                    var match = classes.FirstOrDefault(c => c.Id == requested);
                    if (match is not null)
                    {
                        filterSet = match.QuestionSet;
                    }
                }
                else
                {
                    classId = classes
                        .Where(c => filterSet is null || c.QuestionSet == filterSet)
                        .Select(c => (Guid?)c.Id)
                        .FirstOrDefault();
                }

                SchoolPortalResultsDto? results = null;
                string? classError = null;
                if (classId is Guid chosen)
                {
                    try
                    {
                        results = await api.GetSchoolClassResultsAsync(chosen, ct);
                        if (results is null)
                        {
                            classError = culture["School.Results.ClassFailed"];
                        }
                    }
                    catch
                    {
                        classError = culture["School.Results.ClassFailed"];
                    }
                }

                return SchoolPortalSsrHtml.Results(
                    classes,
                    filterSet,
                    hasBothSets,
                    classId,
                    results,
                    classError,
                    culture);
            },
            ct);

    private static Task<IResult> DetailsAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IFeatureFlags features,
        CancellationToken ct)
        => SchoolPortalRender.HtmlAsync(
            http,
            api,
            culture,
            features,
            "/school/gegevens",
            culture["School.Details.Title"],
            async () =>
            {
                var profile = await api.GetSchoolProfileAsync(ct);
                return profile is null
                    ? SchoolPortalRender.ErrorBody(culture["School.LoadFailed"])
                    : SchoolPortalSsrHtml.Details(profile, culture);
            },
            ct);

    private static Task<IResult> MaterialsAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IFeatureFlags features,
        CancellationToken ct)
        => SchoolPortalRender.HtmlAsync(
            http,
            api,
            culture,
            features,
            "/school/materiaal",
            culture["School.Material.Title"],
            () => Task.FromResult(SchoolPortalSsrHtml.Materials(culture)),
            ct);

    private static Task<IResult> PrivacyAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IFeatureFlags features,
        IAntiforgery antiforgery,
        CancellationToken ct)
        => SchoolPortalRender.HtmlAsync(
            http,
            api,
            culture,
            features,
            "/school/privacy",
            culture["School.Privacy.Title"],
            async () =>
            {
                var privacy = await api.GetSchoolPrivacyAsync(ct);
                if (privacy is null)
                {
                    return SchoolPortalRender.ErrorBody(culture["School.LoadFailed"]);
                }

                var tokens = antiforgery.GetAndStoreTokens(http);
                var done = http.Request.Query["done"].ToString() switch
                {
                    "1" => culture["School.Privacy.DeleteYearDone"],
                    _ => null
                };
                return SchoolPortalSsrHtml.Privacy(
                    privacy,
                    SchoolPortalSsrHelpers.AntiforgeryField(tokens),
                    culture,
                    ResolveFormError(http),
                    done,
                    "");
            },
            ct);

    private static async Task<List<SchoolClassFormSsrHtml.TeacherOption>> LoadTeacherOptionsAsync(
        JobsyApiClient api,
        CancellationToken ct)
    {
        try
        {
            return (await api.GetSchoolTeachersAsync(ct: ct))
                .Select(t => new SchoolClassFormSsrHtml.TeacherOption(t.UserId, t.DisplayName))
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static string? ResolveFormError(HttpContext http)
    {
        var error = http.Request.Query["error"].ToString();
        return string.IsNullOrWhiteSpace(error) ? null : PortalActionError.From(error);
    }

    private static string? ResolveWindowError(HttpContext http)
    {
        var error = http.Request.Query["windowError"].ToString();
        return string.IsNullOrWhiteSpace(error) ? null : PortalActionError.From(error);
    }
}
