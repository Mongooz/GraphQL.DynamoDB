using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.DynamoDB.Web.Db
{
    public class Initialiser
    {
        private readonly IAmazonDynamoDB _dynamo;

        public Initialiser(IAmazonDynamoDB dynamo)
        {
            _dynamo = dynamo;
        }
        public async Task Initialise()
        {
            await TryCreateTables();
        }

        private async Task TryCreateTables()
        {
            try
            {
                await _dynamo.DeleteTableAsync(new DeleteTableRequest { TableName = "Animals" });
                var response = await _dynamo.DescribeTableAsync(new DescribeTableRequest
                {
                    TableName = "Animals"
                });
            }
            catch (ResourceNotFoundException)
            {
                var request = new CreateTableRequest
                {
                    TableName = "Animals",
                    AttributeDefinitions = new List<AttributeDefinition>
                    {
                        new AttributeDefinition
                        {
                            AttributeName = "Id",
                            AttributeType = "N"
                        },
                        new AttributeDefinition
                        {
                            AttributeName = "Genus",
                            AttributeType = "S"
                        }
                    },
                    KeySchema = new List<KeySchemaElement>
                    {
                        new KeySchemaElement
                        {
                            AttributeName = "Id",
                            KeyType = "HASH"
                        },
                        new KeySchemaElement
                        {
                            AttributeName = "Genus",
                            KeyType = "RANGE"
                        },
                    },
                    ProvisionedThroughput = new ProvisionedThroughput
                    {
                        ReadCapacityUnits = 10,
                        WriteCapacityUnits = 5
                    },
                    GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
                    {
                        new GlobalSecondaryIndex
                        {
                            IndexName = "Animals_ByGenus",
                            KeySchema = new List<KeySchemaElement>
                            {
                                new KeySchemaElement
                                {
                                  AttributeName = "Genus",
                                  KeyType = "HASH"
                                }
                            },
                            Projection = new Projection
                            {
                                ProjectionType = ProjectionType.ALL
                            },
                            ProvisionedThroughput = new ProvisionedThroughput
                            {
                                ReadCapacityUnits = 10,
                                WriteCapacityUnits = 5
                            }
                        }
                    }
                };

                await _dynamo.CreateTableAsync(request);

                var items = new[]
{
                    new Dictionary<string, AttributeValue>
                    {
                        { "Id", new AttributeValue { N = "1" }},
                        { "Genus", new AttributeValue { S = "Microcarbo" }},
                        { "Family", new AttributeValue { S = "Phalacrocoracidae" }},
                        { "Order", new AttributeValue { S = "Suliformes" }},
                        { "Class", new AttributeValue { S = "Aves" }},
                        { "CommonNames", new AttributeValue { SS = new List<string> { "Little cormorant", "Javanese Cormorant" } }},
                        { "ScientificName", new AttributeValue { S = "Microcarbo niger" }},
                    }
                };

                foreach (var item in items)
                {
                    await _dynamo.PutItemAsync("Animals", item);
                }
            }
        }
    }

    public static class InitialiserExtensions
    {
        public static async Task<IWebHost> Initialise(this IWebHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<Initialiser>().Initialise();
            }

            return host;
        }
    }
}
