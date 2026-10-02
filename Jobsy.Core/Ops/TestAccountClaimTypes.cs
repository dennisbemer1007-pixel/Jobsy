namespace Jobsy.Core.Ops;

/// <summary>Claim issued at login when <c>User.IsTestAccount</c> is true.</summary>
public static class TestAccountClaimTypes
{
    public const string TestAccount = "lobsy_test_account";
    public const string TestAccountValue = "1";
}
