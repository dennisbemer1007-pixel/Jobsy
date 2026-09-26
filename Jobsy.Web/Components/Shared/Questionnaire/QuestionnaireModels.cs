namespace Jobsy.Web.Components.Shared.Questionnaire;

public enum QuestionnaireSaveStatus
{
    Saved,
    Saving,
    Failed
}

public sealed class QuestionnaireQuestion
{
    public required int Id { get; init; }
    public required string Text { get; init; }
    public required string CategoryKey { get; init; }
    public required string CategoryLabel { get; init; }
    public string? ExampleText { get; init; }
    public string? ExampleLinkLabel { get; init; }
}

public sealed class QuestionnaireCategoryGroup
{
    public required string Key { get; init; }
    public required string Label { get; init; }
    public string? Hint { get; init; }
    public required IReadOnlyList<QuestionnaireQuestion> Questions { get; init; }
}
