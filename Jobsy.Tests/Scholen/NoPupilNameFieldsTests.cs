using System.Reflection;
using System.Text.RegularExpressions;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities.Scholen;

namespace Jobsy.Tests.Scholen;

/// <summary>D3: no pupil name/contact-like fields on Scholen entities or DTOs.</summary>
public class NoPupilNameFieldsTests
{
    private static readonly Regex Forbidden = new(
        @"(?i)(name|naam|voornaam|achternaam|firstname|lastname|email|phone|telefoon|birth|geboorte|address|adres)",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly HashSet<string> Allow = new(StringComparer.Ordinal)
    {
        "School.Name",
        "SchoolClass.Name",
        "SchoolClassAggregate.ClassLabel",
        "SchoolClassSummaryDto.ClassName",
        "SchoolClassSummaryDto.SchoolName",
        "SchoolListItemDto.Name",
        "SchoolDetailDto.Name",
        "CreateSchoolRequest.Name",
        "UpdateSchoolRequest.Name",
        "SchoolAdminListItemDto.TeacherDisplayName",
        "SchoolAdminListItemDto.Email",
        "InviteSchoolAdminRequest.FullName",
        "InviteSchoolAdminRequest.Email",
        "InviteTeacherRequest.FullName",
        "InviteTeacherRequest.Email",
        "SchoolStaffInvite.FullName",
        "SchoolStaffInvite.Email",
        "SchoolStaffInviteResultDto.Email",
        "SchoolStaffInviteResult.Email",
        "SchoolStaffInviteResult.FullName",
    };

    [Fact]
    public void Scholen_entities_and_dtos_have_no_pupil_name_fields()
    {
        var types = typeof(School).Assembly.GetTypes()
            .Where(t => t.Namespace == "Jobsy.Core.Entities.Scholen")
            .Concat(typeof(SchoolListItemDto).Assembly.GetTypes()
                .Where(t => t.Namespace == "Jobsy.Core.Contracts.Scholen"))
            .Where(t => t.IsClass || t.IsValueType)
            .ToList();

        Assert.NotEmpty(types);
        var offenders = new List<string>();
        foreach (var type in types)
        {
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var key = $"{type.Name}.{prop.Name}";
                if (Allow.Contains(key))
                {
                    continue;
                }

                if (Forbidden.IsMatch(prop.Name))
                {
                    offenders.Add(key);
                }
            }
        }

        Assert.True(offenders.Count == 0, "Forbidden pupil-like fields:\n" + string.Join("\n", offenders));
    }
}
