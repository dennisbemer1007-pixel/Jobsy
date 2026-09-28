using System.Reflection;
using System.Text.RegularExpressions;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.ValueObjects;

namespace Jobsy.Tests;

public class CandidateInsightsDtoPiiGuardTests
{
    private static readonly Regex ForbiddenName = new(
        @"(?i)(userid|candidate(id)?$|name$|email|phone|birth|age$|address|latitude|longitude)",
        RegexOptions.Compiled);

    private static readonly HashSet<string> WhitelistedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "DensityCell.CenterLat",
        "DensityCell.CenterLng",
        "VacancyReach.Title",
        "VacancyReach.BranchName",
        "InsightsBranchRef.Name",
        "RankedItem.Label",
        "InsightsDistributionBucket.Label",
        "InsightsDistributionBucket.Key"
    };

    private static readonly HashSet<Type> ForbiddenTypes =
    [
        typeof(User),
        typeof(GeoPoint),
        typeof(Company),
        typeof(Vacancy),
        typeof(CandidateCareerPlan),
        typeof(CandidateCompetency),
        typeof(CandidateMatchSnapshot)
    ];

    [Fact]
    public void CandidateInsightsDto_has_no_pii_shaped_properties()
    {
        Walk(typeof(CandidateInsightsDto), "CandidateInsightsDto");
    }

    private static void Walk(Type type, string path)
    {
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(Guid)
            || type == typeof(DateTime) || type == typeof(DateTime?) || type == typeof(Guid?)
            || type == typeof(int?) || type == typeof(bool) || type == typeof(bool?)
            || type == typeof(double) || type == typeof(double?))
        {
            return;
        }

        if (ForbiddenTypes.Contains(type))
        {
            Assert.Fail($"Forbidden entity/value type {type.Name} at {path}");
        }

        if (type.IsArray)
        {
            Walk(type.GetElementType()!, path + "[]");
            return;
        }

        if (type.IsGenericType)
        {
            foreach (var arg in type.GetGenericArguments())
            {
                Walk(arg, path + "<" + arg.Name + ">");
            }
        }

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var propPath = $"{type.Name}.{prop.Name}";
            if (ForbiddenName.IsMatch(prop.Name) && !WhitelistedPaths.Contains(propPath))
            {
                // Scope branch names and vacancy titles are explicitly allowed.
                if (propPath is "InsightsBranchRef.Name" or "VacancyReach.Title" or "VacancyReach.BranchName"
                    or "DensityCell.CenterLat" or "DensityCell.CenterLng"
                    or "RankedItem.Label" or "InsightsDistributionBucket.Label" or "InsightsDistributionBucket.Key")
                {
                    // allowed
                }
                else
                {
                    Assert.Fail($"PII-shaped property {propPath}");
                }
            }

            Walk(prop.PropertyType, propPath);
        }
    }
}
