using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.DynamoDB.Web.Db;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace GraphQL.DynamoDB.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateWebHostBuilder(args).Build();

            await host.Initialise();

            await host.LoadDynamoDbSchema(new Dictionary<string, IEnumerable<(string, string)>>
            {
                // Optionally declare properties that are not defined in the schema
                { "Animals", new[] { ( "Family", "S" ), ( "Order", "S"), ( "Class", "S" ), ( "ScientificName", "S"), ("CommonNames", "SS") } }
            });
            await host.RunAsync();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
