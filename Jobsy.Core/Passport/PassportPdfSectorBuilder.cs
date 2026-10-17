using System.Globalization;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Passport;

/// <summary>Derives up to three sector cards from roles, tests and experience (no stored sector picker yet).</summary>
public static class PassportPdfSectorBuilder
{
    private static readonly (string Id, string TitleKey, string[] Keywords)[] Catalog =
    [
        ("greenhouse", "Sector.Greenhouse", ["tuin", "oogst", "kas", "glas", "groen", "kweker", "teelt", "horti"]),
        ("logistics", "Sector.Logistics", ["logistiek", "orderpick", "magazijn", "warehouse", "heftruck", "inpak"]),
        ("tech", "Sector.Tech", ["techniek", "productie", "machine", "operator", "montage", "werkplaats"]),
        ("care", "Sector.Care", ["zorg", "welzijn", "helpende", "verzorg", "verpleeg", "stage", "bhv"]),
        ("horeca", "Sector.Horeca", ["horeca", "keuken", "bediening", "hotel"]),
        ("retail", "Sector.Retail", ["retail", "winkel", "kassa", "verkoop"])
    ];

    public static IReadOnlyList<PassportSectorCard> Build(string lang, PassportPdfFacts facts)
    {
        var scores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        void Score(string id, int points)
        {
            scores.TryGetValue(id, out var current);
            scores[id] = current + points;
        }

        foreach (var role in facts.Roles ?? [])
        {
            var match = MatchSector(role);
            if (match is not null)
            {
                Score(match, 4);
            }
        }

        foreach (var job in facts.Experience ?? [])
        {
            var blob = $"{job.Role} {job.Employer} {job.Description}";
            var match = MatchSector(blob);
            if (match is not null)
            {
                Score(match, 3);
            }
        }

        var career = facts.Dna.FirstOrDefault(x => x.Key == PassportDnaLayer.Career);
        if (career is { Done: true })
        {
            foreach (var word in career.Words)
            {
                var match = MatchSector(word);
                if (match is not null)
                {
                    Score(match, 2);
                }
            }
        }

        if (scores.Count == 0 && (facts.Roles?.Count ?? 0) > 0)
        {
            Score("logistics", 1);
        }

        var ordered = scores
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.Ordinal)
            .Take(3)
            .Select(pair => pair.Key)
            .ToList();

        if (ordered.Count == 0)
        {
            return [];
        }

        var cards = new List<PassportSectorCard>(ordered.Count);
        var usedReasons = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < ordered.Count; i++)
        {
            var id = ordered[i];
            var def = Catalog.First(c => c.Id == id);
            var title = PassportPdfStrings.T(lang, def.TitleKey);
            var rankSubtitle = i switch
            {
                0 => PassportPdfStrings.T(lang, "Sector.RankFirst"),
                1 => PassportPdfStrings.T(lang, "Sector.RankAlso"),
                _ => PassportPdfStrings.T(lang, "Sector.RankGrow")
            };
            cards.Add(new PassportSectorCard(
                Rank: i + 1,
                Title: title,
                RankSubtitle: rankSubtitle,
                P1Teaser: Teaser(lang, id, facts),
                WhyBullets: Why(lang, id, facts, usedReasons),
                ExampleRoles: Examples(lang, id, facts)));
        }

        return cards;
    }

    private static string? MatchSector(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var lower = text.ToLowerInvariant();
        foreach (var entry in Catalog)
        {
            if (entry.Keywords.Any(keyword => lower.Contains(keyword, StringComparison.Ordinal)))
            {
                return entry.Id;
            }
        }

        return null;
    }

    private static string Teaser(string lang, string sectorId, PassportPdfFacts facts)
    {
        var careerLayer = facts.Dna.FirstOrDefault(x => x.Key == PassportDnaLayer.Career);
        var competenceLayer = facts.Dna.FirstOrDefault(x => x.Key == PassportDnaLayer.Competence);
        var careerWord = careerLayer is { Words.Count: > 0 } ? careerLayer.Words[0] : null;
        var competenceWord = competenceLayer is { Words.Count: > 0 } ? competenceLayer.Words[0] : null;
        return sectorId switch
        {
            "greenhouse" => PassportPdfStrings.F(lang, "Sector.Teaser.Greenhouse",
                competenceWord ?? PassportPdfStrings.T(lang, "Sector.FallbackTrait"),
                ExperienceHint(facts, ["oogst", "tuin", "kas"]) ?? "—"),
            "logistics" => PassportPdfStrings.F(lang, "Sector.Teaser.Logistics",
                RoleHint(facts, ["orderpick", "magazijn"]) ?? (facts.Roles is { Count: > 0 } roles ? roles[0] : null) ?? "—",
                PaperHint(facts, ["heftruck", "vca"]) ?? "—"),
            "tech" => PassportPdfStrings.T(lang, "Sector.Teaser.Tech"),
            "care" => PassportPdfStrings.T(lang, "Sector.Teaser.Care"),
            "horeca" => PassportPdfStrings.T(lang, "Sector.Teaser.Horeca"),
            "retail" => PassportPdfStrings.T(lang, "Sector.Teaser.Retail"),
            _ => JoinBits(competenceWord, careerWord)
        };
    }

    private static IReadOnlyList<string> Why(
        string lang,
        string sectorId,
        PassportPdfFacts facts,
        HashSet<string> usedReasons)
    {
        var lines = new List<string>(4);

        void TryAdd(string key, string line)
        {
            if (lines.Count >= 4 || !usedReasons.Add(key))
            {
                return;
            }

            lines.Add(line);
        }

        switch (sectorId)
        {
            case "greenhouse":
            {
                var exp = ExperienceHint(facts, ["oogst", "tuin", "kas", "kweker", "teelt"]);
                if (!string.IsNullOrWhiteSpace(exp))
                {
                    TryAdd("gh-exp", PassportPdfStrings.F(lang, "Sector.Why.Experience", exp));
                }

                var role = facts.Roles?.FirstOrDefault(r =>
                    MatchSector(r) == "greenhouse" || r.Contains("tuin", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(role))
                {
                    TryAdd("gh-role", PassportPdfStrings.F(lang, "Sector.Why.RoleMatch", role.Trim()));
                }

                var indoor = facts.WorkPreferences is not null
                    ? WorkPreferenceCatalogs.CanonicalIndoor(facts.WorkPreferences.Indoor)
                    : null;
                if (indoor is "prefer" or "ok")
                {
                    TryAdd("gh-indoor", PassportPdfStrings.T(lang, "Sector.Why.Greenhouse.Indoor"));
                }

                TryAddOnceDna(lang, facts, sectorId, usedReasons, lines, preferCompetence: true);
                break;
            }
            case "logistics":
            {
                var cert = PaperHint(facts, ["heftruck", "vca"]);
                if (!string.IsNullOrWhiteSpace(cert))
                {
                    TryAdd("log-cert", PassportPdfStrings.F(lang, "Sector.Why.Certificate", cert));
                }

                var exp = ExperienceHint(facts, ["orderpick", "magazijn", "logistiek"]);
                if (!string.IsNullOrWhiteSpace(exp))
                {
                    TryAdd("log-exp", PassportPdfStrings.F(lang, "Sector.Why.Experience", exp));
                }

                var role = RoleHint(facts, ["orderpick", "magazijn"]);
                if (!string.IsNullOrWhiteSpace(role))
                {
                    TryAdd("log-role", PassportPdfStrings.F(lang, "Sector.Why.RoleMatch", role.Trim()));
                }

                TryAddOnceDna(lang, facts, sectorId, usedReasons, lines, preferCareer: true);
                break;
            }
            case "tech":
            {
                var role = facts.Roles?.FirstOrDefault(r =>
                    MatchSector(r) == "tech" || r.Contains("productie", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(role))
                {
                    TryAdd("tech-role", PassportPdfStrings.F(lang, "Sector.Why.RoleMatch", role.Trim()));
                }

                var physical = facts.WorkPreferences is not null
                    ? WorkPreferenceCatalogs.CanonicalPhysicalWork(facts.WorkPreferences.PhysicalWork)
                    : null;
                if (physical is "lifting-15" or "heavy")
                {
                    TryAdd("tech-physical", PassportPdfStrings.T(lang, "Sector.Why.Tech.Physical"));
                }

                TryAddOnceDna(lang, facts, sectorId, usedReasons, lines, preferCareer: true);
                break;
            }
            case "care":
            {
                var role = facts.Roles?.FirstOrDefault(r =>
                    MatchSector(r) == "care" || r.Contains("zorg", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(role))
                {
                    TryAdd("care-role", PassportPdfStrings.F(lang, "Sector.Why.RoleMatch", role.Trim()));
                }

                var exp = ExperienceHint(facts, ["zorg", "verpleeg", "stage", "bhv", "ziekenhuis"]);
                if (!string.IsNullOrWhiteSpace(exp))
                {
                    TryAdd("care-exp", PassportPdfStrings.F(lang, "Sector.Why.Experience", exp));
                }

                TryAddOnceDna(lang, facts, sectorId, usedReasons, lines, preferCompetence: true, preferCareer: true);
                break;
            }
            default:
                TryAddOnceDna(lang, facts, sectorId, usedReasons, lines, preferCompetence: true, preferCareer: true);
                break;
        }

        if (lines.Count == 0)
        {
            lines.Add(PassportPdfStrings.T(lang, "Sector.Why.Self"));
        }

        return lines.Take(4).ToArray();
    }

    private static void TryAddOnceDna(
        string lang,
        PassportPdfFacts facts,
        string sectorId,
        HashSet<string> usedReasons,
        List<string> lines,
        bool preferCompetence = false,
        bool preferCareer = false)
    {
        if (lines.Count >= 4)
        {
            return;
        }

        var career = facts.Dna.FirstOrDefault(x => x.Key == PassportDnaLayer.Career);
        var competence = facts.Dna.FirstOrDefault(x => x.Key == PassportDnaLayer.Competence);

        if (preferCareer && career is { Done: true, Words.Count: > 0 })
        {
            var key = "dna-career-" + sectorId;
            if (usedReasons.Add(key))
            {
                lines.Add(PassportPdfStrings.F(lang, "Sector.Why.Career", career.Words[0]));
            }
        }
        else if (preferCompetence && competence is { Done: true, Words.Count: > 0 })
        {
            var key = "dna-comp-" + sectorId;
            if (usedReasons.Add(key))
            {
                lines.Add(PassportPdfStrings.F(lang, "Sector.Why.Competence", competence.Words[0]));
            }
        }
    }

    private static string? Examples(string lang, string sectorId, PassportPdfFacts facts)
    {
        var roles = (facts.Roles ?? [])
            .Where(role => MatchSector(role) == sectorId || sectorId == "logistics")
            .Take(3)
            .ToList();
        if (roles.Count == 0)
        {
            return sectorId switch
            {
                "greenhouse" => PassportPdfStrings.T(lang, "Sector.Examples.Greenhouse"),
                "logistics" => PassportPdfStrings.T(lang, "Sector.Examples.Logistics"),
                "tech" => PassportPdfStrings.T(lang, "Sector.Examples.Tech"),
                _ => null
            };
        }

        return string.Join(" · ", roles);
    }

    private static string? RoleHint(PassportPdfFacts facts, string[] needles)
        => facts.Roles?.FirstOrDefault(role => needles.Any(n => role.Contains(n, StringComparison.OrdinalIgnoreCase)));

    private static string? PaperHint(PassportPdfFacts facts, string[] needles)
        => facts.Certificates?.FirstOrDefault(c => needles.Any(n => c.Name.Contains(n, StringComparison.OrdinalIgnoreCase)))?.Name;

    private static string? ExperienceHint(PassportPdfFacts facts, string[] needles)
    {
        foreach (var job in facts.Experience ?? [])
        {
            var blob = $"{job.Role} {job.Employer} {job.Description}";
            if (needles.Any(n => blob.Contains(n, StringComparison.OrdinalIgnoreCase)))
            {
                if (job.Years is int years && years > 0)
                {
                    return PassportPdfStrings.F(facts.Language, "Sector.YearsExperience", years);
                }

                return job.Role ?? job.Employer;
            }
        }

        return null;
    }

    private static string JoinBits(string? a, string? b)
    {
        if (string.IsNullOrWhiteSpace(a))
        {
            return b ?? "";
        }

        return string.IsNullOrWhiteSpace(b) ? a : $"{a} · {b}";
    }
}
