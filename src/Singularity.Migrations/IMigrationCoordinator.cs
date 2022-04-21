using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Singularity.Migrations;

public interface IMigrationCoordinator<TContext>
{
    Task MigrateTo(
        IEnumerable<Assembly> migrationAssemblies,
        Func<Type, IMigration<TContext>> resolveMigration,
        TContext context,
        long? version = null);
}