using System.Net;
using System.Text;
using Jobsy.Core.Features;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate;
using Jobsy.Web.Components.Candidate.ProfileSections;
using Jobsy.Web.Components.Ui;
using Jobsy.Web.Hosting;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;
using Jobsy.Web.Models;
using Jobsy.Web.Services;

namespace Jobsy.Web.Passport;

internal static class PassportSsrProofHtml
{
    internal sealed record Panel(
        ProofStrengthRules.StrengthView Strength,
        string? HintText,
        IReadOnlyList<JobsyApiClient.VerifiedCompetenceItem> VerifiedCompetences,
        IReadOnlyList<string> ProfileCompetenceLabels,
        IReadOnlyList<JobsyApiClient.ReferenceVerificationListItem> RefRows,
        bool RefVerificationEnabled,
        bool RefVerificationEligible,
        bool HasConfirmedReference,
        Guid? FirstEmployerEntryId,
        string UserIdForTip,
        bool DnaPassportPdf);

    internal static async Task<Panel> LoadPanelAsync(
        JobsyApiClient api,
        MeProfile profile,
        FeatureFlagSnapshot flags,
        CandidateKompasState? kompas,
        CancellationToken ct)
    {
        _ = kompas;
        var prefs = profile.Preferences ?? new CandidatePreferences();
        EnsureEmployerEntryIds(prefs.Employers);

        var snap = new ProofStrengthRules.ProofSnapshot(
            prefs.Employers.Count(e => !string.IsNullOrWhiteSpace(e.EmployerName)),
            (prefs.Educations ?? []).Count(e =>
                !string.Equals(e, EducationLevelLabels.None, StringComparison.OrdinalIgnoreCase)),
            (prefs.Certificates ?? []).Count(c => !string.IsNullOrWhiteSpace(c.Name)),
            profile.References?.Count ?? 0,
            profile.UploadedCv is not null);
        var strength = ProofStrengthRules.Build(snap);

        var refEnabled = flags.ReferenceVerificationEnabled;
        var eligible = false;
        var refRows = Array.Empty<JobsyApiClient.ReferenceVerificationListItem>();
        var verified = Array.Empty<JobsyApiClient.VerifiedCompetenceItem>();
        if (refEnabled)
        {
            try
            {
                eligible = await api.GetReferenceVerificationEligibleAsync(ct);
                refRows = (await api.GetReferenceVerificationListAsync(ct)).ToArray();
                verified = (await api.GetVerifiedCompetencesAsync(ct)).ToArray();
            }
            catch
            {
                eligible = false;
                refRows = [];
                verified = [];
            }
        }

        var hasConfirmed = refRows.Any(r =>
            r.Active && string.Equals(r.DisplayStatus, "confirmed", StringComparison.OrdinalIgnoreCase));
        var firstEntry = prefs.Employers
            .FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.EmployerName))?.EntryId;
        var roleLabels = (prefs.Roles ?? [])
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new Panel(
            strength,
            HintText: null,
            verified,
            roleLabels,
            refRows,
            refEnabled,
            eligible,
            hasConfirmed,
            firstEntry,
            profile.Id.ToString(),
            flags.PassportPdfV2Enabled);
    }

    internal static void Append(
        StringBuilder sb,
        CultureState culture,
        MeProfile profile,
        Panel panel,
        string antiforgeryToken,
        string? openProofItem)
    {
        var prefs = profile.Preferences ?? new CandidatePreferences();
        var refs = profile.References ?? [];

        sb.Append("<div class=\"passport-proof\" data-passport-proof=\"1\" data-testid=\"passport-panel-proof-content\"");
        if (!string.IsNullOrWhiteSpace(openProofItem))
        {
            sb.Append(" data-open-proof-item=\"");
            sb.Append(WebUtility.HtmlEncode(openProofItem.Trim()));
            sb.Append('"');
        }

        sb.Append('>');

        var intro = culture["Passport.Bubble.Proof"];
        sb.Append("<p class=\"passport-proof__intro\">");
        sb.Append(WebUtility.HtmlEncode(intro));
        sb.Append("</p>");
        AppendReadAloud(sb, culture, intro);

        AppendScale(sb, culture, panel);

        sb.Append("<div class=\"passport-proof__grid\">");
        AppendExperienceCard(sb, culture, prefs, panel);
        AppendEducationCard(sb, culture, profile, prefs);
        AppendReviewsCard(sb, culture, refs, profile.UploadedCv);
        sb.Append("</div>");

        AppendCompetenceBlocks(sb, culture, panel);
        AppendRefVerificationTip(sb, culture, panel);
        AppendPdfDownload(sb, culture, panel.DnaPassportPdf);

        AppendSheet(sb, culture, prefs, refs, antiforgeryToken);

        sb.Append("</div>");
        sb.Append("<script src=\"");
        sb.Append(WwwrootAssetCatalog.Url("js/passport-ssr-proof.js"));
        sb.Append("\" defer></script>");
    }

    private static void AppendReadAloud(StringBuilder sb, CultureState culture, string text)
    {
        sb.Append("<button type=\"button\" class=\"read-aloud read-aloud--icon\" data-read-aloud data-read-aloud-state=\"play\" data-read-aloud-text=\"");
        sb.Append(WebUtility.HtmlEncode(text));
        sb.Append("\" aria-pressed=\"false\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["ReadAloud.PlayAria"]));
        sb.Append("\" title=\"");
        sb.Append(WebUtility.HtmlEncode(culture["ReadAloud.Play"]));
        sb.Append("\"><span class=\"read-aloud__icon\" aria-hidden=\"true\">");
        sb.Append(LineIcons.Volume);
        sb.Append("</span></button>");
    }

    private static void AppendScale(StringBuilder sb, CultureState culture, Panel panel)
    {
        var strength = panel.Strength;
        sb.Append("<section class=\"passport-proof__scale\" aria-labelledby=\"passport-proof-scale-title\">");
        sb.Append("<div class=\"passport-proof__scale-head\"><h3 id=\"passport-proof-scale-title\" class=\"passport-proof__h\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Proof.ScaleTitle"]));
        sb.Append("</h3><p class=\"passport-proof__count\">");
        sb.Append(WebUtility.HtmlEncode(strength.Filled == 1
            ? culture["Passport.Proof.CountLabelOne"]
            : culture.Format("Passport.Proof.CountLabel", strength.Filled)));
        sb.Append("</p></div><div class=\"passport-proof__track\" role=\"img\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(strength.Filled == 1
            ? culture.Format("Passport.Proof.ScaleAriaOne", strength.Max)
            : culture.Format("Passport.Proof.ScaleAria", strength.Filled, strength.Max)));
        sb.Append("\"><span class=\"passport-proof__soft\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Proof.Soft"]));
        sb.Append("</span><div class=\"passport-proof__segs\" aria-hidden=\"true\">");
        for (var i = 0; i < strength.Max; i++)
        {
            var filled = i < strength.Filled;
            var step = Math.Min(3, 1 + (i * 2 / Math.Max(1, strength.Max - 1)));
            sb.Append("<span class=\"passport-proof__seg passport-proof__seg--");
            sb.Append(step);
            if (filled)
            {
                sb.Append(" is-filled");
            }

            sb.Append("\"></span>");
        }

        sb.Append("</div><span class=\"passport-proof__hard\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Proof.Hard"]));
        sb.Append("</span></div>");
        if (!string.IsNullOrWhiteSpace(panel.HintText))
        {
            sb.Append("<p class=\"passport-proof__hint\">");
            sb.Append(WebUtility.HtmlEncode(panel.HintText));
            sb.Append("</p>");
        }

        sb.Append("</section>");
    }

    private static void AppendExperienceCard(
        StringBuilder sb,
        CultureState culture,
        CandidatePreferences prefs,
        Panel panel)
    {
        var employers = prefs.Employers
            .Where(e => !string.IsNullOrWhiteSpace(e.EmployerName) || !string.IsNullOrWhiteSpace(e.Role))
            .ToList();

        sb.Append("<article class=\"detail-card passport-proof__card kompas-card--illustrated\">");
        sb.Append("<div class=\"kompas-card-spot\" style=\"background-image:url('");
        sb.Append(WebUtility.HtmlEncode(CandidateIllustratedImages.SpotOpleiding));
        sb.Append("');--kompas-spot-pos:80% 25%;\" aria-hidden=\"true\"></div>");
        sb.Append("<h3 class=\"passport-proof__card-title\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Proof.Experience"]));
        sb.Append("</h3>");
        if (employers.Count == 0)
        {
            sb.Append("<p class=\"muted\">");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Proof.ExperienceEmpty"]));
            sb.Append("</p>");
        }
        else
        {
            sb.Append("<ul class=\"passport-proof__list\">");
            foreach (var emp in employers)
            {
                var verified = IsVerified(panel.RefRows, emp.EntryId);
                sb.Append("<li><strong>");
                sb.Append(WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(emp.Role) ? emp.EmployerName : emp.Role));
                sb.Append("</strong>");
                if (verified)
                {
                    sb.Append("<span class=\"ref-verif-experience-badge\"><span aria-hidden=\"true\">✓</span> ");
                    sb.Append(WebUtility.HtmlEncode(culture["RefVerif.VerifiedBadge"]));
                    sb.Append("</span>");
                }

                sb.Append("<span class=\"muted\">");
                sb.Append(WebUtility.HtmlEncode(emp.EmployerName));
                if (!string.IsNullOrWhiteSpace(emp.StartMonth)
                    || !string.IsNullOrWhiteSpace(emp.EndMonth)
                    || emp.ShowsAsCurrent)
                {
                    sb.Append(" · ");
                    sb.Append(WebUtility.HtmlEncode(FormatRange(culture, emp.StartMonth, emp.EndMonth, emp.ShowsAsCurrent)));
                }

                sb.Append("</span>");
                if (!string.IsNullOrWhiteSpace(emp.Description))
                {
                    sb.Append("<span class=\"passport-proof__job-desc\">");
                    sb.Append(WebUtility.HtmlEncode(emp.Description));
                    sb.Append("</span>");
                }

                if (panel.RefVerificationEnabled && panel.RefVerificationEligible && !verified && emp.EntryId is Guid entryId)
                {
                    sb.Append("<a class=\"btn btn-secondary btn-compact\" data-testid=\"ref-verif-ask-link\" href=\"/candidate/paspoort/referentie/vraag/");
                    sb.Append(entryId.ToString("D"));
                    sb.Append("\">");
                    sb.Append(WebUtility.HtmlEncode(culture["RefVerif.AskReferent"]));
                    sb.Append("</a>");
                }

                sb.Append("</li>");
            }

            sb.Append("</ul>");
        }

        sb.Append("<button type=\"button\" class=\"btn btn-ghost\" data-testid=\"passport-proof-add-employer\" data-passport-proof-open=\"experience\" data-sheet-title=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Proof.Experience"]));
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Proof.AddEmployer"]));
        sb.Append("</button></article>");
    }

    private static void AppendEducationCard(
        StringBuilder sb,
        CultureState culture,
        MeProfile profile,
        CandidatePreferences prefs)
    {
        var educations = (prefs.Educations ?? [])
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .ToList();
        var certificates = (prefs.Certificates ?? [])
            .Where(c => !string.IsNullOrWhiteSpace(c.Name))
            .ToList();
        var learningGoals = (prefs.LearningGoals ?? [])
            .Where(g => !string.IsNullOrWhiteSpace(g))
            .Take(3)
            .ToList();

        sb.Append("<article class=\"detail-card passport-proof__card\"><h3 class=\"passport-proof__card-title\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Proof.Education"]));
        sb.Append("</h3>");
        if (educations.Count > 0)
        {
            sb.Append("<div class=\"passport-proof__pills\" aria-label=\"");
            sb.Append(WebUtility.HtmlEncode(culture["Profile.Education"]));
            sb.Append("\">");
            foreach (var level in educations)
            {
                sb.Append("<span class=\"status-pill\">");
                sb.Append(WebUtility.HtmlEncode(UiLabels.Education(culture, level)));
                sb.Append("</span>");
            }

            sb.Append("</div>");
        }

        if (!string.IsNullOrWhiteSpace(prefs.EducationDirection))
        {
            sb.Append("<p class=\"passport-proof__direction\">");
            sb.Append(WebUtility.HtmlEncode(prefs.EducationDirection));
            sb.Append("</p>");
        }

        var diplomaEvals = profile.DiplomaEvaluations ?? [];
        if (diplomaEvals.Count > 0)
        {
            sb.Append("<ul class=\"passport-proof__list diploma-eval__passport\" data-testid=\"diploma-eval-display\">");
            foreach (var fact in diplomaEvals)
            {
                sb.Append("<li>");
                if (!string.IsNullOrWhiteSpace(fact.DiplomaTitle))
                {
                    sb.Append("<span class=\"muted\">");
                    sb.Append(WebUtility.HtmlEncode(fact.DiplomaTitle));
                    sb.Append("</span>");
                }

                sb.Append("<strong>");
                sb.Append(WebUtility.HtmlEncode(fact.EquivalentLevelText));
                sb.Append("</strong><span class=\"diploma-eval__attribution\">");
                sb.Append(WebUtility.HtmlEncode(DiplomaEvalText.Attribution(culture, fact.IssuingBody, fact.IssuingBodyOther)));
                sb.Append("</span>");
                if (!string.IsNullOrWhiteSpace(fact.EquivalentLevelCode))
                {
                    sb.Append("<span class=\"diploma-eval__choice\">");
                    sb.Append(WebUtility.HtmlEncode(culture.Format(
                        "DiplomaEval.YourChoice",
                        DiplomaEvalText.Level(culture, fact.EquivalentLevelCode))));
                    sb.Append("</span>");
                }

                sb.Append("<span class=\"muted\">");
                sb.Append(WebUtility.HtmlEncode(culture.Format(
                    "DiplomaEval.FactMeta",
                    fact.EvaluationDate.ToString("dd-MM-yyyy"),
                    fact.ReferenceNumber)));
                sb.Append("</span></li>");
            }

            sb.Append("</ul>");
        }

        if (certificates.Count == 0)
        {
            sb.Append("<p class=\"muted\">");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Proof.CertificatesEmpty"]));
            sb.Append("</p>");
        }
        else
        {
            sb.Append("<ul class=\"passport-proof__list\">");
            foreach (var cert in certificates)
            {
                sb.Append("<li><strong>");
                sb.Append(WebUtility.HtmlEncode(cert.Name));
                sb.Append("</strong>");
                if (cert.Year is int y)
                {
                    sb.Append("<span class=\"muted\"> · ");
                    sb.Append(y);
                    sb.Append("</span>");
                }

                sb.Append("</li>");
            }

            sb.Append("</ul>");
        }

        sb.Append("<button type=\"button\" class=\"btn btn-ghost\" data-testid=\"passport-proof-add-certificate\" data-passport-proof-open=\"certificates\" data-sheet-title=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Proof.Education"]));
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Proof.AddCertificate"]));
        sb.Append("</button>");
        if (learningGoals.Count > 0)
        {
            sb.Append("<div class=\"passport-proof__learn\"><p class=\"passport-proof__learn-label\">");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Proof.WantToLearn"]));
            sb.Append("</p><div class=\"passport-proof__pills\">");
            foreach (var goal in learningGoals)
            {
                sb.Append("<span class=\"kompas-chip\">");
                sb.Append(WebUtility.HtmlEncode(goal));
                sb.Append("</span>");
            }

            sb.Append("</div></div>");
        }

        sb.Append("</article>");
    }

    private static void AppendReviewsCard(
        StringBuilder sb,
        CultureState culture,
        IReadOnlyList<CandidateReferenceItem> refs,
        CandidateUploadedCvInfo? uploadedCv)
    {
        sb.Append("<article class=\"detail-card passport-proof__card\"><h3 class=\"passport-proof__card-title\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Proof.Reviews"]));
        sb.Append("</h3>");
        if (refs.Count == 0)
        {
            sb.Append("<p class=\"muted\">");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Proof.ReviewsEmpty"]));
            sb.Append("</p>");
        }
        else
        {
            sb.Append("<ul class=\"passport-proof__list\">");
            foreach (var r in refs)
            {
                sb.Append("<li><strong>");
                sb.Append(WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(r.ContactName) ? r.EmployerName : r.ContactName));
                sb.Append("</strong><span class=\"muted\">");
                sb.Append(WebUtility.HtmlEncode(r.EmployerName));
                sb.Append("</span></li>");
            }

            sb.Append("</ul>");
        }

        sb.Append("<button type=\"button\" class=\"btn btn-ghost\" data-testid=\"passport-proof-add-reference\" data-passport-proof-open=\"references\" data-sheet-title=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Proof.Reviews"]));
        sb.Append('"');
        if (refs.Count >= CandidateProfileEditor.MaxReferences)
        {
            sb.Append(" disabled");
        }

        sb.Append('>');
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Proof.AddReference"]));
        sb.Append("</button><div class=\"passport-proof__cv\"><h4 class=\"passport-proof__cv-title\">");
        sb.Append(WebUtility.HtmlEncode(culture["Profile.OwnCv"]));
        sb.Append("</h4>");
        if (uploadedCv is not null)
        {
            sb.Append("<p class=\"passport-proof__cv-file\">");
            sb.Append(WebUtility.HtmlEncode(uploadedCv.FileName));
            sb.Append("</p><div class=\"passport-proof__cv-actions\"><button type=\"button\" class=\"btn btn-secondary\" data-testid=\"passport-proof-replace-cv\" data-passport-proof-open=\"cv\" data-sheet-title=\"");
            sb.Append(WebUtility.HtmlEncode(culture["Profile.OwnCv"]));
            sb.Append("\">");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Proof.ReplaceCv"]));
            sb.Append("</button><a class=\"btn btn-secondary\" href=\"/api/me/cv\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Proof.DownloadCv"]));
            sb.Append("</a></div>");
        }
        else
        {
            sb.Append("<button type=\"button\" class=\"btn btn-ghost\" data-testid=\"passport-proof-upload-cv\" data-passport-proof-open=\"cv\" data-sheet-title=\"");
            sb.Append(WebUtility.HtmlEncode(culture["Profile.OwnCv"]));
            sb.Append("\">");
            sb.Append(WebUtility.HtmlEncode(culture["Profile.UploadCv"]));
            sb.Append("</button>");
        }

        sb.Append("</div></article>");
    }

    private static void AppendCompetenceBlocks(StringBuilder sb, CultureState culture, Panel panel)
    {
        var verifiedLabels = panel.VerifiedCompetences
            .Select(v => v.Label.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unverified = panel.ProfileCompetenceLabels
            .Where(l => !string.IsNullOrWhiteSpace(l) && !verifiedLabels.Contains(l.Trim()))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();
        if (panel.VerifiedCompetences.Count == 0 && unverified.Count == 0)
        {
            return;
        }

        sb.Append("<section class=\"detail-card passport-dna__card ref-verif-dna-competences\" data-testid=\"passport-proof-competences\">");
        if (panel.VerifiedCompetences.Count > 0)
        {
            AppendVerifiedCompetences(sb, culture, panel.VerifiedCompetences);
        }

        if (unverified.Count > 0)
        {
            sb.Append("<div class=\"ref-verif-unverified\"><p class=\"ref-verif-unverified__title\">");
            sb.Append(WebUtility.HtmlEncode(culture["RefVerif.UnverifiedCompetences.Title"]));
            sb.Append("</p><div class=\"ref-verif-unverified__chips\">");
            foreach (var label in unverified)
            {
                sb.Append("<span class=\"kompas-chip\">");
                sb.Append(WebUtility.HtmlEncode(label));
                sb.Append("</span>");
            }

            sb.Append("</div></div>");
        }

        sb.Append("</section>");
    }

    private static void AppendVerifiedCompetences(
        StringBuilder sb,
        CultureState culture,
        IReadOnlyList<JobsyApiClient.VerifiedCompetenceItem> items)
    {
        var titleId = "ref-verif-verified-title-proof";
        sb.Append("<section class=\"ref-verif-verified-competences\" aria-labelledby=\"");
        sb.Append(titleId);
        sb.Append("\" data-testid=\"ref-verif-verified-competences\"><h3 id=\"");
        sb.Append(titleId);
        sb.Append("\" class=\"passport-proof__h\">");
        sb.Append(WebUtility.HtmlEncode(culture["RefVerif.VerifiedCompetences.Title"]));
        sb.Append("</h3><ul class=\"ref-verif-verified-list\">");
        foreach (var item in items)
        {
            sb.Append("<li><p class=\"ref-verif-verified-list__label\"><span class=\"ref-verif-verified-list__check\" aria-hidden=\"true\">✓</span> ");
            sb.Append(WebUtility.HtmlEncode(item.Label));
            sb.Append("</p><p class=\"ref-verif-verified-list__line\">");
            sb.Append(WebUtility.HtmlEncode(item.RecognitionLine));
            sb.Append("</p></li>");
        }

        sb.Append("</ul><p class=\"ref-verif-disclaimer\">");
        sb.Append(WebUtility.HtmlEncode(culture["RefVerif.VerifiedCompetences.Disclaimer"]));
        sb.Append("</p></section>");
    }

    private static void AppendRefVerificationTip(StringBuilder sb, CultureState culture, Panel panel)
    {
        if (!panel.RefVerificationEnabled || !panel.RefVerificationEligible)
        {
            return;
        }

        var askHref = panel.FirstEmployerEntryId is Guid id
            ? $"/candidate/paspoort/referentie/vraag/{id:D}"
            : "/candidate/paspoort?tab=references";

        if (panel.HasConfirmedReference)
        {
            sb.Append("<p class=\"ref-verif-tip-followup\" data-testid=\"ref-verification-tip-followup\" data-screen=\"passport-proof\"><a class=\"ref-verif-tip-followup__link\" href=\"");
            sb.Append(WebUtility.HtmlEncode(askHref));
            sb.Append("\">");
            sb.Append(WebUtility.HtmlEncode(culture["RefVerif.Tip.AskAnother"]));
            sb.Append("</a></p>");
            return;
        }

        var storageKey = $"{panel.UserIdForTip}:ref-verification-tip";
        sb.Append("<aside class=\"ref-verif-tip\" data-testid=\"ref-verification-tip\" data-screen=\"passport-proof\" data-ref-verif-tip-key=\"");
        sb.Append(WebUtility.HtmlEncode(storageKey));
        sb.Append("\" role=\"note\"><img class=\"ref-verif-tip__mascot\" src=\"");
        sb.Append(WebUtility.HtmlEncode(BrandImages.MarkSvg));
        sb.Append("\" srcset=\"");
        sb.Append(WebUtility.HtmlEncode(BrandImages.SrcSet56));
        sb.Append("\" sizes=\"72px\" alt=\"\" width=\"72\" height=\"72\" decoding=\"async\" /><div class=\"ref-verif-tip__body\"><p class=\"ref-verif-tip__title\">");
        sb.Append(WebUtility.HtmlEncode(culture["RefVerif.Tip.Title"]));
        sb.Append("</p><p class=\"ref-verif-tip__text\">");
        sb.Append(WebUtility.HtmlEncode(culture["RefVerif.Tip.Body"]));
        sb.Append("</p><div class=\"ref-verif-tip__actions\"><a class=\"btn btn-primary\" href=\"");
        sb.Append(WebUtility.HtmlEncode(askHref));
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(culture["RefVerif.Tip.Ask"]));
        sb.Append("</a><button type=\"button\" class=\"ref-verif-tip__notnow\" data-ref-verif-tip-dismiss=\"");
        sb.Append(WebUtility.HtmlEncode(storageKey));
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(culture["RefVerif.Tip.NotNow"]));
        sb.Append("</button></div></div></aside>");
    }

    private static void AppendPdfDownload(StringBuilder sb, CultureState culture, bool dnaPassport)
    {
        sb.Append("<p class=\"passport-proof__note muted\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Proof.LobsyCvNote"]));
        sb.Append("</p><a class=\"btn btn-secondary\" href=\"/api/me/paspoort.pdf\" data-testid=\"passport-proof-download-pdf\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture[dnaPassport ? "Passport.DownloadDna" : "Profile.DownloadLobsyCv"]));
        sb.Append("</a>");
        if (dnaPassport)
        {
            sb.Append("<p class=\"muted\">");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.DownloadDnaHint"]));
            sb.Append("</p>");
        }
    }

    private static void AppendSheet(
        StringBuilder sb,
        CultureState culture,
        CandidatePreferences prefs,
        IReadOnlyList<CandidateReferenceItem> refs,
        string antiforgeryToken)
    {
        sb.Append("<div class=\"passport-proof__backdrop\" data-passport-proof-backdrop hidden></div>");
        sb.Append("<div class=\"passport-proof__sheet\" data-passport-proof-sheet role=\"dialog\" aria-modal=\"true\" aria-labelledby=\"passport-proof-sheet-title\" tabindex=\"-1\" data-testid=\"passport-proof-sheet\" hidden>");
        sb.Append("<div class=\"filter-sheet__header\"><h2 id=\"passport-proof-sheet-title\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Proof.Experience"]));
        sb.Append("</h2><button type=\"button\" class=\"share-modal__close\" data-passport-proof-close data-jobsy-dialog-close aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Common.Close"]));
        sb.Append("\">×</button></div><div class=\"filter-sheet__body\">");

        AppendExperienceSheetPanel(sb, culture, prefs, antiforgeryToken);
        AppendCertificatesSheetPanel(sb, culture, prefs, antiforgeryToken);
        AppendReferencesSheetPanel(sb, culture, refs, antiforgeryToken);
        AppendCvSheetPanel(sb, culture, antiforgeryToken);

        sb.Append("</div><div class=\"filter-sheet__footer\"><button type=\"button\" class=\"filter-sheet__cancel\" data-passport-proof-close>");
        sb.Append(WebUtility.HtmlEncode(culture["Common.Cancel"]));
        sb.Append("</button></div></div>");
    }

    private static void AppendExperienceSheetPanel(
        StringBuilder sb,
        CultureState culture,
        CandidatePreferences prefs,
        string antiforgeryToken)
    {
        var rows = prefs.Employers.ToList();
        if (rows.Count == 0)
        {
            rows.Add(new CandidateEmployerHistory { EntryId = Guid.NewGuid() });
        }

        sb.Append("<div data-passport-proof-sheet-panel=\"experience\" hidden><form method=\"post\" action=\"/api/passport-client/save-proof\" enctype=\"application/x-www-form-urlencoded\" data-enhance=\"false\" data-enhance-nav=\"false\">");
        sb.Append("<input type=\"hidden\" name=\"__RequestVerificationToken\" value=\"");
        sb.Append(WebUtility.HtmlEncode(antiforgeryToken));
        sb.Append("\" /><input type=\"hidden\" name=\"proofSheet\" value=\"experience\" />");
        sb.Append("<fieldset class=\"profile-employers\"><legend>");
        sb.Append(WebUtility.HtmlEncode(culture["Profile.Experience"]));
        sb.Append("</legend><p class=\"profile-employers__hint\">");
        sb.Append(WebUtility.HtmlEncode(culture["Profile.EmployerDatesHint"]));
        sb.Append("</p>");
        for (var i = 0; i < Math.Min(8, rows.Count); i++)
        {
            var emp = rows[i];
            sb.Append("<div class=\"profile-employer-card\"><div class=\"profile-employer-card__top\">");
            sb.Append("<label class=\"profile-employer-card__field profile-employer-card__field--employer\"><span>");
            sb.Append(WebUtility.HtmlEncode(culture["Profile.Employer"]));
            sb.Append("</span><input name=\"empName_");
            sb.Append(i);
            sb.Append("\" placeholder=\"");
            sb.Append(WebUtility.HtmlEncode(culture["Profile.EmployerName"]));
            sb.Append("\" value=\"");
            sb.Append(WebUtility.HtmlEncode(emp.EmployerName));
            sb.Append("\" /></label><label class=\"profile-employer-card__field profile-employer-card__field--role\"><span>");
            sb.Append(WebUtility.HtmlEncode(culture["Profile.Role"]));
            sb.Append("</span><input name=\"empRole_");
            sb.Append(i);
            sb.Append("\" placeholder=\"");
            sb.Append(WebUtility.HtmlEncode(culture["Profile.RolePlaceholder"]));
            sb.Append("\" value=\"");
            sb.Append(WebUtility.HtmlEncode(emp.Role ?? ""));
            sb.Append("\" /></label></div><div class=\"profile-employer-card__dates\">");
            sb.Append("<label class=\"profile-employer-card__field profile-employer-card__field--start\"><span>");
            sb.Append(WebUtility.HtmlEncode(culture["Profile.EmployerFrom"]));
            sb.Append("</span><input name=\"empStart_");
            sb.Append(i);
            sb.Append("\" type=\"month\" value=\"");
            sb.Append(WebUtility.HtmlEncode(emp.StartMonth ?? ""));
            sb.Append("\" /></label>");
            if (!emp.ShowsAsCurrent)
            {
                sb.Append("<label class=\"profile-employer-card__field profile-employer-card__field--end\"><span>");
                sb.Append(WebUtility.HtmlEncode(culture["Profile.EmployerTo"]));
                sb.Append("</span><input name=\"empEnd_");
                sb.Append(i);
                sb.Append("\" type=\"month\" value=\"");
                sb.Append(WebUtility.HtmlEncode(emp.EndMonth ?? ""));
                sb.Append("\" /></label>");
            }

            sb.Append("<label class=\"profile-employer-card__current-inline\"><input type=\"checkbox\" name=\"empCurrent_");
            sb.Append(i);
            sb.Append("\" value=\"true\"");
            if (emp.ShowsAsCurrent)
            {
                sb.Append(" checked");
            }

            sb.Append(" />");
            sb.Append(WebUtility.HtmlEncode(culture["Profile.CurrentJob"]));
            sb.Append("</label></div><label class=\"profile-employer-card__field profile-employer-card__field--description\"><span>");
            sb.Append(WebUtility.HtmlEncode(culture["Profile.WhatDidYouDo"]));
            sb.Append("</span><textarea name=\"empDesc_");
            sb.Append(i);
            sb.Append("\" rows=\"3\" maxlength=\"1000\">");
            sb.Append(WebUtility.HtmlEncode(emp.Description ?? ""));
            sb.Append("</textarea></label><input type=\"hidden\" name=\"empEntryId_");
            sb.Append(i);
            sb.Append("\" value=\"");
            sb.Append(WebUtility.HtmlEncode((emp.EntryId ?? Guid.NewGuid()).ToString("D")));
            sb.Append("\" /></div>");
        }

        sb.Append("</fieldset><div class=\"profile-save-bar\"><button type=\"submit\" class=\"filter-sheet__apply btn btn-primary\" data-testid=\"passport-proof-save\">");
        sb.Append(WebUtility.HtmlEncode(culture["Common.Save"]));
        sb.Append("</button></div></form></div>");
    }

    private static void AppendCertificatesSheetPanel(
        StringBuilder sb,
        CultureState culture,
        CandidatePreferences prefs,
        string antiforgeryToken)
    {
        var selected = new HashSet<string>(prefs.Educations ?? [], StringComparer.OrdinalIgnoreCase);
        var certs = prefs.Certificates?.ToList() ?? [];
        if (certs.Count == 0)
        {
            certs.Add(new CandidateCertificate());
        }

        sb.Append("<div data-passport-proof-sheet-panel=\"certificates\" hidden><form method=\"post\" action=\"/api/passport-client/save-proof\" data-enhance=\"false\" data-enhance-nav=\"false\">");
        sb.Append("<input type=\"hidden\" name=\"__RequestVerificationToken\" value=\"");
        sb.Append(WebUtility.HtmlEncode(antiforgeryToken));
        sb.Append("\" /><input type=\"hidden\" name=\"proofSheet\" value=\"certificates\" />");
        sb.Append("<fieldset class=\"profile-roles profile-check-grid\"><legend>");
        sb.Append(WebUtility.HtmlEncode(culture["Profile.Education"]));
        sb.Append("</legend>");
        foreach (var level in EducationLevelLabels.ProfileAll)
        {
            sb.Append("<label><input type=\"checkbox\" name=\"education\" value=\"");
            sb.Append(WebUtility.HtmlEncode(level));
            sb.Append('"');
            if (selected.Contains(level))
            {
                sb.Append(" checked");
            }

            sb.Append(" />");
            sb.Append(WebUtility.HtmlEncode(UiLabels.Education(culture, level)));
            sb.Append("</label>");
        }

        sb.Append("</fieldset>");
        if (EducationDirectionCatalog.ProfileSupportsDirection(selected))
        {
            sb.Append("<label class=\"profile-education-direction\" data-testid=\"profile-education-direction\">");
            sb.Append(WebUtility.HtmlEncode(culture["Onboarding.EducationDirection"]));
            sb.Append("<input name=\"educationDirection\" type=\"text\" value=\"");
            sb.Append(WebUtility.HtmlEncode(prefs.EducationDirection ?? ""));
            sb.Append("\" /></label>");
        }

        sb.Append("<fieldset class=\"profile-certificates\"><legend>");
        sb.Append(WebUtility.HtmlEncode(culture["Profile.Certificates"]));
        sb.Append("</legend>");
        for (var i = 0; i < Math.Min(10, certs.Count); i++)
        {
            var cert = certs[i];
            sb.Append("<div class=\"profile-cert-row\"><label class=\"profile-cert-row__year\"><span class=\"visually-hidden\">");
            sb.Append(WebUtility.HtmlEncode(culture["Profile.CertificateYear"]));
            sb.Append("</span><input type=\"number\" min=\"1950\" max=\"2100\" name=\"certYear_");
            sb.Append(i);
            sb.Append("\" placeholder=\"");
            sb.Append(WebUtility.HtmlEncode(culture["Profile.CertificateYear"]));
            sb.Append("\" value=\"");
            if (cert.Year is int y)
            {
                sb.Append(y);
            }

            sb.Append("\" /></label><label class=\"profile-cert-row__name\"><span class=\"visually-hidden\">");
            sb.Append(WebUtility.HtmlEncode(culture["Profile.CertificateName"]));
            sb.Append("</span><input type=\"text\" maxlength=\"200\" name=\"certName_");
            sb.Append(i);
            sb.Append("\" placeholder=\"");
            sb.Append(WebUtility.HtmlEncode(culture["Profile.CertificateName"]));
            sb.Append("\" value=\"");
            sb.Append(WebUtility.HtmlEncode(cert.Name));
            sb.Append("\" /></label></div>");
        }

        sb.Append("</fieldset><div class=\"profile-save-bar\"><button type=\"submit\" class=\"filter-sheet__apply btn btn-primary\" data-testid=\"passport-proof-save\">");
        sb.Append(WebUtility.HtmlEncode(culture["Common.Save"]));
        sb.Append("</button></div></form></div>");
    }

    private static void AppendReferencesSheetPanel(
        StringBuilder sb,
        CultureState culture,
        IReadOnlyList<CandidateReferenceItem> refs,
        string antiforgeryToken)
    {
        sb.Append("<div data-passport-proof-sheet-panel=\"references\" hidden><form method=\"post\" action=\"/api/passport-client/save-proof\" data-enhance=\"false\" data-enhance-nav=\"false\">");
        sb.Append("<input type=\"hidden\" name=\"__RequestVerificationToken\" value=\"");
        sb.Append(WebUtility.HtmlEncode(antiforgeryToken));
        sb.Append("\" /><input type=\"hidden\" name=\"proofSheet\" value=\"references\" />");
        sb.Append("<fieldset class=\"profile-employers\"><legend>");
        sb.Append(WebUtility.HtmlEncode(culture["Profile.References"]));
        sb.Append("</legend><p class=\"profile-employers__hint\">");
        sb.Append(WebUtility.HtmlEncode(culture["Profile.ReferencesLead"]));
        sb.Append("</p>");
        for (var i = 0; i < CandidateProfileEditor.MaxReferences; i++)
        {
            var r = i < refs.Count ? refs[i] : new CandidateReferenceItem();
            sb.Append("<div class=\"profile-employer-card\"><div class=\"profile-employer-card__top\">");
            sb.Append("<label class=\"profile-employer-card__field profile-employer-card__field--employer\"><span>");
            sb.Append(WebUtility.HtmlEncode(culture["Profile.ReferenceEmployer"]));
            sb.Append("</span><input name=\"refEmployer_");
            sb.Append(i);
            sb.Append("\" value=\"");
            sb.Append(WebUtility.HtmlEncode(r.EmployerName));
            sb.Append("\" /></label><label class=\"profile-employer-card__field profile-employer-card__field--role\"><span>");
            sb.Append(WebUtility.HtmlEncode(culture["Profile.ReferenceContact"]));
            sb.Append("</span><input name=\"refContact_");
            sb.Append(i);
            sb.Append("\" value=\"");
            sb.Append(WebUtility.HtmlEncode(r.ContactName));
            sb.Append("\" /></label></div><div class=\"profile-employer-card__dates\">");
            sb.Append("<label class=\"profile-employer-card__field\"><span>");
            sb.Append(WebUtility.HtmlEncode(culture["Profile.ReferenceEmail"]));
            sb.Append("</span><input type=\"email\" name=\"refEmail_");
            sb.Append(i);
            sb.Append("\" value=\"");
            sb.Append(WebUtility.HtmlEncode(r.Email));
            sb.Append("\" /></label><label class=\"profile-employer-card__field\"><span>");
            sb.Append(WebUtility.HtmlEncode(culture["Profile.ReferencePhone"]));
            sb.Append("</span><input type=\"tel\" name=\"refPhone_");
            sb.Append(i);
            sb.Append("\" value=\"");
            sb.Append(WebUtility.HtmlEncode(r.Phone));
            sb.Append("\" /></label></div></div>");
        }

        sb.Append("</fieldset><div class=\"profile-save-bar\"><button type=\"submit\" class=\"filter-sheet__apply btn btn-primary\" data-testid=\"passport-proof-save\">");
        sb.Append(WebUtility.HtmlEncode(culture["Common.Save"]));
        sb.Append("</button></div></form></div>");
    }

    private static void AppendCvSheetPanel(StringBuilder sb, CultureState culture, string antiforgeryToken)
    {
        sb.Append("<div data-passport-proof-sheet-panel=\"cv\" hidden><form method=\"post\" action=\"/api/passport-client/save-proof\" enctype=\"multipart/form-data\" data-enhance=\"false\" data-enhance-nav=\"false\">");
        sb.Append("<input type=\"hidden\" name=\"__RequestVerificationToken\" value=\"");
        sb.Append(WebUtility.HtmlEncode(antiforgeryToken));
        sb.Append("\" /><input type=\"hidden\" name=\"proofSheet\" value=\"cv\" />");
        sb.Append("<fieldset class=\"profile-employers\"><legend>");
        sb.Append(WebUtility.HtmlEncode(culture["Profile.OwnCv"]));
        sb.Append("</legend><p class=\"profile-employers__hint\">");
        sb.Append(WebUtility.HtmlEncode(culture["Profile.OwnCvLead"]));
        sb.Append("</p><label class=\"btn btn-primary\">");
        sb.Append(WebUtility.HtmlEncode(culture["Profile.UploadCv"]));
        sb.Append("<input type=\"file\" name=\"cvFile\" accept=\".pdf,.docx,application/pdf,application/vnd.openxmlformats-officedocument.wordprocessingml.document\" /></label></fieldset>");
        sb.Append("<div class=\"profile-save-bar\"><button type=\"submit\" class=\"filter-sheet__apply btn btn-primary\" data-testid=\"passport-proof-save\">");
        sb.Append(WebUtility.HtmlEncode(culture["Common.Save"]));
        sb.Append("</button></div></form></div>");
    }

    internal static string? FormatHint(CultureState culture, ProofStrengthRules.StrengthView view)
    {
        var missing = view.Missing;
        if (missing.Count == 0)
        {
            return view.Filled >= view.Max ? culture["Passport.Proof.HintHard"] : null;
        }

        var key = ProofStrengthRules.HintKey(missing);
        if (missing.Count == 1)
        {
            return missing[0].Kind == ProofStrengthRules.ProofKind.OwnCv
                ? culture[key]
                : culture.Format(key, missing[0].Count);
        }

        return culture.Format(
            key,
            missing[0].Count,
            culture[ProofStrengthRules.NounKey(missing[0].Kind)],
            missing[1].Count,
            culture[ProofStrengthRules.NounKey(missing[1].Kind)]);
    }

    private static bool IsVerified(
        IReadOnlyList<JobsyApiClient.ReferenceVerificationListItem> refRows,
        Guid? entryId)
        => entryId is Guid id
           && refRows.Any(r =>
               r.WorkExperienceEntryId == id
               && string.Equals(r.DisplayStatus, "confirmed", StringComparison.OrdinalIgnoreCase)
               && r.Active);

    private static string FormatRange(
        CultureState culture,
        string? start,
        string? end,
        bool isCurrent = false)
    {
        static string Year(string? m)
            => string.IsNullOrWhiteSpace(m) ? "" : m.Length >= 4 ? m[..4] : m;

        var s = Year(start);
        var e = isCurrent
            ? culture["Profile.EmployerPresent"]
            : string.IsNullOrWhiteSpace(end) ? "…" : Year(end);
        return string.IsNullOrEmpty(s) ? e : $"{s} - {e}";
    }

    private static void EnsureEmployerEntryIds(List<CandidateEmployerHistory> employers)
    {
        foreach (var employer in employers)
        {
            if (employer.EntryId is null || employer.EntryId == Guid.Empty)
            {
                employer.EntryId = Guid.NewGuid();
            }
        }
    }
}
