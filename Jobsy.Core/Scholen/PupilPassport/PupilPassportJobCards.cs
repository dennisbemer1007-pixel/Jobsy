using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Scholen.PupilPassport;

public static class PupilPassportJobCards
{
    public static IReadOnlyList<PupilPassportOccupationCard> Build(
        IReadOnlyList<string> jobKeys,
        IReadOnlyList<string> likeLabels,
        PupilQuestionSet set,
        string language = "nl")
    {
        var cards = new List<PupilPassportOccupationCard>();
        var rank = 1;
        foreach (var key in jobKeys.Take(set == PupilQuestionSet.Groep78 ? 5 : 3))
        {
            var title = PupilPassportLocalizedCopy.JobTitle(key, language);
            var domain = PupilPassportLocalizedCopy.JobDomain(key, language);
            var blurb = PupilPassportLocalizedCopy.JobBlurb(key, set, language);
            var tags = FitTags(likeLabels);
            cards.Add(new PupilPassportOccupationCard(
                rank,
                domain,
                title,
                blurb,
                tags,
                IconFor(key)));
            rank++;
        }

        return cards;
    }

    private static IReadOnlyList<string> FitTags(IReadOnlyList<string> likes)
    {
        var tags = new List<string>(3);
        foreach (var like in likes.Take(2))
        {
            tags.Add(like.ToLowerInvariant());
        }

        if (tags.Count == 0)
        {
            tags.Add(PupilPassportCopy.Get("LeerlingPassport.Card.Fit.Curious"));
        }

        return tags;
    }

    private static string IconFor(string key) => key switch
    {
        "dierenverzorger" or "dierenarts" => "giraffe",
        "bioloog" or "wetenschapper" => "microscope",
        "hovenier" => "flower",
        "kok" or "bakker" => "brief",
        "programmeur" or "ict-medewerker" => "microscope",
        "leraar" => "cap",
        "verpleegkundige" => "heart",
        "brandweer" => "shield",
        "politie" => "shield",
        _ => "spark"
    };
}
