# GraphQL.DynamoDB

An adaptor to use GraphQL in AspNetCore with a dynamo DB backing without defining models.

## Minimum Setup

You can add the package to your AspNetCore app from NuGet

```
Install-Package GraphQL.DynamoDB
```

The easiest way to load the schema is to preload it before starting your web host.

```
        public static async Task Main(string[] args)
        {
            var host = CreateWebHostBuilder(args).Build();
            await host.LoadDynamoDbSchema();
            await host.RunAsync();
        }
```

You'll also need to add configure the services in your Startup.cs

```
using Amazon.DynamoDBv2;
using GraphQL.DynamoDB;
...

            services.AddAWSService<IAmazonDynamoDB>();
            services.AddGraphQL();
```

## How it works

The `LoadDynamoDbSchema` method attempts to load table data by using the AWSSDK API for `ListTablesAsync` and `DescribeTableAsync`. The list of tables are then converted to a GraphQL Schema during instantiation of the `DynamoDBSchema` class. The schema can be accessed by injecting `GraphQL.Types.ISchema` and using it as normal.

### Additional columns

Since DynamoDB is a no-SQL database, we may also need to tell the schema to allow for additional properties by passing them to the `LoadDynamoDbSchema` extension method.

```
            await host.LoadDynamoDbSchema(new Dictionary<string, IEnumerable<(string, string)>>
            {
                // Optionally declare properties that are not defined in the schema
                { "TableName", new[] { ( "StringColumn", "S" ), ( "NumericalColumn", "N"), ("StringArrayColumn", "SS") } }
            });
```

Please note that more complex types are not currently supported.