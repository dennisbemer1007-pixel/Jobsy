using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure;
using Jobsy.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Api.Ops;

/// <summary>
/// Development/ops CLI: <c>dotnet Jobsy.Api.dll proactive-matching run-daily</c>.
/// </summary>
public static class ProactiveMatchingCommand
{
    public const int ExitOk = 0;
    public const int ExitUsage = 1;
    public const int ExitFailed = 2;

    public static async Task<int> RunAsync(string[] args, TextWriter? output = null)
    {
        output ??= Console.Out;
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            await output.WriteLineAsync("Usage: proactive-matching run-daily | run-instant <vacancyId>");
            return args.Length == 0 ? ExitUsage : ExitOk;
        }

        using var host = TestAccountsCommand.BuildCliHost();
        await host.StartAsync();
        try
        {
            using var scope = host.Services.CreateScope();
            var matching = scope.ServiceProvider.GetRequiredService<IProactiveMatchingService>();
            if (string.Equals(args[0], "run-daily", StringComparison.OrdinalIgnoreCase))
            {
                var sent = await matching.RunDailyMatchingAsync();
                await output.WriteLineAsync($"Proactive matching delivered {sent} suggestion(s).");
                return ExitOk;
            }

            if (string.Equals(args[0], "run-instant", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length < 2 || !Guid.TryParse(args[1], out var vacancyId))
                {
                    await output.WriteLineAsync("Usage: proactive-matching run-instant <vacancyId>");
                    return ExitUsage;
                }

                var sent = await matching.RunInstantMatchingForVacancyAsync(vacancyId);
                await output.WriteLineAsync($"Instant proactive matching delivered {sent} suggestion(s).");
                return ExitOk;
            }

            await output.WriteLineAsync("Unknown verb. Usage: proactive-matching run-daily | run-instant <vacancyId>");
            return ExitUsage;
        }
        catch (Exception ex)
        {
            await output.WriteLineAsync(ex.ToString());
            return ExitFailed;
        }
        finally
        {
            await host.StopAsync();
        }
    }
}
