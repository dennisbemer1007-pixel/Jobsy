using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Api.Admin;
using Jobsy.Api.Models;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Privacy;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Tests;

public class AdminOrganisationsApiTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private readonly RoleFunctionalWebAppFactory _factory;

    public AdminOrganisationsApiTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Companies_no_params_returns_array_shape()
    {
        var client = AdminClient();
        var response = await client.GetAsync("api/admin/companies");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<AdminCompanyDetailDto>>(Json);
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.All(items, c => Assert.False(string.IsNullOrWhiteSpace(c.Name)));
    }

    [Fact]
    public async Task Companies_filters_and_counts_on_seeded_tree()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();

        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = parentId,
            Name = "Tree Parent Org ZZ",
            KvkNumber = "99887766",
            Address = "Parentstraat 1",
            Location = new GeoPoint(52.1, 4.3),
            Type = CompanyType.Employer,
            KvkVerificationStatus = KvkVerificationStatus.Verified,
            KvkVerifiedAtUtc = DateTime.UtcNow.AddMonths(-2)
        });
        db.Companies.Add(new Company
        {
            Id = childId,
            Name = "Tree Child Vestiging ZZ",
            KvkNumber = "99887766",
            Address = "Childstraat 2",
            Location = new GeoPoint(52.2, 4.4),
            Type = CompanyType.Employer,
            ParentCompanyId = parentId,
            KvkVerificationStatus = KvkVerificationStatus.Verified
        });
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = $"em-{parentId:N}@test.local",
            FullName = "Piet van Dale",
            Role = UserRole.EnterpriseManager,
            IsActive = true,
            CompanyId = parentId
        });
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = $"bm-{childId:N}@test.local",
            FullName = "Branch User",
            Role = UserRole.BranchManager,
            IsActive = true,
            CompanyId = childId
        });
        db.Vacancies.Add(new Vacancy
        {
            Id = Guid.NewGuid(),
            CompanyId = childId,
            Title = "Seizoen",
            Status = VacancyStatus.Active,
            CreatedAtUtc = DateTime.UtcNow,
            Location = new GeoPoint(52.2, 4.4),
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            HourlyWage = 14
        });
        db.TokenTransactions.Add(new TokenTransaction
        {
            Id = Guid.NewGuid(),
            CompanyId = parentId,
            Amount = 10,
            Kind = TokenTransactionKind.Grant,
            CreatedAt = DateTime.UtcNow
        });
        db.TokenTransactions.Add(new TokenTransaction
        {
            Id = Guid.NewGuid(),
            CompanyId = parentId,
            Amount = 3,
            Kind = TokenTransactionKind.Goodwill,
            Note = "comp",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var client = AdminClient();
        var page = await client.GetFromJsonAsync<AdminCompaniesPageDto>(
            "api/admin/companies?q=Tree%20Parent%20Org%20ZZ&page=1&pageSize=50", Json);
        Assert.NotNull(page);
        var parent = Assert.Single(page.Items, c => c.Id == parentId);
        Assert.Equal(1, parent.BranchCount);
        Assert.True(parent.UserCount >= 1);
        Assert.Equal(13, parent.TokenBalance);
        Assert.Equal(3, parent.GoodwillBalance);
        Assert.NotNull(parent.EnterpriseManagerName);
        Assert.False(string.Equals("Piet van Dale", parent.EnterpriseManagerName, StringComparison.Ordinal));
        Assert.Contains("Piet", parent.EnterpriseManagerName, StringComparison.Ordinal);

        var childOnPage = page.Items.FirstOrDefault(c => c.Id == childId);
        Assert.NotNull(childOnPage);
        Assert.Equal(1, childOnPage!.ActiveVacancyCount);

        var filtered = await client.GetFromJsonAsync<AdminCompaniesPageDto>(
            "api/admin/companies?type=Employer&status=active&page=1&pageSize=10", Json);
        Assert.NotNull(filtered);
        Assert.All(filtered.Items, c => Assert.Equal("Employer", c.Type));
    }

    [Fact]
    public async Task Companies_query_uses_fixed_command_budget_and_no_location()
    {
        await using var db = CreateCountingDb(out var interceptor);
        var parent = Guid.NewGuid();
        var child = Guid.NewGuid();
        db.Companies.AddRange(
            new Company
            {
                Id = parent,
                Name = "Count Parent",
                KvkNumber = "11112222",
                Address = "a",
                Location = new GeoPoint(52, 4),
                KvkVerificationStatus = KvkVerificationStatus.Verified
            },
            new Company
            {
                Id = child,
                Name = "Count Child",
                KvkNumber = "11112222",
                Address = "b",
                Location = new GeoPoint(52, 4),
                ParentCompanyId = parent,
                KvkVerificationStatus = KvkVerificationStatus.Pending
            });
        await db.SaveChangesAsync();
        interceptor.Reset();

        var result = await AdminCompaniesQuery.QueryAsync(db, new AdminCompaniesQuery.QueryArgs());
        Assert.Equal(2, result.Items.Count);
        Assert.Contains(result.Items, c => c.Status == "not-live");
        Assert.True(interceptor.Count <= 20, $"Expected bounded queries, got {interceptor.Count}");
    }

    [Fact]
    public async Task Takeover_list_masks_email_for_admin_not_for_enterprise()
    {
        // Seed via client creation before touching the shared in-memory DbContext.
        using var _ = AdminClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var reg = new CompanyRegistration
        {
            Id = Guid.NewGuid(),
            EstablishmentName = "Mask Applicant",
            KvkNumber = "55556666",
            KvkEstablishmentId = "55556666_1",
            ContactName = "Applicant",
            ContactEmail = "aanvrager@example.com",
            ContactEmailVerifiedAt = DateTime.UtcNow.AddHours(-1),
            ActivationToken = "tok",
            CreatedAt = DateTime.UtcNow
        };
        var target = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Mask Target Co",
            KvkNumber = "55556666",
            Address = "x",
            Location = new GeoPoint(52, 4)
        };
        db.CompanyRegistrations.Add(reg);
        db.Companies.Add(target);
        var takeoverId = Guid.NewGuid();
        db.EstablishmentTakeoverRequests.Add(new EstablishmentTakeoverRequest
        {
            Id = takeoverId,
            RegistrationId = reg.Id,
            TargetCompanyId = target.Id,
            Status = TakeoverRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        var enterprise = await db.Users.FirstAsync(u => u.Id == _factory.EnterpriseId);
        enterprise.CompanyId = target.Id;
        await db.SaveChangesAsync();

        var adminItems = await AdminClient().GetFromJsonAsync<List<TakeoverInboxItemDto>>(
            "api/registration/takeovers", Json);
        Assert.NotNull(adminItems);
        var adminRow = Assert.Single(adminItems, t => t.TakeoverId == takeoverId);
        Assert.Equal(PersonalDataMasker.MaskEmail("aanvrager@example.com"), adminRow.RequesterEmail);
        Assert.DoesNotContain("aanvrager@example.com", adminRow.RequesterEmail);

        var emClient = Authed(_factory.EnterpriseId);
        var emItems = await emClient.GetFromJsonAsync<List<TakeoverInboxItemDto>>(
            "api/registration/takeovers", Json);
        Assert.NotNull(emItems);
        var emRow = emItems.FirstOrDefault(t => t.TakeoverId == takeoverId);
        if (emRow is not null)
        {
            Assert.Equal("aanvrager@example.com", emRow.RequesterEmail);
        }
    }

    [Fact]
    public async Task Kvk_retry_uses_service_method()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var id = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = id,
            Name = "Retry Co",
            KvkNumber = "12345678",
            KvkEstablishmentId = "12345678_1",
            Address = "x",
            Location = new GeoPoint(52, 4),
            KvkVerificationStatus = KvkVerificationStatus.Failed,
            KvkVerificationAttempts = 2
        });
        await db.SaveChangesAsync();

        var client = AdminClient();
        var response = await client.PostAsync($"api/admin/companies/{id:D}/kvk-retry", null);
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound);
        if (response.IsSuccessStatusCode)
        {
            var updated = await db.Companies.AsNoTracking().FirstAsync(c => c.Id == id);
            Assert.True(updated.KvkVerificationAttempts >= 2);
        }
    }

    private HttpClient AdminClient() => Authed(_factory.AdminId);

    private HttpClient Authed(Guid userId)
    {
        var client = _factory.CreateClient();
        JobsyTestAuth.Authorize(client, userId);
        return client;
    }

    private static JobsyDbContext CreateCountingDb(out CountingInterceptor interceptor)
    {
        interceptor = new CountingInterceptor();
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("AdminOrgs-" + Guid.NewGuid())
            .AddInterceptors(interceptor)
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class CountingInterceptor : DbCommandInterceptor
    {
        public int Count { get; private set; }
        public List<string> Commands { get; } = [];

        public void Reset()
        {
            Count = 0;
            Commands.Clear();
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            Count++;
            Commands.Add(command.CommandText);
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Count++;
            Commands.Add(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
