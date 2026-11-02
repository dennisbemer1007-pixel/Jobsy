using Jobsy.Core.Scholen.PupilPassport;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Scholen;

public static class PupilPaspoortSsrHtml
{
    public static string BuildPage(CultureState culture, PupilG78PassportWebDto model, string? displayName, string pdfHref)
    {
        var passport = PupilG78PassportSsrHtml.Render(model with { DisplayName = displayName ?? model.DisplayName }, culture);
        return $"""
            <div class="ll-reis" data-testid="leerling-paspoort-page">
            {passport}
            <nav class="lp-g78-paspoort-actions" data-testid="leerling-paspoort-actions">
            <a class="btn-secondary" href="/leerling/dit-ben-jij">{PupilLeerlingSsrParts.E(culture["LeerlingPassport.Page.Back"])}</a>
            <a class="btn-secondary" href="{PupilLeerlingSsrParts.E(pdfHref)}" data-enhance-nav="false">{PupilLeerlingSsrParts.E(culture["LeerlingPassport.Page.Pdf"])}</a>
            <a class="btn-primary" href="/leerling/droombaan">{PupilLeerlingSsrParts.E(culture["LeerlingStory.CheckDream"])}</a>
            </nav></div>
            """;
    }
}
