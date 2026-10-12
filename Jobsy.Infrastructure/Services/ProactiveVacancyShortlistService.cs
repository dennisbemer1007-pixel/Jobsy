using System.Text.Json;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Services;

public sealed class ProactiveVacancyShortlistService : IProactiveVacancyShortlistService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly JobsyDbContext _db;
    private readonly IProfileVacancyMatchService _matches;
    private readonly IRoutingService _routing;
    private readonly ProactiveMatchingOptions _options;

    public ProactiveVacancyShortlistService(
        JobsyDbContext db,
        IProfileVacancyMatchService matches,
        IRoutingService routing,
        IOptions<ProactiveMatchingOptions> options)
    {
        _db = db;
        _matches = matches;
        _routing = routing;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<ProactiveVacancyShortlistEntryDto>> BuildShortlistAsync(
        Guid companyId,
        Guid vacancyId,
        CancellationToken cancellationToken = default)
    {
        var vacancy = await _db.Vacancies.AsNoTracking()
            .Include(v => v.Company)
            .FirstOrDefaultAsync(v => v.Id == vacancyId && v.CompanyId == companyId, cancellationToken)
            ?? throw new KeyNotFoundException("Vacature niet gevonden.");

        if (vacancy.Status is not VacancyStatus.Active and not VacancyStatus.Draft)
        {
            throw new InvalidOperationException("Alleen actieve vacatures hebben een shortlist.");
        }

        var record = VacancyDiscoveryIndex.ToRecord(vacancy);
        var company = vacancy.Company;

        var candidates = await _db.Users.AsNoTracking()
            .Where(u => u.Role == UserRole.Candidate && u.IsActive)
            .Take(800)
            .ToListAsync(cancellationToken);
        candidates = ProactiveTalentAvailabilityRules.FilterEligible(candidates).ToList();
        if (candidates.Count == 0)
        {
            return [];
        }

        var userIds = candidates.Select(c => c.Id).ToList();
        var competencyRows = await _db.CandidateCompetencies.AsNoTracking()
            .Where(c => userIds.Contains(c.UserId) && c.Status == CandidateCompetencyStatuses.Completed)
            .ToListAsync(cancellationToken);
        var competencies = competencyRows.GroupBy(c => c.UserId).ToDictionary(g => g.Key, g => g.First());
        var careerRows = await _db.CandidateCareerInterests.AsNoTracking()
            .Where(c => userIds.Contains(c.UserId) && c.Status == CandidateCompetencyStatuses.Completed)
            .ToListAsync(cancellationToken);
        var careers = careerRows.GroupBy(c => c.UserId).ToDictionary(g => g.Key, g => g.First());

        var scored = new List<(Jobsy.Core.Entities.User User, int Percent, ProfileVacancyMatch Match, AnonymousTalentCardDto Card)>();
        foreach (var user in candidates)
        {
            if (!competencies.ContainsKey(user.Id) && !careers.ContainsKey(user.Id))
            {
                continue;
            }

            var context = await _matches.TryLoadForUserIdAsync(user.Id, cancellationToken);
            if (context is null)
            {
                continue;
            }

            int? travel = null;
            if (user.HomeLocation is not null && company.Location is not null)
            {
                var route = await _routing.GetRouteAsync(
                    company.Location.Latitude,
                    company.Location.Longitude,
                    user.HomeLocation.Latitude,
                    user.HomeLocation.Longitude,
                    TransportMode.Bike,
                    cancellationToken);
                travel = (int)Math.Round(route.DurationSeconds / 60.0, MidpointRounding.AwayFromZero);
            }

            var batch = new List<(VacancyDiscoveryRecord Record, int? TravelMinutes)> { (record, travel) };
            var scores = await _matches.ScoreAsync(context, batch, cancellationToken);
            if (!scores.TryGetValue(vacancyId, out var match) || !match.Core.LegalEligible)
            {
                continue;
            }

            var prefs = DeserializePrefs(user.PreferencesJson);
            var card = BuildCard(user, prefs, competencies, careers, travel);
            scored.Add((user, match.TotalPercent, match, card));
        }

        var ordered = scored
            .OrderByDescending(s => s.Percent)
            .Take(Math.Clamp(_options.ShortlistTake, 5, 50))
            .ToList();

        var result = new List<ProactiveVacancyShortlistEntryDto>();
        for (var i = 0; i < ordered.Count; i++)
        {
            var row = ordered[i];
            var strengths = ProfileVacancyMatchCalculator.WhyHeadlines(row.Match).ToList();
            if (strengths.Count == 0 && row.Card.MatchTags.Count > 0)
            {
                strengths = row.Card.MatchTags.Take(3).ToList();
            }

            result.Add(new ProactiveVacancyShortlistEntryDto(
                i + 1,
                row.User.Id,
                row.Percent,
                row.Card.MatchTags,
                strengths,
                row.Card.AvailabilitySummary,
                row.Card.TravelMinutes,
                row.Card.RegionLabel));
        }

        return result;
    }

    private static AnonymousTalentCardDto BuildCard(
        Jobsy.Core.Entities.User user,
        CandidatePreferencesDto prefs,
        Dictionary<Guid, Jobsy.Core.Entities.CandidateCompetency> competencies,
        Dictionary<Guid, Jobsy.Core.Entities.CandidateCareerInterest> careers,
        int? travelMinutes)
    {
        competencies.TryGetValue(user.Id, out var competency);
        careers.TryGetValue(user.Id, out var career);
        var matchTags = competency is null
            ? []
            : CompetencyTestCatalog.ParseTagsJson(competency.MatchTagsJson).ToList();
        if (career is not null)
        {
            foreach (var tag in CareerTestCatalog.ParseTagsJson(career.MatchTagsJson))
            {
                if (!matchTags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                {
                    matchTags.Add(tag);
                }
            }
        }

        var availability = LobsyCvModelFactory.FormatTalentPoolAvailability(
            prefs.Availability,
            prefs.FlexibleTimes == true,
            prefs.MinHoursPerWeek,
            prefs.MaxHoursPerWeek,
            prefs.AvailabilityPresets);

        return new AnonymousTalentCardDto(
            user.Id,
            matchTags,
            [],
            null,
            availability,
            prefs.DrivingLicenses?.ToList() ?? [],
            travelMinutes,
            LobsyCvModelFactory.ExtractCity(prefs.HomeAddress) ?? "Westland / Den Haag");
    }

    private static CandidatePreferencesDto DeserializePrefs(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new CandidatePreferencesDto([], null, null);
        }

        try
        {
            return JsonSerializer.Deserialize<CandidatePreferencesDto>(json, JsonOptions)
                   ?? new CandidatePreferencesDto([], null, null);
        }
        catch (JsonException)
        {
            return new CandidatePreferencesDto([], null, null);
        }
    }
}
