using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class Sprint5EmployerUiFoundationTests
{
    [Fact]
    public async Task Allocate_moves_tokens_between_companies()
    {
        await using var db = CreateDb();
        var fromId = Guid.NewGuid();
        var toId = Guid.NewGuid();
        db.Companies.AddRange(
            new Company
            {
                Id = fromId,
                Name = "HQ",
                KvkNumber = "11111111",
                Address = "A",
                Location = new GeoPoint(52, 4),
                Type = CompanyType.Employer
            },
            new Company
            {
                Id = toId,
                Name = "Branch",
                KvkNumber = "11111111",
                Address = "B",
                Location = new GeoPoint(52.1, 4.1),
                Type = CompanyType.Employer
            });
        db.TokenTransactions.Add(new TokenTransaction
        {
            Id = Guid.NewGuid(),
            CompanyId = fromId,
            Amount = 10m,
            Kind = TokenTransactionKind.Grant,
            OldBalance = 0,
            NewBalance = 10m,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var ledger = new TokenLedgerService(db);
        var (fromEntry, toEntry) = await ledger.AllocateAsync(fromId, toId, 4m, note: "test");

        Assert.Equal(6m, fromEntry.NewBalance);
        Assert.Equal(4m, toEntry.NewBalance);
        Assert.Equal(TokenTransactionKind.Allocation, fromEntry.Kind);
        Assert.Equal(TokenTransactionKind.Allocation, toEntry.Kind);
        Assert.Equal(6m, await ledger.GetBalanceAsync(fromId));
        Assert.Equal(4m, await ledger.GetBalanceAsync(toId));
    }

    [Fact]
    public async Task RecordPurchase_credits_purchase_kind()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Buyer",
            KvkNumber = "22222222",
            Address = "C",
            Location = new GeoPoint(52, 4),
            Type = CompanyType.Employer
        });
        await db.SaveChangesAsync();

        var ledger = new TokenLedgerService(db);
        var entry = await ledger.RecordPurchaseAsync(companyId, 5m, note: "stub_pay_abc");

        Assert.Equal(TokenTransactionKind.Purchase, entry.Kind);
        Assert.Equal(5m, entry.NewBalance);
        Assert.Contains("stub_pay_abc", entry.Note);
    }

    [Fact]
    public void Preference_redaction_never_returns_raw_json()
    {
        const string json = """{"roles":["horeca","retail"],"maxTravelMinutes":30,"city":"Den Haag","emailHint":"secret"}""";
        var pending = ApplicationPreferenceRedaction.RedactForEmployer(json, piiRevealed: false);
        Assert.False(string.IsNullOrWhiteSpace(pending));
        Assert.DoesNotContain("secret", pending);
        Assert.DoesNotContain("Den Haag", pending);
        Assert.DoesNotContain("{", pending);
        Assert.Contains("Horeca", pending);
        Assert.Contains("Winkel", pending);
        Assert.Contains("max 30 min", pending);

        var accepted = ApplicationPreferenceRedaction.RedactForEmployer(json, piiRevealed: true);
        Assert.DoesNotContain("{", accepted);
        Assert.DoesNotContain("secret", accepted);
        Assert.Contains("Horeca", accepted);
        Assert.Contains("Den Haag", accepted);
        Assert.True(ApplicationPreferenceRedaction.LooksLikeJson(json));
        Assert.False(ApplicationPreferenceRedaction.LooksLikeJson(accepted));
        Assert.Equal("B, C", ApplicationPreferenceRedaction.ToHumanReadable("""["B","C"]"""));
    }

    [Fact]
    public void Invite_rules_block_peer_and_cross_scope()
    {
        Assert.True(EmployerInviteRules.CanAssignRole(UserRole.EnterpriseManager, UserRole.EnterpriseManager));
        Assert.True(EmployerInviteRules.CanAssignRole(UserRole.EnterpriseManager, UserRole.RegionalManager));
        Assert.True(EmployerInviteRules.CanAssignRole(UserRole.EnterpriseManager, UserRole.BranchManager));
        Assert.False(EmployerInviteRules.CanAssignRole(UserRole.EnterpriseManager, UserRole.Intermediary));
        Assert.True(EmployerInviteRules.CanAssignRole(UserRole.Intermediary, UserRole.Intermediary));
        Assert.False(EmployerInviteRules.CanAssignRole(UserRole.Intermediary, UserRole.EnterpriseManager));
        Assert.False(EmployerInviteRules.CanAssignRole(UserRole.Intermediary, UserRole.BranchManager));
        Assert.False(EmployerInviteRules.CanAssignRole(UserRole.BranchManager, UserRole.RegionalManager));

        var accessible = new HashSet<Guid> { Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") };
        Assert.False(EmployerInviteRules.IsWithinCallerScope(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            [],
            accessible,
            callerIsAdmin: false));
        Assert.True(EmployerInviteRules.IsWithinCallerScope(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            [Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")],
            accessible,
            callerIsAdmin: false));
    }

    [Fact]
    public void HtmlSanitize_strips_tags_and_rejects_javascript_urls()
    {
        Assert.Equal("Hello world", HtmlSanitize.ToPlainPreview("<b>Hello</b> <script>alert(1)</script>world"));
        Assert.Null(HtmlSanitize.NormalizeMediaUrl("javascript:alert(1)"));
        Assert.NotNull(HtmlSanitize.NormalizeMediaUrl("https://cdn.example/img.png"));
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
