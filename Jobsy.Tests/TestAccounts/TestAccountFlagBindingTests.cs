using System.Reflection;

namespace Jobsy.Tests.TestAccounts;

public class TestAccountFlagBindingTests
{
    [Fact]
    public void Api_request_models_do_not_bind_test_flags()
    {
        // Response DTOs may expose the flag to the Web (login claim / admin badge).
        // Request bodies and *Request records must never bind it.
        var asm = typeof(Jobsy.Api.Models.LocalLoginRequest).Assembly;
        var offenders = asm.GetTypes()
            .Where(t => t.IsClass || t.IsValueType)
            .Where(t => t.Namespace?.StartsWith("Jobsy.Api.Models", StringComparison.Ordinal) == true
                        || t.Namespace?.StartsWith("Jobsy.Api.Contracts", StringComparison.Ordinal) == true)
            .Where(t => t.Name.EndsWith("Request", StringComparison.Ordinal)
                        || t.Name.EndsWith("Update", StringComparison.Ordinal)
                        || t.Name.EndsWith("Create", StringComparison.Ordinal)
                        || t.Name.EndsWith("Command", StringComparison.Ordinal))
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            .Where(p => p.Name is "IsTestAccount" or "IsTestData")
            .Select(p => $"{p.DeclaringType?.FullName}.{p.Name}")
            .ToList();

        Assert.True(offenders.Count == 0, string.Join(", ", offenders));
    }
}
