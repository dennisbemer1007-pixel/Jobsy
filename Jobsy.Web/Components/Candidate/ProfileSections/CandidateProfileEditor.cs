using Jobsy.Core.Rules;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Jobsy.Web.Components.Candidate.ProfileSections;

/// <summary>
/// Shared candidate profile form state (classic Profile + passport Bewijzen/Gegevens).
/// Scoped per Blazor circuit.
/// </summary>
public sealed class CandidateProfileEditor : IDisposable
{
    public const int MaxReferences = 3;

    private readonly JobsyApiClient _api;
    private readonly IGeocodingClient _geocoder;
    private readonly NavigationManager _navigation;
    private readonly CultureState _culture;
    private readonly IJSRuntime _js;

    private CancellationTokenSource? _suggestCts;
    private int _suggestGeneration;

    public CandidateProfileEditor(
        JobsyApiClient api,
        IGeocodingClient geocoder,
        NavigationManager navigation,
        CultureState culture,
        IJSRuntime js)
    {
        _api = api;
        _geocoder = geocoder;
        _navigation = navigation;
        _culture = culture;
        _js = js;
    }

    public event Action? Changed;

    /// <summary>Optional hook after a successful save (e.g. reload matched vacancies).</summary>
    public Func<Task>? AfterSaveAsync { get; set; }

    public string? ReturnUrl { get; set; }

    public static readonly string[] AvailabilityDays = DayPartMatrix.DayCodes;
    public static readonly string[] AvailabilitySlots = DayPartMatrix.DayPartCodes;
    private static readonly string[] WeekdayCodes = ["Ma", "Di", "Wo", "Do", "Vr"];

    public bool Saving { get; private set; }
    public bool PdfBusy { get; private set; }
    public bool OpenForWork { get; set; }
    public bool DevicesLoading { get; private set; }
    public bool DevicesBusy { get; private set; }
    public bool DevicesLoadStarted { get; private set; }
    public string? DevicesMessage { get; private set; }
    public List<JobsyApiClient.DeviceSessionItem> Devices { get; private set; } = [];
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public bool WhatsAppAllowed { get; set; }
    public bool AuthenticatorEnabled { get; set; }
    public bool ConsentBusy { get; private set; }
    public DateTime? TalentPoolConsentAt { get; set; }
    public DateTime? TestAiConsentAt { get; set; }
    public DateTime? ParentalConsentAt { get; set; }
    public string ParentalConsentEmail { get; set; } = "";
    public string? ConsentMessage { get; private set; }
    public bool UnsubscribeOpen { get; set; }
    public DateOnly? DobInput { get; set; }
    public string HomeAddress { get; set; } = string.Empty;
    public double? HomeLat { get; set; }
    public double? HomeLng { get; set; }
    public bool HomeLocationDirty { get; set; }
    public int? MaxTravel { get; set; } = 30;
    public string PreferredTransport { get; set; } = "";
    public decimal? MinHours { get; set; } = 8;
    public decimal? MaxHours { get; set; } = 24;
    public bool FlexibleTimes { get; set; }
    public HashSet<string> SelectedRoles { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> SelectedLicenses { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> SelectedEducations { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> Availability { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<CandidateEmployerHistory> Employers { get; } = [];
    public List<CandidateCertificate> Certificates { get; } = [];
    public List<CandidateReferenceItem> References { get; } = [];
    public CandidateUploadedCvInfo? UploadedCv { get; set; }
    public bool CvBusy { get; private set; }
    public List<string> BranchOptions { get; set; } = WorkTypeLabels.All.ToList();
    public List<string> LicenseOptions { get; set; } = DrivingLicenseLabels.All.ToList();
    public List<string> EducationOptions { get; set; } = EducationLevelLabels.ProfileAll.ToList();
    public string? AboutMe { get; set; }
    public string? DefaultMotivation { get; set; }
    public string? Message { get; set; }
    public string? DutchLevel { get; set; }
    public List<CandidateLanguage> SpokenLanguages { get; } = [];
    public HashSet<string> EmployerPreferences { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> LearningGoals { get; } = [];
    public HashSet<string> Hobbies { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> HobbyFreeText { get; } = [];
    public HashSet<string> Dislikes { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> CustomDislikes { get; } = [];
    public bool PrivatePrefsLoaded { get; private set; }
    public bool PrivatePrefsSaving { get; private set; }

    public List<AddressSuggestion> Suggestions { get; private set; } = [];
    public bool ShowSuggestions { get; set; }
    public bool Suggesting { get; private set; }

    public bool IsUnder16 => DobInput is DateOnly dob
        && dob > DateOnly.FromDateTime(DateTime.UtcNow.Date.AddYears(-16));

    public string? AvailabilityKind =>
        CandidateAvailabilityPresets.Detect(MinHours, MaxHours, FlexibleTimes);

    public void Notify() => Changed?.Invoke();

    public void ApplyFromProfile(MeProfile profile)
    {
        var prefs = profile.Preferences ?? new CandidatePreferences();
        DobInput = profile.DateOfBirth;
        OpenForWork = profile.OpenForWork;
        FirstName = profile.FirstName ?? "";
        LastName = profile.LastName ?? "";
        PhoneNumber = profile.PhoneNumber ?? "";
        WhatsAppAllowed = profile.WhatsAppContactAllowed;
        AuthenticatorEnabled = profile.AuthenticatorEnabled;
        TalentPoolConsentAt = profile.TalentPoolConsentAt;
        TestAiConsentAt = profile.TestAiConsentAt;
        ParentalConsentAt = profile.ParentalConsentAt;
        ParentalConsentEmail = profile.ParentalConsentEmail ?? "";
        HomeLat = profile.HomeLatitude;
        HomeLng = profile.HomeLongitude;
        MaxTravel = prefs.MaxTravelMinutes ?? 30;
        PreferredTransport = string.IsNullOrWhiteSpace(prefs.PreferredTransport)
            ? ""
            : TransportLabels.Canonical(prefs.PreferredTransport);
        SelectedRoles.Clear();
        foreach (var role in prefs.Roles ?? [])
        {
            var label = WorkTypeLabels.Expand(WorkTypeLabels.Parse(role)).FirstOrDefault()
                ?? BranchOptions.FirstOrDefault(b => string.Equals(b, role, StringComparison.OrdinalIgnoreCase))
                ?? role;
            if (!string.IsNullOrWhiteSpace(label))
            {
                SelectedRoles.Add(label);
            }
        }

        SelectedLicenses.Clear();
        foreach (var license in prefs.DrivingLicenses ?? [])
        {
            foreach (var normalized in DrivingLicenseLabels.Split(license))
            {
                SelectedLicenses.Add(normalized);
            }
        }

        AboutMe = prefs.AboutMe;
        DefaultMotivation = prefs.DefaultMotivation;
        DutchLevel = prefs.DutchLevel;
        SpokenLanguages.Clear();
        foreach (var lang in prefs.SpokenLanguages ?? [])
        {
            if (!string.IsNullOrWhiteSpace(lang.Code))
            {
                SpokenLanguages.Add(new CandidateLanguage { Code = lang.Code, Level = lang.Level });
            }
        }

        EmployerPreferences.Clear();
        foreach (var code in prefs.EmployerPreferences ?? [])
        {
            if (!string.IsNullOrWhiteSpace(code))
            {
                EmployerPreferences.Add(code);
            }
        }

        LearningGoals.Clear();
        LearningGoals.AddRange((prefs.LearningGoals ?? []).Where(g => !string.IsNullOrWhiteSpace(g)).Take(DiscoveryCatalogs.MaxLearningGoals));

        Hobbies.Clear();
        HobbyFreeText.Clear();
        foreach (var hobby in prefs.Hobbies ?? [])
        {
            if (string.IsNullOrWhiteSpace(hobby))
            {
                continue;
            }

            if (DiscoveryCatalogs.IsKnownHobby(hobby))
            {
                Hobbies.Add(DiscoveryCatalogs.CanonicalHobby(hobby)!);
            }
            else
            {
                HobbyFreeText.Add(hobby.Trim());
            }
        }

        Availability.Clear();
        foreach (var day in prefs.Availability ?? [])
        {
            foreach (var slot in day.Value ?? [])
            {
                Availability.Add($"{day.Key}:{slot}");
            }
        }

        MinHours = prefs.MinHoursPerWeek ?? 8;
        MaxHours = prefs.MaxHoursPerWeek ?? 24;
        FlexibleTimes = prefs.FlexibleTimes == true;
        Employers.Clear();
        Employers.AddRange(prefs.Employers ?? []);
        Certificates.Clear();
        Certificates.AddRange(prefs.Certificates ?? []);
        ApplyCvAndReferences(profile);

        SelectedEducations.Clear();
        foreach (var education in prefs.Educations ?? [])
        {
            foreach (var level in EducationLevelLabels.Split(education))
            {
                SelectedEducations.Add(level);
            }

            foreach (var level in EducationLevelLabels.ProfileAll)
            {
                if (string.Equals(level, EducationLevelLabels.None, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (education.Contains(level, StringComparison.OrdinalIgnoreCase))
                {
                    SelectedEducations.Add(level);
                }
            }
        }

        if (HomeLat is not null && HomeLng is not null)
        {
            if (!string.IsNullOrWhiteSpace(prefs.HomeAddress) && !LooksLikeCoordinates(prefs.HomeAddress))
            {
                HomeAddress = prefs.HomeAddress!;
            }
            else if (!string.IsNullOrWhiteSpace(prefs.HomeAddress))
            {
                HomeAddress = prefs.HomeAddress!;
            }
            else
            {
                HomeAddress = FormatCoordinateFallback(HomeLat.Value, HomeLng.Value);
            }
        }

        Notify();
    }

    public async Task LoadMasterdataAsync()
    {
        try
        {
            var options = await _api.GetMasterdataAsync(audience: "candidate");
            var branches = options.Where(o => o.Category == MasterdataCategories.Branch).Select(o => o.Label).ToList();
            var licenses = options.Where(o => o.Category == MasterdataCategories.DrivingLicense).Select(o => o.Label).ToList();
            var education = options.Where(o => o.Category == MasterdataCategories.EducationLevel).Select(o => o.Label).ToList();
            if (branches.Count > 0) BranchOptions = branches;
            if (licenses.Count > 0) LicenseOptions = licenses;
            if (education.Count > 0) EducationOptions = education;
        }
        catch
        {
            // keep built-in fallbacks
        }
    }

    public void OpenUnsubscribe()
    {
        UnsubscribeOpen = true;
        Notify();
    }

    public void CloseUnsubscribe()
    {
        UnsubscribeOpen = false;
        Notify();
    }

    public void OnUnsubscribeCompleted()
    {
        UnsubscribeOpen = true;
        Notify();
    }

    public void ToggleRole(string role, bool selected)
    {
        if (selected) SelectedRoles.Add(role);
        else SelectedRoles.Remove(role);
        Notify();
    }

    public void ToggleLicense(string license, bool selected)
    {
        if (selected) SelectedLicenses.Add(license);
        else SelectedLicenses.Remove(license);
        Notify();
    }

    public void ToggleEmployerPreference(string code, bool selected)
    {
        if (selected) EmployerPreferences.Add(code);
        else EmployerPreferences.Remove(code);
        Notify();
    }

    public void ToggleHobby(string code, bool selected)
    {
        if (selected) Hobbies.Add(code);
        else Hobbies.Remove(code);
        Notify();
    }

    public void ToggleDislike(string code, bool selected)
    {
        if (selected) Dislikes.Add(code);
        else Dislikes.Remove(code);
        Notify();
    }

    public void AddSpokenLanguage(string code)
    {
        var canonical = DiscoveryCatalogs.CanonicalLanguage(code);
        if (canonical is null || SpokenLanguages.Count >= DiscoveryCatalogs.MaxSpokenLanguages)
        {
            return;
        }

        if (SpokenLanguages.Any(l => string.Equals(l.Code, canonical, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        SpokenLanguages.Add(new CandidateLanguage { Code = canonical });
        Notify();
    }

    public void RemoveSpokenLanguage(string code)
    {
        SpokenLanguages.RemoveAll(l => string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase));
        Notify();
    }

    public void SetSpokenLanguageLevel(string code, string? level)
    {
        var row = SpokenLanguages.FirstOrDefault(l => string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase));
        if (row is null)
        {
            return;
        }

        row.Level = string.IsNullOrWhiteSpace(level) ? null : level.Trim();
        Notify();
    }

    public async Task LoadPrivatePreferencesAsync()
    {
        try
        {
            var prefs = await _api.GetMyPrivatePreferencesAsync();
            Dislikes.Clear();
            CustomDislikes.Clear();
            if (prefs is not null)
            {
                foreach (var d in prefs.Dislikes ?? [])
                {
                    if (!string.IsNullOrWhiteSpace(d))
                    {
                        Dislikes.Add(d);
                    }
                }

                CustomDislikes.AddRange((prefs.CustomDislikes ?? []).Where(c => !string.IsNullOrWhiteSpace(c)));
            }

            PrivatePrefsLoaded = true;
        }
        catch
        {
            PrivatePrefsLoaded = false;
        }

        Notify();
    }

    public async Task SavePrivatePreferencesAsync()
    {
        PrivatePrefsSaving = true;
        Message = null;
        Notify();
        try
        {
            await _api.UpdateMyPrivatePreferencesAsync(
                Dislikes.OrderBy(x => x).ToList(),
                CustomDislikes
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Select(c => c.Trim())
                    .Take(DiscoveryCatalogs.MaxCustomDislikes)
                    .ToList());
            Message = _culture["Profile.Saved"];
        }
        catch (Exception ex)
        {
            Message = ex.Message;
        }
        finally
        {
            PrivatePrefsSaving = false;
            Notify();
        }
    }

    public void ToggleAvailability(string key, bool selected)
    {
        if (selected)
        {
            FlexibleTimes = false;
            Availability.Add(key);
        }
        else
        {
            Availability.Remove(key);
        }

        Notify();
    }

    public void ApplyAvailabilityPreset(string preset)
    {
        if (preset is CandidateAvailabilityPresets.Immediate
            or CandidateAvailabilityPresets.PartTime
            or CandidateAvailabilityPresets.Seasonal)
        {
            var patch = CandidateAvailabilityPresets.Apply(preset);
            FlexibleTimes = patch.FlexibleTimes;
            MinHours = patch.MinHoursPerWeek;
            MaxHours = patch.MaxHoursPerWeek;
            Availability.Clear();
            foreach (var (day, slots) in patch.Availability)
            {
                foreach (var slot in slots)
                {
                    Availability.Add($"{day}:{slot}");
                }
            }

            Notify();
            return;
        }

        if (preset == "flexible")
        {
            FlexibleTimes = true;
            Availability.Clear();
            Notify();
            return;
        }

        FlexibleTimes = false;
        Availability.Clear();
        var officeSlots = preset == "evenings" ? new[] { "Avond" } : new[] { "Ochtend", "Middag" };
        foreach (var day in WeekdayCodes)
        {
            foreach (var slot in officeSlots)
            {
                Availability.Add($"{day}:{slot}");
            }
        }

        Notify();
    }

    public bool IsOfficeHoursPreset() => MatchesPreset(["Ochtend", "Middag"]);
    public bool IsEveningsPreset() => MatchesPreset(["Avond"]);

    private bool MatchesPreset(string[] slots)
    {
        if (FlexibleTimes || Availability.Count != WeekdayCodes.Length * slots.Length)
        {
            return false;
        }

        foreach (var day in WeekdayCodes)
        {
            foreach (var slot in slots)
            {
                if (!Availability.Contains($"{day}:{slot}"))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public void AddEmployer()
    {
        Employers.Add(new CandidateEmployerHistory());
        Notify();
    }

    public void RemoveEmployer(int index)
    {
        Employers.RemoveAt(index);
        Notify();
    }

    public void SetEmployerStartMonth(int index, string? value)
    {
        if (index < 0 || index >= Employers.Count)
        {
            return;
        }

        var normalized = NormalizeMonthInput(value);
        Employers[index].StartMonth = normalized;
        if (normalized is not null
            && !string.IsNullOrWhiteSpace(Employers[index].EndMonth)
            && string.CompareOrdinal(Employers[index].EndMonth, normalized) < 0)
        {
            Employers[index].EndMonth = null;
        }

        Notify();
    }

    public void SetEmployerEndMonth(int index, string? value)
    {
        if (index < 0 || index >= Employers.Count)
        {
            return;
        }

        var normalized = NormalizeMonthInput(value);
        if (normalized is not null
            && !string.IsNullOrWhiteSpace(Employers[index].StartMonth)
            && string.CompareOrdinal(normalized, Employers[index].StartMonth) < 0)
        {
            normalized = null;
        }

        Employers[index].EndMonth = normalized;
        Notify();
    }

    private static string? NormalizeMonthInput(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length >= 7 ? trimmed[..7] : trimmed;
    }

    public void AddCertificate()
    {
        Certificates.Add(new CandidateCertificate());
        Notify();
    }

    public void RemoveCertificate(int index)
    {
        Certificates.RemoveAt(index);
        Notify();
    }

    public void AddReference()
    {
        if (References.Count >= MaxReferences)
        {
            return;
        }

        References.Add(new CandidateReferenceItem());
        Notify();
    }

    public void RemoveReference(int index)
    {
        References.RemoveAt(index);
        Notify();
    }

    public void ApplyCvAndReferences(MeProfile profile)
    {
        UploadedCv = profile.UploadedCv;
        References.Clear();
        References.AddRange(profile.References ?? []);
    }

    public void ToggleEducation(string level, bool selected)
    {
        if (selected)
        {
            if (string.Equals(level, EducationLevelLabels.None, StringComparison.OrdinalIgnoreCase))
            {
                SelectedEducations.Clear();
                SelectedEducations.Add(EducationLevelLabels.None);
                Notify();
                return;
            }

            SelectedEducations.Remove(EducationLevelLabels.None);
            SelectedEducations.Add(level);
        }
        else
        {
            SelectedEducations.Remove(level);
        }

        Notify();
    }

    public async Task OnAddressInputAsync(ChangeEventArgs e)
    {
        HomeAddress = e.Value?.ToString() ?? string.Empty;
        HomeLat = null;
        HomeLng = null;
        HomeLocationDirty = true;
        Message = null;
        await SuggestAddressesAsync(HomeAddress);
        Notify();
    }

    public void OnAddressFocus()
    {
        if (Suggestions.Count > 0 || (HomeAddress.Trim().Length >= 3 && Suggesting))
        {
            ShowSuggestions = true;
            Notify();
        }
    }

    public void OnAddressKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
        {
            ShowSuggestions = false;
            Notify();
        }
    }

    private async Task SuggestAddressesAsync(string query)
    {
        _suggestCts?.Cancel();
        _suggestCts?.Dispose();
        _suggestCts = new CancellationTokenSource();
        var token = _suggestCts.Token;
        var generation = ++_suggestGeneration;

        var trimmed = query.Trim();
        if (trimmed.Length < 3)
        {
            Suggestions = [];
            ShowSuggestions = false;
            Suggesting = false;
            return;
        }

        ShowSuggestions = true;
        Suggesting = true;
        Notify();

        try
        {
            await Task.Delay(280, token);
            var results = await _geocoder.SuggestAsync(trimmed, token);
            if (generation != _suggestGeneration || token.IsCancellationRequested)
            {
                return;
            }

            Suggestions = results.ToList();
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch
        {
            if (generation == _suggestGeneration)
            {
                Suggestions = [];
            }
        }
        finally
        {
            if (generation == _suggestGeneration)
            {
                Suggesting = false;
                Notify();
            }
        }
    }

    public void SelectSuggestion(AddressSuggestion suggestion)
    {
        ShowSuggestions = false;
        Suggestions = [];
        HomeAddress = suggestion.Label;
        HomeLat = suggestion.Latitude;
        HomeLng = suggestion.Longitude;
        HomeLocationDirty = true;
        Message = null;
        Notify();
    }

    public void ClearHomeAddress()
    {
        HomeAddress = string.Empty;
        HomeLat = null;
        HomeLng = null;
        HomeLocationDirty = true;
        Suggestions = [];
        ShowSuggestions = false;
        Message = null;
        Notify();
    }

    public static bool LooksLikeCoordinates(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 2
            && double.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _)
            && double.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _);
    }

    public static string FormatCoordinateFallback(double lat, double lng) =>
        $"{lat.ToString("0.#####", System.Globalization.CultureInfo.InvariantCulture)}, {lng.ToString("0.#####", System.Globalization.CultureInfo.InvariantCulture)}";

    public async Task SaveAsync()
    {
        Saving = true;
        Message = null;
        Notify();
        try
        {
            if (HomeLocationDirty
                && !string.IsNullOrWhiteSpace(HomeAddress)
                && (HomeLat is null || HomeLng is null))
            {
                Message = _culture["Profile.PickAddress"];
                return;
            }

            var clearHome = HomeLocationDirty && string.IsNullOrWhiteSpace(HomeAddress);
            var homeAddressToSave = clearHome
                ? null
                : string.IsNullOrWhiteSpace(HomeAddress) || LooksLikeCoordinates(HomeAddress)
                    ? null
                    : HomeAddress.Trim();
            await _api.UpdateMyProfileAsync(
                openForWork: OpenForWork,
                dateOfBirth: DobInput,
                preferences: new CandidatePreferences
                {
                    Roles = SelectedRoles.OrderBy(r => r).ToList(),
                    MaxTravelMinutes = MaxTravel,
                    PreferredTransport = string.IsNullOrWhiteSpace(PreferredTransport) ? null : PreferredTransport,
                    AboutMe = string.IsNullOrWhiteSpace(AboutMe) ? null : AboutMe.Trim(),
                    DefaultMotivation = string.IsNullOrWhiteSpace(DefaultMotivation) ? null : DefaultMotivation.Trim(),
                    DrivingLicenses = SelectedLicenses.OrderBy(x => x).ToList(),
                    Availability = BuildAvailability(),
                    Employers = Employers
                        .Where(e => !string.IsNullOrWhiteSpace(e.EmployerName))
                        .Select(e => new CandidateEmployerHistory
                        {
                            EmployerName = e.EmployerName.Trim(),
                            Role = string.IsNullOrWhiteSpace(e.Role) ? null : e.Role.Trim(),
                            Years = e.Years,
                            StartMonth = string.IsNullOrWhiteSpace(e.StartMonth) ? null : e.StartMonth.Trim(),
                            EndMonth = string.IsNullOrWhiteSpace(e.EndMonth) ? null : e.EndMonth.Trim(),
                            Description = string.IsNullOrWhiteSpace(e.Description) ? null : e.Description.Trim()
                        })
                        .ToList(),
                    Educations = SelectedEducations
                        .OrderBy(x =>
                        {
                            var idx = EducationOptions.FindIndex(o => string.Equals(o, x, StringComparison.OrdinalIgnoreCase));
                            return idx < 0 ? int.MaxValue : idx;
                        })
                        .ToList(),
                    HomeAddress = homeAddressToSave,
                    MinHoursPerWeek = MinHours,
                    MaxHoursPerWeek = MaxHours,
                    FlexibleTimes = FlexibleTimes,
                    Certificates = Certificates
                        .Where(c => !string.IsNullOrWhiteSpace(c.Name))
                        .Select(c => new CandidateCertificate
                        {
                            Name = c.Name.Trim(),
                            Year = c.Year is >= 1950 and <= 2100 ? c.Year : null
                        })
                        .Take(30)
                        .ToList(),
                    ShowAddressOnCv = false,
                    SpokenLanguages = SpokenLanguages
                        .Where(l => !string.IsNullOrWhiteSpace(l.Code))
                        .Select(l => new CandidateLanguage
                        {
                            Code = l.Code.Trim().ToLowerInvariant(),
                            Level = string.IsNullOrWhiteSpace(l.Level) ? null : l.Level.Trim()
                        })
                        .Take(DiscoveryCatalogs.MaxSpokenLanguages)
                        .ToList(),
                    DutchLevel = string.IsNullOrWhiteSpace(DutchLevel) ? null : DutchLevel.Trim(),
                    EmployerPreferences = EmployerPreferences.OrderBy(x => x).ToList(),
                    LearningGoals = LearningGoals
                        .Where(g => !string.IsNullOrWhiteSpace(g))
                        .Select(g => g.Trim())
                        .Take(DiscoveryCatalogs.MaxLearningGoals)
                        .ToList(),
                    Hobbies = Hobbies
                        .Concat(HobbyFreeText.Where(h => !string.IsNullOrWhiteSpace(h)).Select(h => h.Trim()))
                        .Take(DiscoveryCatalogs.MaxHobbies)
                        .ToList()
                },
                homeLatitude: clearHome || !HomeLocationDirty ? null : HomeLat,
                homeLongitude: clearHome || !HomeLocationDirty ? null : HomeLng,
                clearHomeLocation: clearHome,
                firstName: FirstName.Trim(),
                lastName: LastName.Trim(),
                phoneNumber: string.IsNullOrWhiteSpace(PhoneNumber) ? "" : PhoneNumber.Trim(),
                whatsAppContactAllowed: WhatsAppAllowed && !string.IsNullOrWhiteSpace(PhoneNumber),
                references: References
                    .Where(r => !string.IsNullOrWhiteSpace(r.EmployerName)
                                || !string.IsNullOrWhiteSpace(r.ContactName)
                                || !string.IsNullOrWhiteSpace(r.Email)
                                || !string.IsNullOrWhiteSpace(r.Phone))
                    .Take(MaxReferences)
                    .Select(r => new CandidateReferenceItem
                    {
                        EmployerName = r.EmployerName.Trim(),
                        ContactName = r.ContactName.Trim(),
                        Email = r.Email.Trim(),
                        Phone = r.Phone.Trim()
                    })
                    .ToList());
            HomeLocationDirty = false;
            Message = _culture["Profile.Saved"];
            if (AfterSaveAsync is not null)
            {
                await AfterSaveAsync();
            }

            if (!string.IsNullOrWhiteSpace(ReturnUrl))
            {
                _navigation.NavigateTo(ReturnUrl);
                return;
            }
        }
        catch (Exception ex)
        {
            Message = ex.Message;
        }
        finally
        {
            Saving = false;
            Notify();
        }
    }

    public async Task DownloadLobsyCvAsync()
    {
        PdfBusy = true;
        Message = null;
        Notify();
        try
        {
            await _api.DownloadMyLobsyCvPdfAsync(_js);
            Message = _culture["Profile.LobsyCvDownloaded"];
        }
        catch (Exception ex)
        {
            Message = ex.Message;
        }
        finally
        {
            PdfBusy = false;
            Notify();
        }
    }

    /// <summary>Quick-save for the passport "open for work" switch (same UpdateMyProfile path).</summary>
    public async Task SaveOpenForWorkAsync()
    {
        Saving = true;
        Message = null;
        Notify();
        try
        {
            var updated = await _api.UpdateMyProfileAsync(openForWork: OpenForWork, dateOfBirth: DobInput);
            if (updated is not null)
            {
                OpenForWork = updated.OpenForWork;
            }

            Message = _culture["Profile.Saved"];
            if (AfterSaveAsync is not null)
            {
                await AfterSaveAsync();
            }
        }
        catch (Exception ex)
        {
            Message = ex.Message;
        }
        finally
        {
            Saving = false;
            Notify();
        }
    }

    public Task AcceptTestConsentAsync()
        => UpdateConsentAsync(
            () => _api.AcceptTestAiConsentAsync(),
            _culture["Profile.Consent.Toast.TestAccepted"]);

    public Task WithdrawTestConsentAsync(bool deleteResults)
        => UpdateConsentAsync(
            () => _api.WithdrawTestAiConsentAsync(deleteResults),
            deleteResults
                ? _culture["Profile.Consent.Toast.TestWithdrawnDeleted"]
                : _culture["Profile.Consent.Toast.TestWithdrawn"]);

    public Task AcceptTalentPoolConsentAsync()
        => UpdateConsentAsync(
            () => _api.AcceptTalentPoolConsentAsync(),
            _culture["Profile.Consent.Toast.TalentAccepted"]);

    public Task WithdrawTalentPoolConsentAsync()
        => UpdateConsentAsync(
            () => _api.WithdrawTalentPoolConsentAsync(),
            _culture["Profile.Consent.Toast.TalentWithdrawn"]);

    public async Task RequestParentalConsentAsync()
    {
        ConsentBusy = true;
        ConsentMessage = null;
        Notify();
        try
        {
            await _api.RequestParentalConsentAsync(ParentalConsentEmail);
            ConsentMessage = _culture["Profile.Consent.Toast.ParentalSent"];
        }
        catch (Exception ex)
        {
            ConsentMessage = ex.Message;
        }
        finally
        {
            ConsentBusy = false;
            Notify();
        }
    }

    private async Task UpdateConsentAsync(Func<Task<MeProfile?>> action, string successMessage)
    {
        ConsentBusy = true;
        ConsentMessage = null;
        Notify();
        try
        {
            var updated = await action();
            if (updated is not null)
            {
                TalentPoolConsentAt = updated.TalentPoolConsentAt;
                TestAiConsentAt = updated.TestAiConsentAt;
                ParentalConsentAt = updated.ParentalConsentAt;
            }

            ConsentMessage = successMessage;
        }
        catch (Exception ex)
        {
            ConsentMessage = ex.Message;
        }
        finally
        {
            ConsentBusy = false;
            Notify();
        }
    }

    public async Task OnCvSelectedAsync(InputFileChangeEventArgs e)
    {
        var file = e.File;
        if (file is null)
        {
            return;
        }

        CvBusy = true;
        Message = null;
        Notify();
        try
        {
            var updated = await _api.UploadMyCvAsync(file);
            if (updated is not null)
            {
                ApplyExtractedProfile(updated);
                if (updated.UploadedCv?.FilledFields is { Count: > 0 } filled)
                {
                    Message = _culture.Format("Profile.OwnCvFilled", string.Join(", ", filled));
                }
                else
                {
                    Message = _culture.Format("Profile.OwnCvPresent", updated.UploadedCv?.FileName ?? file.Name);
                }
            }
        }
        catch (Exception ex)
        {
            Message = ex.Message;
        }
        finally
        {
            CvBusy = false;
            Notify();
        }
    }

    private void ApplyExtractedProfile(MeProfile profile)
    {
        var prefs = profile.Preferences ?? new CandidatePreferences();
        FirstName = profile.FirstName ?? FirstName;
        LastName = profile.LastName ?? LastName;
        PhoneNumber = profile.PhoneNumber ?? PhoneNumber;
        AboutMe = prefs.AboutMe ?? AboutMe;
        Employers.Clear();
        Employers.AddRange(prefs.Employers ?? []);
        Certificates.Clear();
        Certificates.AddRange(prefs.Certificates ?? []);
        SelectedLicenses.Clear();
        foreach (var license in prefs.DrivingLicenses ?? [])
        {
            foreach (var normalized in DrivingLicenseLabels.Split(license))
            {
                SelectedLicenses.Add(normalized);
            }
        }

        SelectedEducations.Clear();
        foreach (var education in prefs.Educations ?? [])
        {
            foreach (var level in EducationLevelLabels.Split(education))
            {
                SelectedEducations.Add(level);
            }
        }

        foreach (var role in prefs.Roles ?? [])
        {
            var label = WorkTypeLabels.Expand(WorkTypeLabels.Parse(role)).FirstOrDefault()
                ?? BranchOptions.FirstOrDefault(b => string.Equals(b, role, StringComparison.OrdinalIgnoreCase))
                ?? role;
            if (!string.IsNullOrWhiteSpace(label))
            {
                SelectedRoles.Add(label);
            }
        }

        ApplyCvAndReferences(profile);
    }

    public async Task DownloadOwnCvAsync()
    {
        CvBusy = true;
        Notify();
        try
        {
            await _api.DownloadMyUploadedCvAsync(_js);
        }
        catch (Exception ex)
        {
            Message = ex.Message;
        }
        finally
        {
            CvBusy = false;
            Notify();
        }
    }

    public async Task DeleteOwnCvAsync()
    {
        CvBusy = true;
        Message = null;
        Notify();
        try
        {
            var updated = await _api.DeleteMyCvAsync();
            if (updated is not null)
            {
                ApplyCvAndReferences(updated);
            }
            else
            {
                UploadedCv = null;
            }
        }
        catch (Exception ex)
        {
            Message = ex.Message;
        }
        finally
        {
            CvBusy = false;
            Notify();
        }
    }

    public async Task LoadDevicesAsync()
    {
        if (DevicesLoading)
        {
            return;
        }

        DevicesLoadStarted = true;
        DevicesLoading = true;
        Notify();
        try
        {
            var rows = await _api.GetDeviceSessionsAsync();
            Devices = rows.ToList();
        }
        catch
        {
            Devices = [];
        }
        finally
        {
            DevicesLoading = false;
            Notify();
        }
    }

    public async Task RevokeDeviceAsync(Guid id)
    {
        DevicesBusy = true;
        DevicesMessage = null;
        Notify();
        try
        {
            await _api.RevokeDeviceSessionAsync(id);
            DevicesMessage = _culture["Profile.Devices.Done"];
            DevicesLoadStarted = true;
            await LoadDevicesAsync();
        }
        catch (Exception ex)
        {
            DevicesMessage = ex.Message;
        }
        finally
        {
            DevicesBusy = false;
            Notify();
        }
    }

    public async Task RevokeAllDevicesAsync()
    {
        DevicesBusy = true;
        DevicesMessage = null;
        Notify();
        try
        {
            await _api.RevokeAllDeviceSessionsAsync();
            DevicesMessage = _culture["Profile.Devices.Done"];
            _navigation.NavigateTo("/account/logout", forceLoad: true);
        }
        catch (Exception ex)
        {
            DevicesMessage = ex.Message;
            DevicesBusy = false;
            Notify();
        }
    }

    public Dictionary<string, List<string>> BuildAvailability()
    {
        var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in Availability)
        {
            var parts = key.Split(':', 2);
            if (parts.Length != 2)
            {
                continue;
            }

            if (!map.TryGetValue(parts[0], out var slots))
            {
                slots = [];
                map[parts[0]] = slots;
            }

            if (!slots.Contains(parts[1], StringComparer.OrdinalIgnoreCase))
            {
                slots.Add(parts[1]);
            }
        }

        return map;
    }

    public void Dispose()
    {
        DisposeSuggest();
    }

    public void DisposeSuggest()
    {
        _suggestCts?.Cancel();
        _suggestCts?.Dispose();
        _suggestCts = null;
    }
}
