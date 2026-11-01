namespace Jobsy.Core.Scholen;

/// <summary>Placeholder bank for registry wiring; MBO/HBO items live in <see cref="MboHbo.PupilMboHboCatalog"/>.</summary>
public sealed class PupilQuestionBankMboHbo : PupilQuestionBankBase
{
    public PupilQuestionBankMboHbo()
        : base([], version: 1)
    {
    }
}
