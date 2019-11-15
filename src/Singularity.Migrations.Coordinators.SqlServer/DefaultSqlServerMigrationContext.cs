using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Singularity.Migrations.Coordinators.SqlServer
{
    public class DefaultSqlServerMigrationContext : ISqlServerMigrationContext
    {
        private readonly string _connectionString;

        public DefaultSqlServerMigrationContext(string key, string migrationTableName, string connectionString)
        {
            _connectionString = connectionString;
            Key = key;
            MigrationTableName = migrationTableName;
        }

        public string Key { get; }
        public string MigrationTableName { get; }
        
        public virtual async Task<T> RunInTransaction<T>(
            Func<SqlConnection, SqlTransaction, Task<T>> run, 
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            var connection = new SqlConnection(_connectionString);
            
            await connection.OpenAsync();

            using (var transaction = connection.BeginTransaction(isolationLevel))
            {
                try
                {
                    var response = await run(connection, transaction);

                    transaction.Commit();

                    return response;
                }
                catch (Exception)
                {
                    transaction.Rollback();

                    throw;
                }
                finally
                {
                    connection.Close();
                }
            }
        }
    }
}