using Jobsy.Core.Rules;
using Jobsy.Web.Models;

namespace Jobsy.Web.Passport;

internal static class PassportAvailabilityMatrix
{
    internal static HashSet<string> ToSelectedKeys(IReadOnlyDictionary<string, List<string>>? availability)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (availability is null)
        {
            return set;
        }

        foreach (var (day, parts) in availability)
        {
            if (parts is null)
            {
                continue;
            }

            foreach (var part in parts)
            {
                if (string.IsNullOrWhiteSpace(part))
                {
                    continue;
                }

                set.Add($"{day}:{part}");
            }
        }

        return set;
    }

    internal static Dictionary<string, List<string>> FromSelectedKeys(IEnumerable<string> keys)
    {
        var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in keys)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var parts = raw.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2
                || !DayPartMatrix.IsValidDayCode(parts[0])
                || !DayPartMatrix.IsValidDayPartCode(parts[1]))
            {
                continue;
            }

            var day = DayPartMatrix.NormalizeDayCode(parts[0]);
            var slot = DayPartMatrix.NormalizeDayPartCode(parts[1]);
            if (!map.TryGetValue(day, out var list))
            {
                list = [];
                map[day] = list;
            }

            if (!list.Contains(slot, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(slot);
            }
        }

        return map;
    }

    internal static HashSet<string> ToggleKey(
        IReadOnlyDictionary<string, List<string>>? availability,
        string key)
    {
        var set = ToSelectedKeys(availability);
        if (!set.Remove(key))
        {
            set.Add(key);
        }

        return set;
    }
}
