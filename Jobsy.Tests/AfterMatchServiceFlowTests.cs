using Jobsy.Core.Contracts;
using Jobsy.Core.Email;
using Jobsy.Core.Email.Model;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Infrastructure.Services.AfterMatch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace Jobsy.Tests;

public sealed class AfterMatchServiceFlowTests
{
    [Fact]
    public async Task Propose_slots_and_record_outcome_without_scheduling_mail()
    {
        await using var harness = await AfterMatchDbHarness.CreateAsync();
        if (!harness.SupportsExecuteDelete)
        {
            return;
        }

        var db = harness.Db;
        var (service, appId, candidateId, employerId) = await SeedThreadAsync(db);

        var proposing = await service.ProposeEmployerSlotsAsync(
            appId,
            employerId,
            new ProposeEmployerSlotsRequest(
                InterviewFormat.OnSite,
                "Loosduinseweg 120",
                "Linda",
                null,
                [
                    new ProposedSlotRequest(DateTime.UtcNow.AddDays(3), DateTime.UtcNow.AddDays(3).AddMinutes(30)),
                    new ProposedSlotRequest(DateTime.UtcNow.AddDays(4), DateTime.UtcNow.AddDays(4).AddMinutes(30))
                ]));
        Assert.Equal(InterviewStatus.Proposing, proposing.InterviewStatus);
        Assert.Equal(2, proposing.OpenSlots.Count);

        var slotId = proposing.OpenSlots[0].Id;
        var scheduled = await service.SelectSlotAsync(
            appId,
            candidateId,
            isEmployer: false,
            new SelectSlotRequest(slotId));
        Assert.Equal(InterviewStatus.Scheduled, scheduled.InterviewStatus);

        var interview = await db.Interviews.Include(i => i.Slots).FirstAsync(i => i.ApplicationId == appId);
        var chosen = interview.Slots.Single(s => s.Id == slotId);
        chosen.StartUtc = DateTime.UtcNow.AddDays(-2);
        chosen.EndUtc = DateTime.UtcNow.AddDays(-2).AddMinutes(30);
        await db.SaveChangesAsync();

        var outcome = await service.RecordOutcomeAsync(
            appId,
            employerId,
            new RecordOutcomeRequest(
                AfterMatchOutcomeKind.NotNow,
                "We kozen nu voor iemand met meer ervaring aan de oven."));
        Assert.Equal(AfterMatchOutcomeKind.NotNow, outcome.Outcome);
        Assert.Equal(InterviewStatus.Done, outcome.InterviewStatus);

        var reread = await service.GetThreadForCandidateAsync(appId, candidateId);
        Assert.NotNull(reread);
        Assert.Equal(AfterMatchOutcomeKind.NotNow, reread!.Outcome);
        Assert.Equal(InterviewStatus.Done, reread.InterviewStatus);
        Assert.False(string.IsNullOrWhiteSpace(reread.OutcomeReason));
    }

    [Fact]
    public async Task Reschedule_from_scheduled_interview()
    {
        await using var harness = await AfterMatchDbHarness.CreateAsync();
        var db = harness.Db;
        var (service, appId, candidateId, _) = await SeedThreadAsync(db);
        await SeedScheduledInterviewAsync(db, appId);

        var rescheduled = await service.RescheduleAsync(
            appId,
            candidateId,
            isEmployer: false,
            new RescheduleRequest("Ik ben die dag onverwacht ziek geworden."));
        Assert.Equal(InterviewStatus.RescheduleRequested, rescheduled.InterviewStatus);
    }

    [Fact]
    public async Task OpenSlots_excludes_past_unselected_slots()
    {
        await using var harness = await AfterMatchDbHarness.CreateAsync();
        var db = harness.Db;
        var (service, appId, candidateId, employerId) = await SeedThreadAsync(db);
        var interview = await db.Interviews.Include(i => i.Slots).FirstAsync(i => i.ApplicationId == appId);
        db.InterviewSlots.AddRange(
            new InterviewSlot
            {
                Id = Guid.NewGuid(),
                InterviewId = interview.Id,
                StartUtc = DateTime.UtcNow.AddDays(-2),
                EndUtc = DateTime.UtcNow.AddDays(-2).AddMinutes(30),
                ProposedBy = InterviewSlotProposer.Employer
            },
            new InterviewSlot
            {
                Id = Guid.NewGuid(),
                InterviewId = interview.Id,
                StartUtc = DateTime.UtcNow.AddDays(3),
                EndUtc = DateTime.UtcNow.AddDays(3).AddMinutes(30),
                ProposedBy = InterviewSlotProposer.Employer
            },
            new InterviewSlot
            {
                Id = Guid.NewGuid(),
                InterviewId = interview.Id,
                StartUtc = DateTime.UtcNow.AddDays(4),
                EndUtc = DateTime.UtcNow.AddDays(4).AddMinutes(30),
                ProposedBy = InterviewSlotProposer.Employer
            });
        interview.Status = InterviewStatus.Proposing;
        await db.SaveChangesAsync();

        var thread = await service.GetThreadForCandidateAsync(appId, candidateId);
        Assert.NotNull(thread);
        Assert.Equal(2, thread!.OpenSlots.Count);
        Assert.All(thread.OpenSlots, s => Assert.True(s.StartUtc > DateTime.UtcNow));
    }

    [Fact]
    public async Task MarkCandidateMessagesRead_sets_ReadAtUtc_on_inbound_messages()
    {
        await using var harness = await AfterMatchDbHarness.CreateAsync();
        var db = harness.Db;
        var (service, appId, candidateId, employerId) = await SeedThreadAsync(db);
        db.ApplicationMessages.Add(new ApplicationMessage
        {
            Id = Guid.NewGuid(),
            ApplicationId = appId,
            SenderUserId = employerId,
            Body = "Hoi",
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        await service.MarkCandidateMessagesReadAsync(appId, candidateId);

        var message = await db.ApplicationMessages.SingleAsync(m => m.ApplicationId == appId);
        Assert.NotNull(message.ReadAtUtc);
    }

    [Fact]
    public async Task MarkEmployerMessagesRead_sets_ReadAtUtc_on_candidate_messages()
    {
        await using var harness = await AfterMatchDbHarness.CreateAsync();
        var db = harness.Db;
        var (service, appId, candidateId, employerId) = await SeedThreadAsync(db);
        db.ApplicationMessages.Add(new ApplicationMessage
        {
            Id = Guid.NewGuid(),
            ApplicationId = appId,
            SenderUserId = candidateId,
            Body = "Hoi werkgever",
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        await service.MarkEmployerMessagesReadAsync(appId, employerId);

        var message = await db.ApplicationMessages.SingleAsync(m => m.ApplicationId == appId);
        Assert.NotNull(message.ReadAtUtc);
    }

    [Fact]
    public async Task PreviewMessage_flags_phone_for_candidate_and_iban_for_employer()
    {
        await using var harness = await AfterMatchDbHarness.CreateAsync();
        var db = harness.Db;
        var (service, appId, candidateId, employerId) = await SeedThreadAsync(db);

        var phone = await service.PreviewMessageAsync(appId, candidateId, senderIsEmployer: false, "Bel me op 06 12 34 56 78");
        Assert.Equal(SensitiveMessageContentRules.SensitiveKind.Phone, phone.Kind);
        Assert.True(phone.CanConfirmSend);
        Assert.False(phone.WouldBlock);

        var ibanKind = SensitiveMessageContentRules.SensitiveKind.Iban;
        var share = AfterMatchContactShareState.From(InterviewStatus.Proposing, null);
        Assert.Equal(
            ApplicationMessageBlockReason.IbanBeforeOffer,
            SensitiveMessageContentRules.BlockReasonForEmployer(ibanKind, share));

        var ibanPreview = await service.PreviewMessageAsync(
            appId,
            employerId,
            senderIsEmployer: true,
            "NL91ABNA0417164300");
        Assert.Equal(ibanKind, ibanPreview.Kind);
        Assert.True(ibanPreview.WouldBlock);
    }

    private sealed class AfterMatchDbHarness : IAsyncDisposable
    {
        public JobsyDbContext Db { get; }
        public bool SupportsExecuteDelete { get; }

        private readonly ServiceProvider? _provider;
        private readonly AsyncServiceScope? _scope;
        private readonly string? _adminConnectionString;
        private readonly string? _databaseName;

        private AfterMatchDbHarness(
            JobsyDbContext db,
            bool supportsExecuteDelete,
            ServiceProvider? provider,
            AsyncServiceScope? scope,
            string? adminConnectionString,
            string? databaseName)
        {
            Db = db;
            SupportsExecuteDelete = supportsExecuteDelete;
            _provider = provider;
            _scope = scope;
            _adminConnectionString = adminConnectionString;
            _databaseName = databaseName;
        }

        public static async Task<AfterMatchDbHarness> CreateAsync()
        {
            var baseConnection = Environment.GetEnvironmentVariable("ConnectionStrings__JobsyDb");
            if (!string.IsNullOrWhiteSpace(baseConnection))
            {
                var builder = new NpgsqlConnectionStringBuilder(baseConnection);
                var databaseName = $"jobsy_aftermatch_{Guid.NewGuid():N}";
                var admin = new NpgsqlConnectionStringBuilder(baseConnection) { Database = "postgres", Pooling = false };
                try
                {
                    await using var probe = new NpgsqlConnection(admin.ConnectionString);
                    await probe.OpenAsync();
                }
                catch (Exception ex) when (ex is NpgsqlException or System.Net.Sockets.SocketException or TimeoutException)
                {
                    return CreateInMemory();
                }

                await ExecuteAdminAsync(admin.ConnectionString, $"CREATE DATABASE \"{databaseName}\"");
                builder.Database = databaseName;
                builder.Pooling = false;
                var services = new ServiceCollection();
                services.AddDbContext<JobsyDbContext>(o =>
                    o.UseNpgsql(builder.ConnectionString, npgsql => npgsql.UseNetTopologySuite()));
                var provider = services.BuildServiceProvider();
                var scope = provider.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
                await db.Database.MigrateAsync();
                return new AfterMatchDbHarness(
                    db,
                    supportsExecuteDelete: true,
                    provider,
                    scope,
                    admin.ConnectionString,
                    databaseName);
            }

            return CreateInMemory();
        }

        private static AfterMatchDbHarness CreateInMemory()
        {
            var options = new DbContextOptionsBuilder<JobsyDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options;
            return new AfterMatchDbHarness(
                new JobsyDbContext(options),
                supportsExecuteDelete: false,
                null,
                null,
                null,
                null);
        }

        public async ValueTask DisposeAsync()
        {
            if (_scope is not null)
            {
                await _scope.Value.DisposeAsync();
            }
            else
            {
                await Db.DisposeAsync();
            }
            if (_provider is not null)
            {
                await _provider.DisposeAsync();
            }

            if (_adminConnectionString is not null && _databaseName is not null)
            {
                NpgsqlConnection.ClearAllPools();
                await ExecuteAdminAsync(
                    _adminConnectionString,
                    $"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)");
            }
        }

        private static async Task ExecuteAdminAsync(string adminConnectionString, string sql)
        {
            await using var conn = new NpgsqlConnection(adminConnectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task SeedScheduledInterviewAsync(JobsyDbContext db, Guid appId)
    {
        var interview = await db.Interviews.FirstAsync(i => i.ApplicationId == appId);
        var slot = new InterviewSlot
        {
            Id = Guid.NewGuid(),
            InterviewId = interview.Id,
            StartUtc = DateTime.UtcNow.AddDays(2),
            EndUtc = DateTime.UtcNow.AddDays(2).AddMinutes(30),
            ProposedBy = InterviewSlotProposer.Employer,
            IsSelected = true
        };
        db.InterviewSlots.Add(slot);
        interview.ChosenSlotId = slot.Id;
        interview.Status = InterviewStatus.Scheduled;
        await db.SaveChangesAsync();
    }

    private static async Task<(AfterMatchService Service, Guid AppId, Guid CandidateId, Guid EmployerId)> SeedThreadAsync(
        JobsyDbContext db)
    {
        var companyId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        var employerId = Guid.NewGuid();
        var appId = Guid.NewGuid();

        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Demo BV",
            KvkNumber = "12345678",
            Type = CompanyType.Employer,
            Location = new GeoPoint(52.07, 4.31),
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.AdminCreated
        });
        db.Users.AddRange(
            new User { Id = candidateId, Email = "c@test.local", FullName = "C", Role = UserRole.Candidate, IsActive = true },
            new User { Id = employerId, Email = "e@test.local", FullName = "E", Role = UserRole.BranchManager, CompanyId = companyId, IsActive = true });
        db.UserCompanies.Add(new UserCompany { UserId = employerId, CompanyId = companyId });
        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            CompanyId = companyId,
            Title = "Test",
            Description = "d",
            HourlyWage = 10,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            Status = VacancyStatus.Active,
            Location = new GeoPoint(52.07, 4.31),
            PublishedAtUtc = DateTime.UtcNow
        });
        db.Applications.Add(new Application
        {
            Id = appId,
            VacancyId = vacancyId,
            CandidateUserId = candidateId,
            CandidateName = "Test Kandidaat",
            CandidateEmail = "c@test.local",
            Status = ApplicationStatus.Accepted,
            EmailVerifiedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var mailer = new AfterMatchMailer(new NoOpMailer(), config, NullLogger<AfterMatchMailer>.Instance);
        var statusNotifier = new ApplicationStatusNotifier(
            db,
            new NoOpUserNotifications(),
            new NoOpPush(),
            new NoOpMailer(),
            new AlwaysOnFeatures(),
            new AllowAllDispatchGate());
        var service = new AfterMatchService(db, new NoOpStatusRecorder(), mailer, statusNotifier, config);
        await service.EnsureThreadAsync(appId);
        return (service, appId, candidateId, employerId);
    }

    private sealed class NoOpMailer : ITransactionalMailer
    {
        public Task<EmailSendOutcome> SendAsync(
            ComposedEmail mail,
            string to,
            EmailSendOptions? options = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new EmailSendOutcome(Sent: false, Suppressed: false, Reason: "stub"));
    }

    private sealed class NoOpStatusRecorder : IApplicationStatusRecorder
    {
        public bool SetStatus(
            Application application,
            ApplicationStatus newStatus,
            ApplicationStatusActorKind actorKind,
            Guid? actorUserId,
            DateTime nowUtc,
            bool setRespondedAt = true,
            bool clearRespondedAt = false)
        {
            application.Status = newStatus;
            return true;
        }

        public void RecordCreated(
            Application application,
            ApplicationStatusActorKind actorKind,
            Guid? actorUserId,
            DateTime nowUtc)
        {
        }

        public Task<bool> TryReactAsync(
            Application application,
            ApplicationStatus newStatus,
            Guid? actorUserId,
            DateTime nowUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<bool> TryRecordEmployerViewedAsync(
            Guid applicationId,
            Guid? actorUserId,
            DateTime nowUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class NoOpUserNotifications : IUserNotificationService
    {
        public Task<UserNotification> CreateAsync(NotificationCreateRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new UserNotification
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Title = request.Title,
                Body = request.Body,
                Category = request.Category,
                CreatedAtUtc = DateTime.UtcNow
            });

        public Task CreateForEmailAsync(
            string userEmail,
            string title,
            string body,
            string category,
            string? deepLink = null,
            string? actionLabel = null,
            string? actionUrl = null,
            string? relatedEntityType = null,
            Guid? relatedEntityId = null,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<UserNotification>> ListForUserAsync(Guid userId, int take = 50, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<UserNotification>>([]);

        public Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<UserNotification?> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default)
            => Task.FromResult<UserNotification?>(null);

        public Task<int> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    private sealed class NoOpPush : IPushNotificationService
    {
        public Task SendAsync(PushMessage message, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class AllowAllDispatchGate : IStatusNotificationDispatchGate
    {
        public Task<StatusNotificationDispatchDecision> EvaluateAsync(
            Guid userId,
            string category,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new StatusNotificationDispatchDecision(true, true, true));
    }
}
