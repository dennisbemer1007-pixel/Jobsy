using Jobsy.Core.Reports.TestReflection;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

public interface ITestReflectionAiService
{
    Task<TestReflectionFull?> TryRewriteAsync(
        TestReflectionFull template,
        CandidateFactSheet sheet,
        CancellationToken cancellationToken = default);
}
