using System;
using System.Data;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace Singularity.Migrations.Coordinators.MySql
{
    public class DefaultMySqlMigrationContext : IMySqlMigrationContext
    {
        private readonly string _connectionString;

        public DefaultMySqlMigrationContext(string key, string migrationTableName, string connectionString)
        {
            _connectionString = connectionString;
            Key = key;
            MigrationTableName = migrationTableName;
        }

        public string Key { get; }
        public string MigrationTableName { get; }
        
        public virtual async Task<T> RunInTransaction<T>(
            Func<MySqlConnection, MySqlTransaction, Task<T>> run, 
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            var connection = new MySqlConnection(_connectionString);
            
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