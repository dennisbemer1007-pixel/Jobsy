using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class AboutPageSettingsService : IAboutPageSettingsService
{
    public static readonly Guid SingletonId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    public const string DefaultTitle = "Wie zijn wij";
    public const string DefaultLead = "Over Lobsy — en de mens achter de knop";

    public static readonly string DefaultBodyHtml = """
        <section>
        <h2>Welkom bij Lobsy</h2>
        <p>Als je op deze pagina belandt, ben je waarschijnlijk benieuwd wie er achter dit platform zit en waar we voor staan. Geen dikke handleidingen of kille corporate taal — gewoon het eerlijke verhaal.</p>
        </section>
        <section>
        <h2>Waarom Lobsy is opgericht</h2>
        <p>De arbeidsmarkt in regio’s zoals het Westland en Den Haag zit vol beweging. Van retailketens tot lokale ondernemers in de kassen en winkels: er is voortdurend behoefte aan mensen die snel inzetbaar zijn. En werkzoekenden zoeken werk dat past bij hun dagelijks leven — dichtbij, duidelijk en zonder onnodige drempels.</p>
        <p>Lobsy is vooral gebouwd voor <strong>instapvacatures</strong>: banen waar je snel kunt starten, zonder jarenlange ervaring of een stapel diploma’s. Denk aan winkelvloer, horeca, logistiek, kaswerk en andere lokale rollen waar handjes en motivatie zwaarder wegen dan een perfect cv.</p>
        <p>Bij traditionele vacaturebanken en uitzendplatforms zie je vaak hetzelfde: omslachtige formulieren, reizen naar de andere kant van de regio zonder rekening te houden met fiets of auto, en sollicitaties waar je vervolgens niets meer van hoort. Dat moest anders — slimmer, sneller en menselijker.</p>
        </section>
        <section>
        <h2>Waarom de naam Lobsy?</h2>
        <p>Een platform heeft een eigen smoel nodig. Onze mascotte — de kreeft — loopt als rode draad door de app. Een kreeft is alert, past zich aan zijn omgeving aan en heeft een stevig pantser dat staat voor betrouwbaarheid. Zo willen we Lobsy ook laten werken: helder, praktisch en altijd in beweging om jou verder te helpen.</p>
        </section>
        <section>
        <h2>Gebouwd vanuit de praktijk</h2>
        <p>Lobsy is niet bedacht achter een tekentafel ver weg. Het platform is vanaf het begin gericht op een concreet probleem: hoe vullen lokale ondernemers snel en overzichtelijk instapvacatures, zonder te verdwalen in administratie? En hoe vindt een werkzoekende dichtbij werk dat écht past?</p>
        <p>We begonnen met dat vraagstuk in het Westland — en bouwen daar vanuit verder.</p>
        </section>
        <section>
        <h2>Gebouwd vanuit twee kanten</h2>
        <p>Een goede marktplaats werkt alleen als beide kanten serieus worden genomen. Daarom is Lobsy ontworpen vanuit twee perspectieven:</p>
        <ul>
        <li><strong>Voor de ondernemer en het filiaal:</strong> heldere rollen (van ondernemer tot filiaalmanager) en korte processen. Zodat je snel mensen kunt vinden voor instaprollen, in plaats van te verdwalen in papierwerk.</li>
        <li><strong>Voor de werkzoekende:</strong> geen anonieme sollicitatieput. Lobsy kijkt naar hyper-lokale reistijd en jouw vervoersmiddel (fiets, e-bike of auto). Via chat weet je sneller waar je aan toe bent — zonder eindeloze onzekerheid.</li>
        </ul>
        </section>
        <section>
        <h2>Wie ik ben</h2>
        <p>Mijn naam is Dennis, oprichter van Lobsy. Ik werk als Mendix-developer en tech lead, en bouw graag software die niet alleen werkt, maar ook logisch aanvoelt.</p>
        <p>Met Lobsy breng ik die technische achtergrond samen met een eenvoudige overtuiging: lokale werkzoekenden en ondernemers verdienen een modern, snel platform — vooral voor instapwerk — zonder stoffige uitzendbureau-vibes.</p>
        </section>
        <section>
        <h2>Vragen of sparren?</h2>
        <p>Heb je een vraag, wil je sparren over de mogelijkheden voor jouw organisatie, of ben je benieuwd wat Lobsy voor jou kan betekenen? Neem gerust contact op via <a href="mailto:privacy@lobsy.nl">privacy@lobsy.nl</a> of het contactkanaal in het platform.</p>
        </section>
        """;

    private readonly JobsyDbContext _db;

    public AboutPageSettingsService(JobsyDbContext db)
    {
        _db = db;
    }

    public async Task<AboutPageSnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        var row = await _db.AboutPageSettings.AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
        return ToSnapshot(row);
    }

    public async Task<AboutPageSnapshot> UpdateAsync(
        AboutPageUpdate update,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.AboutPageSettings.FirstOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            row = new AboutPageSettings { Id = SingletonId };
            _db.AboutPageSettings.Add(row);
        }

        row.Title = string.IsNullOrWhiteSpace(update.Title)
            ? DefaultTitle
            : update.Title.Trim();
        row.Lead = string.IsNullOrWhiteSpace(update.Lead)
            ? null
            : update.Lead.Trim();
        row.BodyHtml = HtmlSanitize.ToSafeMarkup(
            string.IsNullOrWhiteSpace(update.BodyHtml) ? DefaultBodyHtml : update.BodyHtml);
        row.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return ToSnapshot(row);
    }

    private static AboutPageSnapshot ToSnapshot(AboutPageSettings? row)
    {
        var title = string.IsNullOrWhiteSpace(row?.Title) ? DefaultTitle : row.Title.Trim();
        var lead = string.IsNullOrWhiteSpace(row?.Lead) ? DefaultLead : row.Lead.Trim();
        var body = string.IsNullOrWhiteSpace(row?.BodyHtml)
            ? HtmlSanitize.ToSafeMarkup(DefaultBodyHtml)
            : HtmlSanitize.ToSafeMarkup(row.BodyHtml);

        return new AboutPageSnapshot(title, lead, body, row?.UpdatedAtUtc);
    }
}
