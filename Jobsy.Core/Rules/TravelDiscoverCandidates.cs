using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>Shared origin + transport filtering for banenkaart discover/pins.</summary>
public static class TravelDiscoverCandidates
{
    public sealed record Row(VacancyDiscoveryRecord Record, int TravelMinutes, double DistanceKm);

    public static List<Row> WithinTravelMinutes(
        IEnumerable<VacancyDiscoveryRecord> records,
        double originLat,
        double originLng,
        TransportMode mode,
        string transport,
        int maxMinutes,
        double? radiusKm)
    {
        var reachKm = TravelReach.MaxCrowFliesKm(mode, maxMinutes, radiusKm);
        var list = new List<Row>();
        foreach (var record in records)
        {
            if (!VacancyDiscoveryQuery.CandidateCanReach(record, transport))
            {
                continue;
            }

            var (travelMinutes, distanceKm) = TravelReach.Estimate(
                originLat,
                originLng,
                record.Latitude,
                record.Longitude,
                mode);
            if (travelMinutes > maxMinutes || distanceKm > reachKm)
            {
                continue;
            }

            if (radiusKm is > 0 && distanceKm > radiusKm.Value)
            {
                continue;
            }

            list.Add(new Row(record, travelMinutes, distanceKm));
        }

        return list;
    }

    public static List<Row> NearestReachable(
        IEnumerable<VacancyDiscoveryRecord> records,
        double originLat,
        double originLng,
        TransportMode mode,
        string transport)
    {
        var list = new List<Row>();
        foreach (var record in records)
        {
            if (!VacancyDiscoveryQuery.CandidateCanReach(record, transport))
            {
                continue;
            }

            var (travelMinutes, distanceKm) = TravelReach.Estimate(
                originLat,
                originLng,
                record.Latitude,
                record.Longitude,
                mode);
            list.Add(new Row(record, travelMinutes, distanceKm));
        }

        return list
            .OrderBy(r => r.DistanceKm)
            .ThenBy(r => r.Record.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }
}
