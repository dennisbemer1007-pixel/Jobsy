using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Scholen.MboHbo;
using Jobsy.Web.Scholen;

namespace Jobsy.Tests.Scholen;

public sealed class PupilMboHboUitkomstSsrTests
{
    [Fact]
    public void Uitkomst_shows_consistent_vakken_progress_from_block_statuses()
    {
        var blocks = new List<PupilMboHboBlockStatusDto>();
        foreach (var block in PupilMboHboCatalog.AllBlocks)
        {
            var meta = PupilMboHboCopy.BlockMeta(block);
            var status = block == PupilMboHboBlock.Vakken
                ? PupilMboHboTileStatus.Completed
                : PupilMboHboTileStatus.NotStarted;
            var answered = block == PupilMboHboBlock.Vakken ? 0 : 0;
            blocks.Add(new PupilMboHboBlockStatusDto(
                block,
                PupilMboHboProgressState.BlockKey(block),
                meta.Title,
                meta.Description,
                meta.QuestionCount,
                answered,
                status));
        }

        var outcome = new PupilMboHboOutcomeDto(
            PupilMboHboBlock.Vakken,
            "vakken",
            "Vakkenpakket",
            AnsweredCount: 0,
            QuestionCount: 40,
            Bars: [],
            Summary: "Klaar",
            Highlights: []);

        var html = PupilMboHboSsrHtml.Uitkomst(outcome, blocks);

        Assert.Contains("Je hebt 40 vragen over vakken ingevuld", html, StringComparison.Ordinal);
        Assert.Contains("1 van 5 is af", html, StringComparison.Ordinal);
        Assert.Contains("Af · 40 van 40", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Je hebt 0 vragen", html, StringComparison.Ordinal);
        var vakkenIdx = html.IndexOf("Vakkenpakket", StringComparison.Ordinal);
        Assert.True(vakkenIdx >= 0);
        var asideSlice = html.Substring(vakkenIdx, Math.Min(220, html.Length - vakkenIdx));
        Assert.DoesNotContain("Nog te doen", asideSlice, StringComparison.Ordinal);
        Assert.Contains("mhbo-part is-done", html, StringComparison.Ordinal);
    }
}