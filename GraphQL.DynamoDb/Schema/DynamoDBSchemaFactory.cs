using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GraphQL.DynamoDb.Schema
{
    public class DynamoDBSchemaFactory
    {
        private readonly ConcurrentBag<DynamoDBTable> _dynamoDbTables = new ConcurrentBag<DynamoDBTable>();
        public IEnumerable<DynamoDBTable> Tables =>  _dynamoDbTables;

        public void AddTable(TableDescription table, IEnumerable<(string, string)> additionalColumns)
        {
            _dynamoDbTables.Add(new DynamoDBTable { TableDescription = table, AdditionalColumns = additionalColumns });
        }


        private readonly IAmazonDynamoDB _dynamo;
        public DynamoDBSchemaFactory(IAmazonDynamoDB dynamo)
        {
            _dynamo = dynamo;
        }

        public async Task Initialise(Dictionary<string, IEnumerable<(string, string)>> additionalColumns)
        {
            string lastEvaluatedTableName = null;
            do
            {
                var request = new ListTablesRequest
                {
                    Limit = 1,
                    ExclusiveStartTableName = lastEvaluatedTableName
                };

                var tables = await _dynamo.ListTablesAsync(request);
                foreach (string tableName in tables.TableNames)
                {
                    var table = await _dynamo.DescribeTableAsync(new DescribeTableRequest
                    {
                        TableName = tableName
                    });

                    var tableColumns = additionalColumns?.ContainsKey(tableName) == true ? additionalColumns[tableName] : null;
                    _dynamoDbTables.Add(new DynamoDBTable { TableDescription = table.Table, AdditionalColumns = tableColumns });
                }

                lastEvaluatedTableName = tables.LastEvaluatedTableName;
            } while (lastEvaluatedTableName != null);
        }
    }
}
