namespace Jobsy.Core.Entities.WerkgeverApk;

/// <summary>Manager recognition sheet (10 Likert answers) for one employee code.</summary>
public class WerkgeverApkManagerSheet
{
    public Guid EmployeeCodeId { get; set; }
    public WerkgeverApkEmployeeCode? EmployeeCode { get; set; }

    /// <summary>JSON map question id (q1..q10) → Likert 1–5.</summary>
    public string AnswersJson { get; set; } = "{}";

    public bool Completed { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}