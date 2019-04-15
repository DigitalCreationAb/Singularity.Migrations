using Amazon.DynamoDBv2;

namespace Singularity.Migrations.Coordinators.DynamoDb
{
    public interface IDynamoDbMigrationContext
    {
        string Key { get; }
        string MigrationTableName { get; }
        IAmazonDynamoDB DynamoDbClient { get; }
    }
}