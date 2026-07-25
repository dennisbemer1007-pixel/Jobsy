using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Jobsy.Infrastructure.Security;

internal static class DataProtectionExtensions
{
    public static IServiceCollection AddJobsyDataProtection(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDataProtection()
            .SetApplicationName("Jobsy.Api");

        services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(
            new ConfigureOptions<KeyManagementOptions>(options =>
            {
                options.XmlRepository = new PostgresXmlRepository(connectionString);
            }));

        return services;
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
