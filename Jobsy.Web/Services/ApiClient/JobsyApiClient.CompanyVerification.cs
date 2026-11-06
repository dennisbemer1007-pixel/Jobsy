using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Jobsy.Web.Services;

public sealed partial class JobsyApiClient
{
    public async Task<CompanyVerificationOptionsItem?> GetCompanyVerificationOptionsAsync(
        CancellationToken ct = default)
        => await _http.GetFromJsonAsync<CompanyVerificationOptionsItem>(
            "api/company-verification/options", ct);

    public async Task<CompanyVerificationEmailStartItem> StartCompanyEmailVerificationAsync(
        string email,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/company-verification/email/start",
            new { email },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadFromJsonAsync<CompanyVerificationErrorItem>(cancellationToken: ct);
            throw new InvalidOperationException(err?.Message ?? "Kon code niet versturen.");
        }

        return await response.Content.ReadFromJsonAsync<CompanyVerificationEmailStartItem>(cancellationToken: ct)
               ?? new CompanyVerificationEmailStartItem();
    }

    public async Task ConfirmCompanyEmailVerificationAsync(string code, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/company-verification/email/confirm",
            new { code },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadFromJsonAsync<CompanyVerificationErrorItem>(cancellationToken: ct);
            throw new InvalidOperationException(err?.Message ?? "Code onjuist.");
        }
    }

    public async Task<CompanyVerificationLetterStartItem> StartCompanyLetterVerificationAsync(
        bool resend = false,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/company-verification/letter",
            new { resend },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadFromJsonAsync<CompanyVerificationErrorItem>(cancellationToken: ct);
            throw new InvalidOperationException(err?.Message ?? "Kon brief niet versturen.");
        }

        return await response.Content.ReadFromJsonAsync<CompanyVerificationLetterStartItem>(cancellationToken: ct)
               ?? new CompanyVerificationLetterStartItem();
    }

    public async Task ConfirmCompanyLetterVerificationAsync(string code, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/company-verification/letter/confirm",
            new { code },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadFromJsonAsync<CompanyVerificationErrorItem>(cancellationToken: ct);
            throw new InvalidOperationException(err?.Message ?? "Code onjuist.");
        }
    }

    public async Task RequestCompanyManualVerificationAsync(
        string reason,
        string? message,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/company-verification/manual",
            new { reason, message },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadFromJsonAsync<CompanyVerificationErrorItem>(cancellationToken: ct);
            throw new InvalidOperationException(err?.Message ?? "Kon verzoek niet indienen.");
        }
    }

    public async Task<IReadOnlyList<AdminCompanyVerificationItem>> GetAdminCompanyVerificationQueueAsync(
        string? tab = null,
        CancellationToken ct = default)
    {
        var url = string.IsNullOrWhiteSpace(tab)
            ? "api/admin/company-verification"
            : $"api/admin/company-verification?tab={Uri.EscapeDataString(tab)}";
        return await _http.GetFromJsonAsync<List<AdminCompanyVerificationItem>>(url, ct) ?? [];
    }

    public async Task<int> GetAdminCompanyVerificationCountAsync(CancellationToken ct = default)
    {
        var dto = await _http.GetFromJsonAsync<AdminCountItem>("api/admin/company-verification/count", ct);
        return dto?.Count ?? 0;
    }

    public async Task ApproveCompanyVerificationAsync(Guid companyId, string? note, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/admin/company-verification/{companyId:D}/approve",
            new { note },
            ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task RejectCompanyVerificationAsync(Guid companyId, string reason, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/admin/company-verification/{companyId:D}/reject",
            new { reason },
            ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task AdminSendCompanyVerificationLetterAsync(Guid companyId, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/admin/company-verification/{companyId:D}/letter",
            new { },
            ct);
        response.EnsureSuccessStatusCode();
    }

    public string GetAdminStubLetterPdfUrl(Guid letterId)
        => $"api/admin/company-verification/letters/{letterId:D}/stub-pdf";
}

public sealed class CompanyVerificationOptionsItem
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = "";
    public string KvkNumber { get; set; } = "";
    public string Status { get; set; } = "";
    public string Method { get; set; } = "";
    public bool EmailAvailable { get; set; }
    public List<string> WebsiteDomains { get; set; } = [];
    public string? SuggestedEmail { get; set; }
    public bool LetterAvailable { get; set; }
    public string? LetterAddressMasked { get; set; }
    public string? LetterUnavailableReason { get; set; }
    public bool ManualPending { get; set; }
    public Guid? ActiveLetterId { get; set; }
    public DateTime? LetterSentAtUtc { get; set; }
    public DateTime? LetterExpiresAtUtc { get; set; }
    public int LetterResendsRemaining { get; set; }
    public DateTime? LetterResendAvailableAtUtc { get; set; }
}

public sealed class CompanyVerificationEmailStartItem
{
    public DateTime? ExpiresAtUtc { get; set; }
    public string? MaskedEmail { get; set; }
}

public sealed class CompanyVerificationLetterStartItem
{
    public Guid? LetterId { get; set; }
    public string? AddressMasked { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public DateTime? ResendAvailableAtUtc { get; set; }
    public int ResendsRemaining { get; set; }
}

public sealed class CompanyVerificationErrorItem
{
    public string? Error { get; set; }
    public string? Message { get; set; }
}

public sealed class AdminCompanyVerificationItem
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = "";
    public string KvkNumber { get; set; } = "";
    public string Tab { get; set; } = "";
    public string Status { get; set; } = "";
    public string? Reason { get; set; }
    public string RequesterName { get; set; } = "";
    public string? RequesterEmail { get; set; }
    public string? RequesterFunction { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public List<string> Heuristics { get; set; } = [];
    public int PriorRejections { get; set; }
    public string? KvkAddress { get; set; }
    public List<string> Websites { get; set; } = [];
    public List<string> SbiCodes { get; set; } = [];
    public Guid? OpenManualRequestId { get; set; }
    public Guid? BlockedLetterId { get; set; }
    public bool StubLetterAvailable { get; set; }
}

public sealed class AdminCountItem
{
    public int Count { get; set; }
}
