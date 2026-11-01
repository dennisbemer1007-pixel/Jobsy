using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Scholen.MboHbo;

public static class PupilMboHboEntryPaths
{
    public const string Uitleg = "/leerling/mbo-hbo/uitleg";
    public const string Overzicht = "/leerling/mbo-hbo/overzicht";
    public const string Paspoort = "/leerling/mbo-hbo/paspoort";

    public static string Test(PupilMboHboBlock block)
        => $"/leerling/mbo-hbo/test/{PupilMboHboProgressState.BlockKey(block)}";

    public static string Uitkomst(PupilMboHboBlock block)
        => $"/leerling/mbo-hbo/uitkomst/{PupilMboHboProgressState.BlockKey(block)}";

    public static bool IsMboHboPath(ReadOnlySpan<char> path)
        => path.StartsWith("/leerling/mbo-hbo", StringComparison.OrdinalIgnoreCase);

    public static string AfterLogin(
        PupilQuestionSet questionSet,
        PupilCodeStatus status,
        PupilProgress? progress,
        bool journeyCompleted)
    {
        if (questionSet != PupilQuestionSet.MboHbo)
        {
            return PupilGroep78EntryPaths.AfterLogin(questionSet, status, progress, journeyCompleted);
        }

        var state = PupilMboHboProgressState.Parse(progress?.MboHboProgressJson);
        if (journeyCompleted || state.AllBlocksComplete())
        {
            return state.IntroSeen ? Overzicht : Uitleg;
        }

        if (!state.IntroSeen)
        {
            return Uitleg;
        }

        return Overzicht;
    }

    public static bool TryParseBlockRoute(ReadOnlySpan<char> segment, out PupilMboHboBlock block)
    {
        block = default;
        if (segment.Equals("vakken", StringComparison.OrdinalIgnoreCase))
        {
            block = PupilMboHboBlock.Vakken;
            return true;
        }

        if (segment.Equals("interesse", StringComparison.OrdinalIgnoreCase))
        {
            block = PupilMboHboBlock.Interesse;
            return true;
        }

        if (segment.Equals("competentie", StringComparison.OrdinalIgnoreCase))
        {
            block = PupilMboHboBlock.Competentie;
            return true;
        }

        if (segment.Equals("beroepen", StringComparison.OrdinalIgnoreCase))
        {
            block = PupilMboHboBlock.Beroepen;
            return true;
        }

        if (segment.Equals("werkwaarden", StringComparison.OrdinalIgnoreCase))
        {
            block = PupilMboHboBlock.Werkwaarden;
            return true;
        }

        return false;
    }
}
