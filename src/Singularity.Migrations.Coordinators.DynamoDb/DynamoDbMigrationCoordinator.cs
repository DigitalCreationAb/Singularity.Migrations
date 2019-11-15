using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Singularity.Migrations.Logging;

namespace Singularity.Migrations.Coordinators.DynamoDb
{
    public class DynamoDbMigrationCoordinator<TContext> : MigrationCoordinator<TContext>
        where TContext : IDynamoDbMigrationContext
    {
        public DynamoDbMigrationCoordinator(IMigrationLoggerFactory loggerFactory) : base(loggerFactory)
        {
        }
        
        protected override async Task Initialize(TContext context)
        {
            try
            {
                await context.DynamoDbClient.DescribeTableAsync(context.MigrationTableName);
            }
            catch (ResourceNotFoundException)
            {
                await context.DynamoDbClient.CreateTableAsync(new CreateTableRequest
                {
                    TableName = context.MigrationTableName,
                    BillingMode = BillingMode.PAY_PER_REQUEST,
                    KeySchema = new List<KeySchemaElement>
                    {
                        new KeySchemaElement("key", KeyType.HASH),
                        new KeySchemaElement("migrationSequenceNumber", KeyType.RANGE)
                    },
                    AttributeDefinitions = new List<AttributeDefinition>
                    {
                        new AttributeDefinition("key", ScalarAttributeType.S),
                        new AttributeDefinition("migrationSequenceNumber", ScalarAttributeType.N)
                    }
                });
            }
            
            await base.Initialize(context);
        }

        protected override async Task<(long sequenceNumber, long version)> ReadHighestMigration(TContext context)
        {
            var response = await context.DynamoDbClient
                .QueryAsync(new QueryRequest(context.MigrationTableName)
                {
                    Limit = 1,
                    ConsistentRead = true,
                    ScanIndexForward = false,
                    KeyConditionExpression = "key = :key",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        [":key"] = new AttributeValue(context.Key)
                    }
                });

            return response.Items.Any() 
                ? (long.Parse(response.Items[0]["migrationSequenceNumber"].N), long.Parse(response.Items[0]["version"].N)) 
                : (0, 0);
        }

        protected override async Task StoreMigrationPoint(TContext context, long sequenceNumber, long version)
        {
            var dynamoDbContext = new DynamoDBContext(context.DynamoDbClient);
            
            var migrationRun = new MigrationRun
            {
                Key = context.Key,
                MigrationSequenceNumber = sequenceNumber + 1,
                Version = version,
                FinishedAt = DateTimeOffset.Now.ToString("O")
            };

            await dynamoDbContext.SaveAsync(
                migrationRun,
                new DynamoDBOperationConfig
                {
                    OverrideTableName = context.MigrationTableName
                });
        }
        
        private class MigrationRun
        {
            [DynamoDBHashKey("key")]
            public string Key { get; set; }

            [DynamoDBRangeKey("migrationSequenceNumber")]
            public long MigrationSequenceNumber { get; set; }

            [DynamoDBProperty("version")]
            public long Version { get; set; }

            [DynamoDBProperty("finishedAt")]
            public string FinishedAt { get; set; }
        }
    }
}