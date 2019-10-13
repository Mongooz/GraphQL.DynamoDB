using GraphQL.DynamoDb.Schema;
using GraphQL.Http;
using GraphQL.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GraphQL.DynamoDb
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddGraphQL(this IServiceCollection services)
        {
            services.AddSingleton<ISchema, DynamoDBSchema>();
            services.AddSingleton<DynamoDBSchemaFactory>();

            return services;
        }

        public static async Task<IWebHost> LoadDynamoDbSchema(this IWebHost host, Dictionary<string, IEnumerable<(string, string)>> additionalColumns = null)
        {
            using (var scope = host.Services.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<DynamoDBSchemaFactory>().Initialise(additionalColumns);
            }

            return host;
        }
    }
}
