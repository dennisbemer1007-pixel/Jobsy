using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Jobsy.Web.Security;

/// <summary>
/// Persists ASP.NET Data Protection keys in Postgres so antiforgery/auth cookies
/// survive Render redeploys (ephemeral container disks wipe the default key ring).
/// Development may fall back to an ephemeral key ring; Production fails startup.
/// </summary>
public static class DataProtectionSetup
{
    public static IServiceCollection AddJobsyDataProtection(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddDataProtection()
            .SetApplicationName("Jobsy.Web");

        var connectionString = ResolveConnectionString(configuration);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "Data Protection: ConnectionStrings:JobsyDb (or DATABASE_URL) is required in Production so auth cookies survive redeploys.");
            }

            return services;
        }

        try
        {
            var normalized = NormalizePostgresConnectionString(connectionString);
            var repository = new PostgresXmlRepository(normalized);
            if (!repository.TryEnsureTable())
            {
                if (!environment.IsDevelopment())
                {
                    throw new InvalidOperationException(
                        "Data Protection: could not create or open Postgres table \"__DataProtectionKeys\". " +
                        "Fix the JobsyDb connection string; ephemeral keys are not allowed outside Development.");
                }

                return services;
            }

            services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(
                new ConfigureOptions<KeyManagementOptions>(options =>
                {
                    options.XmlRepository = repository;
                }));
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex) when (environment.IsDevelopment())
        {
            // Keep ephemeral keys so local DX still boots without Postgres.
            Console.Error.WriteLine(
                $"Data Protection: Postgres key store unavailable ({ex.Message}); using ephemeral keys in Development.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Data Protection: Postgres key store is required in Production so auth cookies survive redeploys. " +
                $"Underlying error: {ex.Message}", ex);
        }

        return services;
    }

    private static string? ResolveConnectionString(IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("JobsyDb");
        if (!string.IsNullOrWhiteSpace(cs))
        {
            return cs;
        }

        return configuration["DATABASE_URL"];
    }

    private static string NormalizePostgresConnectionString(string connectionString)
    {
        var value = connectionString.Trim().Trim('"');
        if (value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(value);
            var userInfo = uri.UserInfo.Split(':', 2);
            var user = Uri.UnescapeDataString(userInfo[0]);
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
            var database = uri.AbsolutePath.Trim('/');
            var needsSsl = !value.Contains("sslmode=", StringComparison.OrdinalIgnoreCase)
                && !value.Contains("Ssl Mode=", StringComparison.OrdinalIgnoreCase);
            var parts = new List<string>
            {
                $"Host={uri.Host}",
                $"Port={(uri.Port > 0 ? uri.Port : 5432)}",
                $"Database={database}",
                $"Username={user}",
                $"Password={password}"
            };
            if (needsSsl)
            {
                parts.Add("Ssl Mode=Require;Trust Server Certificate=true");
            }

            return string.Join(";", parts);
        }

        return value;
    }

    private sealed class PostgresXmlRepository : IXmlRepository
    {
        private readonly string _connectionString;
        private readonly object _gate = new();
        private bool _ensured;

        public PostgresXmlRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public bool TryEnsureTable()
        {
            try
            {
                EnsureTable();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public IReadOnlyCollection<XElement> GetAllElements()
        {
            EnsureTable();
            var elements = new List<XElement>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT \"Xml\" FROM \"__DataProtectionKeys\"", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                elements.Add(XElement.Parse(reader.GetString(0)));
            }

            return elements;
        }

        public void StoreElement(XElement element, string friendlyName)
        {
            EnsureTable();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(
                """
                INSERT INTO "__DataProtectionKeys" ("Id", "Xml")
                VALUES (@id, @xml)
                ON CONFLICT ("Id") DO UPDATE SET "Xml" = EXCLUDED."Xml"
                """,
                conn);
            cmd.Parameters.AddWithValue("id", friendlyName);
            cmd.Parameters.AddWithValue("xml", element.ToString(SaveOptions.DisableFormatting));
            cmd.ExecuteNonQuery();
        }

        private void EnsureTable()
        {
            if (_ensured)
            {
                return;
            }

            lock (_gate)
            {
                if (_ensured)
                {
                    return;
                }

                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();
                using var cmd = new NpgsqlCommand(
                    """
                    CREATE TABLE IF NOT EXISTS "__DataProtectionKeys" (
                        "Id" text PRIMARY KEY,
                        "Xml" text NOT NULL
                    )
                    """,
                    conn);
                cmd.ExecuteNonQuery();
                _ensured = true;
            }
        }
    }
}
