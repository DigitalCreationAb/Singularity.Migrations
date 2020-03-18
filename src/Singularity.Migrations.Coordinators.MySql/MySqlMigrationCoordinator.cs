using System;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Singularity.Migrations.Logging;

namespace Singularity.Migrations.Coordinators.MySql
{
    public class MySqlMigrationCoordinator<TContext> : MigrationCoordinator<TContext>
        where TContext : IMySqlMigrationContext
    {
        public MySqlMigrationCoordinator(IMigrationLoggerFactory loggerFactory) : base(loggerFactory)
        {
        }
        
        protected override async Task Initialize(TContext context)
        {
            await context.RunInTransaction((connection, transaction) =>
            {
                var command = new MySqlCommand(
                    $@"CREATE TABLE IF NOT EXISTS {context.MigrationTableName} (
                                    ProjectId NVARCHAR(50),
                                    MigrationSequenceNumber BIGINT,
                                    Version BIGINT,
                                    FinishedAt DATETIMEOFFSET,

                                    CONSTRAINT PK_{context.MigrationTableName} PRIMARY KEY(ProjectId, MigrationSequenceNumber)
                                );", 
                    connection, 
                    transaction);

                return command.ExecuteNonQueryAsync();
            });

            await base.Initialize(context);
        }

        protected override Task<(long sequenceNumber, long version)> ReadHighestMigration(TContext context)
        {
            return context.RunInTransaction(async (connection, transaction) =>
            {
                var command = new MySqlCommand(
                    $@"SELECT MigrationSequenceNumber, Version 
                                FROM {context.MigrationTableName}
                                WHERE ProjectId = :ProjectId
                                ORDER BY MigrationSequenceNumber DESC
                                LIMIT 1;",
                    connection,
                    transaction);
                
                command.Parameters.AddWithValue(":ProjectId", context.Key);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    var canRead = await reader.ReadAsync();

                    var response = canRead ? (reader.GetInt64(0), reader.GetInt64(1)) : (0, 0);

                    return response;
                }
            });
        }

        protected override Task StoreMigrationPoint(TContext context, long sequenceNumber, long version)
        {
            return context.RunInTransaction((connection, transaction) =>
            {
                var command = new MySqlCommand(
                    $@"INSERT INTO {context.MigrationTableName}(ProjectId, MigrationSequenceNumber, Version, FinishedAt)
                                            VALUES(:ProjectId, :MigrationSequenceNumber, :Version, :FinishedAt);",
                    connection,
                    transaction);

                command.Parameters.AddWithValue(":ProjectId", context.Key);
                command.Parameters.AddWithValue(":MigrationSequenceNumber", sequenceNumber + 1);
                command.Parameters.AddWithValue(":Version", version);
                command.Parameters.AddWithValue(":FinishedAt", DateTimeOffset.Now);

                return command.ExecuteNonQueryAsync();
            });
        }
    }
}