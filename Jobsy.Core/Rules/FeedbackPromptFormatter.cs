using System.Text;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

public static class FeedbackPromptFormatter
{
    public static string BranchNameFor(Guid feedbackId)
        => $"fix/feedback-{feedbackId:N}"[..("fix/feedback-".Length + 8)];

    public static string Build(PlatformFeedback feedback, string targetRef = "main")
    {
        ArgumentNullException.ThrowIfNull(feedback);

        var branch = string.IsNullOrWhiteSpace(feedback.BranchName)
            ? BranchNameFor(feedback.Id)
            : feedback.BranchName.Trim();

        var sb = new StringBuilder();
        sb.AppendLine("# Lobsy feedback-taak");
        sb.AppendLine();
        sb.AppendLine("Los de onderstaande gebruikersfeedback op in deze repository en open een Pull Request.");
        sb.AppendLine("Werk in de bestaande .NET 9 / Blazor-architectuur (Jobsy.Api, Jobsy.Web, Jobsy.Core, Jobsy.Infrastructure, Jobsy.Tests).");
        sb.AppendLine("Volg bestaande patronen voor authz (FallbackPolicy, rollen), AVG (geen extra PII, geen plaintext e-mail in logs) en tests.");
        sb.AppendLine();
        sb.AppendLine("## Feedback");
        sb.AppendLine($"- **ID:** `{feedback.Id:D}`");
        sb.AppendLine($"- **Type:** {TypeLabel(feedback.Type)}");
        sb.AppendLine($"- **Status:** {StatusLabel(feedback.Status)}");
        sb.AppendLine($"- **Ingediend:** {feedback.CreatedAtUtc:yyyy-MM-dd HH:mm} UTC");
        sb.AppendLine($"- **Pagina-URL:** {NullDash(feedback.PageUrl)}");
        sb.AppendLine($"- **Gebruikersrol:** {NullDash(feedback.UserRole)}");
        sb.AppendLine($"- **Gebruiker:** {NullDash(feedback.UserDisplayName)}");
        sb.AppendLine($"- **Browser:** {NullDash(feedback.BrowserInfo)}");
        sb.AppendLine($"- **Device:** {NullDash(feedback.DeviceInfo)}");
        sb.AppendLine($"- **Screenshot:** {(feedback.ScreenshotBytes is { Length: > 0 } ? "bijgevoegd bij deze taak" : "niet beschikbaar")}");
        sb.AppendLine();
        sb.AppendLine("## Omschrijving");
        sb.AppendLine(string.IsNullOrWhiteSpace(feedback.Description)
            ? "(geen omschrijving)"
            : feedback.Description.Trim());
        sb.AppendLine();
        sb.AppendLine("## Waarschijnlijke code-locaties");
        foreach (var hint in HintPathsFor(feedback.PageUrl))
        {
            sb.AppendLine($"- `{hint}`");
        }

        sb.AppendLine();
        sb.AppendLine("## Git / PR");
        sb.AppendLine($"- Maak branch `{branch}` vanaf `{targetRef}` (acceptatie-omgeving wanneer `acc`).");
        sb.AppendLine($"- Open een PR naar `{targetRef}` met een duidelijke titel die het feedback-ID noemt.");
        sb.AppendLine("- Voeg of update tests voor het nieuwe gedrag.");
        sb.AppendLine("- Geen secrets in de repo; geen volledige IBAN of plaintext e-mail in API/UI/logs.");
        return sb.ToString().TrimEnd();
    }

    public static IReadOnlyList<string> HintPathsFor(string? pageUrl)
    {
        var path = NormalizePath(pageUrl);
        if (path.StartsWith("/admin/feedback", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                "Jobsy.Web/Components/Pages/Admin/FeedbackAdmin.razor",
                "Jobsy.Api/Controllers/FeedbackController.cs",
                "Jobsy.Infrastructure/Services/FeedbackService.cs"
            ];
        }

        if (path.StartsWith("/admin", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                "Jobsy.Web/Components/Pages/Admin/",
                "Jobsy.Web/Components/Admin/",
                "Jobsy.Api/Controllers/AdminController.cs"
            ];
        }

        if (path.StartsWith("/candidate", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                "Jobsy.Web/Components/Pages/Candidate/",
                "Jobsy.Api/Controllers/MeController.cs",
                "Jobsy.Api/Controllers/ApplicationsController.cs"
            ];
        }

        if (path.StartsWith("/branch", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/employer", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/regional", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/intermediary", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                "Jobsy.Web/Components/Pages/Branch/",
                "Jobsy.Web/Components/Pages/Employer/",
                "Jobsy.Api/Controllers/VacanciesController.cs",
                "Jobsy.Api/Controllers/ApplicationsController.cs"
            ];
        }

        if (path.StartsWith("/salesmanager", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/ambassadeur", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/partner", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                "Jobsy.Web/Components/Pages/Ambassadeur/",
                "Jobsy.Api/Controllers/SalesManagersController.cs",
                "Jobsy.Api/Controllers/PartnerAffiliateController.cs"
            ];
        }

        if (path is "/" or "/banen" || path.StartsWith("/vacancies", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                "Jobsy.Web/Components/Pages/Banen.razor",
                "Jobsy.Web/wwwroot/js/jobMap.js",
                "Jobsy.Api/Controllers/VacanciesController.cs"
            ];
        }

        if (path.StartsWith("/login", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/register", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/account", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                "Jobsy.Web/Components/Pages/",
                "Jobsy.Api/Controllers/AuthController.cs",
                "Jobsy.Api/Controllers/RegistrationController.cs"
            ];
        }

        return
        [
            "Jobsy.Web/Components/Pages/",
            "Jobsy.Web/Components/Layout/MainLayout.razor",
            "Jobsy.Api/Controllers/"
        ];
    }

    public static string TypeLabel(FeedbackType type) => type switch
    {
        FeedbackType.Bug => "Bug",
        FeedbackType.Error => "Error",
        FeedbackType.Feature => "Feature",
        _ => type.ToString()
    };

    public static string StatusLabel(FeedbackStatus status) => status switch
    {
        FeedbackStatus.New => "Nieuw",
        FeedbackStatus.InProgress => "In behandeling",
        FeedbackStatus.Resolved => "Opgelost",
        _ => status.ToString()
    };

    private static string NormalizePath(string? pageUrl)
    {
        if (string.IsNullOrWhiteSpace(pageUrl))
        {
            return "/";
        }

        if (Uri.TryCreate(pageUrl, UriKind.Absolute, out var absolute))
        {
            return string.IsNullOrEmpty(absolute.AbsolutePath) ? "/" : absolute.AbsolutePath;
        }

        var cut = pageUrl.Split('?', 2)[0].Split('#', 2)[0].Trim();
        return string.IsNullOrEmpty(cut) ? "/" : cut;
    }

    private static string NullDash(string? value)
        => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
}
