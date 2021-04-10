using System;
using System.Data;
using System.Threading.Tasks;
using MySqlConnector;

namespace Singularity.Migrations.Coordinators.MySql
{
    public interface IMySqlMigrationContext
    {
        string Key { get; }
        string MigrationTableName { get; }
        Task<T> RunInTransaction<T>(
            Func<MySqlConnection, MySqlTransaction, Task<T>> run, 
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
    }
}