using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Singularity.Migrations
{
    public abstract class MigrationCoordinator<TContext> : IMigrationCoordinator<TContext>
    {
        public async Task MigrateTo(
            IEnumerable<Assembly> migrationAssemblies, 
            Func<Type, IMigration<TContext>> resolveMigration, 
            TContext context, 
            long? version = null)
        {
            await Initialize(context);
            
            var lastMigration = await ReadHighestMigration(context);

            var versionToMigrateTo = version ?? long.MaxValue;

            var allMigrations = GetMigrations(migrationAssemblies, resolveMigration);

            long? migratedTo = null;

            if (versionToMigrateTo > lastMigration.version)
            {
                var upMigrations = allMigrations
                    .Where(x => x.Version > lastMigration.version && x.Version <= versionToMigrateTo)
                    .OrderBy(x => x.Version);

                foreach (var upMigration in upMigrations)
                {
                    await upMigration.Up(context);

                    migratedTo = upMigration.Version;
                }
            }
            else if (versionToMigrateTo < lastMigration.version)
            {
                var downMigrations = allMigrations
                    .Where(x => x.Version <= lastMigration.version && x.Version >= versionToMigrateTo)
                    .OrderByDescending(x => x.Version);

                foreach (var downMigration in downMigrations)
                {
                    await downMigration.Down(context);

                    migratedTo = downMigration.Version;
                }
            }
            
            if (!migratedTo.HasValue)
                return;

            await StoreMigrationPoint(context, lastMigration.sequenceNumber + 1, migratedTo.Value);
        }

        protected virtual Task Initialize(TContext context)
        {
            return Task.CompletedTask;
        }
        
        protected virtual IEnumerable<IMigration<TContext>> GetMigrations(
            IEnumerable<Assembly> migrationAssemblies,
            Func<Type, IMigration<TContext>> resolveMigration)
        {
            return migrationAssemblies
                .SelectMany(x => x
                    .GetTypes()
                    .Where(y => typeof(IMigration<TContext>).IsAssignableFrom(y)))
                .Select(resolveMigration);
        }

        protected abstract Task<(long sequenceNumber, long version)> ReadHighestMigration(TContext context);

        protected abstract Task StoreMigrationPoint(TContext context, long sequenceNumber, long version);
    }
}