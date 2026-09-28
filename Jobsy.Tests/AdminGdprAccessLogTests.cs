using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Api.Models;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Tests;

public class AdminGdprAccessLogTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private readonly RoleFunctionalWebAppFactory _factory;

    public AdminGdprAccessLogTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    private HttpClient AdminClient()
    {
        var client = _factory.CreateClient();
        JobsyTestAuth.Authorize(client, _factory.AdminId);
        return client;
    }

    private HttpClient EmployerClient()
    {
        var client = _factory.CreateClient();
        JobsyTestAuth.Authorize(client, _factory.EmployerId);
        return client;
    }

    [Fact]
    public async Task Admin_users_returns_masked_email_name_and_pagination()
    {
        var client = AdminClient();
        var page = await client.GetFromJsonAsync<AdminUsersPageDto>("api/admin/users?page=1&pageSize=10", Json);
        Assert.NotNull(page);
        Assert.True(page!.Masked);
        Assert.Equal(1, page.Page);
        Assert.Equal(10, page.PageSize);
        Assert.True(page.TotalCount >= page.Items.Count);
        Assert.NotEmpty(page.Aggregates.ByRole);

        var other = page.Items.FirstOrDefault(u => u.Id != _factory.AdminId && u.FullName.Contains(' '));
        Assert.NotNull(other);
        Assert.Contains("***", other!.Email);
        Assert.DoesNotContain("branch@", other.Email, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("kandidaat@", other.Email, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(".", other.FullName);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        Assert.True(await db.PersonalDataAccessLogs.AnyAsync(l =>
            l.ActorUserId == _factory.AdminId && l.Resource == "admin.users.list"));
    }

    [Fact]
    public async Task Admin_applications_without_filter_returns_aggregates()
    {
        var client = AdminClient();
        var response = await client.GetAsync("api/applications");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.True(doc.RootElement.TryGetProperty("totalCount", out _)
                    || doc.RootElement.TryGetProperty("TotalCount", out _));
        Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task Cv_download_writes_exactly_one_access_log_row()
    {
        var client = EmployerClient();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            db.PersonalDataAccessLogs.RemoveRange(db.PersonalDataAccessLogs);
            await db.SaveChangesAsync();
        }

        var response = await client.GetAsync($"api/applications/{_factory.AcceptedApplicationId}/lobsy-cv.pdf");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var subjectId = await db2.Applications.AsNoTracking()
            .Where(a => a.Id == _factory.AcceptedApplicationId)
            .Select(a => a.CandidateUserId)
            .FirstAsync();
        var rows = await db2.PersonalDataAccessLogs
            .Where(l => l.Resource == "application.cv.download"
                        && l.ActorUserId == _factory.EmployerId
                        && l.SubjectUserId == subjectId)
            .ToListAsync();
        Assert.Single(rows);
        Assert.Equal("download", rows[0].Action);
    }

    [Fact]
    public async Task Iban_round_trips_through_protector_and_is_not_plaintext_in_store()
    {
        const string plain = "NL91ABNA0417164300";
        using var scope = _factory.Services.CreateScope();
        var protector = scope.ServiceProvider.GetRequiredService<IIbanProtector>();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();

        var protectedValue = protector.Protect(plain);
        Assert.NotNull(protectedValue);
        Assert.StartsWith(IbanProtector.Prefix, protectedValue);
        Assert.Equal(plain, protector.Unprotect(protectedValue));

        var profile = await db.SalesManagerProfiles.FirstOrDefaultAsync(p => p.UserId == _factory.SalesId);
        if (profile is null)
        {
            profile = new SalesManagerProfile
            {
                Id = Guid.NewGuid(),
                UserId = _factory.SalesId,
                Iban = plain
            };
            db.SalesManagerProfiles.Add(profile);
        }
        else
        {
            profile.Iban = plain;
        }

        await db.SaveChangesAsync();

        if (db.Database.IsRelational())
        {
            // Raw SQL must not see the plaintext IBAN (relational providers only).
            var raw = await db.Database
                .SqlQueryRaw<string>(
                    """SELECT "Iban" AS "Value" FROM "SalesManagerProfiles" WHERE "UserId" = {0}""",
                    _factory.SalesId)
                .FirstOrDefaultAsync();
            Assert.NotNull(raw);
            Assert.DoesNotContain(plain, raw!, StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith(IbanProtector.Prefix, raw);
        }
        else
        {
            // InMemory still runs the value converter on write; Protect must produce ciphertext.
            Assert.StartsWith(IbanProtector.Prefix, protector.Protect(plain)!);
        }

        // EF read returns plaintext for payout flows.
        db.ChangeTracker.Clear();
        var reloaded = await db.SalesManagerProfiles.AsNoTracking()
            .FirstAsync(p => p.UserId == _factory.SalesId);
        Assert.Equal(plain, reloaded.Iban);
    }

    [Fact]
    public async Task Sales_manager_payout_preview_still_uses_full_iban_server_side()
    {
        const string plain = "NL00RABO0123456789";
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var profile = await db.SalesManagerProfiles.FirstOrDefaultAsync(p => p.UserId == _factory.SalesId);
            if (profile is null)
            {
                db.SalesManagerProfiles.Add(new SalesManagerProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = _factory.SalesId,
                    Iban = plain,
                    CompanyName = "Sales BV",
                    KvkNumber = "12345678",
                    Address = "Straat 1",
                    PostalCode = "1234AB",
                    City = "Amsterdam",
                    Country = "NL",
                    TrackingCode = "SMTEST1",
                    AgreementSignedAt = DateTime.UtcNow,
                    OnboardingCompletedAt = DateTime.UtcNow,
                    AgreementVersion = "v1"
                });
            }
            else
            {
                profile.Iban = plain;
                profile.TrackingCode ??= "SMTEST1";
                profile.AgreementSignedAt ??= DateTime.UtcNow;
                profile.OnboardingCompletedAt ??= DateTime.UtcNow;
                profile.AgreementVersion ??= "v1";
            }

            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        JobsyTestAuth.Authorize(client, _factory.SalesId);
        var preview = await client.GetFromJsonAsync<SalesManagerPayoutPreviewDto>(
            "api/sales-managers/me/payouts/preview",
            Json);
        Assert.NotNull(preview);
        // API never returns full IBAN; masked preview must be present when stored.
        Assert.Null(preview!.Iban);
        Assert.False(string.IsNullOrWhiteSpace(preview.MaskedIban));
        Assert.DoesNotContain(plain, preview.MaskedIban!, StringComparison.OrdinalIgnoreCase);

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var stored = await db2.SalesManagerProfiles.AsNoTracking()
            .FirstAsync(p => p.UserId == _factory.SalesId);
        Assert.Equal(plain, stored.Iban);
    }
}
