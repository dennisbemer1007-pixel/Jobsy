using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Infrastructure.Services.Werkgever.Todo;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests.Werkgever;

public class TokenRequestServiceTests
{
    [Fact]
    public async Task Approve_allocates_exactly_once()
    {
        await using var db = CreateDb();
        var (org, branch, vm, bm) = await SeedAsync(db);
        var ledger = new TokenLedgerService(db);
        await ledger.GrantAsync(org.Id, 50);
        var notifications = new UserNotificationService(db);
        var svc = new TokenRequestService(db, ledger, notifications);

        var created = await svc.CreateAsync(branch.Id, vm.Id, 10, TokenRequestReason.Publiceren, null);
        await svc.ApproveAsync(created.Id, bm.Id);
        await svc.ApproveAsync(created.Id, bm.Id);

        var allocations = await db.TokenTransactions
            .Where(t => t.Kind == TokenTransactionKind.Allocation && t.CompanyId == branch.Id && t.Amount > 0)
            .ToListAsync();
        Assert.Single(allocations);
        Assert.Equal(10m, allocations[0].Amount);
        Assert.Equal(TokenRequestStatus.Toegewezen.ToString(), (await svc.GetAsync(created.Id))!.Status);
    }

    [Fact]
    public async Task Approve_insufficient_balance_writes_nothing()
    {
        await using var db = CreateDb();
        var (org, branch, vm, bm) = await SeedAsync(db);
        var ledger = new TokenLedgerService(db);
        await ledger.GrantAsync(org.Id, 2);
        var svc = new TokenRequestService(db, ledger, new UserNotificationService(db));
        var created = await svc.CreateAsync(branch.Id, vm.Id, 10, TokenRequestReason.Publiceren, null);

        var ex = await Assert.ThrowsAsync<TokenRequestException>(() => svc.ApproveAsync(created.Id, bm.Id));
        Assert.Equal(409, ex.StatusCode);
        Assert.Equal(0, await db.TokenTransactions.CountAsync(t => t.Kind == TokenTransactionKind.Allocation));
        Assert.Equal(TokenRequestStatus.Open.ToString(), (await svc.GetAsync(created.Id))!.Status);
    }

    [Fact]
    public async Task Withdraw_only_when_open()
    {
        await using var db = CreateDb();
        var (org, branch, vm, bm) = await SeedAsync(db);
        var ledger = new TokenLedgerService(db);
        await ledger.GrantAsync(org.Id, 50);
        var svc = new TokenRequestService(db, ledger, new UserNotificationService(db));
        var created = await svc.CreateAsync(branch.Id, vm.Id, 5, TokenRequestReason.Overig, null);
        await svc.ApproveAsync(created.Id, bm.Id);

        var ex = await Assert.ThrowsAsync<TokenRequestException>(() => svc.WithdrawAsync(created.Id, vm.Id));
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task Max_three_open_per_branch()
    {
        await using var db = CreateDb();
        var (_, branch, vm, _) = await SeedAsync(db);
        var svc = new TokenRequestService(db, new TokenLedgerService(db), new UserNotificationService(db));
        await svc.CreateAsync(branch.Id, vm.Id, 1, TokenRequestReason.Publiceren, null);
        await svc.CreateAsync(branch.Id, vm.Id, 1, TokenRequestReason.Verlengen, null);
        await svc.CreateAsync(branch.Id, vm.Id, 1, TokenRequestReason.Uitlichten, null);
        var ex = await Assert.ThrowsAsync<TokenRequestException>(
            () => svc.CreateAsync(branch.Id, vm.Id, 1, TokenRequestReason.Overig, null));
        Assert.Equal("too_many_open_requests", ex.Code);
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task Todo_item_appears_and_disappears()
    {
        await using var db = CreateDb();
        var (org, branch, vm, bm) = await SeedAsync(db);
        var ledger = new TokenLedgerService(db);
        await ledger.GrantAsync(org.Id, 20);
        var svc = new TokenRequestService(db, ledger, new UserNotificationService(db));
        var source = new TokenRequestsTodoSource(db);
        var now = DateTime.UtcNow;

        Assert.Null(await source.BuildAsync([org.Id, branch.Id], WerkgeverDashboardRole.Bedrijfsmanager, now));

        var created = await svc.CreateAsync(branch.Id, vm.Id, 7, TokenRequestReason.Publiceren, null);
        var item = await source.BuildAsync([org.Id, branch.Id], WerkgeverDashboardRole.Bedrijfsmanager, now);
        Assert.NotNull(item);
        Assert.Equal(nameof(WerkgeverTodoKind.TokenRequests), item!.Kind);
        Assert.Equal(1, item.Count);

        await svc.ApproveAsync(created.Id, bm.Id);
        Assert.Null(await source.BuildAsync([org.Id, branch.Id], WerkgeverDashboardRole.Bedrijfsmanager, now));
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("TokReq-" + Guid.NewGuid())
            .Options;
        return new JobsyDbContext(options);
    }

    private static async Task<(Company Org, Company Branch, User Vm, User Bm)> SeedAsync(JobsyDbContext db)
    {
        var org = new Company
        {
            Id = Guid.NewGuid(), Name = "Org", KvkNumber = "1", Address = "a",
            Location = new GeoPoint(52, 4)
        };
        var branch = new Company
        {
            Id = Guid.NewGuid(), Name = "Branch", KvkNumber = "1", Address = "b",
            ParentCompanyId = org.Id, TokensManagedByEnterprise = true,
            Location = new GeoPoint(52, 4)
        };
        var bm = new User
        {
            Id = Guid.NewGuid(), Email = "bm@t.local", FullName = "BM",
            Role = UserRole.EnterpriseManager, IsActive = true, CompanyId = org.Id
        };
        var vm = new User
        {
            Id = Guid.NewGuid(), Email = "vm@t.local", FullName = "VM",
            Role = UserRole.BranchManager, IsActive = true, CompanyId = branch.Id
        };
        db.Companies.AddRange(org, branch);
        db.Users.AddRange(bm, vm);
        await db.SaveChangesAsync();
        return (org, branch, vm, bm);
    }
}
