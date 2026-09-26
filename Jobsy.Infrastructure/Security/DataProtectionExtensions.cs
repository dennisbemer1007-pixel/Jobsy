using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Jobsy.Infrastructure.Security;

internal static class DataProtectionExtensions
{
    public static IServiceCollection AddJobsyDataProtection(
        this IServiceCollection services,
        string connectionString,
        bool isDevelopment,
        IConfiguration? configuration = null)
    {
        services.AddDataProtection()
            .SetApplicationName("Jobsy.Api");

        try
        {
            var repository = new PostgresXmlRepository(connectionString);
            if (!repository.TryEnsureTable())
            {
                if (!isDevelopment && !AllowEphemeral(configuration))
                {
                    throw new InvalidOperationException(
                        "Data Protection: could not create or open Postgres table \"__DataProtectionKeys\". " +
                        "Ephemeral keys are not allowed outside Development.");
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
        catch (Exception ex) when (isDevelopment || AllowEphemeral(configuration))
        {
            // Keep default key ring so local API / test hosts still start.
            Console.Error.WriteLine(
                $"Data Protection: Postgres key store unavailable ({ex.Message}); using ephemeral keys.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Data Protection: Postgres key store is required outside Development. " +
                $"Underlying error: {ex.Message}", ex);
        }

        return services;
    }

    private static bool AllowEphemeral(IConfiguration? configuration)
        => configuration?.GetValue("JobsyAuth:AllowEphemeralDataProtection", false) == true;

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
