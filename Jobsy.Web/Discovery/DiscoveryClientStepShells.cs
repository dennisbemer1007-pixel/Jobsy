using System.Text;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Discovery;

internal static class DiscoveryClientStepShells
{
    public static string Build(int step, CultureState culture) => step switch
    {
        2 => "<div class=\"journey-stack\"><div data-disc-client=\"presetGrid\" class=\"ob-wizard__presets\"></div><div data-disc-client=\"transportGrid\"></div><div data-disc-client=\"licenseGrid\"></div></div>",
        3 => "<div class=\"journey-body\"><div data-disc-client=\"jobsRoot\"></div><div data-disc-client=\"employerGrid\" class=\"journey-chips\"></div><div data-disc-client=\"rolesGrid\"></div></div>",
        4 => "<div class=\"journey-body\"><div data-disc-client=\"educationGrid\"></div><div data-disc-client=\"dreamRoot\"></div><div data-disc-client=\"learningGoals\"></div></div>",
        5 => "<div class=\"journey-body\"><div data-disc-client=\"hobbyGrid\" class=\"journey-chips\"></div><textarea class=\"journey-about\" data-disc-client=\"aboutMe\"></textarea></div>",
        6 => "<div class=\"journey-body\"><div data-disc-client=\"dislikeGrid\" class=\"journey-chips\"></div></div>",
        _ => "",
    };
}
