using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Singularity.Migrations.Coordinators.SqlServer
{
    public interface ISqlServerMigrationContext
    {
        string Key { get; }
        string MigrationTableName { get; }
        Task<T> RunInTransaction<T>(
            Func<SqlConnection, SqlTransaction, Task<T>> run, 
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
    }
}