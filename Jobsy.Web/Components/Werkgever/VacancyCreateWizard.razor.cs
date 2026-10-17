using Jobsy.Web.Components.Werkgever.Sections;
using Jobsy.Web.Components.Werkgever.VacancyWizard;
using Jobsy.Web.Components.Shared;
using Jobsy.Web.Components.KandidaatBanen;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Core.Enums;
using Jobsy.Core.Media;
using Jobsy.Core.Careers;
using Jobsy.Core.Rules;
using Jobsy.Core.Features;
using Jobsy.Web.Models;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Jobsy.Web.Components.Werkgever;

#pragma warning disable IDE0051, IDE0052 // Wizard partial: legacy hooks and rail navigation pending UI wiring

public partial class VacancyCreateWizard : IDisposable
{
    private static readonly System.Text.Json.JsonSerializerOptions ScheduleJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Inject] private JobsyApiClient Api { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;
    [Inject] private CultureState Culture { get; set; } = default!;
    [Inject] private IFeatureFlags FeatureFlags { get; set; } = default!;
    [Inject] private IJSRuntime Js { get; set; } = default!;

    private const int TotalSteps = 6;

    private static readonly IReadOnlyList<(int Id, string Title, string Hint)> WizardSteps =
    [
        (1, "Basis", "Functietitel, categorie en vestiging"),
        (2, "Omschrijving", "Tekst en optionele media"),
        (3, "Loon & uren", "Salaristabel en uren per week"),
        (4, "Rooster", "Dagdelen en werkzaamheden"),
        (5, "Eisen & contact", "Profiel, cultuur en contact"),
        (6, "Controleren", "Samenvatting voor publicatie")
    ];

    private int _step = 1;
    private int _maxStepReached = 1;
    private string? _stepMessage;
    private bool _previewOpen;
    private bool _previewMobileSheet;
    private bool _savedConfirmation;
    private Guid? _savedVacancyId;
    private DateTimeOffset? _savedAt;
    private bool _publishOpen;
    private VacancyListItem? _publishVacancy;
    private bool _wizardJsInit;

    private string CurrentStepTitle => WizardSteps.First(s => s.Id == _step).Title;

    private string CurrentStepLead => _step switch
    {
        1 => "Wat voor functie is het en waar?",
        2 => "Beschrijf de vacature voor kandidaten.",
        3 => "Kies een salaristabel en het aantal uren per week.",
        4 => "Wanneer wordt er gewerkt? Kies dagdelen of 'tijden in overleg'.",
        5 => "Optionele eisen, teamcultuur en contactvoorkeur.",
        6 => "Controleer alles voordat je het concept opslaat.",
        _ => string.Empty
    };

    private string NextStepButtonLabel => _step switch
    {
        1 => "Volgende: Omschrijving",
        2 => "Volgende: Loon & uren",
        3 => "Volgende: Rooster",
        4 => "Volgende: Eisen & contact",
        5 => "Volgende: Controleren",
        _ => "Opslaan als concept"
    };

    private VacancyListItem? PreviewVacancy =>
        VacancyPreviewMapper.FromForm(
            SelectedCompany,
            SelectedCategory,
            SelectedSalaryTable,
            _title,
            _description,
            _imageUrl,
            _videoUrl,
            _minHours,
            _maxHours,
            _flexibleTimes,
            _scheduleSlots,
            _selectedWorkTypes,
            _editVacancyId);

    [SupplyParameterFromQuery(Name = "from")]
    public Guid? From { get; set; }

    [SupplyParameterFromQuery(Name = "edit")]
    public Guid? Edit { get; set; }

    private string _title = "";
    private string? _occupationEscoId;
    private string? _occupationEscoLabel;
    private bool _occupationEscoConfirmed;
    private string _categoryId = "";
    private string _exclusivitySettingId = "";
    private List<ExclusivitySettingItem> _exclusivityOptions = [];
    private List<VacancyCategoryItem> _categories = [];
    private Dictionary<string, string> _categoryFields = new(StringComparer.OrdinalIgnoreCase);
    private bool _suitableFor65Plus;
    private string _description = "";
    private string _imageUrl = "";
    private string _videoUrl = "";
    private string _salaryTableId = "";
    private int? _minimumEmployers;
    private int? _minimumReferences;
    private List<int> _minEmployerOptions = [1, 2, 3];
    private List<int> _minReferenceOptions = [1, 2, 3];
    private bool _isIntermediary;
    private string? _vmPublishNote;
    private string _selectedEstablishmentId = "";
    private bool _kvkBusy;
    private bool _hideClientAddressOnMap = true;
    private int _publicMapRadiusKm = 2;
    private HashSet<string> _selectedWorkTypes = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _selectedLicenses = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _selectedEducationLevels = new(StringComparer.OrdinalIgnoreCase);
    private bool _barrierHigh;
    private string _barrierDiplomas = "";
    private readonly HashSet<string> _barrierCerts = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _barrierHardChecks = new(StringComparer.Ordinal);
    private string _barrierCertExtra = "";
    private int? _barrierYears;
    private int? _barrierHours;
    private HashSet<string> _selectedCulturePillars = new(StringComparer.OrdinalIgnoreCase);
    private bool _cultureError;
    private bool _teamOverride;
    private string _companyCultureName = "";
    private List<(string Label, int Percent)>? _companyCultureSummary;
    private List<string> _companyValueLabels = [];
    private List<string> _branchOptions = WorkTypeLabels.All.ToList();
    private bool _branchPickerExpanded;
    private const int BranchCollapsedChipCount = 4;
    private List<string> _licenseOptions = DrivingLicenseLabels.All.ToList();
    private List<string> _educationOptions = EducationLevelLabels.VacancyAll.ToList();
    private bool _saving;
    private bool _isDuplicate;
    private bool _accessDenied;
    private string? _message;
    private bool _moderationOpen;
    private string? _moderationWarning;
    private string? _moderationSuggestion;
    private Guid? _editVacancyId;
    private bool _proactiveMatchingOn;
    private DateOnly? _draftStartDate;
    private DateOnly? _draftEndDate;
    private Guid _companyId;
    private List<CompanySummary> _companies = [];
    private List<KvkEstablishmentItem> _clientEstablishments = [];
    private List<SalaryTableItem> _salaryTables = [];
    private bool _vacancyDirectContact;
    private bool _vacancyPreferMail;
    private bool _vacancyPreferPhone;
    private bool _vacancyPreferWhatsApp;
    private bool _requireEmailVerification;
    private decimal? _minHours = 8;
    private decimal? _maxHours = 16;
    private bool _flexibleTimes = true;
    private HashSet<string> _scheduleSlots = new(StringComparer.OrdinalIgnoreCase);
    private bool _legalWorksAfter19;
    private bool _legalNightShift;
    private bool _legalAdultSupervisor = true;
    private bool _legalHandlesMoney;
    private bool _legalHeavyWork;
    private string? _openLegalTip;

    private CompanySummary? SelectedCompany =>
        _companies.FirstOrDefault(c => c.Id == _companyId);

    private string SelectedCompanyName => SelectedCompany?.Name ?? "Vestiging";

    private bool SelectedCompanyIsUnverified =>
        SelectedCompany is not null
        && !string.Equals(SelectedCompany.VerificationStatus, "Verified", StringComparison.OrdinalIgnoreCase);

    private string PageHeading =>
        _editVacancyId is not null
            ? "Vacature bewerken"
            : (_isDuplicate ? Culture["Employer.DuplicateVacancy"] : Culture["Employer.CreateVacancy"]);

    private string MapRadiusPreview =>
        $"via {SelectedCategory?.Name ?? VacancyCategoryDefaults.UitzendbureauLabel} · {_publicMapRadiusKm} km";

    private bool ShowSuitableFor65PlusLegal =>
        !_isIntermediary && string.Equals(SelectedCategory?.Slug, "regulier", StringComparison.OrdinalIgnoreCase);

    private bool ShowJobCoachLegal =>
        !_isIntermediary && string.Equals(SelectedCategory?.Slug, "inclusief", StringComparison.OrdinalIgnoreCase);

    private static bool IsLegalStyleCategoryField(string key)
        => string.Equals(key, VacancyCategoryExtraFields.JobCoachAvailable, StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<string> CompanyChannelLabels(CompanySummary c)
    {
        if (c.ContactPreferMail) yield return "Mail";
        if (c.ContactPreferPhone) yield return "Telefoon";
        if (c.ContactPreferWhatsApp) yield return "WhatsApp";
    }

    private void OnVacancyDirectContactChanged()
    {
        if (!_vacancyDirectContact)
        {
            _vacancyPreferMail = false;
            _vacancyPreferPhone = false;
            _vacancyPreferWhatsApp = false;
        }
    }

    /// <summary>
    /// Only persist a vacancy-level override when the UI differs from company defaults.
    /// Matching company settings keeps inheritance (and company contact validation) intact.
    /// </summary>
    private bool ShouldOverrideContactPreference()
    {
        if (SelectedCompany is not { } company)
        {
            return true;
        }

        var companyMail = company.DirectContactEnabled && company.ContactPreferMail;
        var companyPhone = company.DirectContactEnabled && company.ContactPreferPhone;
        var companyWhatsApp = company.DirectContactEnabled && company.ContactPreferWhatsApp;
        return _vacancyDirectContact != company.DirectContactEnabled
               || _vacancyPreferMail != companyMail
               || _vacancyPreferPhone != companyPhone
               || _vacancyPreferWhatsApp != companyWhatsApp;
    }

    private void ApplyCompanyContactDefaults()
    {
        if (_editVacancyId is not null || _isDuplicate || SelectedCompany is not { } company)
        {
            return;
        }

        _vacancyDirectContact = company.DirectContactEnabled;
        _vacancyPreferMail = company.DirectContactEnabled && company.ContactPreferMail;
        _vacancyPreferPhone = company.DirectContactEnabled && company.ContactPreferPhone;
        _vacancyPreferWhatsApp = company.DirectContactEnabled && company.ContactPreferWhatsApp;
        if (_editVacancyId is null && !_isDuplicate)
        {
            _requireEmailVerification = company.RequireEmailVerificationForApplications;
        }
    }

    private SalaryTableItem? SelectedSalaryTable =>
        Guid.TryParse(_salaryTableId, out var sid)
            ? _salaryTables.FirstOrDefault(t => t.Id == sid)
            : null;

    protected override void OnInitialized()
        => Culture.Changed += OnCultureChanged;

    private void OnCultureChanged()
        => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        Culture.Changed -= OnCultureChanged;
        GC.SuppressFinalize(this);
    }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var flags = await FeatureFlags.GetAsync();
            _proactiveMatchingOn = flags.ProactiveMatchingEnabled;
        }
        catch (Exception)
        {
            _proactiveMatchingOn = false;
        }

        var profile = await Api.GetMyProfileAsync();
        var auth = await AuthState.GetAuthenticationStateAsync();
        _isIntermediary = string.Equals(profile?.Role, nameof(UserRole.Intermediary), StringComparison.OrdinalIgnoreCase)
            || auth.User.IsInRole("Intermediary");

        _companies = (await Api.GetMyCompaniesAsync()).ToList();
        if (_isIntermediary)
        {
            // End-client vestigingen only (KVK-backed employers), not the intermediary org itself.
            _companies = _companies
                .Where(c => !string.IsNullOrWhiteSpace(c.KvkNumber)
                            && !string.IsNullOrWhiteSpace(c.KvkEstablishmentId))
                .ToList();
        }

        if (_companies.Count > 0)
        {
            _companyId = _companies[0].Id;
        }
        else if (_isIntermediary)
        {
            _companyId = Guid.Empty;
        }

        await LoadMasterdataAsync();

        if (Edit is Guid editId)
        {
            await PrefillFromAsync(editId, duplicate: false);
        }
        else if (From is Guid sourceId)
        {
            await PrefillFromAsync(sourceId, duplicate: true);
        }
        else
        {
            await LoadCompanyCultureAsync();
            await PrefillDefaultBranchAsync();
        }

        await LoadSalaryTablesAsync();
        await LoadVmPublishNoteAsync(profile?.Role, auth.User);
    }

    private async Task LoadVmPublishNoteAsync(string? profileRole, System.Security.Claims.ClaimsPrincipal user)
    {
        var isVm = string.Equals(profileRole, nameof(UserRole.BranchManager), StringComparison.OrdinalIgnoreCase)
            || user.IsInRole("BranchManager");
        if (!isVm)
        {
            return;
        }

        try
        {
            var company = _companies.FirstOrDefault(c => c.Id == _companyId) ?? _companies.FirstOrDefault();
            if (company is null || !company.TokensManagedByEnterprise)
            {
                return;
            }

            var balances = await Api.GetTokenBalancesAsync();
            var balance = balances.FirstOrDefault(b => b.CompanyId == company.Id)?.Balance ?? company.TokenBalance;
            _vmPublishNote = string.Format(
                Culture["WgVac.Create.VmPublishNote"],
                balance.ToString("0.##"));
        }
        catch
        {
            // optional note
        }
    }

    private async Task PrefillDefaultBranchAsync()
    {
        if (_selectedWorkTypes.Count > 0 || _companyId == Guid.Empty)
        {
            return;
        }

        try
        {
            var extras = await Api.GetCompanyProfileExtrasAsync(_companyId);
            var first = extras?.WorkTypeLabels?.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(first))
            {
                _selectedWorkTypes.Add(first);
            }
        }
        catch
        {
            // Optional prefill — vacancy create still requires an explicit choice if empty.
        }
    }

    private async Task LoadCompanyCultureAsync()
    {
        _companyCultureSummary = null;
        _companyValueLabels = [];
        var company = _companies.FirstOrDefault(c => c.Id == _companyId);
        _companyCultureName = company?.Name ?? "";
        if (_companyId == Guid.Empty)
        {
            return;
        }

        try
        {
            var extras = await Api.GetCompanyProfileExtrasAsync(_companyId);
            if (extras?.CultureScores is { } scores
                && scores.Autonomy is int a)
            {
                _companyCultureSummary =
                [
                    ("Zelfstandig", scores.Autonomy ?? 0),
                    ("Informeel", scores.Informal ?? 0),
                    ("Samenwerken", scores.Collaboration ?? 0),
                    ("Flexibel", scores.Flexibility ?? 0),
                    ("Vernieuwend", scores.Innovation ?? 0),
                    ("Mensen voorop", scores.PeopleFirst ?? 0)
                ];
            }

            if (extras?.ValueCardIds is { Count: > 0 } cards)
            {
                _companyValueLabels = cards
                    .Select(id => CompanyValueCards.TryGet(id, out var c) ? DimensionLabels.For(c.DriverCode) : null)
                    .Where(x => x is not null)
                    .Cast<string>()
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
        }
        catch
        {
            // Inherit UI falls back to the empty-profile message.
        }
    }

    private void SetTeamOverride(bool enabled)
    {
        _teamOverride = enabled;
        if (!enabled)
        {
            _selectedCulturePillars.Clear();
            _cultureError = false;
        }
    }

    private async Task OnClientSearchSelectedAsync(KvkSearchHitItem hit)
    {
        _kvkBusy = true;
        _message = null;
        try
        {
            _clientEstablishments = (await Api.GetKvkEstablishmentsAsync(hit.KvkNumber)).ToList();
            _selectedEstablishmentId = "";
            if (_clientEstablishments.Count == 0)
            {
                _message = "Geen vestigingen gevonden voor dit bedrijf.";
            }
        }
        catch (Exception ex)
        {
            _message = ex.Message;
            _clientEstablishments = [];
        }
        finally
        {
            _kvkBusy = false;
        }
    }

    private async Task OnClientEstablishmentChangedAsync()
    {
        if (string.IsNullOrWhiteSpace(_selectedEstablishmentId))
        {
            return;
        }

        var selected = _clientEstablishments.FirstOrDefault(e =>
            string.Equals(e.KvkEstablishmentId, _selectedEstablishmentId, StringComparison.OrdinalIgnoreCase));
        if (selected is null)
        {
            return;
        }

        _kvkBusy = true;
        _message = null;
        try
        {
            var company = await Api.RegisterIntermediaryClientFromKvkAsync(
                selected.KvkNumber,
                selected.KvkEstablishmentId);
            if (company is null)
            {
                _message = "Opdrachtgever kon niet worden gekoppeld.";
                return;
            }

            var index = _companies.FindIndex(c => c.Id == company.Id);
            if (index >= 0)
            {
                _companies[index] = company;
            }
            else
            {
                _companies.Add(company);
            }

            _companyId = company.Id;
            _salaryTableId = "";
            await LoadSalaryTablesAsync();
        }
        catch (Exception ex)
        {
            _message = ex.Message;
        }
        finally
        {
            _kvkBusy = false;
        }
    }

    private ExclusivitySettingItem? SelectedExclusivity
        => _exclusivityOptions.FirstOrDefault(o => o.Id.ToString() == _exclusivitySettingId);

    private VacancyCategoryItem? SelectedCategory
        => _categories.FirstOrDefault(c => c.Id.ToString() == _categoryId);

    private IReadOnlyList<VacancyCategoryItem> CategoryPickerCategories
    {
        get
        {
            if (_categories.Count == 0)
            {
                return [];
            }

            var primary = _categories
                .Where(c => string.Equals(c.Slug, "regulier", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(c.Slug, "uitzendbureau", StringComparison.OrdinalIgnoreCase))
                .ToList();
            return primary.Count >= 2 ? primary : _categories;
        }
    }

    private void SelectCategory(VacancyCategoryItem category)
    {
        _categoryId = category.Id.ToString("D");
        OnCategoryChanged();
    }

    private static string CategoryPickerHint(VacancyCategoryItem category)
    {
        if (string.Equals(category.Slug, "regulier", StringComparison.OrdinalIgnoreCase))
        {
            return "Zelf werven";
        }

        if (string.Equals(category.Slug, "uitzendbureau", StringComparison.OrdinalIgnoreCase))
        {
            return "Via bureau";
        }

        if (string.Equals(category.PlacementKind, "Internship", StringComparison.OrdinalIgnoreCase))
        {
            return "Stageplek";
        }

        return category.PlacementKind switch
        {
            "Temporary" => "Tijdelijk",
            _ => "Vacaturetype"
        };
    }

    private void OnCategoryChanged()
    {
        var selected = SelectedCategory;
        if (selected is null)
        {
            return;
        }

        var allowed = selected.ExtraFields.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var key in _categoryFields.Keys.ToList())
        {
            if (!allowed.Contains(key))
            {
                _categoryFields.Remove(key);
            }
        }

        if (!string.Equals(selected.Slug, "regulier", StringComparison.OrdinalIgnoreCase))
        {
            _suitableFor65Plus = false;
        }

        if (selected.PlacementKind != "Internship")
        {
            _exclusivitySettingId = "";
            return;
        }

        if (string.IsNullOrWhiteSpace(_exclusivitySettingId)
            || _exclusivityOptions.All(o => o.Id.ToString() != _exclusivitySettingId))
        {
            var open = _exclusivityOptions.FirstOrDefault(o => o.IsOpenOption)
                       ?? _exclusivityOptions.FirstOrDefault();
            _exclusivitySettingId = open?.Id.ToString() ?? "";
        }
    }

    private string GetCategoryField(string key)
        => _categoryFields.TryGetValue(key, out var value) ? value : "";

    private void SetCategoryField(string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            _categoryFields.Remove(key);
        }
        else
        {
            _categoryFields[key] = value.Trim();
        }
    }

    private void SetCategoryBool(string key, ChangeEventArgs e)
    {
        var on = e.Value is bool b ? b : string.Equals(e.Value?.ToString(), "true", StringComparison.OrdinalIgnoreCase);
        SetCategoryField(key, on ? "ja" : "nee");
    }

    private async Task LoadMasterdataAsync()
    {
        try
        {
            var options = await Api.GetMasterdataAsync(audience: "vacancy");
            var branches = options.Where(o => o.Category == MasterdataCategories.Branch).Select(o => o.Label).ToList();
            var licenses = options.Where(o => o.Category == MasterdataCategories.DrivingLicense).Select(o => o.Label).ToList();
            var education = options.Where(o => o.Category == MasterdataCategories.EducationLevel).Select(o => o.Label).ToList();
            var minEmployers = options
                .Where(o => o.Category == MasterdataCategories.MinEmployers)
                .Select(o => int.TryParse(o.Value, out var n) ? n : (int?)null)
                .Where(n => n is >= 1)
                .Select(n => n!.Value)
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            if (branches.Count > 0) _branchOptions = branches;
            if (licenses.Count > 0) _licenseOptions = licenses;
            if (education.Count > 0) _educationOptions = education;
            if (minEmployers.Count > 0) _minEmployerOptions = minEmployers;
        }
        catch
        {
            // keep built-in fallbacks
        }

        try
        {
            _exclusivityOptions = (await Api.GetExclusivitySettingsAsync()).ToList();
        }
        catch
        {
            _exclusivityOptions = [];
        }

        try
        {
            _categories = (await Api.GetVacancyCategoriesAsync()).ToList();
            _categories = _categories
                .Where(c => c.Id != VacancyCategoryDefaults.HighlightId
                            && !string.Equals(c.Slug, "highlight", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (_isIntermediary)
            {
                ApplyIntermediaryCategory();
            }
            else if (string.IsNullOrWhiteSpace(_categoryId) || _categories.All(c => c.Id.ToString() != _categoryId))
            {
                var preferred = _categories.FirstOrDefault(c => c.Id == VacancyCategoryDefaults.RegulierId)
                    ?? _categories.FirstOrDefault(c => string.Equals(c.Slug, "regulier", StringComparison.OrdinalIgnoreCase))
                    ?? _categories.FirstOrDefault(c => !string.Equals(c.Slug, "uitzendbureau", StringComparison.OrdinalIgnoreCase));
                _categoryId = preferred?.Id.ToString() ?? "";
            }
            OnCategoryChanged();
        }
        catch
        {
            _categories = [];
            if (_isIntermediary)
            {
                ApplyIntermediaryCategory();
            }
        }
    }

    private void ApplyIntermediaryCategory()
    {
        var uitzend = _categories.FirstOrDefault(c => c.Id == VacancyCategoryDefaults.UitzendbureauId)
            ?? _categories.FirstOrDefault(c => c.Slug == "uitzendbureau");
        _categoryId = uitzend?.Id.ToString() ?? VacancyCategoryDefaults.UitzendbureauId.ToString("D");
        _suitableFor65Plus = false;
        _exclusivitySettingId = "";
        _categoryFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    private async Task PrefillFromAsync(Guid sourceId, bool duplicate)
    {
        try
        {
            var source = await Api.GetVacancyAsync(sourceId);
            if (source is null || _companies.All(c => c.Id != source.CompanyId))
            {
                _accessDenied = true;
                _message = null;
                return;
            }

            _editVacancyId = duplicate ? null : source.Id;
            _isDuplicate = duplicate;
            _companyId = source.CompanyId;
            _title = duplicate ? AppendCopySuffix(source.Title) : source.Title;
            _draftStartDate = duplicate ? null : source.StartDate;
            _draftEndDate = duplicate ? null : source.EndDate;
            if (_isIntermediary)
            {
                ApplyIntermediaryCategory();
            }
            else
            {
                _categoryId = source.CategoryId?.ToString()
                    ?? _categories.FirstOrDefault(c => string.Equals(c.PlacementKind, source.Kind, StringComparison.OrdinalIgnoreCase))?.Id.ToString()
                    ?? _categoryId;
                _exclusivitySettingId = source.ExclusivitySettingId?.ToString() ?? "";
                _categoryFields = source.CategoryFields is { Count: > 0 }
                    ? new Dictionary<string, string>(source.CategoryFields, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (_categoryFields.TryGetValue(VacancyCategoryExtraFields.OccupationEscoId, out var esco)
                    && !string.IsNullOrWhiteSpace(esco)
                    && OccupationCatalog.Shared.Get(esco.Trim()) is { } occ)
                {
                    _occupationEscoId = occ.Id;
                    _occupationEscoLabel = occ.Nl;
                    _occupationEscoConfirmed = true;
                }
                _suitableFor65Plus = source.SuitableFor65Plus;
            }
            OnCategoryChanged();
            _description = source.Description;
            _requireEmailVerification = source.RequireEmailVerification;
            _imageUrl = source.ImageUrl ?? "";
            _videoUrl = source.VideoUrl ?? "";
            _salaryTableId = source.SalaryTableId?.ToString() ?? "";
            _selectedWorkTypes = source.WorkTypes.ToHashSet(StringComparer.OrdinalIgnoreCase);
            _selectedLicenses = DrivingLicenseLabels.Split(source.RequiredDrivingLicense)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            _selectedEducationLevels = EducationLevelLabels.Split(source.RequiredEducation)
                .Where(x => EducationLevelLabels.VacancyAll.Contains(x, StringComparer.OrdinalIgnoreCase))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            _minimumEmployers = source.MinimumEmployers is int min && _minEmployerOptions.Contains(min)
                ? min
                : null;
            _minimumReferences = source.MinimumReferences is int refs && _minReferenceOptions.Contains(refs)
                ? refs
                : null;
            _selectedCulturePillars = CulturePillarCatalog.Normalize(source.CulturePillars)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            _teamOverride = CulturePillarCatalog.HasProfile(_selectedCulturePillars);
            await LoadCompanyCultureAsync();
            _barrierHigh = string.Equals(source.BarrierKind, "High", StringComparison.OrdinalIgnoreCase);
            _barrierDiplomas = string.Join(Environment.NewLine, source.BarrierDiplomas ?? []);
            _barrierCerts.Clear();
            foreach (var cert in source.BarrierCertifications ?? [])
            {
                if (VacancyBarrierCatalog.SuggestedCertifications.Contains(cert, StringComparer.OrdinalIgnoreCase))
                {
                    _barrierCerts.Add(VacancyBarrierCatalog.SuggestedCertifications
                        .First(c => string.Equals(c, cert, StringComparison.OrdinalIgnoreCase)));
                }
                else
                {
                    _barrierCertExtra = string.IsNullOrWhiteSpace(_barrierCertExtra) ? cert : _barrierCertExtra + ", " + cert;
                }
            }
            _barrierYears = source.BarrierMinExperienceYears;
            _barrierHours = source.BarrierMinExperienceHours;
            _barrierHardChecks.Clear();
            foreach (var kind in source.BarrierHardChecks ?? [])
            {
                var normalized = VacancyHardCheckCatalog.NormalizeKind(kind);
                if (normalized is not null)
                {
                    _barrierHardChecks.Add(normalized);
                }
            }
            _minHours = source.MinHoursPerWeek ?? 8;
            _maxHours = source.MaxHoursPerWeek ?? 16;
            _flexibleTimes = source.FlexibleTimes || string.IsNullOrWhiteSpace(source.ScheduleJson);
            _scheduleSlots = ParseScheduleSlots(source.ScheduleJson);
            if (_scheduleSlots.Count > 0)
            {
                _flexibleTimes = false;
            }

            _legalWorksAfter19 = source.LegalWorksAfter19 == true;
            _legalNightShift = source.LegalNightShift23To06 == true;
            _legalAdultSupervisor = source.LegalAdultSupervisorPresent != false;
            _legalHandlesMoney = source.LegalHandlesMoneyOrClosing == true;
            _legalHeavyWork = source.LegalHeavyOrHazardousWork == true;

            var contact = await Api.GetVacancyContactPreferenceAsync(sourceId);
            if (contact is not null)
            {
                if (contact.OverrideContactPreference)
                {
                    _vacancyDirectContact = contact.DirectContactEnabled;
                    _vacancyPreferMail = contact.ContactPreferMail;
                    _vacancyPreferPhone = contact.ContactPreferPhone;
                    _vacancyPreferWhatsApp = contact.ContactPreferWhatsApp;
                }
                else if (SelectedCompany is { } company)
                {
                    // Inherited company defaults: show effective channels, not empty vacancy flags.
                    _vacancyDirectContact = company.DirectContactEnabled;
                    _vacancyPreferMail = company.DirectContactEnabled && company.ContactPreferMail;
                    _vacancyPreferPhone = company.DirectContactEnabled && company.ContactPreferPhone;
                    _vacancyPreferWhatsApp = company.DirectContactEnabled && company.ContactPreferWhatsApp;
                }
                else
                {
                    _vacancyDirectContact = contact.DirectContactEnabled;
                    _vacancyPreferMail = contact.ContactPreferMail;
                    _vacancyPreferPhone = contact.ContactPreferPhone;
                    _vacancyPreferWhatsApp = contact.ContactPreferWhatsApp;
                }
            }

            SyncHoursFromCategoryFields();
        }
        catch (Exception)
        {
            _accessDenied = true;
            _message = null;
        }
    }

    private static string AppendCopySuffix(string title)
    {
        const string suffix = " (kopie)";
        return title.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            ? title
            : title + suffix;
    }

    private IReadOnlyList<string> BranchPickerVisibleLabels
    {
        get
        {
            if (_branchPickerExpanded || _branchOptions.Count <= BranchCollapsedChipCount)
            {
                return _branchOptions;
            }

            var visible = new List<string>();
            foreach (var label in _branchOptions.Take(BranchCollapsedChipCount - 1))
            {
                visible.Add(label);
            }

            foreach (var selected in _selectedWorkTypes)
            {
                if (!visible.Contains(selected, StringComparer.OrdinalIgnoreCase))
                {
                    visible.Add(selected);
                }
            }

            return visible;
        }
    }

    private bool ShowBranchMoreChip
        => !_branchPickerExpanded && _branchOptions.Count > BranchCollapsedChipCount;

    private Task ExpandBranchPicker()
    {
        _branchPickerExpanded = true;
        return Task.CompletedTask;
    }

    private Task ToggleWorkTypeChip(string label)
    {
        if (!_selectedWorkTypes.Remove(label) && _selectedWorkTypes.Count < WorkTypeLabels.MaxPerVacancy)
        {
            _selectedWorkTypes.Add(label);
        }

        return Task.CompletedTask;
    }

    private void ToggleWorkType(string label, ChangeEventArgs e)
    {
        var on = e.Value is bool b && b || e.Value?.ToString() == "on";
        if (on)
        {
            if (_selectedWorkTypes.Count >= WorkTypeLabels.MaxPerVacancy && !_selectedWorkTypes.Contains(label))
            {
                return;
            }

            _selectedWorkTypes.Add(label);
        }
        else
        {
            _selectedWorkTypes.Remove(label);
        }
    }

    private void ToggleLicense(string license, ChangeEventArgs e)
    {
        var on = e.Value is bool b && b || e.Value?.ToString() == "on";
        if (on)
        {
            _selectedLicenses.Add(license);
        }
        else
        {
            _selectedLicenses.Remove(license);
        }
    }

    private void ToggleEducation(string level, ChangeEventArgs e)
    {
        var on = e.Value is bool b && b || e.Value?.ToString() == "on";
        if (on)
        {
            _selectedEducationLevels.Add(level);
        }
        else
        {
            _selectedEducationLevels.Remove(level);
        }
    }

    private void SetMinimumEmployers(int? value) => _minimumEmployers = value;
    private void SetMinimumReferences(int? value) => _minimumReferences = value;
    private void SetBarrierHigh(bool value) => _barrierHigh = value;

    private void ToggleHardCheck(string kind, ChangeEventArgs e)
    {
        if (e.Value is bool on && on)
        {
            _barrierHardChecks.Add(kind);
        }
        else
        {
            _barrierHardChecks.Remove(kind);
        }
    }

    private void ToggleBarrierCert(string cert, ChangeEventArgs e)
    {
        var on = e.Value is bool b ? b : string.Equals(e.Value?.ToString(), "true", StringComparison.OrdinalIgnoreCase);
        if (on)
        {
            _barrierCerts.Add(cert);
        }
        else
        {
            _barrierCerts.Remove(cert);
        }
    }

    private static string[] SplitLines(string? raw)
        => string.IsNullOrWhiteSpace(raw)
            ? []
            : raw.Split(['\r', '\n', ',', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    private string[] BarrierCertList()
        => _barrierCerts.Concat(SplitLines(_barrierCertExtra)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    private void ToggleCulturePillar(string id, ChangeEventArgs e)
    {
        var on = e.Value is bool b ? b : string.Equals(e.Value?.ToString(), "true", StringComparison.OrdinalIgnoreCase);
        if (on)
        {
            if (_selectedCulturePillars.Count >= CulturePillarCatalog.MaxSelected)
            {
                _cultureError = true;
                return;
            }

            _selectedCulturePillars.Add(id);
        }
        else
        {
            _selectedCulturePillars.Remove(id);
        }

        _cultureError = _selectedCulturePillars.Count is < CulturePillarCatalog.MinSelected
            or > CulturePillarCatalog.MaxSelected;
    }

    private void OnFlexibleTimesChanged(ChangeEventArgs e)
    {
        _flexibleTimes = e.Value is bool b && b || e.Value?.ToString() == "on";
        if (_flexibleTimes)
        {
            _scheduleSlots.Clear();
        }
    }

    private Task OnFlexibleTimesChangedBool(bool value)
    {
        _flexibleTimes = value;
        if (_flexibleTimes)
        {
            _scheduleSlots.Clear();
        }

        return Task.CompletedTask;
    }

    private Task ToggleScheduleSlotFromChipAsync(string key)
    {
        if (!_scheduleSlots.Remove(key))
        {
            _flexibleTimes = false;
            _scheduleSlots.Add(key);
        }

        return Task.CompletedTask;
    }

    private Task OpenPreviewDesktopAsync()
        => OpenPreviewAsync(mobileSheet: false);

    private async Task OpenPreviewFromActionBarAsync()
    {
        var mobile = false;
        try
        {
            mobile = await Js.InvokeAsync<bool>("vacancyWizard.isMobilePreview");
        }
        catch (JSException)
        {
            // optional helper
        }

        await OpenPreviewAsync(mobile);
    }

    private void ToggleScheduleSlot(string key, ChangeEventArgs e)
    {
        var on = e.Value is bool b && b || e.Value?.ToString() == "on";
        if (on)
        {
            _flexibleTimes = false;
            _scheduleSlots.Add(key);
        }
        else
        {
            _scheduleSlots.Remove(key);
        }
    }

    private bool GetLegalFlag(string code) => code switch
    {
        nameof(LegalTaskFlags.WorksAfter19) => _legalWorksAfter19,
        nameof(LegalTaskFlags.NightShift23To06) => _legalNightShift,
        nameof(LegalTaskFlags.AdultSupervisorPresent) => _legalAdultSupervisor,
        nameof(LegalTaskFlags.HandlesMoneyOrClosing) => _legalHandlesMoney,
        nameof(LegalTaskFlags.HeavyOrHazardousWork) => _legalHeavyWork,
        _ => false
    };

    private void SetLegalFlag(string code, ChangeEventArgs e)
    {
        var on = e.Value is bool b && b || e.Value?.ToString() == "on";
        switch (code)
        {
            case nameof(LegalTaskFlags.WorksAfter19):
                _legalWorksAfter19 = on;
                break;
            case nameof(LegalTaskFlags.NightShift23To06):
                _legalNightShift = on;
                break;
            case nameof(LegalTaskFlags.AdultSupervisorPresent):
                _legalAdultSupervisor = on;
                break;
            case nameof(LegalTaskFlags.HandlesMoneyOrClosing):
                _legalHandlesMoney = on;
                break;
            case nameof(LegalTaskFlags.HeavyOrHazardousWork):
                _legalHeavyWork = on;
                break;
        }
    }

    private void ToggleLegalTip(string code)
        => _openLegalTip = string.Equals(_openLegalTip, code, StringComparison.Ordinal) ? null : code;

    private static HashSet<string> ParseScheduleSlots(string? scheduleJson)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(scheduleJson))
        {
            return set;
        }

        try
        {
            var payload = System.Text.Json.JsonSerializer.Deserialize<SchedulePayload>(
                scheduleJson,
                ScheduleJsonOptions);
            if (payload?.Slots is null)
            {
                return set;
            }

            foreach (var (day, parts) in payload.Slots)
            {
                foreach (var part in parts)
                {
                    set.Add($"{day}:{part}");
                }
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // ignore corrupt schedule on duplicate prefill
        }

        return set;
    }

    private Dictionary<string, string[]> BuildScheduleSlotsPayload()
    {
        var result = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        if (_flexibleTimes)
        {
            return result;
        }

        foreach (var group in _scheduleSlots
                     .Select(k => k.Split(':', 2))
                     .Where(p => p.Length == 2)
                     .GroupBy(p => p[0], StringComparer.OrdinalIgnoreCase))
        {
            result[group.Key] = group.Select(p => p[1]).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }

        return result;
    }

    private async Task LoadSalaryTablesAsync()
    {
        if (_companyId == Guid.Empty)
        {
            _salaryTables = [];
            return;
        }

        try
        {
            _salaryTables = (await Api.GetSalaryTablesAsync(_companyId))
                .Where(t => t.IsActive && t.Rates.Count > 0)
                .ToList();
            if (string.IsNullOrWhiteSpace(_salaryTableId))
            {
                var wml = _salaryTables.FirstOrDefault(t => t.IsSystemWml)
                          ?? _salaryTables.FirstOrDefault(t =>
                              t.Name.Equals("Wettelijk Minimumloon", StringComparison.OrdinalIgnoreCase)
                              || t.Name.Equals("WML", StringComparison.OrdinalIgnoreCase));
                if (wml is not null)
                {
                    _salaryTableId = wml.Id.ToString();
                }
            }
            else if (_salaryTables.All(t => t.Id.ToString() != _salaryTableId))
            {
                var wml = _salaryTables.FirstOrDefault(t => t.IsSystemWml)
                          ?? _salaryTables.FirstOrDefault(t =>
                              t.Name.Equals("Wettelijk Minimumloon", StringComparison.OrdinalIgnoreCase)
                              || t.Name.Equals("WML", StringComparison.OrdinalIgnoreCase));
                _salaryTableId = wml?.Id.ToString() ?? _salaryTables.FirstOrDefault()?.Id.ToString() ?? "";
            }

            ApplyCompanyContactDefaults();
        }
        catch
        {
            _salaryTables = [];
        }
    }

    private Task GoBackAsync()
    {
        _stepMessage = null;
        _savedConfirmation = false;
        if (_step > 1)
        {
            _step--;
        }

        return Task.CompletedTask;
    }

    private Task GoNextAsync()
    {
        if (!TryValidateStep(_step, out var error))
        {
            _stepMessage = error;
            return Task.CompletedTask;
        }

        _stepMessage = null;
        if (_step < TotalSteps)
        {
            _step++;
            _maxStepReached = Math.Max(_maxStepReached, _step);
        }

        return Task.CompletedTask;
    }

    private Task GoToStepAsync(int step)
    {
        if (step < 1 || step > TotalSteps || step > _maxStepReached + 1)
        {
            return Task.CompletedTask;
        }

        if (step > _step)
        {
            for (var s = _step; s < step; s++)
            {
                if (!TryValidateStep(s, out var error))
                {
                    _stepMessage = error;
                    _step = s;
                    return Task.CompletedTask;
                }
            }
        }

        _stepMessage = null;
        _step = step;
        _maxStepReached = Math.Max(_maxStepReached, _step);
        return Task.CompletedTask;
    }

    private Task OnFormSubmitAsync()
        => _step == TotalSteps ? SaveAsync() : GoNextAsync();

    private Task OpenPreviewAsync(bool mobileSheet = false)
    {
        _previewMobileSheet = mobileSheet;
        _previewOpen = true;
        return Task.CompletedTask;
    }

    private Task ClosePreviewAsync()
    {
        _previewOpen = false;
        return Task.CompletedTask;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_wizardJsInit)
        {
            _wizardJsInit = true;
            try
            {
                await Js.InvokeVoidAsync("vacancyWizard.init");
            }
            catch (JSException)
            {
                // optional layout helper
            }
        }
    }

    private bool TryValidateStep(int step, out string? error)
    {
        error = null;
        switch (step)
        {
            case 1:
                if (_isIntermediary
                    && (SelectedCompany is null
                        || string.IsNullOrWhiteSpace(SelectedCompany.KvkNumber)
                        || string.IsNullOrWhiteSpace(SelectedCompany.KvkEstablishmentId)))
                {
                    error = "Zoek een KVK-nummer en selecteer een KVK-vestiging van het inhuurende bedrijf.";
                    return false;
                }

                if (_companyId == Guid.Empty)
                {
                    error = _isIntermediary
                        ? "Selecteer eerst het inhuurende bedrijf via KVK + vestiging."
                        : "Geen vestiging gekoppeld aan dit account.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(_title))
                {
                    error = "Vul een functietitel in.";
                    return false;
                }

                if (!_isIntermediary && string.IsNullOrWhiteSpace(_categoryId))
                {
                    error = "Kies een categorie.";
                    return false;
                }

                var branchCount = _selectedWorkTypes.Count(x => !string.IsNullOrWhiteSpace(x));
                if (branchCount is < 1 or > WorkTypeLabels.MaxPerVacancy)
                {
                    error = $"Kies 1 of {WorkTypeLabels.MaxPerVacancy} branches.";
                    return false;
                }

                return true;
            case 2:
                if (string.IsNullOrWhiteSpace(_description))
                {
                    error = "Vul een omschrijving in.";
                    return false;
                }

                return true;
            case 3:
                if (!Guid.TryParse(_salaryTableId, out var salaryTableId)
                    || _salaryTables.All(t => t.Id != salaryTableId))
                {
                    error = "Kies een salaristabel.";
                    return false;
                }

                if (_minHours is not decimal minHours || _maxHours is not decimal maxHours)
                {
                    error = "Vul minimum- en maximumuren per week in.";
                    return false;
                }

                error = HoursRangeRules.Validate(minHours, maxHours);
                if (error is not null)
                {
                    return false;
                }

                return true;
            case 4:
                if (!_flexibleTimes && _scheduleSlots.Count == 0)
                {
                    error = "Selecteer dagdelen of kies ‘Tijden in overleg’.";
                    return false;
                }

                return true;
            case 5:
                if (ShowJobCoachLegal
                    && string.IsNullOrWhiteSpace(GetCategoryField(VacancyCategoryExtraFields.TargetGroup)))
                {
                    error = "Doelgroep is verplicht voor inclusieve vacatures.";
                    return false;
                }

                if (_teamOverride && !CulturePillarCatalog.HasProfile(_selectedCulturePillars))
                {
                    _cultureError = true;
                    error = Culture["Employer.Culture.Error"];
                    return false;
                }

                if (_vacancyDirectContact
                    && !_vacancyPreferMail
                    && !_vacancyPreferPhone
                    && !_vacancyPreferWhatsApp)
                {
                    error = "Kies minstens één contactoptie (Mail, Telefoon of WhatsApp).";
                    return false;
                }

                return true;
            case 6:
                return true;
            default:
                return true;
        }
    }

    private void CloseModeration()
    {
        _moderationOpen = false;
    }

    private async Task SaveAsync()
    {
        if (_isIntermediary
            && (SelectedCompany is null
                || string.IsNullOrWhiteSpace(SelectedCompany.KvkNumber)
                || string.IsNullOrWhiteSpace(SelectedCompany.KvkEstablishmentId)))
        {
            _message = "Zoek een KVK-nummer en selecteer een KVK-vestiging van het inhuurende bedrijf.";
            return;
        }

        if (_companyId == Guid.Empty)
        {
            _message = _isIntermediary
                ? "Selecteer eerst het inhuurende bedrijf via KVK + vestiging."
                : "Geen vestiging gekoppeld aan dit account.";
            return;
        }

        if (!Guid.TryParse(_salaryTableId, out var salaryTableId))
        {
            _message = "Kies een salaristabel.";
            return;
        }

        var table = _salaryTables.FirstOrDefault(t => t.Id == salaryTableId);
        if (table is null || table.Rates.Count == 0)
        {
            _message = "Geselecteerde salaristabel heeft geen tarieven.";
            return;
        }

        var adultRate = table.Rates
            .Where(r => r.AgeYears >= 21)
            .OrderBy(r => r.AgeYears)
            .FirstOrDefault()
            ?? table.Rates.OrderByDescending(r => r.AgeYears).First();

        var branchLabels = _selectedWorkTypes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(WorkTypeLabels.MaxPerVacancy)
            .ToArray();
        if (branchLabels.Length is < 1 or > WorkTypeLabels.MaxPerVacancy)
        {
            _message = $"Kies 1 of {WorkTypeLabels.MaxPerVacancy} branches.";
            return;
        }

        if (_vacancyDirectContact
            && !_vacancyPreferMail
            && !_vacancyPreferPhone
            && !_vacancyPreferWhatsApp)
        {
            _message = "Kies minstens één contactoptie (Mail, Telefoon of WhatsApp).";
            return;
        }

        if (ShowJobCoachLegal
            && string.IsNullOrWhiteSpace(GetCategoryField(VacancyCategoryExtraFields.TargetGroup)))
        {
            _message = "Doelgroep is verplicht voor inclusieve vacatures.";
            return;
        }

        if (_minHours is not decimal minHours || _maxHours is not decimal maxHours)
        {
            _message = "Vul minimum- en maximumuren per week in.";
            return;
        }

        var hoursError = HoursRangeRules.Validate(minHours, maxHours);
        if (hoursError is not null)
        {
            _message = hoursError;
            return;
        }

        if (!_flexibleTimes && _scheduleSlots.Count == 0)
        {
            _message = "Selecteer dagdelen of kies ‘Tijden in overleg’.";
            return;
        }

        if (_teamOverride && !CulturePillarCatalog.HasProfile(_selectedCulturePillars))
        {
            _cultureError = true;
            _message = Culture["Employer.Culture.Error"];
            return;
        }

        if (!_teamOverride)
        {
            _selectedCulturePillars.Clear();
        }

        _saving = true;
        _message = null;
        try
        {
            if (_isIntermediary)
            {
                ApplyIntermediaryCategory();
            }

            SyncCategoryHoursFromRange();
            _description = HtmlSanitize.ToVacancyDescriptionHtml(_description);

            var today = DateOnly.FromDateTime(DateTime.Today);
            var startDate = _editVacancyId is not null ? _draftStartDate ?? today : today;
            var endDate = _editVacancyId is not null ? _draftEndDate ?? startDate.AddMonths(3) : today.AddMonths(3);
            var form = new CreateVacancyForm(
                _companyId,
                _title,
                _description,
                adultRate.HourlyRate,
                startDate,
                endDate,
                TransportMode.Bike,
                branchLabels,
                string.IsNullOrWhiteSpace(_imageUrl) ? null : _imageUrl.Trim(),
                string.IsNullOrWhiteSpace(_videoUrl) ? null : _videoUrl.Trim(),
                salaryTableId,
                DrivingLicenseLabels.Combine(_selectedLicenses),
                EducationLevelLabels.Combine(_selectedEducationLevels, forVacancy: true),
                _minimumEmployers is int min && _minEmployerOptions.Contains(min) ? min : null,
                ShouldOverrideContactPreference(),
                _vacancyDirectContact,
                _vacancyPreferMail,
                _vacancyPreferPhone,
                _vacancyPreferWhatsApp,
                minHours,
                maxHours,
                _flexibleTimes,
                _flexibleTimes ? null : BuildScheduleSlotsPayload(),
                _legalWorksAfter19,
                _legalNightShift,
                _legalAdultSupervisor,
                _legalHandlesMoney,
                _legalHeavyWork,
                ShowClientAddressOnMap: _isIntermediary && !_hideClientAddressOnMap,
                Kind: SelectedCategory?.PlacementKind ?? "Regular",
                ExclusivitySettingId: SelectedCategory?.PlacementKind == "Internship"
                    && Guid.TryParse(_exclusivitySettingId, out var exclId)
                    ? exclId
                    : null,
                CategoryId: Guid.TryParse(_categoryId, out var catId) ? catId : null,
                CategoryFields: _categoryFields.Count > 0 ? new Dictionary<string, string>(_categoryFields) : null,
                SuitableFor65Plus: SelectedCategory?.Slug == "regulier" && _suitableFor65Plus,
                RequireEmailVerification: _requireEmailVerification,
                MinimumReferences: _minimumReferences is int refs && _minReferenceOptions.Contains(refs) ? refs : null,
                CulturePillars: _selectedCulturePillars.ToArray(),
                BarrierKind: _barrierHigh ? "High" : "Low",
                BarrierDiplomas: SplitLines(_barrierDiplomas),
                BarrierCertifications: BarrierCertList(),
                BarrierMinExperienceYears: _barrierHigh ? _barrierYears : null,
                BarrierMinExperienceHours: _barrierHigh ? _barrierHours : null,
                BarrierHardChecks: _barrierHigh ? _barrierHardChecks.ToArray() : null,
                PublicMapRadiusKm: _isIntermediary ? _publicMapRadiusKm : null);

            var saved = _editVacancyId is Guid editId
                ? await Api.UpdateVacancyAsync(editId, form)
                : await Api.CreateVacancyAsync(form);

            if (saved?.Id is Guid savedId)
            {
                _editVacancyId = savedId;
                _savedVacancyId = savedId;
            }

            _savedAt = DateTimeOffset.Now;
            _savedConfirmation = true;
            _maxStepReached = TotalSteps;

            if (!string.IsNullOrWhiteSpace(saved?.ModerationWarning))
            {
                _moderationWarning = saved.ModerationWarning;
                _moderationOpen = true;
            }
        }
        catch (VacancyModerationException ex)
        {
            _moderationWarning = ex.Warning;
            _moderationSuggestion = ex.Suggestion;
            _moderationOpen = true;
        }
        catch (Exception ex)
        {
            _message = ex.Message;
        }
        finally
        {
            _saving = false;
        }
    }

    private void SyncCategoryHoursFromRange()
    {
        if (_minHours is not decimal min || _maxHours is not decimal max)
        {
            return;
        }

        if (!CategoryUsesField(VacancyCategoryExtraFields.HoursPerWeek))
        {
            _categoryFields.Remove(VacancyCategoryExtraFields.HoursPerWeek);
            return;
        }

        _categoryFields[VacancyCategoryExtraFields.HoursPerWeek] = $"{min:0.#}–{max:0.#}";
    }

    private void OnTitleInput()
    {
        if (_occupationEscoConfirmed)
        {
            return;
        }

        var resolved = OccupationCatalog.Shared.Resolve(_title);
        if (resolved is null)
        {
            _occupationEscoId = null;
            _occupationEscoLabel = null;
            _categoryFields.Remove(VacancyCategoryExtraFields.OccupationEscoId);
            return;
        }

        _occupationEscoId = resolved.Id;
        _occupationEscoLabel = resolved.Nl;
    }

    private void ConfirmOccupationEsco()
    {
        if (string.IsNullOrWhiteSpace(_occupationEscoId))
        {
            return;
        }

        _occupationEscoConfirmed = true;
        _categoryFields[VacancyCategoryExtraFields.OccupationEscoId] = _occupationEscoId;
    }

    private void ClearOccupationEsco()
    {
        _occupationEscoConfirmed = false;
        _occupationEscoId = null;
        _occupationEscoLabel = null;
        _categoryFields.Remove(VacancyCategoryExtraFields.OccupationEscoId);
        OnTitleInput();
    }

    private void SyncHoursFromCategoryFields()
    {
        var raw = GetCategoryField(VacancyCategoryExtraFields.HoursPerWeek);
        if (string.IsNullOrEmpty(raw))
        {
            return;
        }

        var parts = raw.Split('–', '-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2
            && decimal.TryParse(parts[0].Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var min)
            && decimal.TryParse(parts[1].Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var max))
        {
            _minHours = min;
            _maxHours = max;
        }
    }

    private bool CategoryUsesField(string key)
        => SelectedCategory?.ExtraFieldDefinitions?.Any(f => string.Equals(f.Key, key, StringComparison.OrdinalIgnoreCase)) == true;

    private static bool IsChipCategoryField(string key)
        => string.Equals(key, VacancyCategoryExtraFields.ContractType, StringComparison.OrdinalIgnoreCase)
           || string.Equals(key, VacancyCategoryExtraFields.ExperienceLevel, StringComparison.OrdinalIgnoreCase);

    private static bool IsMergedHoursCategoryField(string key)
        => string.Equals(key, VacancyCategoryExtraFields.HoursPerWeek, StringComparison.OrdinalIgnoreCase);

    private void SelectCategoryOption(string key, string value)
    {
        SetCategoryField(key, value);
        StateHasChanged();
    }

    private Task OpenPublishAsync()
    {
        if (_savedVacancyId is not Guid id)
        {
            return Task.CompletedTask;
        }

        _publishVacancy = PreviewVacancy;
        if (_publishVacancy is not null)
        {
            _publishVacancy.Id = id;
        }
        _publishOpen = true;
        return Task.CompletedTask;
    }

    private Task ContinueEditingAsync()
    {
        _savedConfirmation = false;
        _step = Math.Min(_step, TotalSteps);
        return Task.CompletedTask;
    }

    private Task ContinueEditingFromSavedAsync()
    {
        _savedConfirmation = false;
        _step = 1;
        return Task.CompletedTask;
    }

    private Task ClosePublishAsync()
    {
        _publishOpen = false;
        _publishVacancy = null;
        return Task.CompletedTask;
    }

    private Task OnPublishedAsync(VacancyProductActionResult? result)
    {
        _message = result?.PendingApproval == true
            ? (result.Message ?? "Wacht op goedkeuring.")
            : "Vacature gepubliceerd.";
        _publishOpen = false;
        return Task.CompletedTask;
    }

    private bool ChecklistTitleOk => !string.IsNullOrWhiteSpace(_title) && (!_isIntermediary || !string.IsNullOrWhiteSpace(_categoryId));
    private bool ChecklistDescriptionOk => !string.IsNullOrWhiteSpace(_description);
    private bool ChecklistSalaryOk => Guid.TryParse(_salaryTableId, out _);
    private bool ChecklistScheduleOk => _flexibleTimes || _scheduleSlots.Count > 0;

    private string ScheduleSummaryLabel
    {
        get
        {
            if (_flexibleTimes)
            {
                return "Tijden in overleg";
            }

            if (_scheduleSlots.Count == 0)
            {
                return "—";
            }

            var count = _scheduleSlots.Count;
            return count == 1 ? "1 dagdeel" : $"{count} dagdelen";
        }
    }

}
