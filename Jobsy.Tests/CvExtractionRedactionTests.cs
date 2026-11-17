using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class CvExtractionRedactionTests
{
    [Fact]
    public void Redact_strips_email_phone_and_bsn()
    {
        const string cv = """
            Jan Jansen
            jan.jansen@example.com
            06-12345678
            BSN 123456782
            Werkervaring bij Bakkerij Noord
            """;

        var redacted = CvExtractionRedaction.Redact(cv, "Jan", "Jansen");

        Assert.DoesNotContain("jan.jansen@example.com", redacted, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("12345678", redacted, StringComparison.Ordinal);
        Assert.DoesNotContain("123456782", redacted, StringComparison.Ordinal);
        Assert.Contains(CvExtractionRedaction.EmailToken, redacted, StringComparison.Ordinal);
        Assert.Contains(CvExtractionRedaction.PhoneToken, redacted, StringComparison.Ordinal);
        Assert.Contains("Bakkerij Noord", redacted, StringComparison.Ordinal);
    }
}
