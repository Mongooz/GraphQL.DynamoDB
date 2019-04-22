using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using GraphQL;
using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GraphQL.DynamoDB.Schema
{
    public class DynamoDBSchema : Types.Schema
    {
        public DynamoDBSchema(DynamoDBSchemaFactory factory, IAmazonDynamoDB dynamoDb)
        {
            Query = GetQuery(factory.Tables, dynamoDb);
        }

        private IObjectGraphType GetQuery(IEnumerable<DynamoDBTable> tables, IAmazonDynamoDB dynamoDb)
        {
            var query = new ObjectGraphType
            {
                Name = "Query"
            };
            foreach (var table in tables)
            {
                var allColumns = table.TableDescription.KeySchema.Select(x => x.AttributeName)
                    .Concat(table.AdditionalColumns.Select(x => x.Item1));

                query.AddField(new FieldType
                {
                    Name = "_scan",
                    Arguments = new QueryArguments(table.TableDescription.KeySchema.ToQueryArguments(table.TableDescription.AttributeDefinitions)),
                    ResolvedType = new ListGraphType(table.TableDescription.AttributeDefinitions.ToObjectGraphType("_scan", table.AdditionalColumns)),
                    Resolver = new Resolvers.AsyncFieldResolver<IEnumerable<Dictionary<string, AttributeValue>>>(context =>
                        ScanAsync(dynamoDb, table.TableDescription.TableName, context.SubFields.Select(field => ToKeySchema(field.Key, allColumns)).ToList())
                    )
                });
                foreach (var index in table.TableDescription.GlobalSecondaryIndexes)
                {
                    query.AddField(new FieldType
                    {
                        Name = index.IndexName,
                        Arguments = new QueryArguments(index.ToQueryArguments(table.TableDescription.AttributeDefinitions)),
                        ResolvedType = new ListGraphType(table.TableDescription.AttributeDefinitions.ToObjectGraphType(index.IndexName, table.AdditionalColumns)),
                        Resolver = new Resolvers.AsyncFieldResolver<IEnumerable<Dictionary<string, AttributeValue>>>(context => QueryAsync(dynamoDb, ToQuery(context, table.TableDescription.TableName, index)))
                    });
                }
                foreach (var index in table.TableDescription.LocalSecondaryIndexes)
                {
                    query.AddField(new FieldType
                    {
                        Name = index.IndexName,
                        Arguments = new QueryArguments(index.ToQueryArguments(table.TableDescription.AttributeDefinitions)),
                        ResolvedType = new ListGraphType(table.TableDescription.AttributeDefinitions.ToObjectGraphType(index.IndexName, table.AdditionalColumns)),
                        Resolver = new Resolvers.AsyncFieldResolver<IEnumerable<Dictionary<string, AttributeValue>>>(context => QueryAsync(dynamoDb, ToQuery(context, table.TableDescription.TableName, index)))
                    });
                }
            }
            return query;
        }

        private string ToKeySchema(string argumentKey, IEnumerable<string> keySchema)
        {
            var fromSchema = keySchema.FirstOrDefault(key => String.Equals(key, argumentKey, StringComparison.InvariantCultureIgnoreCase));
            return fromSchema ?? argumentKey;
        }

        private KeyValuePair<string,object> GetArgument(KeyValuePair<string, object> argument, List<KeySchemaElement> keySchema)
        {
            return new KeyValuePair<string, object>(ToKeySchema(argument.Key, keySchema.Select(key => key.AttributeName)), argument.Value);
        }

        private QueryRequest ToQuery(ResolveFieldContext context, string tableName, GlobalSecondaryIndexDescription index)
        {
            var arguments = context.Arguments.Select(arg => GetArgument(arg, index.KeySchema));
            return new QueryRequest(tableName)
            {
                Select = Select.ALL_ATTRIBUTES,
                IndexName = index.IndexName,
                KeyConditionExpression = String.Join(" and ", arguments.Select(_ => $"{_.Key} = :v_{_.Key}")),
                ExpressionAttributeValues = arguments.ToDictionary(_ => $":v_{_.Key}", _ => new AttributeValue { S = _.Value.ToString() })
            };
        }

        private QueryRequest ToQuery(ResolveFieldContext context, string tableName, LocalSecondaryIndexDescription index)
        {
            var arguments = context.Arguments.Select(arg => GetArgument(arg, index.KeySchema));
            return new QueryRequest(tableName)
            {
                Select = Select.ALL_ATTRIBUTES,
                IndexName = index.IndexName,
                KeyConditionExpression = String.Join(" and ", arguments.Select(_ => $"{_.Key} = :v_{_.Key}")),
                ExpressionAttributeValues = arguments.ToDictionary(_ => $":v_{_.Key}", _ => new AttributeValue { S = _.Value.ToString() })
            };
        }

        private async Task<IEnumerable<Dictionary<string, AttributeValue>>> QueryAsync(IAmazonDynamoDB dynamoDb, QueryRequest query)
        {
            var results = await dynamoDb.QueryAsync(query);

            return results?.Items?.ToList();
        }

        private async Task<IEnumerable<Dictionary<string, AttributeValue>>> ScanAsync(IAmazonDynamoDB dynamoDb, string tableName, List<string> attributesToGet)
        {
            var results = await dynamoDb.ScanAsync(tableName, attributesToGet);

            return results?.Items?.ToList();
        }
    }
}
