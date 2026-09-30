using System.Text.Json;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Jobsy.Web.Components.Registration;

/// <summary>Wizard state that survives step changes; persisted via ProtectedSessionStorage (no passwords).</summary>
public sealed class RegistrationWizardState
{
    public const string StorageKey = "wa.register.wizard.v1";

    public int Step { get; set; } = 1;
    public string SearchQuery { get; set; } = "";
    public string SearchPlace { get; set; } = "";
    public string? SelectedKvk { get; set; }
    public string? CompanyName { get; set; }
    public string? LegalForm { get; set; }
    public string? CompanyAddress { get; set; }
    public List<string> Websites { get; set; } = [];
    public List<string> SbiCodes { get; set; } = [];
    public List<WizardEstablishment> Establishments { get; set; } = [];
    public HashSet<string> SelectedEstablishmentIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string Scope { get; set; } = "Organization"; // Organization | BranchOnly
    public bool ManualMode { get; set; }
    public string ManualName { get; set; } = "";
    public string ManualKvk { get; set; } = "";
    public string ManualAddress { get; set; } = "";
    public string ManualPostcodePlace { get; set; } = "";
    public string ManualEstablishmentNumber { get; set; } = "0001";
    public double? ManualLatitude { get; set; }
    public double? ManualLongitude { get; set; }
    public bool LocationUnknown { get; set; }
    public string ContactName { get; set; } = "";
    public string ContactFunction { get; set; } = "";
    public string ContactEmail { get; set; } = "";
    public string ContactPhone { get; set; } = "";
    public string PreferredLoginProvider { get; set; } = "password"; // password | microsoft | google
    public string? SalesCodeTyped { get; set; }
    public string? SalesCodeResolved { get; set; }
    public Guid? SalesManagerUserId { get; set; }
    public bool SalesCodeKnown { get; set; }
    public string? SalesCodeSource { get; set; }
    public Guid RegistrationId { get; set; }
    public DateTime? VerificationExpiresAt { get; set; }
    public bool InstantlyVerified { get; set; }
    public bool Done { get; set; }

    public bool IsIntermediarySbi =>
        SbiCodes.Any(s =>
        {
            var digits = new string((s ?? "").Where(char.IsDigit).ToArray());
            return digits.StartsWith("78", StringComparison.Ordinal);
        });

    public async Task PersistAsync(ProtectedSessionStorage storage)
    {
        var dto = Snapshot();
        await storage.SetAsync(StorageKey, JsonSerializer.Serialize(dto));
    }

    public async Task RestoreAsync(ProtectedSessionStorage storage)
    {
        var result = await storage.GetAsync<string>(StorageKey);
        if (!result.Success || string.IsNullOrWhiteSpace(result.Value))
        {
            return;
        }

        var dto = JsonSerializer.Deserialize<WizardSnapshot>(result.Value);
        if (dto is null)
        {
            return;
        }

        Apply(dto);
    }

    public async Task ClearAsync(ProtectedSessionStorage storage)
    {
        await storage.DeleteAsync(StorageKey);
    }

    private WizardSnapshot Snapshot() => new()
    {
        Step = Step,
        SearchQuery = SearchQuery,
        SearchPlace = SearchPlace,
        SelectedKvk = SelectedKvk,
        CompanyName = CompanyName,
        LegalForm = LegalForm,
        CompanyAddress = CompanyAddress,
        Websites = Websites,
        SbiCodes = SbiCodes,
        Establishments = Establishments,
        SelectedEstablishmentIds = SelectedEstablishmentIds.ToList(),
        Scope = Scope,
        ManualMode = ManualMode,
        ManualName = ManualName,
        ManualKvk = ManualKvk,
        ManualAddress = ManualAddress,
        ManualPostcodePlace = ManualPostcodePlace,
        ManualEstablishmentNumber = ManualEstablishmentNumber,
        ManualLatitude = ManualLatitude,
        ManualLongitude = ManualLongitude,
        LocationUnknown = LocationUnknown,
        ContactName = ContactName,
        ContactFunction = ContactFunction,
        ContactEmail = ContactEmail,
        ContactPhone = ContactPhone,
        PreferredLoginProvider = PreferredLoginProvider,
        SalesCodeTyped = SalesCodeTyped,
        SalesCodeResolved = SalesCodeResolved,
        SalesManagerUserId = SalesManagerUserId,
        SalesCodeKnown = SalesCodeKnown,
        SalesCodeSource = SalesCodeSource,
        RegistrationId = RegistrationId,
        VerificationExpiresAt = VerificationExpiresAt,
        InstantlyVerified = InstantlyVerified,
        Done = Done
    };

    private void Apply(WizardSnapshot dto)
    {
        Step = dto.Step;
        SearchQuery = dto.SearchQuery ?? "";
        SearchPlace = dto.SearchPlace ?? "";
        SelectedKvk = dto.SelectedKvk;
        CompanyName = dto.CompanyName;
        LegalForm = dto.LegalForm;
        CompanyAddress = dto.CompanyAddress;
        Websites = dto.Websites ?? [];
        SbiCodes = dto.SbiCodes ?? [];
        Establishments = dto.Establishments ?? [];
        SelectedEstablishmentIds = new HashSet<string>(
            dto.SelectedEstablishmentIds ?? [],
            StringComparer.OrdinalIgnoreCase);
        Scope = dto.Scope ?? "Organization";
        ManualMode = dto.ManualMode;
        ManualName = dto.ManualName ?? "";
        ManualKvk = dto.ManualKvk ?? "";
        ManualAddress = dto.ManualAddress ?? "";
        ManualPostcodePlace = dto.ManualPostcodePlace ?? "";
        ManualEstablishmentNumber = dto.ManualEstablishmentNumber ?? "0001";
        ManualLatitude = dto.ManualLatitude;
        ManualLongitude = dto.ManualLongitude;
        LocationUnknown = dto.LocationUnknown;
        ContactName = dto.ContactName ?? "";
        ContactFunction = dto.ContactFunction ?? "";
        ContactEmail = dto.ContactEmail ?? "";
        ContactPhone = dto.ContactPhone ?? "";
        PreferredLoginProvider = dto.PreferredLoginProvider ?? "password";
        SalesCodeTyped = dto.SalesCodeTyped;
        SalesCodeResolved = dto.SalesCodeResolved;
        SalesManagerUserId = dto.SalesManagerUserId;
        SalesCodeKnown = dto.SalesCodeKnown;
        SalesCodeSource = dto.SalesCodeSource;
        RegistrationId = dto.RegistrationId;
        VerificationExpiresAt = dto.VerificationExpiresAt;
        InstantlyVerified = dto.InstantlyVerified;
        Done = dto.Done;
    }

    private sealed class WizardSnapshot
    {
        public int Step { get; set; }
        public string? SearchQuery { get; set; }
        public string? SearchPlace { get; set; }
        public string? SelectedKvk { get; set; }
        public string? CompanyName { get; set; }
        public string? LegalForm { get; set; }
        public string? CompanyAddress { get; set; }
        public List<string>? Websites { get; set; }
        public List<string>? SbiCodes { get; set; }
        public List<WizardEstablishment>? Establishments { get; set; }
        public List<string>? SelectedEstablishmentIds { get; set; }
        public string? Scope { get; set; }
        public bool ManualMode { get; set; }
        public string? ManualName { get; set; }
        public string? ManualKvk { get; set; }
        public string? ManualAddress { get; set; }
        public string? ManualPostcodePlace { get; set; }
        public string? ManualEstablishmentNumber { get; set; }
        public double? ManualLatitude { get; set; }
        public double? ManualLongitude { get; set; }
        public bool LocationUnknown { get; set; }
        public string? ContactName { get; set; }
        public string? ContactFunction { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? PreferredLoginProvider { get; set; }
        public string? SalesCodeTyped { get; set; }
        public string? SalesCodeResolved { get; set; }
        public Guid? SalesManagerUserId { get; set; }
        public bool SalesCodeKnown { get; set; }
        public string? SalesCodeSource { get; set; }
        public Guid RegistrationId { get; set; }
        public DateTime? VerificationExpiresAt { get; set; }
        public bool InstantlyVerified { get; set; }
        public bool Done { get; set; }
    }
}

public sealed class WizardEstablishment
{
    public string KvkEstablishmentId { get; set; } = "";
    public string EstablishmentNumber { get; set; } = "";
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsInUse { get; set; }
    public bool IsHoofdvestiging { get; set; }
}
