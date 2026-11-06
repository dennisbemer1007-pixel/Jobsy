using Jobsy.Core.Enums;
using Jobsy.Core.Security;

namespace Jobsy.Tests.Scholen;

public class ScholenMfaPolicyTests
{
    [Theory]
    [InlineData(UserRole.SchoolAdmin, true)]
    [InlineData(UserRole.Teacher, true)]
    [InlineData(UserRole.Candidate, false)]
    [InlineData(UserRole.Ambassadeur, false)]
    [InlineData(UserRole.Admin, true)]
    public void IsRequired_includes_school_staff(UserRole role, bool expected)
        => Assert.Equal(expected, MfaPolicy.IsRequired(role));
}
