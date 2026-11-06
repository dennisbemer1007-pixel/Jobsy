using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests.E2eFlow;

internal static class E2eFlowMailSetup
{
    public static async Task ConfigureLocalSmtpAsync(string connectionString, int port = 2525)
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.UseNetTopologySuite())
            .Options;
        await using var db = new JobsyDbContext(options);
        var credentials = new IntegrationCredentialService(db, new PassthroughSecretProtector());
        await credentials.UpsertAsync(
            IntegrationKey.Mail,
            new IntegrationCredentialUpdate(
                BaseUrl: $"smtp://127.0.0.1:{port}",
                ClientId: "e2e@jobsy.local",
                ClientSecret: "e2e-local-smtp-secret",
                FromAddress: "Lobsy E2E <e2e@jobsy.local>",
                ClearApiKey: true));
        await db.SaveChangesAsync();
    }
}
