using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Singularity.Migrations.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Raven.Client;
using Raven.Client.Documents.Operations.Expiration;
using Raven.Client.Exceptions;
using Raven.Client.Exceptions.Documents.Session;

namespace Singularity.Migrations.Coordinators.RavenDb;

public class RavenDbMigrationCoordinator<TContext> : LockableMigrationCoordinator<TContext>
    where TContext : IRavenDbMigrationContext
{
    public RavenDbMigrationCoordinator(IMigrationLoggerFactory loggerFactory)
        : base(loggerFactory)
    {
    }

    protected override async Task Initialize(TContext context)
    {
        context.DocumentStore.EnsureDatabaseExists(context.MigrationDbName);

        await context.DocumentStore.ExecuteIndexAsync(new MigrationRunSequenceIndex(), context.MigrationDbName);

        await context
            .DocumentStore
            .Maintenance
            .ForDatabase(context.MigrationDbName)
            .SendAsync(new ConfigureExpirationOperation(new ExpirationConfiguration
            {
                Disabled = false,
                DeleteFrequencyInSec = 10
            }));

        await base.Initialize(context);
    }

    protected override async Task<(long sequenceNumber, long version)> ReadHighestMigration(TContext context)
    {
        using var session = context.DocumentStore.OpenAsyncSession(context.MigrationDbName);
        
        var projectId = context.ProjectId;

        var migrationRun = await session
            .Query<MigrationRun, MigrationRunSequenceIndex>()
            .Where(x => x.ProjectId == projectId)
            .OrderByDescending(x => x.MigrationSequenceNumber)
            .FirstOrDefaultAsync();

        return migrationRun != null ? (migrationRun.MigrationSequenceNumber, migrationRun.Version) : (0L, 0L);
    }

    protected override async Task WithLock(TContext context, string lockId, TimeSpan timeout, Func<Task> execute)
    {
        using var session = context.DocumentStore.OpenAsyncSession(context.MigrationDbName);

        var elapsed = new Stopwatch();

        while (elapsed.Elapsed < timeout)
        {
            try
            {
                var lockItem = LockItem.Create(context.ProjectId, lockId);

                await session.StoreAsync(lockItem, "", lockItem.Id);

                session.Advanced.GetMetadataFor(lockItem)[Constants.Documents.Metadata.Expires] =
                    DateTime.UtcNow + timeout;

                await session.SaveChangesAsync();

                await execute();

                session.Delete(lockItem);

                await session.SaveChangesAsync();

                return;
            }
            catch (NonUniqueObjectException e)
            {
                Logger
                    .Information(e, "Failed getting lock");
                
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
            catch (ConcurrencyException e)
            {
                Logger
                    .Information(e, "Failed getting lock");
                
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        throw new TimeoutException("Lock timeout reached");
    }

    protected override async Task StoreMigrationPoint(TContext context, long sequenceNumber, long version)
    {
        using var session = context.DocumentStore.OpenAsyncSession(context.MigrationDbName);
        
        await session.StoreAsync(MigrationRun.Create(context.ProjectId, sequenceNumber, version));

        await session.SaveChangesAsync();
    }

    private class LockItem
    {
        public string Id { get; set; } = null!;
        public string LockId { get; set; } = null!;

        public static LockItem Create(string projectId, string lockId)
        {
            return new LockItem
            {
                Id = BuildId(projectId),
                LockId = lockId
            };
        }

        public static string BuildId(string projectId)
        {
            return $"locks/{projectId}";
        }
    }
    
    private class MigrationRun
    {
        public string Id { get; set; } = null!;
        public string ProjectId { get; set; } = null!;
        public long MigrationSequenceNumber { get; set; }
        public long Version { get; set; }
        public DateTimeOffset FinishedAt { get; set; }

        public static MigrationRun Create(string projectId, long sequenceNumber, long version)
        {
            return new MigrationRun
            {
                Version = version,
                ProjectId = projectId,
                Id = BuildId(projectId, sequenceNumber),
                FinishedAt = DateTimeOffset.Now,
                MigrationSequenceNumber = sequenceNumber
            };
        }

        private static string BuildId(string projectId, long sequenceNumber)
        {
            return $"MigrationRuns/{projectId}/{sequenceNumber}";
        }
    }

    private class MigrationRunSequenceIndex : AbstractIndexCreationTask<MigrationRun>
    {
        public MigrationRunSequenceIndex()
        {
            Map = docs => docs.Select(doc => new
            {
                doc.ProjectId,
                doc.MigrationSequenceNumber
            });
        }
    }
}