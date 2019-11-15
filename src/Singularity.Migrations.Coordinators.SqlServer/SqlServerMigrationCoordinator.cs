using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Singularity.Migrations.Logging;

namespace Singularity.Migrations.Coordinators.SqlServer
{
    public class SqlServerMigrationCoordinator<TContext> : MigrationCoordinator<TContext>
        where TContext : ISqlServerMigrationContext
    {
        public SqlServerMigrationCoordinator(IMigrationLoggerFactory loggerFactory) : base(loggerFactory)
        {
        }
        
        protected override async Task Initialize(TContext context)
        {
            using (var transaction = context.Connection.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                try
                {
                    var command = new SqlCommand(
                        $@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{context.MigrationTableName}' and xtype='U')
                                CREATE TABLE {context.MigrationTableName} (
                                    ProjectId NVARCHAR(50),
                                    MigrationSequenceNumber BIGINT,
                                    Version BIGINT,
                                    FinishedAt DATETIMEOFFSET,

                                    CONSTRAINT PK_{context.MigrationTableName} PRIMARY KEY(ProjectId, MigrationSequenceNumber)
                                )
                                GO;", 
                        context.Connection, 
                        transaction);

                    await command.ExecuteNonQueryAsync();
                
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    
                    throw;
                }
            }

            await base.Initialize(context);
        }

        protected override async Task<(long sequenceNumber, long version)> ReadHighestMigration(TContext context)
        {
            using (var transaction = context.Connection.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                try
                {
                    var command = new SqlCommand(
                        $@"SELECT TOP 1 MigrationSequenceNumber, Version 
                                FROM {context.MigrationTableName}
                                WHERE ProjectId = @ProjectId
                                ORDER BY MigrationSequenceNumber DESC;",
                        context.Connection,
                        transaction);

                    command.Parameters.AddWithValue("@ProjectId", context.Key);

                    var reader = await command.ExecuteReaderAsync();

                    var canRead = await reader.ReadAsync();
                    
                    var response = canRead ? (reader.GetInt64(0), reader.GetInt64(1)) : (0, 0);
                    
                    transaction.Commit();

                    return response;
                }
                catch (Exception)
                {
                    transaction.Rollback();

                    throw;
                }
            }
        }

        protected override async Task StoreMigrationPoint(TContext context, long sequenceNumber, long version)
        {
            using (var transaction = context.Connection.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                try
                {
                    var command = new SqlCommand(
                        $@"INSERT INTO {context.MigrationTableName}(ProjectId, MigrationSequenceNumber, Version, FinishedAt)
                                            VALUES(@ProjectId, @MigrationSequenceNumber, @Version, @FinishedAt);",
                        context.Connection,
                        transaction);
                    
                    command.Parameters.AddWithValue("@ProjectId", context.Key);
                    command.Parameters.AddWithValue("@MigrationSequenceNumber", sequenceNumber + 1);
                    command.Parameters.AddWithValue("@Version", version);
                    command.Parameters.AddWithValue("@FinishedAt", DateTimeOffset.Now);
                    
                    await command.ExecuteNonQueryAsync();
                
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    
                    throw;
                }
            }
        }
    }
}