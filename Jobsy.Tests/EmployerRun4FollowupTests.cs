using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Time;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Web.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class EmployerRun4FollowupTests
{
    [Theory]
    [InlineData(
        "Solliciteer via Jobsy — we reageren doorgaans binnen één werkdag. (Mock vacaturetekst #4.)",
        "Je kunt direct solliciteren. We reageren doorgaans binnen één werkdag.")]
    [InlineData(
        "Intro. Solliciteer via Jobsy - we reageren doorgaans binnen één werkdag. (Mock vacaturetekst #26.)",
        "Intro. Je kunt direct solliciteren. We reageren doorgaans binnen één werkdag.")]
    [InlineData(
        "Solliciteer via Jobsy — we reageren doorgaans binnen één werkdag. (Haaglanden testdata #3.)",
        "Je kunt direct solliciteren. We reageren doorgaans binnen één werkdag.")]
    public void Seed_sentence_is_rewritten_once(string original, string expected)
    {
        var once = MockVacancyCopy.Rewrite(original);
        Assert.Equal(expected, once);
        Assert.DoesNotContain("Solliciteer via Jobsy", once, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Mock vacaturetekst", once, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Haaglanden testdata", once, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(once, MockVacancyCopy.Rewrite(once));
    }

    [Fact]
    public void Real_vacancy_text_is_left_alone()
    {
        const string real = "Je kunt direct solliciteren. We reageren doorgaans binnen één werkdag.";
        Assert.Equal(real, MockVacancyCopy.Rewrite(real));
        Assert.Equal("Kaswerk bij Jobsy Westland.", MockVacancyCopy.Rewrite("Kaswerk bij Jobsy Westland."));
    }

    [Fact]
    public async Task Existing_seed_rows_are_rewritten_and_a_second_run_changes_nothing()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("MockCopy-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new JobsyDbContext(options);
        db.Vacancies.Add(Vacancy(
            "Horeca",
            "Solliciteer via Jobsy — we reageren doorgaans binnen één werkdag. (Mock vacaturetekst #4.)"));
        db.Vacancies.Add(Vacancy("Echt", "Je begint met een rondleiding. Daarna werk je zelfstandig."));
        await db.SaveChangesAsync();

        await MockVacancyCopyBackfill.BackfillAsync(db, NullLogger.Instance);
        var rows = await db.Vacancies.AsNoTracking().OrderBy(v => v.Title).ToListAsync();
        Assert.Equal("Je begint met een rondleiding. Daarna werk je zelfstandig.", rows[0].Description);
        Assert.Equal(MockVacancyCopy.Replacement, rows[1].Description);

        await MockVacancyCopyBackfill.BackfillAsync(db, NullLogger.Instance);
        Assert.Equal(MockVacancyCopy.Replacement, (await db.Vacancies.SingleAsync(v => v.Title == "Horeca")).Description);
    }

    [Fact]
    public void Confirmation_notice_does_not_repeat_the_vacancy_title()
    {
        var body = ApplicationRules.ConfirmationNoticeBody("De Kas");
        Assert.Equal("De Kas: je sollicitatie is ontvangen.", body);
        Assert.DoesNotContain("Horeca medewerker", body, StringComparison.Ordinal);
    }

    [Fact]
    public void Closed_admin_vacancy_shows_the_close_date_not_the_planned_end()
    {
        var end = new DateOnly(2026, 12, 31);
        var closed = new DateTime(2026, 10, 2, 22, 30, 0, DateTimeKind.Utc);
        var label = ClosedVacancyDeadline.Label(
            "Archived",
            end,
            closed,
            UiStrings.Get("WgVac.ClosedOn", "nl"),
            UiStrings.Get("WgVac.Closed", "nl"));
        Assert.StartsWith("Gesloten ", label, StringComparison.Ordinal);
        Assert.DoesNotContain("31-12-2026", label, StringComparison.Ordinal);
        Assert.Equal(
            "Gesloten",
            ClosedVacancyDeadline.Label("Fulfilled", end, null, "Gesloten {0}", "Gesloten"));
        Assert.Equal("31-12-2026", ClosedVacancyDeadline.Label("Active", end, closed, "Gesloten {0}", "Gesloten"));
    }

    [Fact]
    public void Migration_rewrites_the_same_sentence_as_the_startup_backfill()
    {
        var root = FindRepoRoot();
        var sql = File.ReadAllText(Path.Combine(
            root,
            "Jobsy.Infrastructure/Data/Migrations/20261004145500_RewriteMockVacancySeedCopy.cs"));
        Assert.Contains(MockVacancyCopy.Replacement, sql, StringComparison.Ordinal);
        Assert.Contains("Solliciteer via Jobsy", sql, StringComparison.Ordinal);
        Assert.Contains("ILIKE '%Solliciteer via Jobsy%'", sql, StringComparison.Ordinal);

        var services = new ServiceCollection();
        services.AddDbContext<JobsyDbContext>(o =>
            o.UseNpgsql(
                "Host=127.0.0.1;Database=JobsyCopyCheck;Username=postgres;Password=postgres",
                npgsql => npgsql.UseNetTopologySuite()));
        using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        Assert.Contains(
            "20261004145500_RewriteMockVacancySeedCopy",
            db.Database.GetMigrations());
    }

    [Theory]
    [InlineData("nl", "Je sollicitatie is verstuurd naar de werkgever. Je ziet de status onder Mijn sollicitaties.")]
    [InlineData("en", "Your application has been sent to the employer. You can see the status under My applications.")]
    [InlineData("pl", "Twoja aplikacja została wysłana do pracodawcy. Status zobaczysz w Moje aplikacje.")]
    [InlineData("ro", "Aplicația ta a fost trimisă angajatorului. Vezi statusul la Aplicațiile mele.")]
    [InlineData("ar", "تم إرسال طلبك إلى صاحب العمل. يمكنك رؤية الحالة في طلباتي.")]
    public void Success_copy_is_one_sentence_in_every_locale(string lang, string expected)
    {
        var body = UiStrings.Get("Apply.SentBody", lang);
        Assert.Equal(expected, body);
        Assert.DoesNotContain("bevestigd", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("confirmed", body, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(1, "1 nieuwe sollicitatie")]
    [InlineData(4, "4 nieuwe sollicitaties")]
    public void New_application_label_uses_the_dutch_plural(int count, string expected)
    {
        var culture = new CultureState(null!, null!, null!);
        culture.InitializeFromLanguage("nl");
        var form = CountPhrase.Form(culture.Language, count);
        Assert.Equal(expected, string.Format(UiStrings.Get($"WgVac.Apps.NewAria.{form}", "nl"), count));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repo root not found.");
    }

    private static Vacancy Vacancy(string title, string description) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Description = description,
        CompanyId = Guid.NewGuid(),
        Status = VacancyStatus.Active,
        StartDate = new DateOnly(2026, 1, 1),
        EndDate = new DateOnly(2026, 12, 31),
        Location = new GeoPoint(52.01, 4.21),
        CreatedVia = VacancySource.Manual,
        CreatedAtUtc = DateTime.UtcNow
    };
}
