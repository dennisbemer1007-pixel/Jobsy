using Jobsy.Core.Email;
using Jobsy.Core.Email.Model;
using EmailRecipient = Jobsy.Core.Email.EmailRecipient;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class KandidatenkaartMailService
{
    private readonly JobsyDbContext _db;
    private readonly ITransactionalMailer _mailer;
    private readonly IPlatformFeatureService _platform;
    private readonly IEmailLanguageResolver _languages;

    public KandidatenkaartMailService(
        JobsyDbContext db,
        ITransactionalMailer mailer,
        IPlatformFeatureService platform,
        IEmailLanguageResolver languages)
    {
        _db = db;
        _mailer = mailer;
        _platform = platform;
        _languages = languages;
    }

    public async Task SendBundledInvitesAsync(
        IReadOnlyList<KandidatenkaartInviteDelivery> deliveries,
        CancellationToken cancellationToken)
    {
        if (deliveries.Count == 0)
        {
            return;
        }

        var baseUrl = await GetPublicWebBaseUrlAsync(cancellationToken);
        var byUser = deliveries.GroupBy(d => d.CandidateUserId);
        foreach (var group in byUser)
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == group.Key, cancellationToken);
            if (user is null || string.IsNullOrWhiteSpace(user.Email))
            {
                continue;
            }

            var first = group.First();
            var campaign = await _db.KandidatenkaartInviteCampaigns.AsNoTracking()
                .Include(c => c.Vacancy).ThenInclude(v => v.Company)
                .FirstOrDefaultAsync(c => c.Id == first.CampaignId, cancellationToken);
            if (campaign?.Vacancy is null)
            {
                continue;
            }

            var culture = await _languages.ResolveAsync(new EmailRecipient.User(user.Id), cancellationToken);
            var links = EmailLinks.For(baseUrl);
            var vacancyUrl = links.Absolute($"/vacatures/{campaign.VacancyId}");
            var mail = TransactionalEmails.KandidatenkaartInviteBundled(
                baseUrl,
                user.FullName,
                campaign.Vacancy.Company.Name,
                campaign.Vacancy.Title,
                first.MatchPercent,
                vacancyUrl,
                culture);
            await _mailer.SendAsync(mail, user.Email, cancellationToken: cancellationToken);
        }
    }

    public async Task SendReminderAsync(KandidatenkaartInviteDelivery delivery, CancellationToken cancellationToken)
    {
        var baseUrl = await GetPublicWebBaseUrlAsync(cancellationToken);
        var bundle = await LoadDeliveryContextAsync(delivery, cancellationToken);
        if (bundle is null)
        {
            return;
        }

        var (user, vacancy, companyName) = bundle.Value;
        var culture = await _languages.ResolveAsync(new EmailRecipient.User(user.Id), cancellationToken);
        var links = EmailLinks.For(baseUrl);
        var mail = TransactionalEmails.KandidatenkaartInviteReminder(
            baseUrl,
            user.FullName,
            vacancy.Title,
            companyName,
            links.Absolute($"/vacatures/{vacancy.Id}"),
            culture);
        await _mailer.SendAsync(mail, user.Email!, cancellationToken: cancellationToken);
    }

    public async Task SendInactivityCheckAsync(User user, CancellationToken cancellationToken)
    {
        var baseUrl = await GetPublicWebBaseUrlAsync(cancellationToken);
        var culture = await _languages.ResolveAsync(new EmailRecipient.User(user.Id), cancellationToken);
        var links = EmailLinks.For(baseUrl);
        var mail = TransactionalEmails.KandidatenkaartInactivityCheck(
            baseUrl,
            user.FullName,
            links.Absolute("/candidate/settings/kandidatenkaart"),
            culture);
        await _mailer.SendAsync(mail, user.Email!, cancellationToken: cancellationToken);
    }

    public async Task SendWithdrawnAsync(KandidatenkaartInviteDelivery delivery, CancellationToken cancellationToken)
    {
        var baseUrl = await GetPublicWebBaseUrlAsync(cancellationToken);
        var bundle = await LoadDeliveryContextAsync(delivery, cancellationToken);
        if (bundle is null)
        {
            return;
        }

        var (user, vacancy, companyName) = bundle.Value;
        var culture = await _languages.ResolveAsync(new EmailRecipient.User(user.Id), cancellationToken);
        var links = EmailLinks.For(baseUrl);
        var mail = TransactionalEmails.KandidatenkaartInviteWithdrawn(
            baseUrl,
            user.FullName,
            vacancy.Title,
            companyName,
            links.Absolute("/"),
            culture);
        await _mailer.SendAsync(mail, user.Email!, cancellationToken: cancellationToken);
    }

    public async Task SendMissedOutAsync(KandidatenkaartInviteDelivery delivery, CancellationToken cancellationToken)
    {
        var baseUrl = await GetPublicWebBaseUrlAsync(cancellationToken);
        var bundle = await LoadDeliveryContextAsync(delivery, cancellationToken);
        if (bundle is null)
        {
            return;
        }

        var (user, vacancy, companyName) = bundle.Value;
        var similar = await FindSimilarVacanciesAsync(user, vacancy, cancellationToken);
        var culture = await _languages.ResolveAsync(new EmailRecipient.User(user.Id), cancellationToken);
        var links = EmailLinks.For(baseUrl);
        var mail = TransactionalEmails.KandidatenkaartMissedOut(
            baseUrl,
            user.FullName,
            vacancy.Title,
            companyName,
            similar,
            links.Absolute("/"),
            culture);
        await _mailer.SendAsync(mail, user.Email!, cancellationToken: cancellationToken);
    }

    private async Task<(User User, Vacancy Vacancy, string CompanyName)?> LoadDeliveryContextAsync(
        KandidatenkaartInviteDelivery delivery,
        CancellationToken cancellationToken)
    {
        var row = await _db.KandidatenkaartInviteDeliveries.AsNoTracking()
            .Include(d => d.CandidateUser)
            .Include(d => d.Campaign).ThenInclude(c => c!.Vacancy).ThenInclude(v => v!.Company)
            .FirstOrDefaultAsync(d => d.Id == delivery.Id, cancellationToken);
        if (row?.CandidateUser is null || row.Campaign?.Vacancy?.Company is null)
        {
            return null;
        }

        return (row.CandidateUser, row.Campaign.Vacancy, row.Campaign.Vacancy.Company.Name);
    }

    private async Task<IReadOnlyList<(string Title, string Company)>> FindSimilarVacanciesAsync(
        User user,
        Vacancy source,
        CancellationToken cancellationToken)
    {
        if (user.HomeLocation is null)
        {
            return [];
        }

        var origin = source.Location ?? source.Company.Location;
        if (origin is null)
        {
            return [];
        }

        var candidates = await _db.Vacancies.AsNoTracking()
            .Include(v => v.Company)
            .Where(v => v.Status == VacancyStatus.Active && v.PublishedAtUtc != null && v.Id != source.Id)
            .Take(200)
            .ToListAsync(cancellationToken);

        return candidates
            .Where(v => v.Location is not null
                        && GeoDistance.IsWithinKm(origin, v.Location, 35)
                        && GeoDistance.IsWithinKm(user.HomeLocation, v.Location, 40))
            .OrderBy(v => GeoDistance.HaversineKm(user.HomeLocation, v.Location!))
            .Take(3)
            .Select(v => (v.Title, v.Company.Name))
            .ToList();
    }

    private async Task<string> GetPublicWebBaseUrlAsync(CancellationToken cancellationToken)
    {
        var settings = await _platform.GetAsync(cancellationToken);
        return settings.PublicWebBaseUrl ?? "https://lobsy.nl";
    }
}
