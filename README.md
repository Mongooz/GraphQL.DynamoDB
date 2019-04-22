# GraphQL.DynamoDB

An adaptor to use GraphQL in AspNetCore with a dynamo DB backing without defining models.

## Minimum Setup

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