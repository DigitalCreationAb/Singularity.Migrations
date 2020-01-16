using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Cosmos.Table.Queryable;
using Singularity.Migrations.Logging;

namespace Singularity.Migrations.Coordinators.AzureTableStorage
{
    public class TableStorageMigrationCoordinator<TContext> : MigrationCoordinator<TContext> 
        where TContext : TableStorageMigrationContext
    {
        public TableStorageMigrationCoordinator(IMigrationLoggerFactory loggerFactory) : base(loggerFactory)
        {
        }
        
        protected override async Task Initialize(TContext context)
        {
            var table = context.GetMigrationTable();

            await table.CreateIfNotExistsAsync();

            await base.Initialize(context);
        }

        protected override Task<(long sequenceNumber, long version)> ReadHighestMigration(TContext context)
        {
            var table = context.GetMigrationTable();

            var result = (from migrationRun in table
                        .CreateQuery<MigrationRun>()
                    where migrationRun.PartitionKey == context.Key
                    select migrationRun)
                .AsTableQuery()
                .Execute()
                .OrderByDescending(x => x.GetSequenceNumber())
                .FirstOrDefault();

            return Task.FromResult(result == null ? (0L, 0L) : (result.GetSequenceNumber(), result.Version));
        }

        protected override async Task StoreMigrationPoint(TContext context, long sequenceNumber, long version)
        {
            var table = context.GetMigrationTable();

            var insertOperation = TableOperation
                .InsertOrMerge(new MigrationRun(context.Key, sequenceNumber, version, DateTimeOffset.Now));

            await table.ExecuteAsync(insertOperation);
        }
        
        public class MigrationRun : TableEntity
        {
            public MigrationRun()
            {
                
            }

            public MigrationRun(
                string key,
                long migrationSequenceNumber,
                long version,
                DateTimeOffset finishedAt) : this()
            {
                PartitionKey = key;
                RowKey = migrationSequenceNumber.ToString();
                Version = version;
                FinishedAt = finishedAt;
            }
            
            public long Version { get; set; }
            public DateTimeOffset FinishedAt { get; set; }

            public long GetSequenceNumber()
            {
                return long.TryParse(RowKey, out var number) ? number : 0;
            }
        }
    }
}