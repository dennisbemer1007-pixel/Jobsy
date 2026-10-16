using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Scholen.PupilPassport;

public static class PupilPassportJobCards
{
    public static IReadOnlyList<PupilPassportOccupationCard> Build(
        IReadOnlyList<string> jobKeys,
        IReadOnlyList<string> likeLabels,
        PupilQuestionSet set)
    {
        var cards = new List<PupilPassportOccupationCard>();
        var rank = 1;
        foreach (var key in jobKeys.Take(3))
        {
            var title = PupilRiasecJobIdeas.TitleFor(key);
            var domain = PupilPassportCopy.JobDomain(key);
            var blurb = PupilPassportCopy.JobBlurb(key, set);
            var tags = FitTags(likeLabels);
            cards.Add(new PupilPassportOccupationCard(
                rank,
                domain,
                title,
                blurb,
                tags,
                EmojiFor(key)));
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

    private static string EmojiFor(string key) => key switch
    {
        "dierenverzorger" or "dierenarts" => "🦒",
        "bioloog" or "wetenschapper" => "🔬",
        "hovenier" => "🌷",
        "kok" or "bakker" => "🍳",
        "programmeur" or "ict-medewerker" => "💻",
        "leraar" => "📚",
        "verpleegkundige" => "💊",
        "brandweer" => "🚒",
        "politie" => "👮",
        _ => "✨"
    };
}
