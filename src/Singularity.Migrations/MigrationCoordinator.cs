using Singularity.Migrations.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Singularity.Migrations;

public abstract class MigrationCoordinator<TContext> : IMigrationCoordinator<TContext>
{
    protected readonly IMigrationLogger Logger;

    protected MigrationCoordinator(IMigrationLoggerFactory loggerFactory)
    {
        Logger = loggerFactory.CreateLogger(GetType());
    }

    public virtual async Task MigrateTo(
        IEnumerable<Assembly> migrationAssemblies,
        Func<Type, IMigration<TContext>> resolveMigration,
        TContext context,
        long? version = null)
    {
        await Initialize(context).ConfigureAwait(false);

        await RunMigration(
            context,
            version ?? long.MaxValue,
            migrationAssemblies,
            resolveMigration);
    }

    protected virtual async Task RunMigration(
        TContext context,
        long versionToMigrateTo,
        IEnumerable<Assembly> migrationAssemblies,
        Func<Type, IMigration<TContext>> resolveMigration)
    {
        var lastMigration = await ReadHighestMigration(context).ConfigureAwait(false);

        Logger.Information("Starting migration from last migration {0}", lastMigration.version);

        var migrations = GetMigrations(migrationAssemblies.ToList(), resolveMigration);

        var source = new List<RunnableMigration>();

        Logger.Information("Migrating to version {0}", versionToMigrateTo);

        if (versionToMigrateTo > lastMigration.version)
        {
            var list = migrations.Where(
                    x =>
                    {
                        if (x.Version > lastMigration.version)
                            return x.Version <= versionToMigrateTo;

                        return false;
                    }).OrderBy(x => x.Version)
                .ToList();

            source.AddRange(list.Select(RunnableMigration.Up));

            Logger.Information("Added {0} up migrations to the queue", list.Count);
        }
        else if (versionToMigrateTo < lastMigration.version)
        {
            var list = migrations.Where(
                x =>
                {
                    if (x.Version <= lastMigration.version)
                        return x.Version >= versionToMigrateTo;
                    return false;
                }).OrderByDescending(
                x => x.Version).ToList();

            source.AddRange(list.Select(RunnableMigration.Down));

            Logger.Information("Added {0} down migrations to the queue", list.Count);
        }

        if (!source.Any())
        {
            Logger.Information("Didn't find any migrations to run");
        }
        else
        {
            var result = await RunMigrations(source, context).ConfigureAwait(false);

            if (result.migratedTo.HasValue)
            {
                await StoreMigrationPoint(context, lastMigration.sequenceNumber + 1L, result.migratedTo.Value)
                    .ConfigureAwait(false);

                Logger.Information("Finished migrating to version {0}", result.migratedTo.Value);
            }

            if (result.error != null)
                throw result.error;
        }
    }

    protected virtual Task Initialize(TContext context)
    {
        return Task.CompletedTask;
    }

    protected virtual async Task<(long? migratedTo, Exception? error)> RunMigrations(
        IEnumerable<RunnableMigration> migrations,
        TContext context)
    {
        long? migratedTo = null;

        foreach (var migration in migrations)
        {
            try
            {
                migratedTo = await migration.Run(context).ConfigureAwait(false);

                Logger.Information("Ran migration {0}", migration.Version);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Problem while running migration {0}", migration.Version);

                return (migratedTo, ex);
            }
        }

        return (migratedTo, null);
    }

    protected virtual IEnumerable<IMigration<TContext>> GetMigrations(
        IReadOnlyList<Assembly> migrationAssemblies,
        Func<Type, IMigration<TContext>> resolveMigration)
    {
        var list = migrationAssemblies
            .SelectMany(x => x.GetTypes().Where(y => typeof(IMigration<TContext>).IsAssignableFrom(y)))
            .Select(resolveMigration)
            .ToList();

        Logger.Information("Found {0} migrations for context {1} from assemblies {2}", list.Count,
            typeof(TContext),
            string.Join(" ", migrationAssemblies.Select(x => x.ToString())));

        return list;
    }

    protected abstract Task<(long sequenceNumber, long version)> ReadHighestMigration(TContext context);

    protected abstract Task StoreMigrationPoint(
        TContext context,
        long sequenceNumber,
        long version);

    protected class RunnableMigration
    {
        private readonly IMigration<TContext> _migration;
        private readonly Func<TContext, Task> _run;

        private RunnableMigration(IMigration<TContext> migration, Func<TContext, Task> run)
        {
            _migration = migration;
            _run = run;
        }

        public long Version => _migration.Version;

        public async Task<long> Run(TContext context)
        {
            await _run(context).ConfigureAwait(false);

            return _migration.Version;
        }

        public static RunnableMigration Up(IMigration<TContext> migration)
        {
            return new RunnableMigration(migration, migration.Up);
        }

        public static RunnableMigration Down(IMigration<TContext> migration)
        {
            return new RunnableMigration(migration, migration.Down);
        }
    }
}