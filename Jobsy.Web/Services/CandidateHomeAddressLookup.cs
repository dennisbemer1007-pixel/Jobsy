using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Models;

namespace Jobsy.Web.Services;

/// <summary>Shared home-address geocoding for onboarding and profile (banenkaart-style lookup).</summary>
public static class CandidateHomeAddressLookup
{
    public static void HydrateDraftFromStored(OnboardingProfileDraft draft, string? homeAddress)
    {
        if (string.IsNullOrWhiteSpace(homeAddress))
        {
            return;
        }

        draft.AddressQuery = homeAddress.Trim();
        var (postcode, city) = PostcodeMatch.SplitHomeAddress(homeAddress);
        draft.Postcode = postcode;
        draft.City = city;
        if (draft.HomeLat is not null && draft.HomeLng is not null)
        {
            draft.PostcodeResolved = true;
            draft.PostcodeMissing = false;
        }
    }

    public static string BuildHomeAddressForSave(OnboardingProfileDraft draft)
    {
        var query = (draft.AddressQuery ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(query) && draft.HomeLat is not null && draft.HomeLng is not null)
        {
            return query;
        }

        var postcode = PostcodeMatch.Normalize(draft.Postcode);
        var city = (draft.City ?? string.Empty).Trim();
        if (PostcodeMatch.IsNlPostcode(postcode) && !string.IsNullOrWhiteSpace(city))
        {
            return $"{postcode} {city}";
        }

        if (PostcodeMatch.IsNlPostcode(postcode))
        {
            return postcode;
        }

        return query;
    }

    public static void ApplySuggestionToDraft(OnboardingProfileDraft draft, AddressSuggestion item)
    {
        draft.HomeLat = item.Latitude;
        draft.HomeLng = item.Longitude;
        draft.AddressQuery = item.Label;
        draft.Suggestions.Clear();

        var postcode = PostcodeMatch.Normalize(draft.Postcode);
        if (PostcodeMatch.IsNlPostcode(postcode))
        {
            draft.Postcode = postcode;
            var city = PostcodeMatch.CityFromLabel(item.Label, postcode);
            if (!string.IsNullOrWhiteSpace(city))
            {
                draft.City = city;
                draft.ShowCity = false;
            }
            else
            {
                var split = PostcodeMatch.SplitHomeAddress(item.Label);
                if (!string.IsNullOrWhiteSpace(split.City))
                {
                    draft.City = split.City;
                    draft.ShowCity = false;
                }
                else
                {
                    draft.ShowCity = true;
                }
            }
        }
        else
        {
            var split = PostcodeMatch.SplitHomeAddress(item.Label);
            draft.Postcode = split.Postcode;
            draft.City = split.City;
            if (string.IsNullOrWhiteSpace(split.Postcode))
            {
                draft.ShowCity = string.IsNullOrWhiteSpace(split.City);
            }
        }

        draft.Postcode = PostcodeMatch.Normalize(draft.Postcode);
        draft.PostcodeResolved = true;
        draft.PostcodeMissing = false;
    }

    public static void ClearDraftLocation(OnboardingProfileDraft draft)
    {
        draft.AddressQuery = string.Empty;
        draft.Postcode = string.Empty;
        draft.City = string.Empty;
        draft.HomeLat = null;
        draft.HomeLng = null;
        draft.PostcodeResolved = false;
        draft.PostcodeMissing = false;
        draft.ShowCity = false;
        draft.Suggestions.Clear();
    }

    public static void MarkDraftInputChanged(OnboardingProfileDraft draft)
    {
        draft.HomeLat = null;
        draft.HomeLng = null;
        draft.PostcodeResolved = false;
        draft.PostcodeMissing = false;
    }

    public static async Task<bool> TryResolvePostcodeAsync(
        OnboardingProfileDraft draft,
        IGeocodingClient geocoder,
        CancellationToken cancellationToken,
        bool debounce = true)
    {
        try
        {
            if (debounce)
            {
                await Task.Delay(350, cancellationToken);
            }

            var fromQuery = PostcodeMatch.Normalize(ExtractNlPostcodeCandidate(draft.AddressQuery));
            var postcode = PostcodeMatch.IsNlPostcode(fromQuery)
                ? fromQuery
                : PostcodeMatch.Normalize(draft.Postcode);
            if (!PostcodeMatch.IsNlPostcode(postcode))
            {
                return false;
            }

            draft.Postcode = postcode;
            var result = PostcodeMatch.Pick(await geocoder.SuggestAsync(postcode, cancellationToken), postcode);
            if (result is null)
            {
                draft.PostcodeMissing = true;
                draft.ShowCity = true;
                return false;
            }

            ApplySuggestionToDraft(draft, result);
            if (string.IsNullOrWhiteSpace(draft.AddressQuery))
            {
                draft.AddressQuery = result.Label;
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch
        {
            draft.PostcodeMissing = true;
            draft.ShowCity = true;
            return false;
        }
    }

    public static async Task<string?> ReverseGeocodeLabelAsync(
        IGeocodingClient geocoder,
        double lat,
        double lng)
    {
        try
        {
            return await geocoder.ReverseAsync(lat, lng);
        }
        catch
        {
            return null;
        }
    }

    private static string ExtractNlPostcodeCandidate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        var compact = trimmed.Replace(" ", "").ToUpperInvariant();
        if (compact.Length >= 6 && PostcodeMatch.IsNlPostcode(PostcodeMatch.Normalize(compact[..6])))
        {
            return compact[..6];
        }

        return trimmed;
    }
}
