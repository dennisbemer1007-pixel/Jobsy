using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using Jobsy.Core.Enums;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Web.Hosting;
using Jobsy.Web.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace Jobsy.Web.Services;

public sealed partial class JobsyApiClient
{
    public async Task<PartnerSalesCatalog?> GetPartnerSalesCatalogAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<PartnerSalesCatalog>("api/sales-commercial/catalog", ct);

    public async Task<SalesReferralVisitResult?> RecordSalesReferralVisitAsync(
        string code,
        string? channel,
        bool countClick,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/sales-commercial/referral/visit",
            new { code, channel, countClick },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<SalesReferralVisitResult>(cancellationToken: ct);
    }

    public async Task UpdateSalesCommercialSettingsAsync(
        decimal baseTokenValueEuro,
        decimal highlightCarouselTokens,
        decimal highlightPulseTokens,
        int highlightCarouselDays,
        decimal startHighlightBonusTokens,
        decimal? directCommissionRate = null,
        decimal? indirectCommissionRate = null,
        int? commissionDurationDays = null,
        decimal? partnerCommissionRate = null,
        decimal? year2DirectCommissionRate = null,
        decimal? year3DirectCommissionRate = null,
        decimal? referredYear1DirectCommissionRate = null,
        int? commissionHoldDays = null,
        decimal? payoutMinimumEuro = null,
        int? ibanChangeHoldDays = null,
        int? attributionCookieDays = null,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            "api/sales-commercial/admin/settings",
            new
            {
                baseTokenValueEuro,
                highlightCarouselTokens,
                highlightPulseTokens,
                highlightCarouselDays,
                startHighlightBonusTokens,
                directCommissionRate,
                indirectCommissionRate,
                commissionDurationDays,
                partnerCommissionRate,
                year2DirectCommissionRate,
                year3DirectCommissionRate,
                referredYear1DirectCommissionRate,
                commissionHoldDays,
                payoutMinimumEuro,
                ibanChangeHoldDays,
                attributionCookieDays
            },
            ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateVacancyTypeCostAsync(
        string kind,
        decimal costTokens,
        bool isActive,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            "api/sales-commercial/admin/vacancy-type-costs",
            new { kind, costTokens, isActive },
            ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<SalesPackageItem?> UpsertSalesPackageAsync(SalesPackageItem package, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            "api/sales-commercial/admin/packages",
            new
            {
                id = package.Id == Guid.Empty ? (Guid?)null : package.Id,
                name = package.Name,
                code = package.Code,
                category = package.Category,
                tokenAmount = package.TokenAmount,
                priceEuro = package.PriceEuro,
                description = package.Description,
                isActive = package.IsActive,
                sortOrder = package.SortOrder
            },
            ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SalesPackageItem>(cancellationToken: ct);
    }

    public async Task DeleteSalesPackageAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/sales-commercial/admin/packages/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task DownloadPartnerFlyerPdfAsync(
        Microsoft.JSInterop.IJSRuntime js,
        string? trackingCode,
        CancellationToken ct = default)
    {
        var qs = string.IsNullOrWhiteSpace(trackingCode)
            ? "api/sales-commercial/flyer.pdf"
            : $"api/sales-commercial/flyer.pdf?trackingCode={Uri.EscapeDataString(trackingCode.Trim())}";
        var response = await _http.GetAsync(qs, ct);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var base64 = Convert.ToBase64String(bytes);
        await SendBrowserDownloadAsync(js, "lobsy-partner-flyer.pdf", base64, "application/pdf");
    }

    public async Task<PartnerAffiliateMeModel?> GetPartnerAffiliateMeAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<PartnerAffiliateMeModel>("api/partner-affiliate/me", ct);

    public async Task<IReadOnlyList<PartnerAffiliateReferralRowModel>> GetPartnerAffiliateReferralsAsync(
        CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<PartnerAffiliateReferralRowModel>>(
            "api/partner-affiliate/referrals", ct) ?? [];

    public async Task<PartnerAffiliateToolkitModel?> GetPartnerAffiliateToolkitAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<PartnerAffiliateToolkitModel>("api/partner-affiliate/toolkit", ct);

    public async Task DownloadAmbassadeurFlyerPdfAsync(
        Microsoft.JSInterop.IJSRuntime js,
        string kind,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/ambassadeurs/me/flyers/{Uri.EscapeDataString(kind)}", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Flyer downloaden mislukt.");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var fileName = kind.Contains("entrepreneur", StringComparison.OrdinalIgnoreCase)
            ? "lobsy-ondernemersflyer.pdf"
            : "lobsy-kandidatenflyer.pdf";
        var base64 = Convert.ToBase64String(bytes);
        await SendBrowserDownloadAsync(js, fileName, base64, "application/pdf");
    }

    public async Task<AmbassadeurInviteResult?> InviteAmbassadeurAsync(
        string email,
        string fullName,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/ambassadeurs/invite", new { email, fullName }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Uitnodigen mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<AmbassadeurInviteResult>(cancellationToken: ct);
    }

    public async Task<List<AmbassadeurListItem>> GetAmbassadeursAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<AmbassadeurListItem>>("api/ambassadeurs", ct) ?? [];

    public async Task<AmbassadeurSettingsDto?> GetAmbassadeurSettingsAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<AmbassadeurSettingsDto>("api/ambassadeurs/settings", ct);

    public async Task<AmbassadeurSettingsDto?> UpdateAmbassadeurSettingsAsync(
        int candidateThreshold,
        decimal percentPerThreshold,
        decimal maxCommissionPercentage,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            "api/ambassadeurs/settings",
            new { candidateThreshold, percentPerThreshold, maxCommissionPercentage },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Instellingen opslaan mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<AmbassadeurSettingsDto>(cancellationToken: ct);
    }

    public async Task SetAmbassadeurCommissionOverrideAsync(
        Guid userId,
        decimal? percentageOverride,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            $"api/ambassadeurs/{userId:D}/commission-override",
            new { percentageOverride },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Override opslaan mislukt.");
        }
    }

    public async Task<AmbassadeurDashboard?> GetMyAmbassadeurDashboardAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/ambassadeurs/me/dashboard", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                ExtractMessage(body) ?? $"Ambassadeur-dashboard mislukt ({(int)response.StatusCode}).");
        }

        return await response.Content.ReadFromJsonAsync<AmbassadeurDashboard>(cancellationToken: ct);
    }

    public async Task<AmbassadeurProfile?> GetMyAmbassadeurProfileAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/ambassadeurs/me/profile", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Ambassadeur-profiel mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<AmbassadeurProfile>(cancellationToken: ct);
    }

    public async Task<AmbassadeurProfile?> UpdateMyAmbassadeurProfileAsync(
        AmbassadeurProfileForm form,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync("api/ambassadeurs/me/profile", form, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Profiel opslaan mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<AmbassadeurProfile>(cancellationToken: ct);
    }

    public async Task<AmbassadeurProfile?> SignAmbassadeurAgreementAsync(CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/ambassadeurs/me/sign-agreement", new { }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Ondertekenen mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<AmbassadeurProfile>(cancellationToken: ct);
    }

    public async Task<SalesManagerPayoutPreview?> GetMyAmbassadeurPayoutPreviewAsync(
        decimal? amountExVat = null,
        CancellationToken ct = default)
    {
        var url = amountExVat is null
            ? "api/ambassadeurs/me/payouts/preview"
            : $"api/ambassadeurs/me/payouts/preview?amountExVat={amountExVat.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        return await _http.GetFromJsonAsync<SalesManagerPayoutPreview>(url, ct);
    }

    public async Task<SalesManagerPayoutCheckoutResult?> CreateMyAmbassadeurPayoutCheckoutAsync(
        decimal amountExVat,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/ambassadeurs/me/payouts/checkout",
            new { amountExVat },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Uitbetaling starten mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<SalesManagerPayoutCheckoutResult>(cancellationToken: ct);
    }

    public async Task<SalesManagerPayoutCompleteResult?> CompleteMyAmbassadeurPayoutCheckoutAsync(
        string paymentId,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/ambassadeurs/me/payouts/complete",
            new { paymentId },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Uitbetaling afronden mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<SalesManagerPayoutCompleteResult>(cancellationToken: ct);
    }

    public async Task<BranchFlyerRouteDto?> GetBranchFlyerRouteAsync(
        Guid companyId,
        CancellationToken ct = default)
        => await _http.GetFromJsonAsync<BranchFlyerRouteDto>(
            $"api/employer-flyers/public/branches/{companyId:D}/route",
            ct);

    public async Task<PublicCompanyPage?> GetPublicCompanyByKvkAsync(
        string kvkNumber,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync(
            $"api/public/companies/{Uri.EscapeDataString(kvkNumber.Trim())}",
            ct);
        return await ReadPublicCompanyOrNullAsync(response, ct);
    }

    public async Task<PublicCompanyPage?> GetPublicCompanyByVestigingAsync(
        string kvkNumber,
        string vestigingsnummer,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync(
            $"api/public/companies/{Uri.EscapeDataString(kvkNumber.Trim())}/{Uri.EscapeDataString(vestigingsnummer.Trim())}",
            ct);
        return await ReadPublicCompanyOrNullAsync(response, ct);
    }

    private static async Task<PublicCompanyPage?> ReadPublicCompanyOrNullAsync(
        HttpResponseMessage response,
        CancellationToken ct)
    {
        // Missing or failing lookups must not throw HttpRequestException
        // ("500 Internal Server Error") into the public HTML.
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<PublicCompanyPage>(cancellationToken: ct);
    }

    public async Task DownloadBranchRaamflyerPdfAsync(
        Microsoft.JSInterop.IJSRuntime js,
        Guid companyId,
        string format = "A4",
        CancellationToken ct = default)
    {
        var qs = $"api/employer-flyers/branch/{companyId:D}.pdf?format={Uri.EscapeDataString(format)}";
        var response = await _http.GetAsync(qs, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Raamflyer downloaden mislukt.");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var size = string.Equals(format, "A3", StringComparison.OrdinalIgnoreCase) ? "A3" : "A4";
        var base64 = Convert.ToBase64String(bytes);
        await SendBrowserDownloadAsync(js,
            $"lobsy-raamflyer-{size}.pdf",
            base64,
            "application/pdf");
    }

    public async Task DownloadOverviewRaamflyerPdfAsync(
        Microsoft.JSInterop.IJSRuntime js,
        string? title = null,
        string format = "A4",
        IEnumerable<Guid>? companyIds = null,
        CancellationToken ct = default)
    {
        var qs = $"api/employer-flyers/overview.pdf?format={Uri.EscapeDataString(format)}";
        if (!string.IsNullOrWhiteSpace(title))
        {
            qs += $"&title={Uri.EscapeDataString(title.Trim())}";
        }

        if (companyIds is not null)
        {
            foreach (var id in companyIds)
            {
                qs += $"&companyIds={id:D}";
            }
        }

        var response = await _http.GetAsync(qs, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Overzichtsflyer downloaden mislukt.");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var size = string.Equals(format, "A3", StringComparison.OrdinalIgnoreCase) ? "A3" : "A4";
        var base64 = Convert.ToBase64String(bytes);
        await SendBrowserDownloadAsync(js,
            $"lobsy-overzicht-raamflyer-{size}.pdf",
            base64,
            "application/pdf");
    }

    public async Task<IReadOnlyList<SalesManagerCostFinanceItem>> GetSalesManagerCostsAsync(
        int? year = null,
        int? quarter = null,
        CancellationToken ct = default)
    {
        var url = BuildTokenFinanceUrl("api/vat/sales-manager-costs", year, quarter);
        return await _http.GetFromJsonAsync<List<SalesManagerCostFinanceItem>>(url, ct) ?? [];
    }

    public async Task<SalesManagerInviteResult?> InviteSalesManagerAsync(
        string email,
        string fullName,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/sales-managers/invite", new { email, fullName }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Uitnodigen mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<SalesManagerInviteResult>(cancellationToken: ct);
    }

    public async Task<SalesManagerApplicationItem?> SubmitSalesManagerApplicationAsync(
        string candidateEmail,
        string candidateFullName,
        string motivation,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/sales-managers/me/applications",
            new { candidateEmail, candidateFullName, motivation },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Aanbeveling indienen mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<SalesManagerApplicationItem>(cancellationToken: ct);
    }

    public async Task<List<SalesManagerApplicationItem>> GetMySalesManagerApplicationsAsync(
        CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<SalesManagerApplicationItem>>(
               "api/sales-managers/me/applications", ct) ?? [];

    public async Task<List<SalesManagerApplicationItem>> GetSalesManagerApplicationsAsync(
        bool pendingOnly = true,
        CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<SalesManagerApplicationItem>>(
               $"api/sales-managers/applications?pendingOnly={pendingOnly.ToString().ToLowerInvariant()}",
               ct) ?? [];

    public async Task<SalesManagerApplicationItem?> ApproveSalesManagerApplicationAsync(
        Guid applicationId,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsync(
            $"api/sales-managers/applications/{applicationId:D}/approve", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Goedkeuren mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<SalesManagerApplicationItem>(cancellationToken: ct);
    }

    public async Task<SalesManagerApplicationItem?> RejectSalesManagerApplicationAsync(
        Guid applicationId,
        string? reason = null,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/sales-managers/applications/{applicationId:D}/reject",
            new { reason },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Afwijzen mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<SalesManagerApplicationItem>(cancellationToken: ct);
    }

    public async Task<List<SalesManagerListItem>> GetSalesManagersAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<SalesManagerListItem>>("api/sales-managers", ct) ?? [];

    public async Task<SalesManagerDashboard?> GetMySalesManagerDashboardAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/sales-managers/me/dashboard", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                ExtractMessage(body)
                ?? $"Salesmanager-dashboard mislukt ({(int)response.StatusCode}). Is de API (poort 5200) gestart en gemigreerd?");
        }

        return await response.Content.ReadFromJsonAsync<SalesManagerDashboard>(cancellationToken: ct);
    }

    public async Task<SalesManagerDashboard?> GetSalesManagerDashboardAsync(Guid userId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/sales-managers/{userId}/dashboard", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? $"Dashboard ophalen mislukt ({(int)response.StatusCode}).");
        }

        return await response.Content.ReadFromJsonAsync<SalesManagerDashboard>(cancellationToken: ct);
    }

    public async Task<SalesManagerProfile?> GetMySalesManagerProfileAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/sales-managers/me/profile", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                ExtractMessage(body)
                ?? $"Salesmanager-profiel mislukt ({(int)response.StatusCode}). Is de API (poort 5200) gestart?");
        }

        return await response.Content.ReadFromJsonAsync<SalesManagerProfile>(cancellationToken: ct);
    }

    public async Task<SalesManagerProfile?> UpdateMySalesManagerProfileAsync(
        SalesManagerProfileForm form,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync("api/sales-managers/me/profile", form, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Profiel opslaan mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<SalesManagerProfile>(cancellationToken: ct);
    }

    public async Task<SalesManagerProfile?> SignSalesManagerAgreementAsync(CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/sales-managers/me/sign-agreement", new { }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Ondertekenen mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<SalesManagerProfile>(cancellationToken: ct);
    }

    public async Task<List<SelfBillingInvoiceItem>> GetMySelfBillingInvoicesAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<SelfBillingInvoiceItem>>("api/sales-managers/me/invoices", ct) ?? [];

    public async Task<SalesManagerPayoutPreview?> GetMyPayoutPreviewAsync(
        decimal? amountExVat = null,
        CancellationToken ct = default)
    {
        var url = amountExVat is null
            ? "api/sales-managers/me/payouts/preview"
            : $"api/sales-managers/me/payouts/preview?amountExVat={amountExVat.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        return await _http.GetFromJsonAsync<SalesManagerPayoutPreview>(url, ct);
    }

    public async Task<SalesManagerPayoutCheckoutResult?> CreateMyPayoutCheckoutAsync(
        decimal amountExVat,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/sales-managers/me/payouts/checkout",
            new { amountExVat },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Uitbetaling starten mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<SalesManagerPayoutCheckoutResult>(cancellationToken: ct);
    }

    public async Task<SalesManagerPayoutCompleteResult?> CompleteMyPayoutCheckoutAsync(
        string paymentId,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/sales-managers/me/payouts/complete",
            new { paymentId },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Uitbetaling afronden mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<SalesManagerPayoutCompleteResult>(cancellationToken: ct);
    }

    public async Task DownloadMySelfBillingInvoiceAsync(
        Guid invoiceId,
        string invoiceNumber,
        IJSRuntime js,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/sales-managers/me/invoices/{invoiceId}/download", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Download mislukt.");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var fileName = string.IsNullOrWhiteSpace(invoiceNumber) ? $"{invoiceId:N}.pdf" : $"{invoiceNumber}.pdf";
        var base64 = Convert.ToBase64String(bytes);
        await SendBrowserDownloadAsync(js, fileName, base64, "application/pdf");
    }

    public async Task<SelfBillingInvoiceItem?> MarkSelfBillingInvoicePaidAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"api/sales-managers/invoices/{invoiceId}/mark-paid", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Markeren als betaald mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<SelfBillingInvoiceItem>(cancellationToken: ct);
    }

    public async Task<OnboardingCheckoutResult?> CreateOnboardingCheckoutAsync(Guid companyId, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"api/companies/{companyId}/onboarding/checkout", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Onboarding-checkout mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<OnboardingCheckoutResult>(cancellationToken: ct);
    }

    public async Task<OnboardingCompleteResult?> CompleteOnboardingCheckoutAsync(
        Guid companyId,
        string paymentId,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/companies/{companyId}/onboarding/complete",
            new { paymentId },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Onboarding-betaling afronden mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<OnboardingCompleteResult>(cancellationToken: ct);
    }

    public async Task<Jobsy.Core.Contracts.Sales.SalesDashboardDto?> GetSalesPartnerDashboardAsync(
        string period = "year",
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/sales/me/dashboard?period={Uri.EscapeDataString(period)}", ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<Jobsy.Core.Contracts.Sales.SalesDashboardDto>(cancellationToken: ct);
    }

    public async Task<Jobsy.Core.Contracts.Sales.SalesEmployerPageDto?> GetSalesPartnerEmployersAsync(
        string? q = null,
        string? status = null,
        int? year = null,
        int page = 1,
        CancellationToken ct = default)
    {
        var qs = new List<string> { $"page={page}" };
        if (!string.IsNullOrWhiteSpace(q))
        {
            qs.Add($"q={Uri.EscapeDataString(q)}");
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            qs.Add($"status={Uri.EscapeDataString(status)}");
        }

        if (year is not null)
        {
            qs.Add($"year={year}");
        }

        var response = await _http.GetAsync($"api/sales/me/employers?{string.Join('&', qs)}", ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<Jobsy.Core.Contracts.Sales.SalesEmployerPageDto>(cancellationToken: ct);
    }

    public async Task<Jobsy.Core.Contracts.Sales.SalesEmployerDetailDto?> GetSalesPartnerEmployerAsync(
        Guid companyId,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/sales/me/employers/{companyId:D}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Jobsy.Core.Contracts.Sales.SalesEmployerDetailDto>(cancellationToken: ct);
    }
}
