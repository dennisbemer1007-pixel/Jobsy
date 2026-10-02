using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Core.Enums;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Web.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace Jobsy.Web.Services;

public sealed partial class JobsyApiClient
{
    public async Task<IReadOnlyList<TokenBalance>> GetTokenBalancesAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<TokenBalance>>("api/tokens/balance", ct) ?? [];

    public async Task<IReadOnlyList<TokenPackItem>> GetTokenPacksAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<TokenPackItem>>("api/tokens/packs", ct) ?? [];

    public async Task<IReadOnlyList<TokenSpendCostItem>> GetTokenCostsAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<TokenSpendCostItem>>("api/tokens/costs", ct) ?? [];

    public async Task<IReadOnlyList<TokenLogItem>> GetTokenLogsAsync(string? companyName = null, CancellationToken ct = default)
    {
        var url = "api/tokens/logs";
        if (!string.IsNullOrWhiteSpace(companyName))
        {
            url += $"?companyName={Uri.EscapeDataString(companyName)}";
        }

        return await _http.GetFromJsonAsync<List<TokenLogItem>>(url, ct) ?? [];
    }

    public async Task<TokenTopUpQuote?> GetTokenTopUpQuoteAsync(
        Guid companyId,
        decimal requiredTokens,
        CancellationToken ct = default)
    {
        var url =
            $"api/tokens/top-up-quote?companyId={companyId:D}&requiredTokens={requiredTokens.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        return await _http.GetFromJsonAsync<TokenTopUpQuote>(url, ct);
    }

    public async Task<CheckoutResult?> CreateTokenCheckoutAsync(
        Guid companyId,
        int packSize,
        PendingActionCheckoutRequest? pendingAction = null,
        string? paymentMethod = null,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/tokens/checkout",
            new { companyId, packSize, pendingAction, paymentMethod },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }

        return await response.Content.ReadFromJsonAsync<CheckoutResult>(cancellationToken: ct);
    }

    public async Task<CompleteCheckoutResult?> CompleteTokenCheckoutAsync(
        string paymentId,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/tokens/checkout/complete",
            new { paymentId },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }

        return await response.Content.ReadFromJsonAsync<CompleteCheckoutResult>(cancellationToken: ct);
    }

    public async Task<CompleteCheckoutResult?> CompleteTokenCheckoutBySessionAsync(
        Guid checkoutId,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/tokens/checkout/complete",
            new { checkoutId },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }

        return await response.Content.ReadFromJsonAsync<CompleteCheckoutResult>(cancellationToken: ct);
    }

    public async Task AllocateTokensAsync(
        Guid fromCompanyId,
        Guid toCompanyId,
        decimal amount,
        string? note = null,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/tokens/allocate",
            new { fromCompanyId, toCompanyId, amount, note },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }
    }

    public async Task GrantTokensAsync(
        Guid companyId,
        decimal amount,
        string note,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/tokens/goodwill",
            new { companyId, amount, note },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        }
    }

    public async Task<IReadOnlyList<TokenPurchaseFinanceItem>> GetTokenPurchasesAsync(
        int? year = null,
        int? quarter = null,
        CancellationToken ct = default)
    {
        var url = BuildTokenFinanceUrl("api/tokens/finance/purchases", year, quarter);
        return await _http.GetFromJsonAsync<List<TokenPurchaseFinanceItem>>(url, ct) ?? [];
    }

    public async Task<IReadOnlyList<ConsumerPurchaseFinanceItem>> GetConsumerPurchasesAsync(
        int? year = null,
        int? quarter = null,
        CancellationToken ct = default)
    {
        var url = BuildTokenFinanceUrl("api/tokens/finance/consumer-purchases", year, quarter);
        return await _http.GetFromJsonAsync<List<ConsumerPurchaseFinanceItem>>(url, ct) ?? [];
    }

    public async Task<IReadOnlyList<TokenGoodwillFinanceItem>> GetTokenGoodwillAsync(
        int? year = null,
        int? quarter = null,
        CancellationToken ct = default)
    {
        var url = BuildTokenFinanceUrl("api/tokens/finance/goodwill", year, quarter);
        return await _http.GetFromJsonAsync<List<TokenGoodwillFinanceItem>>(url, ct) ?? [];
    }

    public async Task<IReadOnlyList<VatBufferTransferItem>> GetVatBufferTransfersAsync(
        int? year = null,
        int? quarter = null,
        CancellationToken ct = default)
    {
        var url = BuildTokenFinanceUrl("api/tokens/finance/vat-transfers", year, quarter);
        return await _http.GetFromJsonAsync<List<VatBufferTransferItem>>(url, ct) ?? [];
    }

    public async Task DownloadTokenPurchasesCsvAsync(
        Microsoft.JSInterop.IJSRuntime js,
        int? year = null,
        int? quarter = null,
        CancellationToken ct = default)
    {
        var url = BuildTokenFinanceUrl("api/tokens/finance/purchases/export", year, quarter);
        var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var fileName = $"token-aankopen-{(year?.ToString() ?? "all")}-Q{(quarter?.ToString() ?? "all")}.csv";
        var base64 = Convert.ToBase64String(bytes);
        await SendBrowserDownloadAsync(js, fileName, base64, "text/csv;charset=utf-8");
    }

    public async Task DownloadTokenGoodwillCsvAsync(
        Microsoft.JSInterop.IJSRuntime js,
        int? year = null,
        int? quarter = null,
        CancellationToken ct = default)
    {
        var url = BuildTokenFinanceUrl("api/tokens/finance/goodwill/export", year, quarter);
        var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var fileName = $"token-goodwill-{(year?.ToString() ?? "all")}-Q{(quarter?.ToString() ?? "all")}.csv";
        var base64 = Convert.ToBase64String(bytes);
        await SendBrowserDownloadAsync(js, fileName, base64, "text/csv;charset=utf-8");
    }

    public async Task DownloadTokenInvoicePdfAsync(
        Microsoft.JSInterop.IJSRuntime js,
        Guid invoiceId,
        string invoiceNumber,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/tokens/invoices/{invoiceId}/pdf", ct);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var fileName = $"{invoiceNumber}.pdf";
        var base64 = Convert.ToBase64String(bytes);
        await SendBrowserDownloadAsync(js, fileName, base64, "application/pdf");
    }

    public async Task<IReadOnlyList<VatOpenPeriodItem>> GetVatOpenPeriodsAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<VatOpenPeriodItem>>("api/vat/open-periods", ct) ?? [];

    public async Task<VatDeclarationPreviewItem?> PreviewVatDeclarationAsync(
        int year,
        int quarter,
        CancellationToken ct = default)
        => await _http.GetFromJsonAsync<VatDeclarationPreviewItem>(
            $"api/vat/preview?year={year}&quarter={quarter}", ct);

    public async Task<VatDeclarationListItem?> GenerateVatDeclarationAsync(
        int year,
        int quarter,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/vat/generate", new { year, quarter }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(TryExtractMessage(body) ?? body);
        }

        return await response.Content.ReadFromJsonAsync<VatDeclarationListItem>(cancellationToken: ct);
    }

    public async Task<IReadOnlyList<VatDeclarationListItem>> GetVatDeclarationsAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<VatDeclarationListItem>>("api/vat/declarations", ct) ?? [];

    public async Task DownloadVatDeclarationPdfAsync(
        IJSRuntime js,
        Guid declarationId,
        string periodLabel,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/vat/declarations/{declarationId}/pdf", ct);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var fileName = $"BTW-aangifte-{periodLabel}.pdf";
        var base64 = Convert.ToBase64String(bytes);
        await SendBrowserDownloadAsync(js, fileName, base64, "application/pdf");
    }

    private static string BuildTokenFinanceUrl(string path, int? year, int? quarter)
    {
        var qs = new List<string>();
        if (year is int y)
        {
            qs.Add($"year={y}");
        }

        if (quarter is int q)
        {
            qs.Add($"quarter={q}");
        }

        return qs.Count == 0 ? path : $"{path}?{string.Join('&', qs)}";
    }
}
