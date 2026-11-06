namespace Jobsy.Web.Hosting;

/// <summary>
/// Same masking as <c>Jobsy.Infrastructure.Services.EmailServiceStub.RedactEmail</c>, duplicated
/// here because error pages never reference the Infrastructure/DB project (hard rule: error
/// pages make no API or DB call to render).
/// </summary>
public static class AccountMasking
{
    public static string RedactEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "";
        }

        var at = email.IndexOf('@');
        if (at <= 1)
        {
            return "***";
        }

        return email[0] + "***" + email[at..];
    }
}
