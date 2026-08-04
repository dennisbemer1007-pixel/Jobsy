using Jobsy.Core.Entities;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class ExclusivityRulesTests
{
    [Fact]
    public void Open_option_skips_applicant_extras()
    {
        var setting = new ExclusivitySetting
        {
            Name = ExclusivityRules.DefaultOpenName,
            IsOpenOption = true,
            IsActive = true
        };

        Assert.False(ExclusivityRules.RequiresApplicantExtras(setting));
        Assert.Null(ExclusivityRules.ValidateApplicantExtras(setting, null, null, null));
    }

    [Fact]
    public void Exclusive_requires_matching_school_email_and_student_number()
    {
        var setting = new ExclusivitySetting
        {
            Name = "Exclusief voor Inholland",
            IsOpenOption = false,
            IsActive = true,
            SchoolDomain = "student.inholland.nl",
            StudentNumberPattern = @"^\d{7,8}$",
            Educations =
            [
                new ExclusivityEducation { Name = "Informatica", IsActive = true }
            ]
        };

        Assert.Contains("school", ExclusivityRules.ValidateSchoolEmail(setting, "persoonlijk@gmail.com")!, StringComparison.OrdinalIgnoreCase);
        Assert.Null(ExclusivityRules.ValidateSchoolEmail(setting, "jan@student.inholland.nl"));
        Assert.Contains("studentnummer", ExclusivityRules.ValidateStudentNumber(setting, "abc")!, StringComparison.OrdinalIgnoreCase);
        Assert.Null(ExclusivityRules.ValidateStudentNumber(setting, "12345678"));
        Assert.Contains("opleiding", ExclusivityRules.ValidateStudyProgram(setting, "Economie")!, StringComparison.OrdinalIgnoreCase);
        Assert.Null(ExclusivityRules.ValidateStudyProgram(setting, "Informatica"));
        Assert.Null(ExclusivityRules.ValidateApplicantExtras(
            setting, "12345678", "jan@student.inholland.nl", "Informatica"));
    }

    [Fact]
    public void Pattern_syntax_validation()
    {
        Assert.Null(ExclusivityRules.ValidatePatternSyntax(@"^\d{7,8}$"));
        Assert.NotNull(ExclusivityRules.ValidatePatternSyntax("["));
    }
}
