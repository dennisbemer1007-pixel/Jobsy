using Jobsy.Core.Rules;
using Jobsy.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Jobsy.Web.Components.Pages.Candidate;

public partial class CareerCompassPanel
{
    [Parameter]
    public RiasecScoreSet? CareerDirection { get; set; }

    private RoleFitCheckSnapshot? BuildInsight(string title)
    {
        if (Competencies is not { IsComplete: true } c || CareerDirection is not { IsComplete: true } r)
        {
            return null;
        }

        return RoleFitCheckBuilder.Build(
            title,
            new CompetencyScores(c.Samenwerken, c.Resultaatgerichtheid, c.Stressbestendigheid, c.Innovatie, c.Extraversie),
            new RiasecScores(r.Realistic, r.Investigative, r.Artistic, r.Social, r.Enterprising, r.Conventional),
            fromDeepAnalysis: Compass?.FromDeepAnalysis == true);
    }
}
