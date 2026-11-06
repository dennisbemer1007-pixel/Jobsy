using System.Reflection;
using Jobsy.Api.Authorization;
using Jobsy.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Jobsy.Tests;

public class VerifiedCompanyGateCoverageTests
{
    [Fact]
    public void Gated_actions_carry_RequiresVerifiedCompany()
    {
        var required = new HashSet<string>(StringComparer.Ordinal)
        {
            $"{nameof(VacanciesController)}.{nameof(VacanciesController.Publish)}",
            $"{nameof(VacanciesController)}.{nameof(VacanciesController.Highlight)}",
            $"{nameof(VacanciesController)}.{nameof(VacanciesController.PushBom)}",
            $"{nameof(VacanciesController)}.{nameof(VacanciesController.Extend)}",
            $"{nameof(TokensController)}.{nameof(TokensController.CreateCheckout)}",
            $"{nameof(TokensController)}.{nameof(TokensController.CompleteCheckout)}",
            $"{nameof(ApplicationsController)}.{nameof(ApplicationsController.GetForManagedCompanies)}",
            $"{nameof(ApplicationsController)}.{nameof(ApplicationsController.React)}",
            $"{nameof(ApplicationsController)}.{nameof(ApplicationsController.MarkEmployerContact)}",
            $"{nameof(ApplicationsController)}.{nameof(ApplicationsController.FulfillVacancy)}",
            $"{nameof(TalentPoolController)}.Search",
            $"{nameof(TalentPoolController)}.Unlock",
            $"{nameof(TalentPoolController)}.Withdraw",
            $"{nameof(TalentPoolController)}.ListRequests",
            $"{nameof(CandidateInsightsController)}.Get",
            $"{nameof(CandidateInsightsController)}.Branches",
        };

        var found = CollectAttributedActions().ToHashSet(StringComparer.Ordinal);
        foreach (var key in required)
        {
            Assert.True(found.Contains(key), $"Missing [RequiresVerifiedCompany] on {key}");
        }
    }

    [Fact]
    public void Allowed_actions_do_not_carry_RequiresVerifiedCompany()
    {
        var forbidden = new HashSet<string>(StringComparer.Ordinal)
        {
            $"{nameof(VacanciesController)}.{nameof(VacanciesController.Create)}",
            $"{nameof(VacanciesController)}.{nameof(VacanciesController.Update)}",
            $"{nameof(VacanciesController)}.{nameof(VacanciesController.MarkReady)}",
            $"{nameof(VacanciesController)}.{nameof(VacanciesController.ClearReady)}",
            $"{nameof(VacanciesController)}.{nameof(VacanciesController.GetManaged)}",
            $"{nameof(VacanciesController)}.{nameof(VacanciesController.Deactivate)}",
            $"{nameof(TokensController)}.{nameof(TokensController.GetBalances)}",
            $"{nameof(CompanyUsersController)}.Invite",
        };

        var found = CollectAttributedActions().ToHashSet(StringComparer.Ordinal);
        foreach (var key in forbidden)
        {
            // Soft match: only assert when the method exists under that name.
            if (MethodExists(key))
            {
                Assert.False(found.Contains(key), $"Unexpected [RequiresVerifiedCompany] on allowed action {key}");
            }
        }
    }

    private static bool MethodExists(string key)
    {
        var parts = key.Split('.');
        if (parts.Length != 2)
        {
            return false;
        }

        var type = typeof(VacanciesController).Assembly.GetTypes()
            .FirstOrDefault(t => t.Name == parts[0]);
        return type?.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Any(m => m.Name == parts[1]) == true;
    }

    private static IEnumerable<string> CollectAttributedActions()
    {
        var controllers = typeof(VacanciesController).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(ControllerBase)));

        foreach (var type in controllers)
        {
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (!method.GetCustomAttributes().Any(a => a is HttpMethodAttribute))
                {
                    continue;
                }

                if (method.GetCustomAttributes(typeof(RequiresVerifiedCompanyAttribute), inherit: true).Any()
                    || type.GetCustomAttributes(typeof(RequiresVerifiedCompanyAttribute), inherit: true).Any())
                {
                    yield return $"{type.Name}.{method.Name}";
                }
            }
        }
    }
}
