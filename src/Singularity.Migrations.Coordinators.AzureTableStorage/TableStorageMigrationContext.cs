using Microsoft.Azure.Cosmos.Table;

namespace Singularity.Migrations.Coordinators.AzureTableStorage
{
    public class TableStorageMigrationContext
    {
        private readonly CloudStorageAccount _storageAccount;

        public TableStorageMigrationContext(
            string key,
            string migrationTableName,
            string tableStorageConnectionString)
            : this(key, migrationTableName, CloudStorageAccount.Parse(tableStorageConnectionString))
        {
            
        }

        public TableStorageMigrationContext(string key, string migrationTableName, CloudStorageAccount storageAccount)
        {
            _storageAccount = storageAccount;
            Key = key;
            MigrationTableName = migrationTableName;
        }

        public string Key { get; }
        public string MigrationTableName { get; }

        public CloudTableClient GetCloudTableClient()
        {
            return _storageAccount.CreateCloudTableClient();
        }

        public CloudTable GetMigrationTable()
        {
            var tableClient = GetCloudTableClient();

            return tableClient.GetTableReference(MigrationTableName);
        }
    }
}