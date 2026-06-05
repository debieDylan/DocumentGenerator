using System.Reflection;
using Npgsql;

namespace DocumentGenerator.Infrastructure.Persistence;

public sealed class PostgresMigrationRunner(NpgsqlDataSource dataSource)
{
    public async Task ApplyAsync(CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using NpgsqlCommand createTable = connection.CreateCommand();
        createTable.CommandText = """
            CREATE TABLE IF NOT EXISTS schema_migrations (
                name text PRIMARY KEY,
                applied_at timestamptz NOT NULL
            );
            """;
        await createTable.ExecuteNonQueryAsync(cancellationToken);

        Assembly assembly = typeof(PostgresMigrationRunner).Assembly;
        const string resourcePrefix = "DocumentGenerator.Infrastructure.Persistence.Migrations.";
        string[] resourceNames = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(resourcePrefix, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();

        foreach (string resourceName in resourceNames)
        {
            string migrationName = resourceName[resourcePrefix.Length..];
            if (await HasMigrationAsync(connection, migrationName, cancellationToken))
            {
                continue;
            }

            await using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                continue;
            }

            using StreamReader reader = new(stream);
            string sql = await reader.ReadToEndAsync(cancellationToken);
            await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
            await using NpgsqlCommand command = connection.CreateCommand();
            command.Transaction = transaction;
#pragma warning disable CA2100
            command.CommandText = sql;
#pragma warning restore CA2100
            await command.ExecuteNonQueryAsync(cancellationToken);

            await using NpgsqlCommand record = connection.CreateCommand();
            record.Transaction = transaction;
            record.CommandText = "INSERT INTO schema_migrations(name, applied_at) VALUES (@name, now());";
            record.Parameters.AddWithValue("name", migrationName);
            await record.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
    }

    private static async Task<bool> HasMigrationAsync(
        NpgsqlConnection connection,
        string migrationName,
        CancellationToken cancellationToken)
    {
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS(SELECT 1 FROM schema_migrations WHERE name = @name);";
        command.Parameters.AddWithValue("name", migrationName);
        object? exists = await command.ExecuteScalarAsync(cancellationToken);
        return exists is true;
    }
}
