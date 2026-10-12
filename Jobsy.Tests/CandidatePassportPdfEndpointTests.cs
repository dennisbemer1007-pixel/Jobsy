using Jobsy.Web.Hosting;

namespace Jobsy.Tests;

public sealed class CandidatePassportPdfEndpointTests
{
    [Fact]
    public void Lobsy_cv_download_path_is_under_web_origin()
    {
        Assert.Equal("/candidate/download/lobsy-cv.pdf", CandidatePassportPdfEndpoints.LobsyCvPath);
    }
}
