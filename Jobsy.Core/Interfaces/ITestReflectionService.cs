using Jobsy.Core.Enums;
using Jobsy.Core.Reports.TestReflection;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

public interface ITestReflectionService
{
    Task<TestReflectionState> GetFullMirrorAsync(
        Guid userId,
        AssessmentKind kind,
        string language,
        CancellationToken cancellationToken = default);

    Task SaveFeedbackAsync(
        Guid userId,
        AssessmentKind kind,
        TestDepthLevel level,
        TestReflectionFeedback feedback,
        CancellationToken cancellationToken = default);
}
