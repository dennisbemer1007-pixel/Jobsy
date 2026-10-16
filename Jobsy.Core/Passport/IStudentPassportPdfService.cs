using Jobsy.Core.Entities;

namespace Jobsy.Core.Passport;

public interface IStudentPassportPdfService
{
    Task<(byte[] Pdf, string FileName)> RenderForCandidateAsync(
        User user,
        bool phoneVerificationRequired,
        CancellationToken cancellationToken = default);
}
