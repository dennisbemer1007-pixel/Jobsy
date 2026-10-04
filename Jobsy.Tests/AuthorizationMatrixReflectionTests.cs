using System.Reflection;
using System.Security.Claims;
using Jobsy.Core.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Tests;

/// <summary>
/// Reflection guard: mutating API actions must not admit RegionalManager
/// (except an explicit allow-list), and must declare authorization.
/// Depends on prompt 03 (<c>EmployerMutateRoles</c>); before 03 the five
/// talent/culture/onboarding mutators would fail this suite.
/// </summary>
public class AuthorizationMatrixReflectionTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private readonly RoleFunctionalWebAppFactory _factory;

    public AuthorizationMatrixReflectionTests(RoleFunctionalWebAppFactory factory)
        => _factory = factory;

    /// <summary>
    /// Mutating endpoints RegionalManager may call (own account hygiene only).
    /// </summary>
    public static readonly (string Controller, string Action, string Reason)[] RegionalManagerMutationAllowList =
    [
        ("MeController", "UpdateLanguage", "Own UI language"),
        ("MeController", "UpdateProfile", "Own profile fields"),
        ("MeController", "UpdateDateOfBirth", "Own date of birth"),
        ("MeController", "CompleteCandidateHowTo", "Own how-to completion"),
        ("MeController", "UploadMyCv", "Own CV upload"),
        ("MeController", "DeleteMyCv", "Own CV delete"),
        ("MeController", "AcceptConsent", "Own consent"),
        ("MeController", "AcceptTestAiConsent", "Own AI consent"),
        ("MeController", "WithdrawTestAiConsent", "Own AI consent withdraw"),
        ("MeController", "AcceptTalentPoolConsent", "Own talent-pool consent"),
        ("MeController", "WithdrawTalentPoolConsent", "Own talent-pool consent withdraw"),
        ("MeController", "RequestParentalConsent", "Own parental consent request"),
        ("DeviceSessionsController", "Create", "Own device session create"),
        ("DeviceSessionsController", "Refresh", "Own device session refresh"),
        ("DeviceSessionsController", "Revoke", "Own device session revoke"),
        ("DeviceSessionsController", "RevokeAll", "Own sessions revoke-all"),
        ("DeviceSessionsController", "CreateHandoff", "Own device handoff"),
        ("DeviceSessionsController", "CreateForLogin", "Own login device session"),
        ("DeviceSessionsController", "ExchangeHandoff", "Own device handoff exchange"),
        ("NotificationsController", "MarkRead", "Own notifications"),
        ("NotificationsController", "MarkAllRead", "Own notifications"),
        ("FeedbackController", "Submit", "Product feedback from any signed-in role"),
        ("WebPushController", "Subscribe", "Own push subscription"),
        ("WebPushController", "Unsubscribe", "Own push subscription"),
        ("MfaController", "Enroll", "Own MFA enroll"),
        ("MfaController", "Verify", "Own MFA challenge"),
        ("MfaController", "RegenerateRecoveryCodes", "Own MFA recovery codes"),
        ("MeEmailPreferencesController", "Put", "Own e-mail notification preferences"),
        ("MeReminderWhatsAppController", "Put", "Own WhatsApp reminder opt-in"),
        ("PrivacyController", "RequestUnsubscribe", "Own unsubscribe request"),
        ("PrivacyController", "ConfirmUnsubscribe", "Own unsubscribe confirm"),
        ("PrivacyController", "DeleteAccount", "Own account delete"),
        ("DashboardController", "Refresh", "Refresh own role dashboard cache"),
        ("AssistantController", "Chat", "In-app assistant (no tenant mutate)"),
        ("CandidateActionsController", "SetUnavailable", "Own availability (token)"),
        ("CandidateActionsController", "SetUnavailableAuthenticated", "Own availability"),
        ("CandidateActionsController", "WithdrawOthers", "Own application withdraw-others (token)"),
        ("CandidateActionsController", "WithdrawOthersAuthenticated", "Own application withdraw-others"),
        ("VacancyEngagementController", "RecordClick", "Anonymous/signed click signal"),
        ("VacancyEngagementController", "Like", "Own like"),
        ("VacancyEngagementController", "Unlike", "Own unlike"),
        ("VacancyEngagementController", "Share", "Own share signal"),
    ];

    [Fact]
    public async Task Policy_role_map_matches_authorization_options()
    {
        using var scope = _factory.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IAuthorizationPolicyProvider>();

        foreach (var (policyName, expectedRoles) in AuthorizationPolicyRoleMap.PolicyRoles)
        {
            if (policyName == JobsyPolicies.RequireApiKey)
            {
                var apiKeyPolicy = await provider.GetPolicyAsync(policyName);
                Assert.NotNull(apiKeyPolicy);
                continue;
            }

            var policy = await provider.GetPolicyAsync(policyName);
            Assert.NotNull(policy);

            var roleReqs = policy!.Requirements.OfType<RolesAuthorizationRequirement>().ToList();
            Assert.True(roleReqs.Count > 0, $"Policy {policyName} has no RolesAuthorizationRequirement");

            var actual = roleReqs
                .SelectMany(r => r.AllowedRoles)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(r => r, StringComparer.Ordinal)
                .ToArray();
            var expected = expectedRoles
                .Distinct(StringComparer.Ordinal)
                .OrderBy(r => r, StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void Mutating_actions_do_not_admit_RegionalManager_except_allow_list()
    {
        var allow = RegionalManagerMutationAllowList
            .Select(a => (a.Controller, a.Action))
            .ToHashSet();

        var offenders = new List<string>();
        foreach (var action in EnumerateMutatingActions())
        {
            if (!action.AdmitsRole(JobsyRoles.RegionalManager))
            {
                continue;
            }

            if (allow.Contains((action.ControllerName, action.ActionName)))
            {
                continue;
            }

            offenders.Add(
                $"{action.ControllerName}.{action.ActionName} [{action.HttpMethod}] roles=[{string.Join(',', action.EffectiveRoles.OrderBy(r => r))}]");
        }

        Assert.True(
            offenders.Count == 0,
            "Mutating actions still admit RegionalManager (add EmployerMutateRoles or allow-list with reason):\n"
            + string.Join("\n", offenders));
    }

    [Fact]
    public void Mutating_actions_without_AllowAnonymous_have_Authorize()
    {
        var missing = EnumerateMutatingActions()
            .Where(a => !a.AllowAnonymous && !a.HasAuthorize)
            .Select(a => $"{a.ControllerName}.{a.ActionName} [{a.HttpMethod}]")
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();

        // FallbackPolicy requires authenticated user, but every mutate should still declare intent.
        Assert.True(
            missing.Count == 0,
            "Mutating actions missing [Authorize] (class or action):\n" + string.Join("\n", missing));
    }

    [Fact]
    public void Allow_list_entries_exist_as_mutating_actions()
    {
        var actions = EnumerateMutatingActions()
            .Select(a => (a.ControllerName, a.ActionName))
            .ToHashSet();

        var missing = RegionalManagerMutationAllowList
            .Where(a => !actions.Contains((a.Controller, a.Action)))
            .Select(a => $"{a.Controller}.{a.Action}")
            .ToList();

        Assert.True(
            missing.Count == 0,
            "Allow-list references unknown mutating actions (rename or remove):\n"
            + string.Join("\n", missing));
    }

    private static IEnumerable<ControllerActionAuth> EnumerateMutatingActions()
    {
        var assembly = typeof(Jobsy.Api.Controllers.TalentPoolController).Assembly;
        foreach (var type in assembly.GetTypes().Where(t =>
                     t.IsClass && !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t)))
        {
            var classAuth = type.GetCustomAttributes<AuthorizeAttribute>(inherit: true).ToArray();
            var classAllowAnon = type.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true) is not null;

            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                var http = method.GetCustomAttributes()
                    .OfType<HttpMethodAttribute>()
                    .SelectMany(a => a.HttpMethods)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                if (http.Length == 0)
                {
                    continue;
                }

                var isMutating = http.Any(m =>
                    m.Equals("POST", StringComparison.OrdinalIgnoreCase)
                    || m.Equals("PUT", StringComparison.OrdinalIgnoreCase)
                    || m.Equals("PATCH", StringComparison.OrdinalIgnoreCase)
                    || m.Equals("DELETE", StringComparison.OrdinalIgnoreCase));
                if (!isMutating)
                {
                    continue;
                }

                var methodAuth = method.GetCustomAttributes<AuthorizeAttribute>(inherit: true).ToArray();
                var methodAllowAnon = method.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true) is not null;

                yield return ControllerActionAuth.Create(
                    type.Name,
                    method.Name,
                    string.Join(',', http),
                    classAuth,
                    methodAuth,
                    classAllowAnon || methodAllowAnon);
            }
        }
    }

    private sealed class ControllerActionAuth
    {
        public required string ControllerName { get; init; }
        public required string ActionName { get; init; }
        public required string HttpMethod { get; init; }
        public required bool AllowAnonymous { get; init; }
        public required bool HasAuthorize { get; init; }
        public required IReadOnlyList<string> EffectiveRoles { get; init; }
        public required bool AuthenticatedAnyRole { get; init; }

        public bool AdmitsRole(string role)
        {
            // Anonymous endpoints are not a RegionalManager privilege leak.
            if (AllowAnonymous)
            {
                return false;
            }

            // Bare [Authorize] / authenticated-any → every signed-in role, including RegionalManager.
            if (AuthenticatedAnyRole && EffectiveRoles.Count == 0)
            {
                return true;
            }

            return EffectiveRoles.Contains(role, StringComparer.Ordinal);
        }

        public static ControllerActionAuth Create(
            string controller,
            string action,
            string http,
            AuthorizeAttribute[] classAuth,
            AuthorizeAttribute[] methodAuth,
            bool allowAnonymous)
        {
            var all = classAuth.Concat(methodAuth).ToArray();
            var hasAuthorize = all.Length > 0;
            // ASP.NET AND-combines Authorize attributes: intersection of role sets.
            List<HashSet<string>>? roleSets = null;
            var authenticatedAny = false;

            foreach (var attr in all)
            {
                var roles = ExpandAuthorize(attr);
                if (roles is null)
                {
                    // [Authorize] or policy without roles → any authenticated user
                    authenticatedAny = true;
                    continue;
                }

                roleSets ??= [];
                roleSets.Add(roles);
            }

            HashSet<string> effective;
            if (roleSets is null || roleSets.Count == 0)
            {
                effective = [];
            }
            else
            {
                effective = new HashSet<string>(roleSets[0], StringComparer.Ordinal);
                for (var i = 1; i < roleSets.Count; i++)
                {
                    effective.IntersectWith(roleSets[i]);
                }
            }

            return new ControllerActionAuth
            {
                ControllerName = controller,
                ActionName = action,
                HttpMethod = http,
                AllowAnonymous = allowAnonymous,
                HasAuthorize = hasAuthorize,
                EffectiveRoles = effective.ToList(),
                AuthenticatedAnyRole = authenticatedAny && effective.Count == 0
            };
        }

        private static HashSet<string>? ExpandAuthorize(AuthorizeAttribute attr)
        {
            // PupilSession is scheme+claims only — never admits staff/RegionalManager.
            if (string.Equals(attr.Policy, JobsyPolicies.PupilSession, StringComparison.Ordinal))
            {
                return new HashSet<string>(StringComparer.Ordinal);
            }

            var roles = new HashSet<string>(StringComparer.Ordinal);
            if (!string.IsNullOrWhiteSpace(attr.Roles))
            {
                foreach (var part in attr.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    roles.Add(part);
                }
            }

            if (!string.IsNullOrWhiteSpace(attr.Policy)
                && AuthorizationPolicyRoleMap.PolicyRoles.TryGetValue(attr.Policy, out var policyRoles))
            {
                foreach (var r in policyRoles)
                {
                    roles.Add(r);
                }
            }

            // Bare [Authorize] or unknown empty policy → any authenticated.
            if (roles.Count == 0
                && string.IsNullOrWhiteSpace(attr.Roles)
                && (string.IsNullOrWhiteSpace(attr.Policy)
                    || !AuthorizationPolicyRoleMap.PolicyRoles.ContainsKey(attr.Policy!)))
            {
                return null;
            }

            return roles;
        }
    }
}
