using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.PupilPassport;
using Jobsy.Web.Localization;

namespace Jobsy.Tests.Scholen;

public sealed class PupilPassportCopyGuardTests
{
    [Fact]
    public void Passport_copy_has_no_em_dash_or_pupil_counters()
    {
        foreach (var (key, value) in PupilPassportCopy.All)
        {
            Assert.DoesNotContain('\u2014', value);
            Assert.DoesNotContain('!', value);

            if (key.StartsWith("LeerlingPassport.", StringComparison.Ordinal)
                && key.Contains(".Vo.", StringComparison.Ordinal) == false)
            {
                Assert.DoesNotContain("alle vragen", value, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("alle werelden", value, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("60 vragen", value, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void G78_kicker_and_badge_are_counter_free()
    {
        Assert.Equal("Expeditie afgerond", PupilPassportCopy.Get("LeerlingPassport.Kicker"));
        Assert.Equal("Je route is compleet", PupilPassportCopy.Get("LeerlingPassport.Badge.Sub"));
    }

    [Theory]
    [InlineData('R')]
    [InlineData('I')]
    [InlineData('A')]
    [InlineData('S')]
    [InlineData('E')]
    [InlineData('C')]
    public void All_riasec_tip_letter_keys_resolve(char letter)
    {
        var tips = PupilPassportCopy.HomeClassTips(null, letter);
        Assert.Equal(2, tips.Count);
        Assert.All(tips, tip =>
        {
            Assert.DoesNotContain("LeerlingPassport.", tip, StringComparison.Ordinal);
            Assert.DoesNotContain('\u2014', tip);
        });
    }

    [Fact]
    public void Every_tip_dictionary_key_resolves_to_copy_not_itself()
    {
        foreach (var key in PupilPassportCopy.All.Keys.Where(k => k.StartsWith("LeerlingPassport.Tip.", StringComparison.Ordinal)))
        {
            var value = PupilPassportCopy.Get(key);
            Assert.NotEqual(key, value);
            Assert.DoesNotContain('\u2014', value);
        }
    }

    [Fact]
    public void English_passport_tip_strings_have_no_em_dash_or_raw_keys()
    {
        var nl = new Dictionary<string, string>(StringComparer.Ordinal);
        var en = new Dictionary<string, string>(StringComparer.Ordinal);
        var pl = new Dictionary<string, string>(StringComparer.Ordinal);
        var ro = new Dictionary<string, string>(StringComparer.Ordinal);
        var ar = new Dictionary<string, string>(StringComparer.Ordinal);
        UiStringsLeerlingPaspoort.MergeAll(nl, en, pl, ro, ar);

        foreach (var (key, value) in en.Where(p => p.Key.StartsWith("LeerlingPassport.Tip.", StringComparison.Ordinal)))
        {
            Assert.NotEqual(key, value);
            Assert.DoesNotContain('\u2014', value);
        }
    }
}
