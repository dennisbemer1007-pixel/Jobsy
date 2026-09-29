using System.Security.Claims;
using Jobsy.Web.Models;
using Jobsy.Web.Navigation;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Jobsy.Web.Werkgever;

/// <summary>
/// Loads companies (+ regions when allowed) into <see cref="EmployerScopeState"/> once per circuit.
/// </summary>
public sealed class EmployerScopeBootstrap
{
    private readonly JobsyApiClient _api;
    private readonly EmployerScopeState _scope;
    private readonly AuthenticationStateProvider _auth;
    private readonly NavigationManager _nav;
    private bool _loaded;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public EmployerScopeBootstrap(
        JobsyApiClient api,
        EmployerScopeState scope,
        AuthenticationStateProvider auth,
        NavigationManager nav)
    {
        _api = api;
        _scope = scope;
        _auth = auth;
        _nav = nav;
    }

    public async Task EnsureLoadedAsync(CancellationToken ct = default)
    {
        if (_loaded && _scope.CompanyIds.Count > 0)
        {
            return;
        }

        await _gate.WaitAsync(ct);
        try
        {
            if (_loaded && _scope.CompanyIds.Count > 0)
            {
                return;
            }

            var auth = await _auth.GetAuthenticationStateAsync();
            var role = WerkgeverNav.ResolveRole(auth.User);
            if (role is null)
            {
                return;
            }

            IReadOnlyList<CompanySummary> companies;
            try
            {
                companies = await _api.GetMyCompaniesAsync(ct);
            }
            catch
            {
                companies = [];
            }

            IReadOnlyList<RegionItem> regions = [];
            if (role is EmployerRole.Bedrijfsmanager)
            {
                try
                {
                    regions = await _api.GetRegionsAsync(ct);
                }
                catch
                {
                    regions = [];
                }
            }

            var vestigingen = companies
                .Where(c => c.ParentCompanyId is not null || companies.All(x => x.ParentCompanyId is null))
                .OrderBy(c => c.Name)
                .ToList();
            if (vestigingen.Count == 0)
            {
                vestigingen = companies.OrderBy(c => c.Name).ToList();
            }

            var options = new List<EmployerScopeOption>();
            var map = new Dictionary<string, IReadOnlyList<Guid>>(StringComparer.OrdinalIgnoreCase);

            if (role is EmployerRole.Bedrijfsmanager or EmployerRole.Intermediair)
            {
                var allIds = companies.Select(c => c.Id).ToList();
                var orgLabel = companies.FirstOrDefault(c => c.ParentCompanyId is null)?.Name
                               ?? companies.FirstOrDefault()?.Name
                               ?? "Organisatie";
                var org = new EmployerScopeOption(
                    EmployerScopeKind.Organisation,
                    null,
                    orgLabel,
                    $"{vestigingen.Count} vestigingen");
                options.Add(org);
                map[org.Key] = allIds.Count > 0 ? allIds : vestigingen.Select(v => v.Id).ToList();

                foreach (var region in regions)
                {
                    var regionIds = region.Companies.Select(c => c.CompanyId).ToList();
                    if (regionIds.Count == 0)
                    {
                        continue;
                    }

                    var opt = new EmployerScopeOption(
                        EmployerScopeKind.Region,
                        region.Id,
                        region.Name,
                        $"{regionIds.Count} vestigingen");
                    options.Add(opt);
                    map[opt.Key] = regionIds;
                }
            }
            else if (role is EmployerRole.Regiomanager)
            {
                var allIds = companies.Select(c => c.Id).ToList();
                var opt = new EmployerScopeOption(
                    EmployerScopeKind.Region,
                    companies.FirstOrDefault()?.Id,
                    "Mijn regio",
                    $"{allIds.Count} vestigingen");
                options.Add(opt);
                map[opt.Key] = allIds;
            }

            foreach (var v in vestigingen)
            {
                var opt = new EmployerScopeOption(EmployerScopeKind.Vestiging, v.Id, v.Name);
                options.Add(opt);
                map[opt.Key] = [v.Id];
            }

            string? requested = null;
            try
            {
                var uri = new Uri(_nav.Uri);
                var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
                if (query.TryGetValue("scope", out var scopeVal))
                {
                    requested = scopeVal.ToString();
                }
            }
            catch
            {
                // ignore
            }

            _scope.Initialize(role.Value, options, map, requested);
            _loaded = true;
        }
        finally
        {
            _gate.Release();
        }
    }
}
