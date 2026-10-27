using Jobsy.Core.Scholen.PupilPassport;

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
}
