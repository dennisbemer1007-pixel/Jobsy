namespace Jobsy.Core.Rules;

/// <summary>Deterministic step action kinds returned by the API (D17).</summary>
public enum CareerStepActionKind
{
    Courses = 0,
    AddProof = 1,
    Vacancies = 2,
    Complete = 3,
    Undo = 4
}

public static class CareerStepActionNames
{
    public static string ToApi(CareerStepActionKind kind) => kind switch
    {
        CareerStepActionKind.Courses => "Courses",
        CareerStepActionKind.AddProof => "AddProof",
        CareerStepActionKind.Vacancies => "Vacancies",
        CareerStepActionKind.Complete => "Complete",
        CareerStepActionKind.Undo => "Undo",
        _ => kind.ToString()
    };
}
