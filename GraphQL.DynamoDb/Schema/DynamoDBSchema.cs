using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using GraphQL;
using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GraphQL.DynamoDb.Schema
{
    public class DynamoDBSchema : Types.Schema
    {
        public DynamoDBSchema(DynamoDBSchemaFactory factory, IAmazonDynamoDB dynamoDb)
        {
            Query = GetQuery(factory.Tables, dynamoDb);
            Mutation = GetMutation(factory.Tables, dynamoDb);
        }

        private IObjectGraphType GetQuery(IEnumerable<DynamoDBTable> tables, IAmazonDynamoDB dynamoDb)
        {
            var query = new ObjectGraphType { Name = "Query" };
            foreach (var table in tables)
            {
                var tableType = new ObjectGraphType { Name = table.TableDescription.TableName };

                var allColumns = table.TableDescription.KeySchema.Select(x => x.AttributeName)
                    .Concat(table.AdditionalColumns.Select(x => x.Item1));

                tableType.AddField(new FieldType
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
                    tableType.AddField(new FieldType
                    {
                        Name = index.IndexName,
                        Arguments = new QueryArguments(index.ToQueryArguments(table.TableDescription.AttributeDefinitions)),
                        ResolvedType = new ListGraphType(table.TableDescription.AttributeDefinitions.ToObjectGraphType(index.IndexName, table.AdditionalColumns)),
                        Resolver = new Resolvers.AsyncFieldResolver<IEnumerable<Dictionary<string, AttributeValue>>>(context => QueryAsync(dynamoDb, ToQuery(context, table.TableDescription.TableName, index)))
                    });
                }
                foreach (var index in table.TableDescription.LocalSecondaryIndexes)
                {
                    tableType.AddField(new FieldType
                    {
                        Name = index.IndexName,
                        Arguments = new QueryArguments(index.ToQueryArguments(table.TableDescription.AttributeDefinitions)),
                        ResolvedType = new ListGraphType(table.TableDescription.AttributeDefinitions.ToObjectGraphType(index.IndexName, table.AdditionalColumns)),
                        Resolver = new Resolvers.AsyncFieldResolver<IEnumerable<Dictionary<string, AttributeValue>>>(context => QueryAsync(dynamoDb, ToQuery(context, table.TableDescription.TableName, index)))
                    });
                }

                query.AddField(new FieldType
                {
                    Name = tableType.Name,
                    ResolvedType = tableType,
                    Resolver = new Resolvers.FuncFieldResolver<object>(context => context.SubFields)
                });
            }
            return query;
        }

        private IObjectGraphType GetMutation(IEnumerable<DynamoDBTable> tables, IAmazonDynamoDB dynamoDb)
        {
            var mutation = new ObjectGraphType { Name = "Mutation" };
            foreach (var table in tables)
            {
                var allColumns = table.TableDescription.KeySchema.Select(x => x.AttributeName)
                    .Concat(table.AdditionalColumns.Select(x => x.Item1));

                var input = table.TableDescription.AttributeDefinitions.ToInputObjectGraphType($"{table.TableDescription.TableName}Input", table.AdditionalColumns);
                mutation.AddField(new FieldType
                {
                    Name = $"create{table.TableDescription.TableName}",
                    Arguments = new QueryArguments(new QueryArgument(input) { Name = table.TableDescription.TableName }) ,
                    ResolvedType = new ListGraphType(table.TableDescription.AttributeDefinitions.ToObjectGraphType($"create{table.TableDescription.TableName}", table.AdditionalColumns)),
                    Resolver = new Resolvers.AsyncFieldResolver<Dictionary<string, AttributeValue>>(context =>
                        PutItemAsync(dynamoDb, table.TableDescription.TableName, context.Arguments.ToDictionary(arg => arg.Key, arg => new AttributeValue(arg.Value?.ToString())), context.SubFields.Select(field => ToKeySchema(field.Key, allColumns)).ToList()))
                });
            }
            return mutation;
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

        private async Task<Dictionary<string, AttributeValue>> PutItemAsync(IAmazonDynamoDB dynamoDb, string tableName, Dictionary<string, AttributeValue> item, List<string> attributesToGet)
        {
            try
            {
                var result = await dynamoDb.PutItemAsync(tableName, item, ReturnValue.ALL_NEW);
                return result.Attributes;
            }
            catch (Exception e)
            {
                return new Dictionary<string, AttributeValue> { { "order", new AttributeValue { S = e.Message } } };
            }
        }
    }
}
