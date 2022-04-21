using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Singularity.Migrations.Logging;

namespace Singularity.Migrations;

public abstract class LockableMigrationCoordinator<TContext> : MigrationCoordinator<TContext>
{
    private readonly TimeSpan _lockTimeout;

    protected LockableMigrationCoordinator(IMigrationLoggerFactory loggerFactory, TimeSpan? lockTimeout = null)
        : base(loggerFactory)
    {
        _lockTimeout = lockTimeout ?? TimeSpan.FromSeconds(30);
    }

    protected override async Task RunMigration(
        TContext context,
        long versionToMigrateTo,
        IEnumerable<Assembly> migrationAssemblies,
        Func<Type, IMigration<TContext>> resolveMigration)
    {
        await WithLock(
            context,
            Guid.NewGuid().ToString(),
            _lockTimeout,
            () => base.RunMigration(context, versionToMigrateTo, migrationAssemblies, resolveMigration));
    }

    protected abstract Task WithLock(TContext context, string lockId, TimeSpan timeout, Func<Task> execute);
}