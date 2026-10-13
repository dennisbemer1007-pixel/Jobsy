using Jobsy.Web.Models;

namespace Jobsy.Web.Services;

/// <summary>
/// Shared KVK search client for the app design system (<see cref="Components.Shared.KvkSearchField"/>)
/// and the public wizard wrapper added in werkgever-aanmelding 05.
/// </summary>
public sealed class KvkSearchClient
{
    private readonly JobsyApiClient _api;

    public KvkSearchClient(JobsyApiClient api)
    {
        _api = api;
    }

    public Task<KvkSearchResultItem> SearchAsync(
        string query,
        string? place = null,
        int page = 1,
        CancellationToken cancellationToken = default)
        => _api.SearchKvkAsync(query, place, page, cancellationToken);

    public Task<KvkCompanyProfileItem> GetProfileAsync(
        string kvkNumber,
        CancellationToken cancellationToken = default)
        => _api.GetKvkProfileAsync(kvkNumber, cancellationToken);

    public Task<IReadOnlyList<KvkEstablishmentItem>> GetEstablishmentsAsync(
        string kvkNumber,
        CancellationToken cancellationToken = default)
        => _api.GetKvkEstablishmentsAsync(kvkNumber, cancellationToken);
}
