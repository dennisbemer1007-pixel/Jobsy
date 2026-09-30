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

    public async Task<IReadOnlyList<VacancyListItem>> GetPublicCompanyVacanciesAsync(
        string kvkNumber,
        string? vestigingsnummer = null,
        CancellationToken ct = default)
    {
        var path = string.IsNullOrWhiteSpace(vestigingsnummer)
            ? $"api/public/companies/{Uri.EscapeDataString(kvkNumber.Trim())}/vacancies"
            : $"api/public/companies/{Uri.EscapeDataString(kvkNumber.Trim())}/{Uri.EscapeDataString(vestigingsnummer.Trim())}/vacancies";
        var response = await _http.GetAsync(path, ct);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        return await response.Content.ReadFromJsonAsync<List<VacancyListItem>>(cancellationToken: ct)
               ?? [];
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

    public async Task<Jobsy.Core.Interfaces.SalesRecommendOverviewDto?> GetSalesRecommendOverviewAsync(
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/sales/me/recommend", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Aanbevelingen laden mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<Jobsy.Core.Interfaces.SalesRecommendOverviewDto>(cancellationToken: ct);
    }

    public async Task<SalesManagerApplicationItem?> SubmitSalesRecommendAsync(
        string candidateEmail,
        string candidateFullName,
        string motivation,
        bool referrerConfirmedPermission,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/sales/me/recommend",
            new { candidateEmail, candidateFullName, motivation, referrerConfirmedPermission },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Aanbeveling indienen mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<SalesManagerApplicationItem>(cancellationToken: ct);
    }

    public async Task ObjectSalesRecommendAsync(string token, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/sales/recommend/object",
            new { token },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Verwijderen mislukt.");
        }
    }

    public async Task<SalesManagerApplicationItem?> SubmitSalesManagerApplicationAsync(
        string candidateEmail,
        string candidateFullName,
        string motivation,
        bool referrerConfirmedPermission = true,
        CancellationToken ct = default)
        => await SubmitSalesRecommendAsync(
            candidateEmail, candidateFullName, motivation, referrerConfirmedPermission, ct);

    public async Task<List<SalesManagerApplicationItem>> GetMySalesManagerApplicationsAsync(
        CancellationToken ct = default)
    {
        var overview = await GetSalesRecommendOverviewAsync(ct);
        return overview?.Applications.Select(a => new SalesManagerApplicationItem
        {
            Id = a.Id,
            CandidateFullName = a.DisplayName,
            Status = a.Status,
            StatusLabelKey = a.StatusLabelKey,
            CreatedAtUtc = a.CreatedAtUtc,
            RejectionReason = a.RejectionReason,
            PersonalDataClearedAtUtc = a.PersonalDataCleared ? DateTime.UtcNow : null
        }).ToList() ?? [];
    }

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
        // Legacy callers: compose from portal profile + wallet (old me/dashboard removed in 09).
        var profile = await GetSalesPortalProfileAsync(ct);
        if (profile is null)
        {
            return null;
        }

        decimal available = 0m;
        try
        {
            var wallet = await GetSalesWalletAsync(ct);
            available = wallet.Available;
        }
        catch
        {
            // ignore — balance optional for shell
        }

        return new SalesManagerDashboard
        {
            UserId = profile.UserId,
            Email = profile.Email,
            FullName = profile.FullName,
            TrackingCode = profile.TrackingCode,
            IsOnboardingComplete = profile.IsOnboardingComplete,
            BalanceExVat = available,
            UninvoicedExVat = available,
            CanRecruitSalesManagers = profile.CanRecruitSalesManagers,
            ReferredBySalesManagerUserId = profile.ReferredBySalesManagerUserId
        };
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
        var portal = await GetSalesPortalProfileAsync(ct);
        if (portal is null)
        {
            return null;
        }

        return new SalesManagerProfile
        {
            UserId = portal.UserId,
            Email = portal.Email,
            FullName = portal.FullName,
            CompanyName = portal.CompanyName,
            KvkNumber = portal.KvkNumber,
            VatNumber = portal.VatNumber,
            Address = portal.Address,
            PostalCode = portal.PostalCode,
            City = portal.City,
            Country = portal.Country,
            TrackingCode = portal.TrackingCode,
            IsOnboardingComplete = portal.IsOnboardingComplete,
            CanRecruitSalesManagers = portal.CanRecruitSalesManagers,
            ReferredBySalesManagerUserId = portal.ReferredBySalesManagerUserId,
            AgreementSignedAt = portal.AgreementSignedAt,
            AgreementVersion = portal.AgreementVersion,
            OnboardingCompletedAt = portal.OnboardingCompletedAt,
            Iban = portal.MaskedIban
        };
    }

    public async Task<SalesManagerProfile?> UpdateMySalesManagerProfileAsync(
        SalesManagerProfileForm form,
        CancellationToken ct = default)
    {
        var portal = await UpdateSalesPortalCompanyAsync(new
        {
            companyName = form.CompanyName,
            kvkNumber = form.KvkNumber,
            vatNumber = form.VatNumber,
            address = form.Address,
            postalCode = form.PostalCode,
            city = form.City,
            country = form.Country
        }, ct);
        return await GetMySalesManagerProfileAsync(ct) ?? new SalesManagerProfile
        {
            UserId = portal.UserId,
            Email = portal.Email,
            FullName = portal.FullName,
            CompanyName = portal.CompanyName,
            TrackingCode = portal.TrackingCode,
            IsOnboardingComplete = portal.IsOnboardingComplete
        };
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
    {
        var rows = await GetSalesInvoicesAsync(ct);
        return rows.Select(i => new SelfBillingInvoiceItem
        {
            Id = i.Id,
            InvoiceNumber = i.InvoiceNumber,
            SubtotalExVat = i.SubtotalExVat,
            VatAmount = 0m,
            TotalInclVat = i.TotalInclVat,
            Status = i.Status,
            CreatedAt = i.CreatedAtUtc,
            IssuedAt = i.CreatedAtUtc,
            PaidAt = null
        }).ToList();
    }

    public async Task<SalesManagerPayoutPreview?> GetMyPayoutPreviewAsync(
        decimal? amountExVat = null,
        CancellationToken ct = default)
    {
        var preview = await GetSalesPayoutPreviewAsync(ct);
        return new SalesManagerPayoutPreview
        {
            AvailableExVat = preview.AvailableExVat,
            AmountExVat = preview.AmountExVat,
            VatAmount = preview.VatAmount,
            AmountInclVat = preview.TotalInclVat,
            MaskedIban = preview.MaskedIban,
            CanPayout = preview.CanRequest,
            BlockReason = preview.Blockers.FirstOrDefault()?.MessageKey
        };
    }

    public Task<SalesManagerPayoutCheckoutResult?> CreateMyPayoutCheckoutAsync(
        decimal amountExVat,
        CancellationToken ct = default)
        => throw new InvalidOperationException("Uitbetalen gaat nu via een aanvraag.");

    public Task<SalesManagerPayoutCompleteResult?> CompleteMyPayoutCheckoutAsync(
        string paymentId,
        CancellationToken ct = default)
        => throw new InvalidOperationException("Uitbetalen gaat nu via een aanvraag.");

    public async Task DownloadMySelfbillingInvoiceAsync(
        Guid invoiceId,
        string invoiceNumber,
        IJSRuntime js,
        CancellationToken ct = default)
    {
        var fileName = string.IsNullOrWhiteSpace(invoiceNumber) ? $"{invoiceId:N}.pdf" : $"{invoiceNumber}.pdf";
        await DownloadSalesInvoicePdfAsync(js, invoiceId, fileName, ct);
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

    public async Task<Jobsy.Core.Sales.SalesLinkToolkitDto?> GetSalesLinkToolkitAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/sales/me/link", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Jobsy.Core.Sales.SalesLinkToolkitDto>(cancellationToken: ct);
    }

    public async Task<byte[]?> GetSalesQrPngAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/sales/me/materials/qr.png", ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    public async Task DownloadSalesQrPngAsync(IJSRuntime js, CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/sales/me/materials/qr.png", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "QR downloaden mislukt.");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                       ?? "lobsy-qr.png";
        var base64 = Convert.ToBase64String(bytes);
        await SendBrowserDownloadAsync(js, fileName, base64, "image/png");
    }

    public async Task DownloadSalesMaterialPdfAsync(
        IJSRuntime js,
        string kind,
        string? trackingCode,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/sales/me/materials/{Uri.EscapeDataString(kind)}.pdf", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Materiaal downloaden mislukt.");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var code = string.IsNullOrWhiteSpace(trackingCode) ? "code" : trackingCode.Trim().ToUpperInvariant();
        var fileName = kind.Trim().ToLowerInvariant() switch
        {
            "flyer" => $"lobsy-flyer-{code}.pdf",
            "visitekaartje" or "visitekaartjes" or "cards" => $"lobsy-visitekaartje-{code}.pdf",
            "prijskaart" or "prices" => $"lobsy-prijskaart-{code}.pdf",
            "presentatie" or "presentation" => $"lobsy-presentatie-{code}.pdf",
            _ => $"lobsy-materiaal-{code}.pdf"
        };
        var base64 = Convert.ToBase64String(bytes);
        await SendBrowserDownloadAsync(js, fileName, base64, "application/pdf");
    }

    public async Task<Jobsy.Core.Sales.SalesPortalProfileDto?> GetSalesPortalProfileAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/sales/me/profile", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Profiel laden mislukt.");
        }

        return await response.Content.ReadFromJsonAsync<Jobsy.Core.Sales.SalesPortalProfileDto>(cancellationToken: ct);
    }

    public async Task<Jobsy.Core.Sales.SalesPortalProfileDto> UpdateSalesPortalCompanyAsync(
        object form,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync("api/sales/me/profile/company", form, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Bedrijfsgegevens opslaan mislukt.");
        }

        return (await response.Content.ReadFromJsonAsync<Jobsy.Core.Sales.SalesPortalProfileDto>(cancellationToken: ct))!;
    }

    public async Task<Jobsy.Core.Sales.SalesPortalProfileDto> UpdateSalesPortalVatAsync(
        object form,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync("api/sales/me/profile/vat", form, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Btw-keuze opslaan mislukt.");
        }

        return (await response.Content.ReadFromJsonAsync<Jobsy.Core.Sales.SalesPortalProfileDto>(cancellationToken: ct))!;
    }

    public async Task<Jobsy.Core.Sales.SalesPortalProfileDto> UpdateSalesEmailPrefsAsync(
        Jobsy.Core.Sales.SalesEmailPrefs prefs,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync("api/sales/me/profile/email-prefs", prefs, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Voorkeuren opslaan mislukt.");
        }

        return (await response.Content.ReadFromJsonAsync<Jobsy.Core.Sales.SalesPortalProfileDto>(cancellationToken: ct))!;
    }

    public async Task<Jobsy.Core.Sales.SalesPortalProfileDto> GiveSalesSelfBillingConsentAsync(CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/sales/me/profile/consent", new { }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Toestemming geven mislukt.");
        }

        return (await response.Content.ReadFromJsonAsync<Jobsy.Core.Sales.SalesPortalProfileDto>(cancellationToken: ct))!;
    }

    public async Task<Jobsy.Core.Sales.SalesPortalProfileDto> RevokeSalesSelfBillingConsentAsync(CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/sales/me/profile/consent/revoke", new { }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Toestemming intrekken mislukt.");
        }

        return (await response.Content.ReadFromJsonAsync<Jobsy.Core.Sales.SalesPortalProfileDto>(cancellationToken: ct))!;
    }

    public async Task<SalesIbanChangeBeginClientResult> BeginSalesIbanChangeAsync(
        string iban,
        string holderName,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/sales/me/payout-account/change",
            new { iban, holderName },
            ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new InvalidOperationException(ExtractMessage(body) ?? "Te veel IBAN-wijzigingen.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(ExtractMessage(body) ?? "IBAN-wijziging starten mislukt.");
        }

        return System.Text.Json.JsonSerializer.Deserialize<SalesIbanChangeBeginClientResult>(
                   body,
                   new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? new SalesIbanChangeBeginClientResult();
    }

    public async Task<Jobsy.Core.Sales.SalesPortalProfileDto> ConfirmSalesIbanChangeAsync(
        string code,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/sales/me/payout-account/change/confirm",
            new { code },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Bevestigen mislukt.");
        }

        return (await response.Content.ReadFromJsonAsync<Jobsy.Core.Sales.SalesPortalProfileDto>(cancellationToken: ct))!;
    }

    public async Task<Jobsy.Core.Sales.SalesPortalProfileDto> ConfirmSalesIbanChangeEmailAsync(
        string token,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/sales/me/payout-account/change/confirm-email",
            new { token },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Bevestigen mislukt.");
        }

        return (await response.Content.ReadFromJsonAsync<Jobsy.Core.Sales.SalesPortalProfileDto>(cancellationToken: ct))!;
    }

    public async Task DownloadSalesAgreementPdfAsync(IJSRuntime js, CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/sales/me/agreement.pdf", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Overeenkomst downloaden mislukt.");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        await SendBrowserDownloadAsync(js, "lobsy-overeenkomst.pdf", Convert.ToBase64String(bytes), "application/pdf");
    }

    public async Task DownloadSalesConsentPdfAsync(IJSRuntime js, CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/sales/me/self-billing-consent.pdf", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Toestemming downloaden mislukt.");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        await SendBrowserDownloadAsync(
            js,
            "lobsy-self-billing-toestemming.pdf",
            Convert.ToBase64String(bytes),
            "application/pdf");
    }

    public async Task<Jobsy.Core.Sales.SalesWalletOverviewDto> GetSalesWalletAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/sales/me/wallet", ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Jobsy.Core.Sales.SalesWalletOverviewDto>(cancellationToken: ct))!;
    }

    public async Task<Jobsy.Core.Sales.SalesWalletEntriesPageDto> GetSalesWalletEntriesAsync(
        int? periodYear,
        string? kind,
        string? state,
        int page = 1,
        CancellationToken ct = default)
    {
        var qs = new List<string> { $"page={page}" };
        if (periodYear is int y) qs.Add($"period={y}");
        if (!string.IsNullOrWhiteSpace(kind)) qs.Add($"kind={Uri.EscapeDataString(kind)}");
        if (!string.IsNullOrWhiteSpace(state)) qs.Add($"state={Uri.EscapeDataString(state)}");
        var response = await _http.GetAsync($"api/sales/me/wallet/entries?{string.Join('&', qs)}", ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Jobsy.Core.Sales.SalesWalletEntriesPageDto>(cancellationToken: ct))!;
    }

    public async Task<IReadOnlyList<Jobsy.Core.Sales.SalesPayoutListItemDto>> GetSalesPayoutsAsync(
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/sales/me/payouts", ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<Jobsy.Core.Sales.SalesPayoutListItemDto>>(cancellationToken: ct))
               ?? [];
    }

    public async Task<IReadOnlyList<Jobsy.Core.Sales.SalesInvoiceListItemDto>> GetSalesInvoicesAsync(
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/sales/me/invoices", ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<Jobsy.Core.Sales.SalesInvoiceListItemDto>>(cancellationToken: ct))
               ?? [];
    }

    public async Task<Jobsy.Core.Sales.SalesPayoutPreviewDto> GetSalesPayoutPreviewAsync(
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/sales/me/payouts/preview", ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Jobsy.Core.Sales.SalesPayoutPreviewDto>(cancellationToken: ct))!;
    }

    public async Task<Jobsy.Core.Sales.SalesPayoutRequestDto> RequestSalesPayoutAsync(
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/sales/me/payouts", new { }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Aanvragen mislukt.");
        }

        return (await response.Content.ReadFromJsonAsync<Jobsy.Core.Sales.SalesPayoutRequestDto>(cancellationToken: ct))!;
    }

    public async Task CancelSalesPayoutAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"api/sales/me/payouts/{id:D}/cancel", new { }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Annuleren mislukt.");
        }
    }

    public async Task DownloadSalesInvoicePdfAsync(IJSRuntime js, Guid id, string fileName, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/sales/me/invoices/{id:D}/pdf", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Factuur downloaden mislukt.");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var safe = string.IsNullOrWhiteSpace(fileName) ? id.ToString("N") : fileName;
        if (!safe.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            safe += ".pdf";
        }

        await SendBrowserDownloadAsync(js, safe, Convert.ToBase64String(bytes), "application/pdf");
    }

    public async Task DownloadSalesJaaroverzichtAsync(IJSRuntime js, int year, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/sales/me/jaaroverzicht/{year}.pdf", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Jaaroverzicht downloaden mislukt.");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        await SendBrowserDownloadAsync(js, $"jaaroverzicht-{year}.pdf", Convert.ToBase64String(bytes), "application/pdf");
    }

    public async Task<List<Jobsy.Core.Sales.SalesPayoutRunListItemDto>> GetAdminSalesPayoutRunsAsync(
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/admin/sales/payout-runs", ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<Jobsy.Core.Sales.SalesPayoutRunListItemDto>>(cancellationToken: ct))
               ?? [];
    }

    public async Task<Jobsy.Core.Sales.SalesPayoutRunDetailDto> GetAdminSalesPayoutRunAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/admin/sales/payout-runs/{id:D}", ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Jobsy.Core.Sales.SalesPayoutRunDetailDto>(cancellationToken: ct))!;
    }

    public async Task<Jobsy.Core.Sales.SalesPayoutRunDto> CreateAdminSalesExtraPayoutRunAsync(
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/admin/sales/payout-runs", new { }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Extra ronde maken mislukt.");
        }

        return (await response.Content.ReadFromJsonAsync<Jobsy.Core.Sales.SalesPayoutRunDto>(cancellationToken: ct))!;
    }

    public async Task RejectAdminSalesPayoutLineAsync(
        Guid runId,
        Guid requestId,
        string reason,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/admin/sales/payout-runs/{runId:D}/lines/{requestId:D}/reject",
            new { reason },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Afwijzen mislukt.");
        }
    }

    public async Task<Jobsy.Core.Sales.SalesPayoutRunDto> ApproveAdminSalesPayoutRunAsync(
        Guid runId,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/admin/sales/payout-runs/{runId:D}/approve",
            new { },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Goedkeuren mislukt.");
        }

        return (await response.Content.ReadFromJsonAsync<Jobsy.Core.Sales.SalesPayoutRunDto>(cancellationToken: ct))!;
    }

    public async Task DownloadAdminSalesPayoutExportAsync(
        IJSRuntime js,
        Guid runId,
        string format,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync(
            $"api/admin/sales/payout-runs/{runId:D}/export?format={Uri.EscapeDataString(format)}",
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Export mislukt.");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                       ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                       ?? $"payout-{runId:N}.{format}";
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        await SendBrowserDownloadAsync(js, fileName, Convert.ToBase64String(bytes), contentType);
    }

    public async Task<Jobsy.Core.Sales.SalesPayoutRunDto> MarkAdminSalesPayoutRunPaidAsync(
        Guid runId,
        IReadOnlyList<Guid>? invoiceIds = null,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/admin/sales/payout-runs/{runId:D}/mark-paid",
            new { invoiceIds },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Markeren als betaald mislukt.");
        }

        return (await response.Content.ReadFromJsonAsync<Jobsy.Core.Sales.SalesPayoutRunDto>(cancellationToken: ct))!;
    }

    public async Task<List<Jobsy.Core.Sales.SalesParkedBalanceItem>> GetAdminSalesParkedBalancesAsync(
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/admin/sales/parked-balances", ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<Jobsy.Core.Sales.SalesParkedBalanceItem>>(cancellationToken: ct))
               ?? [];
    }

    public async Task BookAdminSalesLedgerCorrectionAsync(
        Guid beneficiaryUserId,
        Guid? companyId,
        decimal amountExVat,
        string reason,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/admin/sales/ledger/corrections",
            new { beneficiaryUserId, companyId, amountExVat, reason },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Correctie boeken mislukt.");
        }
    }

    public async Task ReassignAdminSalesAttributionAsync(
        Guid companyId,
        Guid? toBeneficiaryUserId,
        string reason,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/admin/sales/attribution/{companyId:D}",
            new { toBeneficiaryUserId, reason },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(ExtractMessage(body) ?? "Toewijzing wijzigen mislukt.");
        }
    }

    public async Task<List<Jobsy.Core.Sales.SalesAttributionHistoryItem>> GetAdminSalesAttributionHistoryAsync(
        Guid companyId,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/admin/sales/attribution/{companyId:D}/history", ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<Jobsy.Core.Sales.SalesAttributionHistoryItem>>(cancellationToken: ct))
               ?? [];
    }
}

public sealed class SalesIbanChangeBeginClientResult
{
    public string Method { get; set; } = "totp";
    public bool Applied { get; set; }
    public string? Message { get; set; }
    public Jobsy.Core.Sales.SalesPortalProfileDto? Profile { get; set; }
}
