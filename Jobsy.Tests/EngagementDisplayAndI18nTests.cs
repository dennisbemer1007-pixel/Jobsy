using Jobsy.Core.Rules;
using Jobsy.Web.Localization;

namespace Jobsy.Tests;

public class EngagementDisplayAndI18nTests
{
    [Fact]
    public void Card_shows_max_two_badges_checked_first_with_overflow()
    {
        var items = new[]
        {
            new EngagePick(EngagementCatalog.Duurzaam, false),
            new EngagePick(EngagementCatalog.Leerbedrijf, true),
            new EngagePick(EngagementCatalog.Lokaal, false),
            new EngagePick(EngagementCatalog.EerlijkLoon, true)
        };
        var ordered = items
            .OrderByDescending(i => i.Checked)
            .ThenBy(i => i.Id, StringComparer.Ordinal)
            .ToList();
        var visible = ordered.Take(EngagementCatalog.MaxCardBadges).ToList();
        var overflow = Math.Max(0, ordered.Count - EngagementCatalog.MaxCardBadges);
        Assert.Equal(2, visible.Count);
        Assert.All(visible, v => Assert.True(v.Checked));
        Assert.Equal(2, overflow);
    }

    [Fact]
    public void Honest_labels_and_catalog_keys_exist_in_five_languages()
    {
        string[] keys =
        [
            "WaEngage.Label.SelfDeclared",
            "WaEngage.Label.CheckedAdmin",
            "WaEngage.Label.CheckedSbb",
            "WaEngage.WhatMeans",
            "WaEngage.MatchBonus",
            ..EngagementCatalog.All.Select(i => i.TitleKey),
            ..EngagementCatalog.All.Select(i => i.HintKey)
        ];
        foreach (var lang in new[] { "nl", "en", "pl", "ro", "ar" })
        {
            foreach (var key in keys)
            {
                var value = UiStrings.Get(key, lang);
                Assert.False(string.IsNullOrWhiteSpace(value), $"{lang}:{key}");
                Assert.NotEqual(key, value);
            }
        }
    }

    [Fact]
    public void Removal_mail_includes_reason()
    {
        var mail = Jobsy.Core.Email.TransactionalEmails.EngagementClaimRemoved(
            "Groen & Zorg", "duurzaamheid", "Bewijs ontbreekt");
        Assert.Contains("Bewijs ontbreekt", mail.Html, StringComparison.Ordinal);
        Assert.Contains("duurzaamheid", mail.Html, StringComparison.Ordinal);
    }

    private sealed record EngagePick(string Id, bool Checked);
}
