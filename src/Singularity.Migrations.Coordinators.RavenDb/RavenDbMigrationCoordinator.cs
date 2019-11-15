using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Singularity.Migrations.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Singularity.Migrations.Coordinators.RavenDb
{
    public class RavenDbMigrationCoordinator<TContext> : MigrationCoordinator<TContext>
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

            await base.Initialize(context);
        }

        protected override async Task<(long sequenceNumber, long version)> ReadHighestMigration(TContext context)
        {
            using (var session = context.DocumentStore.OpenAsyncSession(context.MigrationDbName))
            {
                var projectId = context.ProjectId;
                
                var migrationRun = await session
                    .Query<MigrationRun, MigrationRunSequenceIndex>()
                    .Where(x =>x.ProjectId == projectId)
                    .OrderByDescending(x => x.MigrationSequenceNumber)
                    .FirstOrDefaultAsync();
                
                return migrationRun != null ? (migrationRun.MigrationSequenceNumber, migrationRun.Version) : (0L, 0L);
            }
        }

        protected override async Task StoreMigrationPoint(TContext context, long sequenceNumber, long version)
        {
            using (var session = context.DocumentStore.OpenAsyncSession(context.MigrationDbName))
            {
                await session.StoreAsync(MigrationRun.Create(context.ProjectId, sequenceNumber, version));
                
                await session.SaveChangesAsync();
            }
        }

        private class MigrationRun
        {
            public string Id { get; set; }

            public string ProjectId { get; set; }

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
}