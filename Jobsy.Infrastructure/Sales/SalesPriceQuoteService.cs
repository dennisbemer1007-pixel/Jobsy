using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Sales;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Sales;

public sealed class SalesPriceQuoteService : ISalesPriceQuote
{
    private readonly JobsyDbContext _db;

    public SalesPriceQuoteService(JobsyDbContext db)
    {
        _db = db;
    }

    public async Task<SalesPriceQuote> GetAsync(CancellationToken cancellationToken = default)
    {
        var packs = await _db.TokenPricings.AsNoTracking()
            .Where(p => p.IsActive && p.PackSize > 0 && p.PriceEuro > 0)
            .OrderBy(p => p.PackSize)
            .ToListAsync(cancellationToken);

        var packQuotes = packs
            .Select(p => new SalesTokenPackQuote(
                p.PackSize,
                Round2(p.PriceEuro),
                Round2(p.PriceEuro / p.PackSize)))
            .ToList();

        decimal min;
        decimal max;
        if (packQuotes.Count == 0)
        {
            // Defensive fallback when packs are not seeded; sales surfaces prefer real packs.
            min = max = VacancyProductRules.DefaultBaseTokenValueEuro;
        }
        else
        {
            min = packQuotes.Min(p => p.PricePerToken);
            max = packQuotes.Max(p => p.PricePerToken);
        }

        var actions = new List<SalesActionCostQuote>();

        var typeCosts = await _db.VacancyTypeTokenCosts.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Kind)
            .ToListAsync(cancellationToken);

        foreach (var row in typeCosts)
        {
            actions.Add(new SalesActionCostQuote(
                $"vacancy.{row.Kind}",
                VacancyKindLabels.ToDutch(row.Kind),
                row.CostTokens,
                Round2(row.CostTokens * min)));
        }

        var spend = await _db.TokenSpendCosts.AsNoTracking()
            .Where(c => c.IsActive
                        && (c.Reason == TokenSpendReason.Highlight
                            || c.Reason == TokenSpendReason.Publish
                            || c.Reason == TokenSpendReason.PushBom
                            || c.Reason == TokenSpendReason.Extend))
            .OrderBy(c => c.Reason)
            .ToListAsync(cancellationToken);

        foreach (var row in spend)
        {
            // Publish is already covered by vacancy type Regular when present.
            if (row.Reason == TokenSpendReason.Publish
                && typeCosts.Any(t => t.Kind == VacancyKind.Regular))
            {
                continue;
            }

            actions.Add(new SalesActionCostQuote(
                $"spend.{row.Reason}",
                SpendLabel(row.Reason),
                row.CostTokens,
                Round2(row.CostTokens * min)));
        }

        // Highlight from commercial settings when spend row missing.
        if (actions.All(a => a.Key != $"spend.{TokenSpendReason.Highlight}"))
        {
            var settings = await _db.SalesCommercialSettings.AsNoTracking()
                .OrderBy(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);
            var highlight = settings?.HighlightCarouselTokens
                            ?? VacancyProductRules.DefaultHighlightCarouselTokens;
            actions.Add(new SalesActionCostQuote(
                $"spend.{TokenSpendReason.Highlight}",
                SpendLabel(TokenSpendReason.Highlight),
                highlight,
                Round2(highlight * min)));
        }

        return new SalesPriceQuote(min, max, packQuotes, actions);
    }

    private static string SpendLabel(TokenSpendReason reason) => reason switch
    {
        TokenSpendReason.Publish => "Vacature plaatsen",
        TokenSpendReason.Highlight => "Uitlichten (highlight)",
        TokenSpendReason.PushBom => "Pushbericht",
        TokenSpendReason.Extend => "Verlengen",
        _ => reason.ToString()
    };

    private static decimal Round2(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
